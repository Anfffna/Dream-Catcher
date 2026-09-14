using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ClientPhoneRequestController : MonoBehaviour
{
    [Header("Ссылки")]

    [SerializeField]
    private ClientNPCController clientNPC;

    [SerializeField]
    private WorkPhoneManualController phone;

    [SerializeField]
    private DialogueChoiceController choices;

    [Header("Условия")]

    [Tooltip("Пусто — любой вариант дела.")]
    [SerializeField]
    private string requiredVariantId;

    [SerializeField]
    private DirectionDecision requiredDecision = DirectionDecision.None;

    [SerializeField]
    private bool requirePersonalQuestion;

    [SerializeField]
    private string helpButtonText = "Помочь";

    [SerializeField]
    private string refuseButtonText = "Отказать";

    [Header("Особый телефонный сценарий")]

    [SerializeField]
    private string contactId = "security";

    [SerializeField]
    private string scenarioId = "woman_gray_coat";

    [Tooltip("ID варианта, который действительно вызывает охрану.")]
    [SerializeField]
    private string actionChoiceId = "check_documents";

    [Header("Реакции посетителя")]

    [SerializeField]
    private List<DialogueManager.DialogueLine> helpedDialogue =
        new List<DialogueManager.DialogueLine>();

    [SerializeField]
    private List<DialogueManager.DialogueLine> refusedDialogue =
        new List<DialogueManager.DialogueLine>();

    [SerializeField]
    private List<DialogueManager.DialogueLine> cancelledCallDialogue =
        new List<DialogueManager.DialogueLine>();

    [Header("Действие после подтверждённого звонка")]

    [Tooltip(
        "Ждать NotifyActionCompleted от сцены сопровождения. " +
        "Выключать для проверки без охранника.")]
    [SerializeField]
    private bool waitForActionCompletion = true;

    [SerializeField]
    private UnityEvent onActionRequested = new UnityEvent();

    [SerializeField]
    private UnityEvent onHelpCompleted = new UnityEvent();

    [SerializeField]
    private UnityEvent onRefused = new UnityEvent();

    private DialogueManager dialogueManager;
    private ClientNPCController subscribedClient;
    private WorkPhoneManualController subscribedPhone;
    private Coroutine routine;

    private bool active;
    private bool played;
    private bool finalBlockApplied;
    private bool ownsDialogue;
    private bool finalChoiceRequested;

    private bool actionCommitted;
    private bool phoneCallEnded;
    private bool waitingForPhone;

    private bool waitingForAction;
    private bool actionCompleted;

    private int selectedRequestChoice = -1;

    private void OnEnable()
    {
        ResolveAndSubscribe();
    }

    private void OnDisable()
    {
        if (subscribedClient != null)
        {
            subscribedClient.FinalDialogueChoiceRequested -=
                HandleFinalDialogueChoiceRequested;

            subscribedClient.FinalDialogueChoicePromptReady -=
                HandleFinalDialogueChoicePromptReady;
        }

        if (subscribedPhone != null)
        {
            subscribedPhone.PhoneChoiceCommitted -= HandlePhoneChoice;
            subscribedPhone.PhoneCallEnded -= HandlePhoneCallEnded;
        }

        subscribedClient = null;
        subscribedPhone = null;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ReleaseResources();
        played = false;
    }

    private void ResolveAndSubscribe()
    {
        if (clientNPC == null)
            clientNPC = GetComponent<ClientNPCController>();

        if (phone == null)
        {
            phone = FindFirstObjectByType<WorkPhoneManualController>(
                FindObjectsInactive.Include);
        }

        if (choices == null)
        {
            choices = FindFirstObjectByType<DialogueChoiceController>(
                FindObjectsInactive.Include);
        }

        if (clientNPC != null)
            dialogueManager = clientNPC.DialogueManagerReference;

        if (subscribedClient != clientNPC)
        {
            if (subscribedClient != null)
            {
                subscribedClient.FinalDialogueChoiceRequested -=
                    HandleFinalDialogueChoiceRequested;

                subscribedClient.FinalDialogueChoicePromptReady -=
                    HandleFinalDialogueChoicePromptReady;
            }

            subscribedClient = clientNPC;

            if (subscribedClient != null)
            {
                subscribedClient.FinalDialogueChoiceRequested +=
                    HandleFinalDialogueChoiceRequested;

                subscribedClient.FinalDialogueChoicePromptReady +=
                    HandleFinalDialogueChoicePromptReady;
            }
        }

        if (subscribedPhone != phone)
        {
            if (subscribedPhone != null)
            {
                subscribedPhone.PhoneChoiceCommitted -= HandlePhoneChoice;
                subscribedPhone.PhoneCallEnded -= HandlePhoneCallEnded;
            }

            subscribedPhone = phone;

            if (subscribedPhone != null)
            {
                subscribedPhone.PhoneChoiceCommitted += HandlePhoneChoice;
                subscribedPhone.PhoneCallEnded += HandlePhoneCallEnded;
            }
        }
    }

    private void HandleFinalDialogueChoicePromptReady(
    ClientNPCController client)
    {
        if (client != clientNPC ||
            !finalChoiceRequested ||
            active ||
            played)
        {
            return;
        }

        finalChoiceRequested = false;

        played = true;
        active = true;

        selectedRequestChoice = -1;

        actionCommitted = false;
        phoneCallEnded = false;
        waitingForPhone = false;

        actionCompleted = false;
        waitingForAction = false;


        /*
         * Очень важно:
         * теперь ClientNPC после нажатия
         * на плашку НЕ сможет сразу
         * запустить Take_SON3.
         */
        clientNPC.BlockFinalCompletion(this);
        finalBlockApplied = true;


        /*
         * Последняя реплика Final Dialogue
         * теперь принадлежит этой
         * специальной сцене.
         */
        ownsDialogue = true;


        if (!choices.ShowChoices(
                this,
                helpButtonText,
                refuseButtonText,
                HandleRequestChoice))
        {
            Debug.LogError(
                "Не удалось показать " +
                "Помочь / Отказать.",
                this);

            /*
             * Не оставляем ChoicePrompt
             * зависшим навечно.
             */
            if (dialogueManager != null &&
                dialogueManager.DialogueActive &&
                dialogueManager.ChoicePromptReady)
            {
                dialogueManager
                    .FinishChoicePrompt(false);
            }

            ReleaseResources();
            return;
        }


        routine =
            StartCoroutine(
                RequestRoutine());
    }

    private bool ConditionsMatch()
    {
        if (clientNPC == null ||
            ClientNPCController.CurrentActiveClient != clientNPC)
        {
            return false;
        }

        VisitorCaseData.VisitorCaseVariant variant =
            CurrentClientContext.CurrentVariant;

        if (variant == null)
            return false;

        if (!string.IsNullOrWhiteSpace(requiredVariantId) &&
            variant.VariantId != requiredVariantId)
        {
            return false;
        }

        if (requiredDecision != DirectionDecision.None &&
            clientNPC.SubmittedDecision != requiredDecision)
        {
            return false;
        }

        if (requirePersonalQuestion)
        {
            ClientQuestionDialogueController questionController =
                clientNPC.QuestionDialogueControllerReference;

            if (questionController == null ||
                !questionController.PersonalQuestionAsked)
            {
                return false;
            }
        }

        return true;
    }

    private bool HandleFinalDialogueChoiceRequested(
    ClientNPCController client)
    {
        if (client != clientNPC ||
            active ||
            played ||
            finalChoiceRequested ||
            !ConditionsMatch())
        {
            return false;
        }

        ResolveAndSubscribe();

        if (phone == null ||
            choices == null ||
            dialogueManager == null ||
            string.IsNullOrWhiteSpace(
                helpButtonText) ||
            string.IsNullOrWhiteSpace(
                refuseButtonText) ||
            string.IsNullOrWhiteSpace(
                contactId) ||
            string.IsNullOrWhiteSpace(
                scenarioId) ||
            string.IsNullOrWhiteSpace(
                actionChoiceId))
        {
            Debug.LogError(
                "ClientPhoneRequestController: " +
                "заполни ссылки, кнопки и ID.",
                this);

            return false;
        }

        if (!phone.HasScenarioChoice(
                contactId,
                scenarioId,
                actionChoiceId))
        {
            Debug.LogError(
                "ClientPhoneRequestController: " +
                "не найден вариант действия " +
                contactId + " / " +
                scenarioId + " / " +
                actionChoiceId +
                ". Проверь ID, Use Choices и Role.",
                this);

            return false;
        }

        /*
         * Старое правило оставляем:
         * входящий штрафной звонок важнее.
         */
        if (phone.HasPriorityIncomingCall)
        {
            Debug.LogWarning(
                "Телефонная просьба пропущена: " +
                "у клиента уже ожидается " +
                "приоритетный штрафной звонок.",
                this);

            return false;
        }

        finalChoiceRequested = true;

        return true;
    }

    private IEnumerator RequestRoutine()
    {
        while (active &&
               selectedRequestChoice < 0)
        {
            yield return null;
        }

        if (!active)
            yield break;

        /*
         * DialogueChoiceController уже
         * вызвал FinishChoicePrompt(true).
         * Теперь убираем оставленную
         * последнюю реплику.
         */
        HideOwnedPanel();


        // =====================================================
        // ОТКАЗ
        // =====================================================

        if (selectedRequestChoice == 1)
        {
            yield return
                PlayResponse(
                    refusedDialogue);

            if (active)
                onRefused?.Invoke();

            ReleaseResources();
            yield break;
        }


        // =====================================================
        // ПОМОЩЬ
        // =====================================================

        if (!phone.TryBeginScenarioOverride(
                this,
                contactId,
                scenarioId))
        {
            Debug.LogError(
                "Не удалось включить сценарий " +
                contactId + " / " +
                scenarioId +
                ". Проверь доступность телефона.",
                this);

            ReleaseResources();
            yield break;
        }

        waitingForPhone = true;


        /*
         * Если игрок просто взял телефон
         * и положил без звонка —
         * PhoneCallEnded не приходит,
         * женщина продолжает ждать.
         */
        while (active &&
               !phoneCallEnded)
        {
            yield return null;
        }

        if (!active)
            yield break;

        waitingForPhone = false;

        phone.EndScenarioOverride(this);


        // Позвонил, но нужное действие не выбрал.
        if (!actionCommitted)
        {
            yield return
                PlayResponse(
                    cancelledCallDialogue);

            if (active)
                onRefused?.Invoke();

            ReleaseResources();
            yield break;
        }


        waitingForAction = true;
        actionCompleted = false;

        onActionRequested?.Invoke();


        while (active &&
               waitForActionCompletion &&
               !actionCompleted)
        {
            yield return null;
        }

        waitingForAction = false;

        if (!active)
            yield break;


        yield return
            PlayResponse(
                helpedDialogue);

        if (active)
            onHelpCompleted?.Invoke();


        ReleaseResources();
    }

    private void HandleRequestChoice(int index)
    {
        if (!active || selectedRequestChoice >= 0)
            return;

        selectedRequestChoice = index;
    }

    private void HandlePhoneChoice(
        string selectedContact,
        string selectedScenario,
        string selectedChoice)
    {
        if (!active || !waitingForPhone ||
            selectedContact != contactId ||
            selectedScenario != scenarioId)
        {
            return;
        }

        if (selectedChoice == actionChoiceId)
            actionCommitted = true;
    }

    private void HandlePhoneCallEnded(
        string selectedContact,
        string selectedScenario)
    {
        if (!active || !waitingForPhone ||
            selectedContact != contactId ||
            selectedScenario != scenarioId)
        {
            return;
        }

        phoneCallEnded = true;
    }

    public void NotifyActionCompleted()
    {
        if (active && waitingForAction)
            actionCompleted = true;
    }

    private IEnumerator PlayResponse(
        List<DialogueManager.DialogueLine> lines)
    {
        if (LastValidLine(lines) < 0)
            yield break;

        while (DialogueManager.AnyDialogueActive)
            yield return null;

        if (!active || dialogueManager == null)
            yield break;

        clientNPC.RestoreClientDialogueVoice();
        ownsDialogue = true;

        dialogueManager.StartDialogue(lines, false);

        while (dialogueManager != null &&
               dialogueManager.DialogueActive)
        {
            yield return null;
        }

        HideOwnedPanel();

        if (clientNPC != null)
            clientNPC.RestoreWorkControlAfterSpecialDialogue();
    }

    private static int LastValidLine(
        List<DialogueManager.DialogueLine> lines)
    {
        if (lines == null)
            return -1;

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (lines[i] != null)
                return i;
        }

        return -1;
    }

    private void HideOwnedPanel()
    {
        if (!ownsDialogue)
            return;

        if (dialogueManager != null)
            dialogueManager.HidePersistentDialogue();

        ownsDialogue = false;
    }

    private void ReleaseResources()
    {
        active = false;
        finalChoiceRequested = false;
        waitingForPhone = false;
        waitingForAction = false;
        routine = null;

        if (choices != null)
            choices.HideChoices(this);

        HideOwnedPanel();

        if (phone != null)
            phone.EndScenarioOverride(this);

        if (finalBlockApplied && clientNPC != null)
            clientNPC.ReleaseFinalCompletion(this);

        finalBlockApplied = false;
    }
}
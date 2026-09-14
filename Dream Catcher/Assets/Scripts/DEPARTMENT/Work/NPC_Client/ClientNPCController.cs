using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientNPCController : MonoBehaviour, IInteractable
{
    public static ClientNPCController CurrentActiveClient
    {
        get;
        private set;
    }

    private enum ClientDialogueStage
    {
        WaitingForApproach,
        FirstDialogueReady,
        FirstDialogueRunning,
        WaitingForDirectionTab,
        QuestionDialogueReady,
        GiveSon3DialogueReady,
        GiveSon3DialogueRunning,
        WaitingForSon3Return,
        FinalDialogueRunning,
        TakeSon3AnimationRunning,
        Completed,

        // Добавлено в конец, чтобы сохранить значения старых состояний.
        WaitingForSpecialFinal
    }

    [Header("Данные клиента")]

    [SerializeField]
    private VisitorCaseData visitorData;

    [SerializeField]
    private int activeVariantIndex;

    [SerializeField]
    private ClientInfoPanelController clientInfoPanel;

    [Header("Вариативный диалог")]

    [SerializeField]
    private ClientQuestionDialogueController questionDialogueController;

    [SerializeField]
    private ComputerInterfaceNavigation computerNavigation;

    [Header("Голос клиента")]

    [SerializeField]
    private AudioSource voiceAudioSource;

    [Header("Анимация")]

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string approachTriggerName = "Podhodit";

    [SerializeField]
    private string giveSon3TriggerName = "Give_SON3";

    [SerializeField]
    private string takeSon3TriggerName = "Take_SON3";

    [SerializeField]
    private float takeSon3StartTimeout = 2f;

    [SerializeField]
    private float giveSon3StartTimeout = 2f;

    [Range(0f, 1f)]
    [SerializeField]
    private float giveSon3ReadyNormalizedTime = 0.9f;

    [SerializeField]
    private int animatorLayerIndex;

    [SerializeField]
    private float approachStartTimeout = 5f;

    [Header("SON-3")]

    [SerializeField]
    private Son3DragController son3;

    [SerializeField]
    private WorkSon3TrayController son3Tray;

    [SerializeField]
    private Transform workItemsRoot;

    [Header("Взаимодействие")]

    [SerializeField]
    private Collider interactionCollider;

    [SerializeField]
    private string defaultLayerName = "Default";

    [SerializeField]
    private string interactableLayerName = "Interactable";

    [SerializeField]
    private string clientColliderObjectName = "ClientInteractionCollider";

    [Header("Диалог")]

    [SerializeField]
    private DialogueManager dialogueManager;

    [SerializeField]
    private string dialogueManagerObjectName = "DialogueManager";

    [Header("Текущее состояние")]

    [SerializeField]
    private ClientDialogueStage dialogueStage =
        ClientDialogueStage.WaitingForApproach;

    [SerializeField]
    private bool approachStarted;

    [SerializeField]
    private bool interactionAvailable;

    [SerializeField]
    private bool dialogueInteractionLocked;

    [SerializeField]
    private bool directionTabOpened;

    private bool directionSubmitted;
    private bool waitingForSon3Return;

    private DirectionDecision submittedDecision = DirectionDecision.None;

    private VisitorCaseData.VisitorCaseVariant activeVariant;

    private Coroutine approachCoroutine;
    private Coroutine dialogueCoroutine;
    private Coroutine takeSon3Coroutine;
    private Coroutine giveSon3Coroutine;
    private bool giveSon3AnimationReady;

    private readonly HashSet<UnityEngine.Object> finalCompletionBlockers =
        new HashSet<UnityEngine.Object>();

    private Coroutine finalContinuationCoroutine;
    private bool finalContinuationStarted;

    private readonly List<DialogueManager.DialogueLine>
        finalDialogueChoicePrefix =
            new List<DialogueManager.DialogueLine>();

    public bool IsFinished =>
        dialogueStage == ClientDialogueStage.Completed;

    public DialogueManager DialogueManagerReference => dialogueManager;

    public ClientQuestionDialogueController
        QuestionDialogueControllerReference => questionDialogueController;

    public bool IsFinalDialogueRunning =>
        dialogueStage == ClientDialogueStage.FinalDialogueRunning;

    public DirectionDecision SubmittedDecision => submittedDecision;

    public event Action<ClientNPCController> FinalDialogueStarted;
    public event Action<ClientNPCController> ClientFinished;

    // Кто-то из сюжетных компонентов может попросить,
    // чтобы последняя реплика Final Dialogue стала ChoicePrompt.
    public event Func<ClientNPCController, bool>
        FinalDialogueChoiceRequested;

    // Последняя реплика полностью допечаталась
    // и готова к показу внешних плашек.
    public event Action<ClientNPCController>
        FinalDialogueChoicePromptReady;

    // Обычная финальная последовательность завершилась,
    // но очередь ещё не получила ClientFinished.
    public event Action<ClientNPCController> FinalSequenceFinishing;

    private void Awake()
    {
        FindReferences();
        ApplyClientData();
        SetInteractionAvailable(false);
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeToComputerNavigation();
    }

    private void Reset()
    {
        FindReferences();
    }

    private void OnDisable()
    {
        StopFinalContinuation();
        UnsubscribeFromComputerNavigation();

        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
            approachCoroutine = null;
        }

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        if (giveSon3Coroutine != null)
        {
            StopCoroutine(giveSon3Coroutine);
            giveSon3Coroutine = null;
        }

        if (takeSon3Coroutine != null)
        {
            StopCoroutine(takeSon3Coroutine);
            takeSon3Coroutine = null;
        }

        if (questionDialogueController != null &&
            questionDialogueController.IsOpen)
        {
            questionDialogueController.CloseDialogue();
        }

        dialogueInteractionLocked = false;

        if (son3 != null)
            son3.ReturnedToOriginalPlace -= HandleSon3Returned;

        if (CurrentActiveClient == this)
            CurrentActiveClient = null;
    }

    public void Initialize(
        VisitorCaseData newVisitorData,
        int newVariantIndex,
        ClientInfoPanelController newClientInfoPanel)
    {
        visitorData = newVisitorData;
        activeVariantIndex = newVariantIndex;

        if (newClientInfoPanel != null)
            clientInfoPanel = newClientInfoPanel;

        ResetRuntimeStateForNewClient();
        ApplyClientData();
    }

    public void StartApproach()
    {
        if (approachStarted)
            return;

        FindReferences();
        ApplyClientData();

        if (animator == null ||
            interactionCollider == null ||
            activeVariant == null)
        {
            return;
        }

        CurrentActiveClient = this;

        if (SessionStatsManager.Instance != null)
            SessionStatsManager.Instance.TrackClient(this);

        approachStarted = true;
        dialogueStage = ClientDialogueStage.WaitingForApproach;

        SetInteractionAvailable(false);

        animator.ResetTrigger(approachTriggerName);
        animator.SetTrigger(approachTriggerName);

        if (approachCoroutine != null)
            StopCoroutine(approachCoroutine);

        approachCoroutine =
            StartCoroutine(WaitForApproachToFinish());
    }

    public void Interact()
    {
        if (!interactionAvailable || dialogueInteractionLocked)
            return;

        if (DialogueManager.AnyDialogueActive)
            return;

        if (dialogueStage == ClientDialogueStage.FirstDialogueReady)
        {
            StartFirstDialogue();
            return;
        }

        if (dialogueStage == ClientDialogueStage.QuestionDialogueReady)
        {
            ToggleQuestionDialogue();
            return;
        }

        if (dialogueStage == ClientDialogueStage.GiveSon3DialogueReady)
            StartGiveSon3Dialogue();
    }

    public void ApplyClientInformation()
    {
        if (visitorData == null ||
            activeVariant == null ||
            clientInfoPanel == null)
        {
            return;
        }

        clientInfoPanel.ShowClient(visitorData, activeVariant);
    }

    public void UnlockQuestionDialogue()
    {
        directionTabOpened = true;
        TryUnlockQuestionDialogue();
    }

    public void SetInteractionAvailable(bool available)
    {
        FindInteractionReferences();
        interactionAvailable = available;

        if (interactionCollider == null)
            return;

        string layerName =
            available ? interactableLayerName : defaultLayerName;

        int targetLayer = LayerMask.NameToLayer(layerName);

        if (targetLayer < 0)
            return;

        interactionCollider.gameObject.layer = targetLayer;
    }

    public void MakeInteractable()
    {
        SetInteractionAvailable(true);
    }

    public void MakeNotInteractable()
    {
        SetInteractionAvailable(false);
    }

    private IEnumerator WaitForApproachToFinish()
    {
        int approachStateHash = Animator.StringToHash(approachTriggerName);

        float elapsed = 0f;
        bool enteredApproachState = false;

        while (elapsed < approachStartTimeout)
        {
            AnimatorStateInfo currentState =
                animator.GetCurrentAnimatorStateInfo(animatorLayerIndex);

            AnimatorStateInfo nextState =
                animator.GetNextAnimatorStateInfo(animatorLayerIndex);

            bool currentIsApproach =
                currentState.shortNameHash == approachStateHash ||
                currentState.IsName(approachTriggerName);

            bool nextIsApproach =
                nextState.shortNameHash == approachStateHash ||
                nextState.IsName(approachTriggerName);

            if (currentIsApproach || nextIsApproach)
            {
                enteredApproachState = true;
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!enteredApproachState)
        {
            approachCoroutine = null;
            yield break;
        }

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(animatorLayerIndex);

            bool isApproachState =
                stateInfo.shortNameHash == approachStateHash ||
                stateInfo.IsName(approachTriggerName);

            if (isApproachState)
                break;

            yield return null;
        }

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(animatorLayerIndex);

            bool isApproachState =
                stateInfo.shortNameHash == approachStateHash ||
                stateInfo.IsName(approachTriggerName);

            bool isTransitioning =
                animator.IsInTransition(animatorLayerIndex);

            if (isApproachState &&
                stateInfo.normalizedTime >= 1f &&
                !isTransitioning)
            {
                break;
            }

            if (!isApproachState && !isTransitioning)
                break;

            yield return null;
        }

        dialogueStage = ClientDialogueStage.FirstDialogueReady;
        SetInteractionAvailable(true);
        approachCoroutine = null;
    }

    private void StartFirstDialogue()
    {
        if (activeVariant == null ||
            activeVariant.FirstDialogue == null ||
            activeVariant.FirstDialogue.Count == 0)
        {
            return;
        }

        FindDialogueManagerByExactName();

        if (dialogueManager == null)
            return;

        dialogueInteractionLocked = true;
        dialogueStage = ClientDialogueStage.FirstDialogueRunning;

        SetInteractionAvailable(false);
        ApplyVoiceSettings();

        dialogueManager.StartDialogue(activeVariant.FirstDialogue, false);

        if (!dialogueManager.DialogueActive)
        {
            dialogueInteractionLocked = false;
            dialogueStage = ClientDialogueStage.FirstDialogueReady;
            SetInteractionAvailable(true);
            return;
        }

        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine =
            StartCoroutine(WaitForFirstDialogueToFinish());
    }

    private IEnumerator WaitForFirstDialogueToFinish()
    {
        bool giveSon3Triggered = false;

        while (dialogueManager != null && dialogueManager.DialogueActive)
        {
            bool shouldGiveSon3 =
                activeVariant != null &&
                activeVariant.GiveSon3DuringFirstDialogue;

            int giveSon3Index =
                activeVariant != null
                    ? activeVariant.GiveSon3DialogueIndex
                    : -1;

            if (shouldGiveSon3 &&
                !giveSon3Triggered &&
                giveSon3Index >= 0 &&
                dialogueManager.CurrentLineIndex >= giveSon3Index)
            {
                giveSon3Triggered = true;
                StartGiveSon3Animation();
            }

            yield return null;
        }

        bool needsSon3 =
            activeVariant != null &&
            activeVariant.GiveSon3DuringFirstDialogue;

        if (needsSon3 && !giveSon3Triggered)
        {
            giveSon3Triggered = true;
            StartGiveSon3Animation();
        }

        if (needsSon3 && giveSon3Triggered)
        {
            while (!giveSon3AnimationReady && giveSon3Coroutine != null)
                yield return null;
        }

        if (needsSon3 &&
            giveSon3AnimationReady &&
            son3 != null &&
            son3Tray != null)
        {
            son3.PrepareForPlayer(workItemsRoot, son3Tray);
            son3Tray.EnablePlacement();
        }

        dialogueStage = ClientDialogueStage.WaitingForDirectionTab;
        SetInteractionAvailable(false);

        yield return null;
        RestoreWorkStateAfterDialogue();

        yield return null;
        RestoreWorkStateAfterDialogue();

        dialogueInteractionLocked = false;
        dialogueCoroutine = null;

        TryUnlockQuestionDialogue();
    }

    private void StartGiveSon3Animation()
    {
        if (giveSon3AnimationReady ||
            giveSon3Coroutine != null ||
            animator == null)
        {
            return;
        }

        animator.ResetTrigger(giveSon3TriggerName);
        animator.SetTrigger(giveSon3TriggerName);

        giveSon3Coroutine =
            StartCoroutine(WaitForGiveSon3Ready());
    }

    private IEnumerator WaitForGiveSon3Ready()
    {
        int stateHash = Animator.StringToHash(giveSon3TriggerName);

        float elapsed = 0f;
        bool enteredState = false;

        while (elapsed < giveSon3StartTimeout)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(animatorLayerIndex);

            bool isGiveSon3State =
                stateInfo.shortNameHash == stateHash ||
                stateInfo.IsName(giveSon3TriggerName);

            if (isGiveSon3State)
            {
                enteredState = true;
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!enteredState)
        {
            Debug.LogError(
                "ClientNPCController: NPC \"" + gameObject.name +
                "\" не вошёл в состояние " + giveSon3TriggerName + ".");

            giveSon3Coroutine = null;
            yield break;
        }

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(animatorLayerIndex);

            bool isGiveSon3State =
                stateInfo.shortNameHash == stateHash ||
                stateInfo.IsName(giveSon3TriggerName);

            if (isGiveSon3State &&
                stateInfo.normalizedTime >= giveSon3ReadyNormalizedTime)
            {
                break;
            }

            if (!isGiveSon3State &&
                !animator.IsInTransition(animatorLayerIndex))
            {
                break;
            }

            yield return null;
        }

        giveSon3AnimationReady = true;
        giveSon3Coroutine = null;
    }

    private void ToggleQuestionDialogue()
    {
        if (questionDialogueController == null)
            return;

        if (questionDialogueController.IsOpen)
        {
            questionDialogueController.CloseDialogue();
            return;
        }

        ApplyVoiceSettings();
        questionDialogueController.OpenDialogue();
    }

    private void HandleElectronicDirectionOpened()
    {
        directionTabOpened = true;
        TryUnlockQuestionDialogue();
    }

    private void TryUnlockQuestionDialogue()
    {
        if (!directionTabOpened &&
            computerNavigation != null &&
            computerNavigation.IsElectronicDirectionSelected)
        {
            directionTabOpened = true;
        }

        if (!directionTabOpened ||
            dialogueStage != ClientDialogueStage.WaitingForDirectionTab)
        {
            return;
        }

        dialogueStage = ClientDialogueStage.QuestionDialogueReady;
        SetInteractionAvailable(true);
    }

    public void NotifyDirectionSubmitted(DirectionDecision decision)
    {
        if (directionSubmitted)
            return;

        submittedDecision = decision;
        directionSubmitted = true;
        waitingForSon3Return = false;

        if (questionDialogueController != null &&
            questionDialogueController.IsOpen)
        {
            questionDialogueController.CloseDialogue();
        }

        dialogueStage = ClientDialogueStage.GiveSon3DialogueReady;
        SetInteractionAvailable(true);
    }

    public void BlockFinalCompletion(UnityEngine.Object source)
    {
        if (source != null)
            finalCompletionBlockers.Add(source);
    }

    public void ReleaseFinalCompletion(UnityEngine.Object source)
    {
        if (source != null)
            finalCompletionBlockers.Remove(source);
    }

    private bool IsFinalCompletionBlocked()
    {
        finalCompletionBlockers.RemoveWhere(blocker => blocker == null);
        return finalCompletionBlockers.Count > 0;
    }

    private void StartGiveSon3Dialogue()
    {
        if (!directionSubmitted || activeVariant == null)
            return;

        FindDialogueManagerByExactName();

        if (dialogueManager == null)
            return;

        if (activeVariant.GiveSon3Dialogue == null ||
            activeVariant.GiveSon3Dialogue.Count == 0)
        {
            BeginWaitingForSon3Return();
            return;
        }

        dialogueInteractionLocked = true;
        dialogueStage = ClientDialogueStage.GiveSon3DialogueRunning;

        SetInteractionAvailable(false);
        ApplyVoiceSettings();

        dialogueManager.StartDialogue(activeVariant.GiveSon3Dialogue, false);

        if (!dialogueManager.DialogueActive)
        {
            dialogueInteractionLocked = false;
            BeginWaitingForSon3Return();
            return;
        }

        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine =
            StartCoroutine(WaitForGiveSon3DialogueToFinish());
    }

    private IEnumerator WaitForGiveSon3DialogueToFinish()
    {
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        dialogueInteractionLocked = false;

        yield return null;

        RestoreWorkStateAfterDialogue();
        dialogueCoroutine = null;
        BeginWaitingForSon3Return();
    }

    private void BeginWaitingForSon3Return()
    {
        waitingForSon3Return = true;
        dialogueStage = ClientDialogueStage.WaitingForSon3Return;

        SetInteractionAvailable(false);

        if (son3 == null)
            return;

        son3.ReturnedToOriginalPlace -= HandleSon3Returned;
        son3.ReturnedToOriginalPlace += HandleSon3Returned;

        son3.EnableReturnToOriginalPlace();
    }

    private void HandleSon3Returned()
    {
        if (!waitingForSon3Return)
            return;

        waitingForSon3Return = false;

        if (son3 != null)
            son3.ReturnedToOriginalPlace -= HandleSon3Returned;

        StartFinalDialogue();
    }

    private bool ShouldUseFinalDialogueChoice()
    {
        if (FinalDialogueChoiceRequested == null)
            return false;

        Delegate[] handlers =
            FinalDialogueChoiceRequested
                .GetInvocationList();

        for (int i = 0; i < handlers.Length; i++)
        {
            Func<ClientNPCController, bool> handler =
                handlers[i] as
                    Func<ClientNPCController, bool>;

            if (handler != null &&
                handler(this))
            {
                return true;
            }
        }

        return false;
    }

    private void StartFinalDialogue()
    {
        FindDialogueManagerByExactName();

        if (dialogueManager == null ||
            activeVariant == null)
        {
            CompleteFinalDialogue();
            return;
        }

        bool personalQuestionAsked =
            questionDialogueController != null &&
            questionDialogueController
                .PersonalQuestionAsked;

        List<DialogueManager.DialogueLine>
            resolvedFinalDialogue =
                activeVariant.ResolveFinalDialogue(
                    personalQuestionAsked,
                    submittedDecision);

        if (resolvedFinalDialogue == null ||
            resolvedFinalDialogue.Count == 0)
        {
            CompleteFinalDialogue();
            return;
        }

        dialogueInteractionLocked = true;
        dialogueStage =
            ClientDialogueStage.FinalDialogueRunning;

        SetInteractionAvailable(false);
        ApplyVoiceSettings();


        // =====================================================
        // ОСОБЫЙ FINAL DIALOGUE С ВЫБОРОМ
        // =====================================================

        if (ShouldUseFinalDialogueChoice())
        {
            if (dialogueCoroutine != null)
                StopCoroutine(dialogueCoroutine);

            dialogueCoroutine =
                StartCoroutine(
                    WaitForFinalDialogueChoiceToFinish(
                        resolvedFinalDialogue));

            /*
             * StartCoroutine выполняется сразу
             * до первого yield, поэтому к этому
             * моменту первая часть диалога
             * уже запущена.
             */
            FinalDialogueStarted?.Invoke(this);

            return;
        }


        // =====================================================
        // ОБЫЧНЫЙ FINAL DIALOGUE — СТАРОЕ ПОВЕДЕНИЕ
        // =====================================================

        dialogueManager.StartDialogue(
            resolvedFinalDialogue,
            false);

        if (!dialogueManager.DialogueActive)
        {
            dialogueInteractionLocked = false;
            CompleteFinalDialogue();
            return;
        }

        FinalDialogueStarted?.Invoke(this);

        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine =
            StartCoroutine(
                WaitForFinalDialogueToFinish());
    }

    private IEnumerator
    WaitForFinalDialogueChoiceToFinish(
        List<DialogueManager.DialogueLine> lines)
    {
        int finalIndex = -1;

        for (int i = lines.Count - 1;
             i >= 0;
             i--)
        {
            if (lines[i] != null)
            {
                finalIndex = i;
                break;
            }
        }

        if (finalIndex < 0)
        {
            dialogueInteractionLocked = false;
            dialogueCoroutine = null;

            CompleteFinalDialogue();
            yield break;
        }


        // =====================================================
        // ВСЕ РЕПЛИКИ ДО ПОСЛЕДНЕЙ
        // =====================================================

        finalDialogueChoicePrefix.Clear();

        for (int i = 0;
             i < finalIndex;
             i++)
        {
            if (lines[i] != null)
            {
                finalDialogueChoicePrefix.Add(
                    lines[i]);
            }
        }

        if (finalDialogueChoicePrefix.Count > 0)
        {
            /*
             * keepLastLineVisible = true:
             * панель не пропадает между
             * предпоследней и последней репликой.
             */
            dialogueManager.StartDialogue(
                finalDialogueChoicePrefix,
                false,
                true);

            while (dialogueManager != null &&
                   dialogueManager.DialogueActive)
            {
                yield return null;
            }
        }


        if (dialogueManager == null)
        {
            dialogueInteractionLocked = false;
            dialogueCoroutine = null;

            CompleteFinalDialogue();
            yield break;
        }


        // =====================================================
        // ПОСЛЕДНЯЯ РЕПЛИКА
        // =====================================================

        /*
         * Используем УЖЕ существующий
         * ChoicePrompt DialogueManager.
         *
         * Поэтому последняя реплика:
         * - нормально печатается;
         * - не закрывается LMB;
         * - остаётся на экране;
         * - ждёт внешние плашки.
         */
        dialogueManager.ShowChoicePrompt(
            lines[finalIndex],
            false);

        while (dialogueManager != null &&
               dialogueManager.DialogueActive &&
               !dialogueManager.ChoicePromptReady)
        {
            yield return null;
        }

        if (dialogueManager == null)
        {
            dialogueInteractionLocked = false;
            dialogueCoroutine = null;

            CompleteFinalDialogue();
            yield break;
        }


        // Последняя строка полностью готова.
        FinalDialogueChoicePromptReady?.Invoke(this);


        // Теперь ждём клика по одной из плашек.
        // DialogueChoiceController сам вызовет
        // FinishChoicePrompt().
        while (dialogueManager != null &&
               dialogueManager.DialogueActive)
        {
            yield return null;
        }


        dialogueInteractionLocked = false;

        yield return null;

        RestoreWorkStateAfterDialogue();


        /*
         * Здесь как раз будет стоять
         * ClientPhoneRequestController.
         *
         * Поэтому Take_SON3 НЕ начнётся,
         * пока женщина ждёт звонка/охрану.
         */
        while (IsFinalCompletionBlocked())
            yield return null;


        dialogueCoroutine = null;

        CompleteFinalDialogue();
    }

    private IEnumerator WaitForFinalDialogueToFinish()
    {
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        dialogueInteractionLocked = false;

        yield return null;

        RestoreWorkStateAfterDialogue();

        // Старые события, например передача жетона,
        // по-прежнему могут задерживать Take_SON3.
        while (IsFinalCompletionBlocked())
            yield return null;

        dialogueCoroutine = null;
        CompleteFinalDialogue();
    }

    private void CompleteFinalDialogue()
    {
        SetInteractionAvailable(false);

        if (animator == null)
        {
            FinishClient();
            return;
        }

        dialogueStage = ClientDialogueStage.TakeSon3AnimationRunning;

        animator.ResetTrigger(takeSon3TriggerName);
        animator.SetTrigger(takeSon3TriggerName);

        if (takeSon3Coroutine != null)
            StopCoroutine(takeSon3Coroutine);

        takeSon3Coroutine =
            StartCoroutine(WaitForTakeSon3AnimationToFinish());
    }

    private IEnumerator WaitForTakeSon3AnimationToFinish()
    {
        int stateHash = Animator.StringToHash(takeSon3TriggerName);

        float elapsed = 0f;
        bool enteredState = false;

        while (elapsed < takeSon3StartTimeout)
        {
            AnimatorStateInfo currentState =
                animator.GetCurrentAnimatorStateInfo(animatorLayerIndex);

            AnimatorStateInfo nextState =
                animator.GetNextAnimatorStateInfo(animatorLayerIndex);

            bool currentMatches =
                currentState.shortNameHash == stateHash ||
                currentState.IsName(takeSon3TriggerName);

            bool nextMatches =
                nextState.shortNameHash == stateHash ||
                nextState.IsName(takeSon3TriggerName);

            if (currentMatches || nextMatches)
            {
                enteredState = true;
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!enteredState)
        {
            takeSon3Coroutine = null;
            FinishClient();
            yield break;
        }

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(animatorLayerIndex);

            bool isTakeSon3State =
                stateInfo.shortNameHash == stateHash ||
                stateInfo.IsName(takeSon3TriggerName);

            bool isTransitioning =
                animator.IsInTransition(animatorLayerIndex);

            if (isTakeSon3State &&
                stateInfo.normalizedTime >= 1f &&
                !isTransitioning)
            {
                break;
            }

            if (!isTakeSon3State && !isTransitioning)
                break;

            yield return null;
        }

        takeSon3Coroutine = null;
        FinishClient();
    }

    private void FinishClient()
    {
        if (IsFinished || finalContinuationStarted)
            return;

        finalContinuationStarted = true;
        dialogueStage = ClientDialogueStage.WaitingForSpecialFinal;

        SetInteractionAvailable(false);

        // Подписчик ставит свой блокировщик синхронно.
        FinalSequenceFinishing?.Invoke(this);

        if (!isActiveAndEnabled)
            return;

        if (!IsFinalCompletionBlocked())
        {
            CompleteFinalContinuation();
            return;
        }

        finalContinuationCoroutine =
            StartCoroutine(WaitForFinalContinuation());
    }

    private IEnumerator WaitForFinalContinuation()
    {
        while (IsFinalCompletionBlocked())
            yield return null;

        CompleteFinalContinuation();
    }

    private void CompleteFinalContinuation()
    {
        finalContinuationCoroutine = null;
        finalContinuationStarted = false;

        FinishClientImmediately();
    }

    private void FinishClientImmediately()
    {
        if (IsFinished)
            return;

        dialogueStage = ClientDialogueStage.Completed;
        SetInteractionAvailable(false);
        ClientFinished?.Invoke(this);
    }

    private void StopFinalContinuation()
    {
        if (finalContinuationCoroutine != null)
        {
            StopCoroutine(finalContinuationCoroutine);
            finalContinuationCoroutine = null;
        }

        finalContinuationStarted = false;
    }

    public void RestoreClientDialogueVoice()
    {
        ApplyVoiceSettings();
    }

    public void RestoreWorkControlAfterSpecialDialogue()
    {
        RestoreWorkStateAfterDialogue();
    }

    private void ApplyClientData()
    {
        if (visitorData == null)
        {
            activeVariant = null;
            return;
        }

        activeVariant = visitorData.GetVariant(activeVariantIndex);

        ApplyClientInformation();
        ApplyVoiceSettings();

        if (questionDialogueController != null)
            questionDialogueController.Configure(activeVariant);
    }

    private void ApplyVoiceSettings()
    {
        if (dialogueManager == null || voiceAudioSource == null)
            return;

        if (visitorData != null && visitorData.VoiceClip != null)
            voiceAudioSource.clip = visitorData.VoiceClip;

        dialogueManager.defaultVoiceAudioSource = voiceAudioSource;
    }

    private void ResetRuntimeStateForNewClient()
    {
        StopFinalContinuation();

        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
            approachCoroutine = null;
        }

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        if (giveSon3Coroutine != null)
        {
            StopCoroutine(giveSon3Coroutine);
            giveSon3Coroutine = null;
        }

        giveSon3AnimationReady = false;

        if (takeSon3Coroutine != null)
        {
            StopCoroutine(takeSon3Coroutine);
            takeSon3Coroutine = null;
        }

        if (questionDialogueController != null &&
            questionDialogueController.IsOpen)
        {
            questionDialogueController.CloseDialogue();
        }

        approachStarted = false;
        interactionAvailable = false;
        dialogueInteractionLocked = false;
        directionTabOpened = false;
        directionSubmitted = false;
        submittedDecision = DirectionDecision.None;
        waitingForSon3Return = false;

        finalCompletionBlockers.Clear();

        if (son3 != null)
            son3.ReturnedToOriginalPlace -= HandleSon3Returned;

        dialogueStage = ClientDialogueStage.WaitingForApproach;
        SetInteractionAvailable(false);
    }

    private void RestoreWorkStateAfterDialogue()
    {
        WorkSessionManager workSession = WorkSessionManager.Instance;

        if (workSession == null || !workSession.IsSeated)
            return;

        if (workSession.seatController != null)
            workSession.seatController.RestoreWorkControlAfterPause();

        if (workSession.cursorController != null)
            workSession.cursorController.ShowWorkCursor();
    }

    private void FindReferences()
    {
        FindAnimator();
        FindInteractionReferences();
        FindSon3Tray();
        FindDialogueManagerByExactName();
        FindQuestionDialogueController();
        FindComputerNavigation();
        FindVoiceAudioSource();
    }

    private void FindAnimator()
    {
        if (animator != null)
            return;

        animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void FindInteractionReferences()
    {
        if (son3 == null)
            son3 = GetComponentInChildren<Son3DragController>(true);

        if (interactionCollider != null)
            return;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null &&
                colliders[i].gameObject.name == clientColliderObjectName)
            {
                interactionCollider = colliders[i];
                return;
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];

            if (currentCollider == null)
                continue;

            if (son3 != null &&
                currentCollider.transform.IsChildOf(son3.transform))
            {
                continue;
            }

            interactionCollider = currentCollider;
            return;
        }
    }

    private void FindSon3Tray()
    {
        if (son3Tray == null)
        {
            son3Tray = FindFirstObjectByType<WorkSon3TrayController>(
                FindObjectsInactive.Include);
        }
    }

    private void FindDialogueManagerByExactName()
    {
        if (dialogueManager != null &&
            dialogueManager.gameObject.name == dialogueManagerObjectName)
        {
            return;
        }

        dialogueManager = null;

        GameObject dialogueObject =
            GameObject.Find(dialogueManagerObjectName);

        if (dialogueObject != null)
            dialogueManager = dialogueObject.GetComponent<DialogueManager>();

        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>(
                FindObjectsInactive.Include);
        }
    }

    private void FindQuestionDialogueController()
    {
        if (questionDialogueController == null)
        {
            questionDialogueController =
                FindFirstObjectByType<ClientQuestionDialogueController>(
                    FindObjectsInactive.Include);
        }
    }

    private void FindComputerNavigation()
    {
        if (computerNavigation == null)
        {
            computerNavigation =
                FindFirstObjectByType<ComputerInterfaceNavigation>(
                    FindObjectsInactive.Include);
        }
    }

    private void FindVoiceAudioSource()
    {
        if (voiceAudioSource != null)
            return;

        voiceAudioSource = GetComponent<AudioSource>();

        if (voiceAudioSource == null)
            voiceAudioSource = GetComponentInChildren<AudioSource>(true);
    }

    private void SubscribeToComputerNavigation()
    {
        if (computerNavigation == null)
            return;

        computerNavigation.ElectronicDirectionOpened -=
            HandleElectronicDirectionOpened;

        computerNavigation.ElectronicDirectionOpened +=
            HandleElectronicDirectionOpened;
    }

    private void UnsubscribeFromComputerNavigation()
    {
        if (computerNavigation != null)
        {
            computerNavigation.ElectronicDirectionOpened -=
                HandleElectronicDirectionOpened;
        }
    }
}
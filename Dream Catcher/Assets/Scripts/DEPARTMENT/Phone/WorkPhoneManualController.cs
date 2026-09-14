using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkPhoneManualController : MonoBehaviour
{
    public enum PhoneChoiceRole
    {
        Action = 0,
        Question = 1,
        EndCall = 2
    }

    [Serializable]
    public class PhoneChoiceData
    {
        [Header("Выбор")]

        [Tooltip("Постоянный ID варианта. Например ask_about_work или goodbye.")]
        [SerializeField]
        private string choiceId;

        [Tooltip("Текст на глобальной плашке.")]
        [SerializeField]
        private string buttonText;

        [Header("Диалог после выбора")]

        [SerializeField]
        private List<DialogueManager.DialogueLine> responseDialogue =
            new List<DialogueManager.DialogueLine>();

        [Header("После ответа")]

        [Tooltip("После реплик автоматически положить телефон.")]
        [SerializeField]
        private bool hangUpAfterResponse = true;

        [Header("Назначение варианта")]

        [Tooltip(
            "Action — сюжетное действие. " +
            "Question — запоминаемый вопрос. " +
            "EndCall — завершение звонка без зачёта вопроса.")]
        [SerializeField]
        private PhoneChoiceRole role = PhoneChoiceRole.Action;

        [SerializeField]
        private bool initiallyAvailable = true;

        [Tooltip("Разрешать повтор уже заданного вопроса.")]
        [SerializeField]
        private bool allowRepeatQuestion;

        public string ChoiceId => choiceId;
        public string ButtonText => buttonText;

        public List<DialogueManager.DialogueLine> ResponseDialogue =>
            responseDialogue;

        public bool HangUpAfterResponse => hangUpAfterResponse;
        public PhoneChoiceRole Role => role;
        public bool InitiallyAvailable => initiallyAvailable;
        public bool AllowRepeatQuestion => allowRepeatQuestion;

        public bool HasButton =>
            !string.IsNullOrWhiteSpace(buttonText);
    }

    [Serializable]
    public class PhoneCallData
    {
        [Header("Первый диалог")]

        [SerializeField]
        private List<DialogueManager.DialogueLine> openingDialogue =
            new List<DialogueManager.DialogueLine>();

        [Header("Вариативный диалог")]

        [SerializeField]
        private bool useChoices;

        [SerializeField]
        private PhoneChoiceData firstChoice = new PhoneChoiceData();

        [SerializeField]
        private PhoneChoiceData secondChoice = new PhoneChoiceData();

        [Header("Дополнительные варианты")]

        [Tooltip("Варианты после первых двух. Отображаются страницами.")]
        [SerializeField]
        private List<PhoneChoiceData> additionalChoices =
            new List<PhoneChoiceData>();

        public List<DialogueManager.DialogueLine> OpeningDialogue =>
            openingDialogue;

        public PhoneChoiceData FirstChoice => firstChoice;
        public PhoneChoiceData SecondChoice => secondChoice;

        public List<PhoneChoiceData> AdditionalChoices =>
            additionalChoices;

        public bool ChoicesEnabled => useChoices;

        public int ChoiceCount =>
            useChoices ? 2 + (additionalChoices?.Count ?? 0) : 0;

        public bool HasChoices
        {
            get
            {
                if (!useChoices)
                    return false;

                for (int i = 0; i < ChoiceCount; i++)
                {
                    PhoneChoiceData choice = GetChoice(i);

                    if (choice != null && choice.HasButton)
                        return true;
                }

                return false;
            }
        }

        public PhoneChoiceData GetChoice(int index)
        {
            if (!useChoices)
                return null;

            if (index == 0)
                return firstChoice;

            if (index == 1)
                return secondChoice;

            int additionalIndex = index - 2;

            if (additionalChoices == null ||
                additionalIndex < 0 ||
                additionalIndex >= additionalChoices.Count)
            {
                return null;
            }

            return additionalChoices[additionalIndex];
        }
    }

    [Serializable]
    public class PhoneCallScenario
    {
        [Tooltip("Постоянный ID особого разговора.")]
        [SerializeField]
        private string scenarioId;

        [SerializeField]
        private PhoneCallData callData = new PhoneCallData();

        public string ScenarioId => scenarioId;
        public PhoneCallData CallData => callData;
    }

    [Serializable]
    public class PhoneContact
    {
        [Header("Контакт")]

        [SerializeField]
        private string contactId;

        [SerializeField]
        private GameObject screenObject;

        [Header("Голос")]

        [SerializeField]
        private AudioClip voiceClip;

        [Header("Обычный звонок")]

        [SerializeField]
        private PhoneCallData defaultCall = new PhoneCallData();

        [Header("Особые ситуации")]

        [SerializeField]
        private List<PhoneCallScenario> scenarios =
            new List<PhoneCallScenario>();

        [Header("Когда больше нет доступных вопросов")]

        [SerializeField]
        private List<DialogueManager.DialogueLine>
            noAvailableQuestionsDialogue =
                new List<DialogueManager.DialogueLine>
                {
                    new DialogueManager.DialogueLine
                    {
                        text = "Не стоит его больше беспокоить.",
                        useAudio = false
                    }
                };

        public string ContactId => contactId;
        public GameObject ScreenObject => screenObject;
        public AudioClip VoiceClip => voiceClip;

        public List<DialogueManager.DialogueLine>
            NoAvailableQuestionsDialogue =>
                noAvailableQuestionsDialogue;

        public PhoneCallData ResolveCall(string activeScenarioId)
        {
            if (TryGetScenarioCall(activeScenarioId, out PhoneCallData call))
                return call;

            return defaultCall;
        }

        public bool HasScenario(string id)
        {
            return TryGetScenarioCall(id, out _);
        }

        public bool TryGetScenarioCall(string id, out PhoneCallData call)
        {
            call = null;

            if (string.IsNullOrEmpty(id) || scenarios == null)
                return false;

            for (int i = 0; i < scenarios.Count; i++)
            {
                PhoneCallScenario scenario = scenarios[i];

                if (scenario == null || scenario.ScenarioId != id)
                    continue;

                call = scenario.CallData;
                return true;
            }

            return false;
        }
    }

    [Header("Основной телефон")]

    [SerializeField]
    private WorkPhonePenaltyController phoneController;

    [Header("Phone Canvas")]

    [SerializeField]
    private GameObject phoneCanvas;

    [SerializeField]
    private Button leftButton;

    [SerializeField]
    private Button rightButton;

    [SerializeField]
    private Button callButton;

    [SerializeField]
    private Button hangUpButton;

    [Header("Глобальный вариативный диалог")]

    [SerializeField]
    private DialogueChoiceController choiceController;

    [Header("Dialogue Manager")]

    [SerializeField]
    private DialogueManager dialogueManager;

    [Header("Звук телефона")]

    [SerializeField]
    private AudioSource phoneSfxAudioSource;

    [SerializeField]
    private AudioClip phonePowerOnClip;

    [SerializeField]
    private AudioClip dialToneClip;

    [SerializeField]
    private float minimumAnswerDelay = 1.2f;

    [SerializeField]
    private float maximumAnswerDelay = 3.8f;

    [Header("Звук кнопок телефона")]

    [SerializeField]
    private AudioSource phoneButtonAudioSource;

    [SerializeField]
    private AudioClip[] phoneButtonClickClips;

    [Header("Голос контактов")]

    [SerializeField]
    private AudioSource contactVoiceAudioSource;

    [Header("Контакты")]

    [SerializeField]
    private List<PhoneContact> contacts = new List<PhoneContact>();

    [Header("Текст уже заданного вопроса")]

    [SerializeField]
    private Color askedQuestionTextColor =
        new Color(0.55f, 0.55f, 0.55f, 1f);

    // Старые события сохранены.
    public event Action<string, string, string> PhoneChoiceResolved;
    public event Action<string, string> PhoneCallCancelled;

    // Ответ завершён. Телефон ещё может находиться в руке.
    public event Action<string, string, string> PhoneChoiceCommitted;

    // Исходящий звонок завершён, телефон возвращён.
    public event Action<string, string> PhoneCallEnded;

    public static bool AnyManualPhoneOpen { get; private set; }

    public bool IsPhoneOpen => phoneOpen;

    public bool WantsPhoneColliderInteractable => CanStartManualUse;

    public bool HasPriorityIncomingCall =>
        phoneController != null &&
        phoneController.PenaltyCallPendingOrActive;

    public bool CanStartManualUse
    {
        get
        {
            if (!isActiveAndEnabled || phoneOpen || sequenceBusy)
                return false;

            if (WorkSessionManager.Instance == null ||
                !WorkSessionManager.Instance.IsSeated)
            {
                return false;
            }

            if (DialogueManager.AnyDialogueActive)
                return false;

            if (DialogueChoiceController.BlockWorldInteraction)
                return false;

            if (ClientQuestionDialogueController.AnyQuestionDialogueOpen)
                return false;

            if (HasPriorityIncomingCall)
                return false;

            return true;
        }
    }

    private int selectedContactIndex;

    private bool phoneOpen;
    private bool sequenceBusy;
    private bool outgoingCallActive;
    private bool ownsPhoneDialogue;

    private Coroutine sequenceCoroutine;

    private PhoneContact currentContact;
    private PhoneCallData currentCallData;
    private string currentScenarioId;
    private bool currentCallChoiceResolved;

    private AudioSource previousDefaultVoiceSource;
    private bool voiceOverrideActive;

    private readonly Dictionary<string, string> activeScenarios =
        new Dictionary<string, string>();

    private readonly List<DialogueManager.DialogueLine> choiceOpeningPrefix =
        new List<DialogueManager.DialogueLine>();

    private readonly List<DialogueChoiceController.ChoiceOption>
        displayedChoices =
            new List<DialogueChoiceController.ChoiceOption>(8);

    private readonly HashSet<
        (string contact, string scenario, string question)>
        askedQuestions =
            new HashSet<(string, string, string)>();

    private readonly Dictionary<
        (string contact, string scenario, string question), bool>
        questionAvailability =
            new Dictionary<(string, string, string), bool>();

    private UnityEngine.Object scenarioOverrideOwner;
    private string overriddenContactId;
    private string overriddenScenarioId;
    private string previousScenarioId;

    private void Awake()
    {
        FindReferences();
        AddButtonListeners();
        HidePhoneUIImmediately();
    }

    private void OnEnable()
    {
        FindReferences();
    }

    private void OnDisable()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        StopPhoneSfx();

        if (choiceController != null)
            choiceController.HideChoices(this);

        HideOwnedPhoneDialogue();
        RestoreDialogueVoice();

        if (phoneOpen && phoneController != null)
            phoneController.AbortPhoneMotion();

        phoneOpen = false;
        sequenceBusy = false;
        outgoingCallActive = false;
        currentContact = null;
        currentCallData = null;
        currentScenarioId = null;
        currentCallChoiceResolved = false;
        AnyManualPhoneOpen = false;

        HidePhoneUIImmediately();
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
    }

    public bool TryOpenPhone()
    {
        FindReferences();

        if (!CanStartManualUse)
            return false;

        if (phoneController == null ||
            !phoneController.CanUseCameraMotion())
        {
            return false;
        }

        if (sequenceCoroutine != null)
            return false;

        phoneOpen = true;
        sequenceBusy = true;
        outgoingCallActive = false;
        AnyManualPhoneOpen = true;
        selectedContactIndex = 0;

        currentContact = null;
        currentCallData = null;
        currentScenarioId = null;
        currentCallChoiceResolved = false;

        SetPhoneButtons(false, false, false);

        sequenceCoroutine = StartCoroutine(OpenPhoneRoutine());
        return true;
    }

    private void PlayPhoneButtonClick()
    {
        if (phoneButtonAudioSource == null ||
            phoneButtonClickClips == null ||
            phoneButtonClickClips.Length == 0)
        {
            return;
        }

        AudioClip randomClip =
            phoneButtonClickClips[
                UnityEngine.Random.Range(0, phoneButtonClickClips.Length)
            ];

        if (randomClip != null)
            phoneButtonAudioSource.PlayOneShot(randomClip);
    }

    private IEnumerator OpenPhoneRoutine()
    {
        yield return null;

        FindReferences();

        if (phoneController == null)
        {
            FinishForcedClose();
            yield break;
        }

        yield return phoneController.PlayManualTakeAnimation();

        if (!phoneController.LastAnimationSucceeded)
        {
            FinishForcedClose();
            yield break;
        }

        PlayPowerOnSound();
        yield return null;

        phoneController.PreparePhoneForCameraHold();
        phoneController.AttachPhoneForManualUse();

        ShowSelectedContact();

        if (phoneCanvas != null)
            phoneCanvas.SetActive(true);

        sequenceBusy = false;
        sequenceCoroutine = null;
        SetPhoneButtons(true, true, true);
    }

    private IEnumerator PlayOpeningDialogueRoutine()
    {
        if (currentCallData == null || dialogueManager == null)
            yield break;

        List<DialogueManager.DialogueLine> lines =
            currentCallData.OpeningDialogue;

        int finalIndex = FindLastValidDialogueLineIndex(lines);

        if (finalIndex < 0)
            yield break;

        bool needsChoices =
            HasAvailableChoices(
                currentContact, currentScenarioId, currentCallData) &&
            choiceController != null;

        while (DialogueManager.AnyDialogueActive)
            yield return null;

        ownsPhoneDialogue = true;

        if (!needsChoices)
        {
            dialogueManager.StartDialogue(lines, false);

            while (dialogueManager != null &&
                   dialogueManager.DialogueActive)
            {
                yield return null;
            }

            HideOwnedPhoneDialogue();
            yield break;
        }

        choiceOpeningPrefix.Clear();

        for (int i = 0; i < finalIndex; i++)
        {
            if (lines[i] != null)
                choiceOpeningPrefix.Add(lines[i]);
        }

        if (choiceOpeningPrefix.Count > 0)
        {
            dialogueManager.StartDialogue(
                choiceOpeningPrefix, false, true);

            while (dialogueManager != null &&
                   dialogueManager.DialogueActive)
            {
                yield return null;
            }
        }

        while (DialogueManager.AnyDialogueActive)
            yield return null;

        if (dialogueManager == null)
            yield break;

        dialogueManager.ShowChoicePrompt(lines[finalIndex], false);

        while (dialogueManager != null &&
               dialogueManager.DialogueActive &&
               !dialogueManager.ChoicePromptReady)
        {
            yield return null;
        }
    }

    private int FindLastValidDialogueLineIndex(
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

    private void PreviousContact()
    {
        if (!CanNavigateContacts() || contacts == null || contacts.Count == 0)
            return;

        PlayPhoneButtonClick();
        selectedContactIndex--;

        if (selectedContactIndex < 0)
            selectedContactIndex = contacts.Count - 1;

        ShowSelectedContact();
    }

    private void NextContact()
    {
        if (!CanNavigateContacts() || contacts == null || contacts.Count == 0)
            return;

        PlayPhoneButtonClick();
        selectedContactIndex++;

        if (selectedContactIndex >= contacts.Count)
            selectedContactIndex = 0;

        ShowSelectedContact();
    }

    private bool CanNavigateContacts()
    {
        return phoneOpen &&
            !sequenceBusy &&
            !outgoingCallActive &&
            !DialogueManager.AnyDialogueActive &&
            !DialogueChoiceController.AnyChoiceOpen;
    }

    private void ShowSelectedContact()
    {
        if (contacts == null)
            return;

        for (int i = 0; i < contacts.Count; i++)
        {
            PhoneContact contact = contacts[i];

            if (contact == null || contact.ScreenObject == null)
                continue;

            contact.ScreenObject.SetActive(i == selectedContactIndex);
        }
    }

    private PhoneContact GetSelectedContact()
    {
        if (contacts == null || contacts.Count == 0)
            return null;

        if (selectedContactIndex < 0 ||
            selectedContactIndex >= contacts.Count)
        {
            selectedContactIndex = 0;
        }

        return contacts[selectedContactIndex];
    }

    private void CallSelectedContact()
    {
        if (!CanNavigateContacts())
            return;

        FindReferences();

        PhoneContact contact = GetSelectedContact();

        if (contact == null || phoneController == null)
            return;

        string scenario = GetActiveScenarioId(contact.ContactId);
        PhoneCallData call = contact.ResolveCall(scenario);

        PlayPhoneButtonClick();

        if (!CanCallContact(contact, scenario, call))
        {
            sequenceBusy = true;
            SetPhoneButtons(false, false, false);

            sequenceCoroutine =
                StartCoroutine(ShowUnavailableCallRoutine(contact));

            return;
        }

        if (!ValidateCallConfiguration(contact, scenario, call))
            return;

        currentContact = contact;
        currentScenarioId = scenario;
        currentCallData = call;
        currentCallChoiceResolved = false;

        sequenceBusy = true;
        outgoingCallActive = true;

        SetPhoneButtons(false, false, false);

        if (choiceController != null)
            choiceController.HideChoices(this);

        sequenceCoroutine =
            StartCoroutine(CallContactRoutine(contact));
    }

    private IEnumerator CallContactRoutine(PhoneContact contact)
    {
        yield return null;

        PlayDialTone();

        if (phoneController == null)
        {
            FinishForcedClose();
            yield break;
        }

        yield return phoneController.PlayManualCallEarAnimation();

        if (!phoneController.LastAnimationSucceeded)
        {
            FinishForcedClose();
            yield break;
        }

        float minimum = Mathf.Min(minimumAnswerDelay, maximumAnswerDelay);
        float maximum = Mathf.Max(minimumAnswerDelay, maximumAnswerDelay);
        float answerDelay = UnityEngine.Random.Range(minimum, maximum);

        yield return new WaitForSecondsRealtime(answerDelay);

        StopPhoneSfx();
        ConfigureContactVoice(contact);

        bool needsChoices = HasAvailableChoices(
            currentContact, currentScenarioId, currentCallData);

        yield return PlayOpeningDialogueRoutine();

        if (!phoneOpen)
            yield break;

        // При меню выбора последняя реплика должна существовать
        // как готовый ChoicePrompt.
        if (needsChoices &&
            (dialogueManager == null ||
             !dialogueManager.DialogueActive ||
             !dialogueManager.ChoicePromptReady))
        {
            Debug.LogError(
                "Телефон: не удалось подготовить последнюю реплику " +
                "перед вариантами ответа.", this);

            FinishForcedClose();
            yield break;
        }

        sequenceBusy = false;
        sequenceCoroutine = null;

        SetPhoneButtons(false, false, true);
        ShowCurrentChoices();
    }

    private void ShowCurrentChoices()
    {
        if (!phoneOpen || !outgoingCallActive ||
            currentContact == null ||
            currentCallData == null ||
            !currentCallData.ChoicesEnabled)
        {
            return;
        }

        FindReferences();

        if (choiceController == null)
            return;

        displayedChoices.Clear();

        for (int i = 0; i < currentCallData.ChoiceCount; i++)
        {
            PhoneChoiceData choice = currentCallData.GetChoice(i);

            if (!IsChoiceAvailable(currentContact, currentScenarioId, choice))
                continue;

            bool asked =
                choice.Role == PhoneChoiceRole.Question &&
                WasQuestionAsked(
                    currentContact.ContactId,
                    currentScenarioId,
                    choice.ChoiceId);

            displayedChoices.Add(
                new DialogueChoiceController.ChoiceOption
                {
                    Index = i,
                    Text = choice.ButtonText,
                    Interactable = IsChoiceSelectable(
                        currentContact, currentScenarioId, choice),
                    OverrideTextColor = asked,
                    TextColor = askedQuestionTextColor
                });
        }

        if (displayedChoices.Count == 0)
        {
            choiceController.HideChoices(this);
            return;
        }

        if (!choiceController.ShowOptions(
                this, displayedChoices, HandleDialogueChoice))
        {
            Debug.LogError(
                "Телефон: не удалось показать варианты ответа. " +
                "Проверь ссылки и кнопки страниц.", this);
        }
    }

    private void HandleDialogueChoice(int index)
    {
        if (!phoneOpen || sequenceBusy || currentCallData == null)
            return;

        PhoneChoiceData choice = currentCallData.GetChoice(index);

        if (!IsChoiceSelectable(currentContact, currentScenarioId, choice))
            return;

        sequenceBusy = true;
        SetPhoneButtons(false, false, false);

        sequenceCoroutine = StartCoroutine(PlayChoiceRoutine(choice));
    }

    private IEnumerator PlayChoiceRoutine(PhoneChoiceData choice)
    {
        yield return null;

        bool shouldEndCall = ShouldEndPhoneCall(choice);

        if (dialogueManager != null &&
            FindLastValidDialogueLineIndex(choice.ResponseDialogue) >= 0)
        {
            while (DialogueManager.AnyDialogueActive)
                yield return null;

            ownsPhoneDialogue = true;

            dialogueManager.StartDialogue(
                choice.ResponseDialogue,
                false,
                !shouldEndCall);

            while (dialogueManager != null &&
                   dialogueManager.DialogueActive)
            {
                yield return null;
            }

            if (shouldEndCall)
                HideOwnedPhoneDialogue();
        }

        string resolvedContactId =
            currentContact != null ? currentContact.ContactId : null;

        string resolvedScenarioId = currentScenarioId;
        string resolvedChoiceId = choice.ChoiceId;

        // Этот старый флаг означает наличие выбранного варианта.
        // История вопросов хранится отдельно.
        currentCallChoiceResolved = true;

        CommitPhoneChoice(choice);

        if (!isActiveAndEnabled || !phoneOpen)
            yield break;

        if (shouldEndCall)
        {
            yield return ClosePhoneRoutine(false);

            PhoneChoiceResolved?.Invoke(
                resolvedContactId,
                resolvedScenarioId,
                resolvedChoiceId);

            yield break;
        }

        PhoneChoiceResolved?.Invoke(
            resolvedContactId,
            resolvedScenarioId,
            resolvedChoiceId);

        if (!isActiveAndEnabled || !phoneOpen)
            yield break;

        sequenceBusy = false;
        sequenceCoroutine = null;

        SetPhoneButtons(false, false, true);
        ShowCurrentChoices();
    }

    private void HangUpPressed()
    {
        if (!phoneOpen || sequenceBusy)
            return;

        if (DialogueManager.AnyDialogueActive && !ownsPhoneDialogue)
            return;

        if (DialogueChoiceController.AnyChoiceOpen &&
            (choiceController == null ||
             !choiceController.IsOwnedBy(this)))
        {
            return;
        }

        PlayPhoneButtonClick();
        sequenceBusy = true;

        if (choiceController != null)
            choiceController.HideChoices(this);

        SetPhoneButtons(false, false, false);

        sequenceCoroutine =
            StartCoroutine(ClosePhoneRoutine(true));
    }

    private IEnumerator ClosePhoneRoutine(bool notifyCancellation)
    {
        yield return null;

        string cancelledContactId =
            currentContact != null ? currentContact.ContactId : null;

        string cancelledScenarioId = currentScenarioId;
        bool hadActiveCall = outgoingCallActive;
        bool choiceWasResolved = currentCallChoiceResolved;

        HideOwnedPhoneDialogue();
        StopPhoneSfx();

        if (choiceController != null)
            choiceController.HideChoices(this);

        RestoreDialogueVoice();

        if (phoneCanvas != null)
            phoneCanvas.SetActive(false);

        outgoingCallActive = false;

        if (phoneController != null)
        {
            phoneController.DetachPhoneForManualUse();

            yield return phoneController.PlayManualPutAnimation();

            if (!phoneController.LastAnimationSucceeded)
                phoneController.AbortPhoneMotion();
        }

        phoneOpen = false;
        sequenceBusy = false;
        AnyManualPhoneOpen = false;

        currentContact = null;
        currentCallData = null;
        currentScenarioId = null;
        currentCallChoiceResolved = false;
        sequenceCoroutine = null;

        if (notifyCancellation && hadActiveCall && !choiceWasResolved)
        {
            PhoneCallCancelled?.Invoke(
                cancelledContactId, cancelledScenarioId);
        }

        if (hadActiveCall)
        {
            PhoneCallEnded?.Invoke(
                cancelledContactId, cancelledScenarioId);
        }
    }

    private void FinishForcedClose()
    {
        StopPhoneSfx();

        if (choiceController != null)
            choiceController.HideChoices(this);

        HideOwnedPhoneDialogue();
        RestoreDialogueVoice();

        if (phoneController != null)
            phoneController.AbortPhoneMotion();

        HidePhoneUIImmediately();

        phoneOpen = false;
        sequenceBusy = false;
        outgoingCallActive = false;

        currentContact = null;
        currentCallData = null;
        currentScenarioId = null;
        currentCallChoiceResolved = false;

        AnyManualPhoneOpen = false;
        sequenceCoroutine = null;
    }

    private void HideOwnedPhoneDialogue()
    {
        if (!ownsPhoneDialogue)
            return;

        if (dialogueManager != null)
            dialogueManager.HidePersistentDialogue();

        ownsPhoneDialogue = false;
    }

    // =====================================================
    // ДОСТУПНОСТЬ И ИСТОРИЯ ВОПРОСОВ
    // =====================================================

    private static (string, string, string) QuestionKey(
        string contactId, string scenarioId, string choiceId)
    {
        return (
            contactId ?? "",
            scenarioId ?? "",
            choiceId ?? "");
    }

    public bool WasQuestionAsked(
        string contactId, string scenarioId, string choiceId)
    {
        return askedQuestions.Contains(
            QuestionKey(contactId, scenarioId, choiceId));
    }

    public void SetQuestionAvailable(
        string contactId,
        string scenarioId,
        string choiceId,
        bool available)
    {
        if (string.IsNullOrWhiteSpace(contactId) ||
            string.IsNullOrWhiteSpace(choiceId))
        {
            return;
        }

        questionAvailability[
            QuestionKey(contactId, scenarioId, choiceId)] = available;

        RefreshChoicesIfNeeded(contactId, scenarioId);
    }

    public void ResetAskedQuestion(
        string contactId, string scenarioId, string choiceId)
    {
        askedQuestions.Remove(
            QuestionKey(contactId, scenarioId, choiceId));

        RefreshChoicesIfNeeded(contactId, scenarioId);
    }

    private void RefreshChoicesIfNeeded(
        string contactId, string scenarioId)
    {
        if (phoneOpen && outgoingCallActive && !sequenceBusy &&
            currentContact != null &&
            currentContact.ContactId == contactId &&
            (currentScenarioId ?? "") == (scenarioId ?? ""))
        {
            ShowCurrentChoices();
        }
    }

    private bool IsChoiceAvailable(
        PhoneContact contact,
        string scenarioId,
        PhoneChoiceData choice)
    {
        if (contact == null || choice == null || !choice.HasButton)
            return false;

        var key = QuestionKey(
            contact.ContactId, scenarioId, choice.ChoiceId);

        if (questionAvailability.TryGetValue(key, out bool available))
            return available;

        return choice.InitiallyAvailable;
    }

    private bool IsChoiceSelectable(
        PhoneContact contact,
        string scenarioId,
        PhoneChoiceData choice)
    {
        if (!IsChoiceAvailable(contact, scenarioId, choice))
            return false;

        if (choice.Role != PhoneChoiceRole.Question)
            return true;

        if (string.IsNullOrWhiteSpace(choice.ChoiceId))
            return false;

        return choice.AllowRepeatQuestion ||
            !WasQuestionAsked(
                contact.ContactId, scenarioId, choice.ChoiceId);
    }

    private bool HasAvailableChoices(
        PhoneContact contact,
        string scenarioId,
        PhoneCallData call)
    {
        if (call == null || !call.ChoicesEnabled)
            return false;

        for (int i = 0; i < call.ChoiceCount; i++)
        {
            if (IsChoiceAvailable(contact, scenarioId, call.GetChoice(i)))
                return true;
        }

        return false;
    }

    private bool CanCallContact(
        PhoneContact contact,
        string scenarioId,
        PhoneCallData call)
    {
        if (call == null || !call.ChoicesEnabled)
            return true;

        for (int i = 0; i < call.ChoiceCount; i++)
        {
            PhoneChoiceData choice = call.GetChoice(i);

            if (choice == null || choice.Role == PhoneChoiceRole.EndCall)
                continue;

            if (IsChoiceSelectable(contact, scenarioId, choice))
                return true;
        }

        return false;
    }

    private bool ValidateCallConfiguration(
        PhoneContact contact,
        string scenarioId,
        PhoneCallData call)
    {
        if (call == null || !call.ChoicesEnabled)
            return true;

        int visibleCount = 0;

        for (int i = 0; i < call.ChoiceCount; i++)
        {
            PhoneChoiceData choice = call.GetChoice(i);

            if (!IsChoiceAvailable(contact, scenarioId, choice))
                continue;

            visibleCount++;

            if (choice.Role == PhoneChoiceRole.Question &&
                string.IsNullOrWhiteSpace(choice.ChoiceId))
            {
                Debug.LogError(
                    "Телефон: у запоминаемого вопроса не заполнен Choice Id.",
                    this);
                return false;
            }
        }

        if (visibleCount == 0)
            return true;

        if (dialogueManager == null ||
            FindLastValidDialogueLineIndex(call.OpeningDialogue) < 0)
        {
            Debug.LogError(
                "Телефон: для разговора с вариантами нужен DialogueManager " +
                "и хотя бы одна реплика в Opening Dialogue.", this);
            return false;
        }

        if (choiceController == null ||
            !choiceController.CanDisplayOptionCount(visibleCount))
        {
            Debug.LogError(
                "Телефон: проверь DialogueChoiceController, " +
                "его тексты, кнопки и кнопки страниц.", this);
            return false;
        }

        return true;
    }

    private IEnumerator ShowUnavailableCallRoutine(PhoneContact contact)
    {
        yield return null;

        while (DialogueManager.AnyDialogueActive)
            yield return null;

        List<DialogueManager.DialogueLine> lines =
            contact.NoAvailableQuestionsDialogue;

        if (dialogueManager != null &&
            FindLastValidDialogueLineIndex(lines) >= 0)
        {
            ownsPhoneDialogue = true;
            dialogueManager.StartDialogue(lines, false);

            while (dialogueManager != null &&
                   dialogueManager.DialogueActive)
            {
                yield return null;
            }

            HideOwnedPhoneDialogue();
        }

        sequenceBusy = false;
        sequenceCoroutine = null;

        if (phoneOpen)
            SetPhoneButtons(true, true, true);
    }

    private bool ShouldEndPhoneCall(PhoneChoiceData choice)
    {
        return choice.HangUpAfterResponse ||
            choice.Role == PhoneChoiceRole.EndCall;
    }

    private void CommitPhoneChoice(PhoneChoiceData choice)
    {
        if (currentContact == null || choice == null)
            return;

        string contactId = currentContact.ContactId;
        string scenarioId = currentScenarioId;

        if (choice.Role == PhoneChoiceRole.Question &&
            !string.IsNullOrWhiteSpace(choice.ChoiceId))
        {
            askedQuestions.Add(
                QuestionKey(contactId, scenarioId, choice.ChoiceId));
        }

        PhoneChoiceCommitted?.Invoke(
            contactId, scenarioId, choice.ChoiceId);
    }

    // =====================================================
    // ОСОБЫЕ СЦЕНАРИИ
    // =====================================================

    public bool SetContactScenario(string contactId, string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(contactId) ||
            string.IsNullOrWhiteSpace(scenarioId))
        {
            return false;
        }

        PhoneContact contact = FindContact(contactId);

        if (contact == null)
            return false;

        activeScenarios[contactId] = scenarioId;
        return true;
    }

    public void ClearContactScenario(string contactId)
    {
        if (string.IsNullOrWhiteSpace(contactId))
            return;

        activeScenarios.Remove(contactId);
    }

    private string GetActiveScenarioId(string contactId)
    {
        if (string.IsNullOrWhiteSpace(contactId))
            return null;

        if (activeScenarios.TryGetValue(contactId, out string scenarioId))
            return scenarioId;

        return null;
    }

    private PhoneContact FindContact(string contactId)
    {
        if (contacts == null)
            return null;

        for (int i = 0; i < contacts.Count; i++)
        {
            PhoneContact contact = contacts[i];

            if (contact != null && contact.ContactId == contactId)
                return contact;
        }

        return null;
    }

    public bool HasScenarioChoice(
        string contactId, string scenarioId, string choiceId)
    {
        PhoneContact contact = FindContact(contactId);

        if (contact == null ||
            !contact.TryGetScenarioCall(scenarioId, out PhoneCallData call) ||
            call == null ||
            !call.ChoicesEnabled)
        {
            return false;
        }

        for (int i = 0; i < call.ChoiceCount; i++)
        {
            PhoneChoiceData choice = call.GetChoice(i);

            if (choice != null &&
                choice.HasButton &&
                choice.ChoiceId == choiceId &&
                choice.Role != PhoneChoiceRole.EndCall)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryBeginScenarioOverride(
        UnityEngine.Object owner,
        string contactId,
        string scenarioId)
    {
        if (owner == null || scenarioOverrideOwner != null ||
            phoneOpen || sequenceBusy)
        {
            return false;
        }

        PhoneContact contact = FindContact(contactId);

        if (contact == null || !contact.HasScenario(scenarioId))
            return false;

        string previous = GetActiveScenarioId(contactId);

        if (!SetContactScenario(contactId, scenarioId))
            return false;

        scenarioOverrideOwner = owner;
        overriddenContactId = contactId;
        overriddenScenarioId = scenarioId;
        previousScenarioId = previous;

        return true;
    }

    public void EndScenarioOverride(UnityEngine.Object owner)
    {
        if (owner == null || scenarioOverrideOwner != owner)
            return;

        if (GetActiveScenarioId(overriddenContactId) ==
            overriddenScenarioId)
        {
            if (string.IsNullOrEmpty(previousScenarioId))
                activeScenarios.Remove(overriddenContactId);
            else
                activeScenarios[overriddenContactId] = previousScenarioId;
        }

        scenarioOverrideOwner = null;
        overriddenContactId = null;
        overriddenScenarioId = null;
        previousScenarioId = null;
    }

    // =====================================================
    // ГОЛОС
    // =====================================================

    private void ConfigureContactVoice(PhoneContact contact)
    {
        if (dialogueManager == null || contactVoiceAudioSource == null)
            return;

        if (!voiceOverrideActive)
        {
            previousDefaultVoiceSource =
                dialogueManager.defaultVoiceAudioSource;

            voiceOverrideActive = true;
        }

        dialogueManager.ResetVoiceAudioSource(contactVoiceAudioSource);

        contactVoiceAudioSource.clip =
            contact != null ? contact.VoiceClip : null;

        dialogueManager.defaultVoiceAudioSource = contactVoiceAudioSource;
    }

    private void RestoreDialogueVoice()
    {
        if (!voiceOverrideActive)
            return;

        if (dialogueManager != null)
        {
            dialogueManager.ResetVoiceAudioSource(contactVoiceAudioSource);
            dialogueManager.defaultVoiceAudioSource =
                previousDefaultVoiceSource;
        }

        previousDefaultVoiceSource = null;
        voiceOverrideActive = false;
    }

    // =====================================================
    // ЗВУКИ
    // =====================================================

    private void PlayPowerOnSound()
    {
        if (phoneSfxAudioSource == null || phonePowerOnClip == null)
            return;

        phoneSfxAudioSource.Stop();
        phoneSfxAudioSource.loop = false;
        phoneSfxAudioSource.PlayOneShot(phonePowerOnClip);
    }

    private void PlayDialTone()
    {
        if (phoneSfxAudioSource == null || dialToneClip == null)
            return;

        phoneSfxAudioSource.Stop();
        phoneSfxAudioSource.clip = dialToneClip;
        phoneSfxAudioSource.loop = true;
        phoneSfxAudioSource.Play();
    }

    private void StopPhoneSfx()
    {
        if (phoneSfxAudioSource == null)
            return;

        phoneSfxAudioSource.Stop();
        phoneSfxAudioSource.loop = false;
    }

    // =====================================================
    // КНОПКИ
    // =====================================================

    private void AddButtonListeners()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(PreviousContact);
            leftButton.onClick.AddListener(PreviousContact);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(NextContact);
            rightButton.onClick.AddListener(NextContact);
        }

        if (callButton != null)
        {
            callButton.onClick.RemoveListener(CallSelectedContact);
            callButton.onClick.AddListener(CallSelectedContact);
        }

        if (hangUpButton != null)
        {
            hangUpButton.onClick.RemoveListener(HangUpPressed);
            hangUpButton.onClick.AddListener(HangUpPressed);
        }
    }

    private void RemoveButtonListeners()
    {
        if (leftButton != null)
            leftButton.onClick.RemoveListener(PreviousContact);

        if (rightButton != null)
            rightButton.onClick.RemoveListener(NextContact);

        if (callButton != null)
            callButton.onClick.RemoveListener(CallSelectedContact);

        if (hangUpButton != null)
            hangUpButton.onClick.RemoveListener(HangUpPressed);
    }

    private void SetPhoneButtons(bool navigation, bool call, bool hangUp)
    {
        if (leftButton != null)
            leftButton.interactable = navigation;

        if (rightButton != null)
            rightButton.interactable = navigation;

        if (callButton != null)
            callButton.interactable = call;

        if (hangUpButton != null)
            hangUpButton.interactable = hangUp;
    }

    private void HidePhoneUIImmediately()
    {
        if (phoneCanvas != null)
            phoneCanvas.SetActive(false);

        if (contacts != null)
        {
            for (int i = 0; i < contacts.Count; i++)
            {
                PhoneContact contact = contacts[i];

                if (contact != null && contact.ScreenObject != null)
                    contact.ScreenObject.SetActive(false);
            }
        }

        if (choiceController != null)
            choiceController.HideChoices(this);
    }

    private void FindReferences()
    {
        if (phoneController == null)
            phoneController = GetComponent<WorkPhonePenaltyController>();

        if (dialogueManager == null)
        {
            GameObject dialogueObject = GameObject.Find("DialogueManager");

            if (dialogueObject != null)
                dialogueManager = dialogueObject.GetComponent<DialogueManager>();
        }

        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>(
                FindObjectsInactive.Include);
        }

        if (choiceController == null)
        {
            choiceController = FindFirstObjectByType<DialogueChoiceController>(
                FindObjectsInactive.Include);
        }
    }

    private void OnValidate()
    {
        minimumAnswerDelay = Mathf.Max(0f, minimumAnswerDelay);
        maximumAnswerDelay = Mathf.Max(
            minimumAnswerDelay, maximumAnswerDelay);
    }
}
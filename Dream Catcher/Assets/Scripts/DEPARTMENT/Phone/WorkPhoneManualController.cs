using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkPhoneManualController :
    MonoBehaviour
{
    // =====================================================
    // ВАРИАНТ ОТВЕТА ВНУТРИ ТЕЛЕФОННОГО РАЗГОВОРА
    // =====================================================

    [Serializable]
    public class PhoneChoiceData
    {
        [Header("Выбор")]

        [Tooltip(
            "Технический ID ответа. " +
            "Например check_gray_coat или goodbye."
        )]
        [SerializeField]
        private string choiceId;


        [Tooltip(
            "Текст, который появится " +
            "на глобальной вариативной плашке."
        )]
        [SerializeField]
        private string buttonText;


        [Header("Диалог после выбора")]

        [Tooltip(
            "Реплики, которые проиграются " +
            "после выбора этой плашки."
        )]
        [SerializeField]
        private List<DialogueManager.DialogueLine>
            responseDialogue =
                new List<
                    DialogueManager.DialogueLine>();


        [Header("После ответа")]

        [Tooltip(
            "Если включено — после окончания " +
            "Response Dialogue телефон " +
            "автоматически кладётся обратно."
        )]
        [SerializeField]
        private bool hangUpAfterResponse =
            true;


        public string ChoiceId =>
            choiceId;

        public string ButtonText =>
            buttonText;

        public List<DialogueManager.DialogueLine>
            ResponseDialogue =>
                responseDialogue;

        public bool HangUpAfterResponse =>
            hangUpAfterResponse;

        public bool HasButton =>
            !string.IsNullOrWhiteSpace(
                buttonText
            );
    }


    // =====================================================
    // ОДИН ВАРИАНТ ТЕЛЕФОННОГО РАЗГОВОРА
    // =====================================================

    [Serializable]
    public class PhoneCallData
    {
        [Header("Первый диалог")]

        [Tooltip(
            "Разговор сразу после того, " +
            "как контакт взял трубку."
        )]
        [SerializeField]
        private List<DialogueManager.DialogueLine>
            openingDialogue =
                new List<
                    DialogueManager.DialogueLine>();


        [Header("Вариативный диалог")]

        [Tooltip(
            "Показывать ли после Opening Dialogue " +
            "две глобальные плашки выбора."
        )]
        [SerializeField]
        private bool useChoices;


        [Tooltip(
            "Первая вариативная плашка."
        )]
        [SerializeField]
        private PhoneChoiceData firstChoice =
            new PhoneChoiceData();


        [Tooltip(
            "Вторая вариативная плашка."
        )]
        [SerializeField]
        private PhoneChoiceData secondChoice =
            new PhoneChoiceData();


        public List<DialogueManager.DialogueLine>
            OpeningDialogue =>
                openingDialogue;


        public PhoneChoiceData FirstChoice =>
            firstChoice;


        public PhoneChoiceData SecondChoice =>
            secondChoice;


        public bool HasChoices
        {
            get
            {
                if (!useChoices)
                    return false;

                bool firstExists =
                    firstChoice != null &&
                    firstChoice.HasButton;

                bool secondExists =
                    secondChoice != null &&
                    secondChoice.HasButton;

                return
                    firstExists ||
                    secondExists;
            }
        }


        public PhoneChoiceData GetChoice(
            int index)
        {
            if (!useChoices)
                return null;

            if (index == 0)
                return firstChoice;

            if (index == 1)
                return secondChoice;

            return null;
        }
    }


    // =====================================================
    // ОСОБЫЙ СЦЕНАРИЙ КОНТАКТА
    // =====================================================

    [Serializable]
    public class PhoneCallScenario
    {
        [Tooltip(
            "Технический ID особой ситуации. " +
            "Например woman_gray_coat."
        )]
        [SerializeField]
        private string scenarioId;


        [SerializeField]
        private PhoneCallData callData =
            new PhoneCallData();


        public string ScenarioId =>
            scenarioId;


        public PhoneCallData CallData =>
            callData;
    }


    // =====================================================
    // КОНТАКТ
    // =====================================================

    [Serializable]
    public class PhoneContact
    {
        [Header("Контакт")]

        [Tooltip(
            "Постоянный технический ID. " +
            "Например boss или security."
        )]
        [SerializeField]
        private string contactId;


        [Tooltip(
            "Объект имени контакта " +
            "на экране телефона."
        )]
        [SerializeField]
        private GameObject screenObject;


        [Header("Голос")]

        [Tooltip(
            "Зацикленная голосовая дорожка " +
            "этого контакта."
        )]
        [SerializeField]
        private AudioClip voiceClip;


        [Header("Обычный звонок")]

        [Tooltip(
            "Разговор, который используется, " +
            "если для контакта сейчас " +
            "не активирован особый сценарий."
        )]
        [SerializeField]
        private PhoneCallData defaultCall =
            new PhoneCallData();


        [Header("Особые ситуации")]

        [Tooltip(
            "Сюжетные варианты разговора " +
            "для этого же контакта."
        )]
        [SerializeField]
        private List<PhoneCallScenario>
            scenarios =
                new List<PhoneCallScenario>();


        public string ContactId =>
            contactId;


        public GameObject ScreenObject =>
            screenObject;


        public AudioClip VoiceClip =>
            voiceClip;


        public PhoneCallData ResolveCall(
            string activeScenarioId)
        {
            if (!string.IsNullOrEmpty(
                    activeScenarioId) &&
                scenarios != null)
            {
                for (int i = 0;
                     i < scenarios.Count;
                     i++)
                {
                    PhoneCallScenario scenario =
                        scenarios[i];

                    if (scenario == null)
                        continue;


                    if (scenario.ScenarioId ==
                        activeScenarioId)
                    {
                        return
                            scenario.CallData;
                    }
                }
            }


            return defaultCall;
        }
    }


    // =====================================================
    // ОСНОВНОЙ ТЕЛЕФОН
    // =====================================================

    [Header("Основной телефон")]

    [Tooltip(
        "Существующий WorkPhonePenaltyController " +
        "на этом же телефоне."
    )]
    [SerializeField]
    private WorkPhonePenaltyController
        phoneController;


    // =====================================================
    // PHONE CANVAS
    // =====================================================

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


    // =====================================================
    // ГЛОБАЛЬНЫЙ ВАРИАТИВНЫЙ ДИАЛОГ
    // =====================================================

    [Header("Глобальный вариативный диалог")]

    [Tooltip(
        "Глобальный DialogueChoiceController. " +
        "Если пусто — найдётся автоматически."
    )]
    [SerializeField]
    private DialogueChoiceController
        choiceController;


    // =====================================================
    // DIALOGUE MANAGER
    // =====================================================

    [Header("Dialogue Manager")]

    [Tooltip(
        "Глобальный DialogueManager. " +
        "Если пусто — найдётся автоматически."
    )]
    [SerializeField]
    private DialogueManager dialogueManager;


    // =====================================================
    // PHONE SFX
    // =====================================================

    [Header("Звук телефона")]

    [Tooltip(
        "Отдельный AudioSource " +
        "для включения телефона и гудков."
    )]
    [SerializeField]
    private AudioSource phoneSfxAudioSource;


    [Tooltip(
        "Звук включения телефона " +
        "после TakePhone."
    )]
    [SerializeField]
    private AudioClip phonePowerOnClip;


    [Tooltip(
        "Зацикленный гудок исходящего вызова."
    )]
    [SerializeField]
    private AudioClip dialToneClip;


    [Tooltip(
        "Минимальное время до ответа."
    )]
    [SerializeField]
    private float minimumAnswerDelay =
        1.2f;


    [Tooltip(
        "Максимальное время до ответа."
    )]
    [SerializeField]
    private float maximumAnswerDelay =
        3.8f;

    [Header("Звук кнопок телефона")]

    [Tooltip(
    "AudioSource только для звука " +
    "нажатия физических кнопок телефона."
    )]
    [SerializeField]
    private AudioSource phoneButtonAudioSource;


    [Tooltip(
        "Звук нажатия кнопки телефона."
    )]
    [SerializeField]
    private AudioClip phoneButtonClickClip;

    // =====================================================
    // VOICE
    // =====================================================

    [Header("Голос контактов")]

    [Tooltip(
        "Один AudioSource для голосов " +
        "всех телефонных контактов."
    )]
    [SerializeField]
    private AudioSource
        contactVoiceAudioSource;


    // =====================================================
    // CONTACTS
    // =====================================================

    [Header("Контакты")]

    [SerializeField]
    private List<PhoneContact> contacts =
        new List<PhoneContact>();


    // =====================================================
    // EVENTS
    // =====================================================

    /*
     * contactId
     * scenarioId
     * choiceId
     *
     * Например:
     * security / woman_gray_coat / check_gray_coat
     *
     * Будущая сюжетная система женщины
     * сможет подписаться на это событие.
     */
    public event Action<
        string,
        string,
        string>
            PhoneChoiceResolved;


    /*
     * Вызывается, если игрок уже
     * установил звонок, но просто
     * положил трубку без выбора.
     *
     * contactId
     * scenarioId
     */
    public event Action<
        string,
        string>
            PhoneCallCancelled;


    // =====================================================
    // GLOBAL STATE
    // =====================================================

    public static bool AnyManualPhoneOpen
    {
        get;
        private set;
    }


    // =====================================================
    // PUBLIC STATE
    // =====================================================

    public bool IsPhoneOpen =>
        phoneOpen;


    /*
     * 3D Collider телефона нужен
     * только когда телефон лежит
     * на столе и его можно взять.
     *
     * Когда телефон уже в руке,
     * используются UI-кнопки,
     * поэтому Collider больше
     * не нужен как Interactable.
     */
    public bool WantsPhoneColliderInteractable =>
        CanStartManualUse;


    public bool CanStartManualUse
    {
        get
        {
            if (phoneOpen ||
                sequenceBusy)
            {
                return false;
            }


            // Ручной телефон доступен
            // только во время работы
            // за столом.
            if (WorkSessionManager.Instance ==
                    null ||
                !WorkSessionManager.Instance
                    .IsSeated)
            {
                return false;
            }


            if (DialogueManager
                .AnyDialogueActive)
            {
                return false;
            }


            if (DialogueChoiceController
                .AnyChoiceOpen)
            {
                return false;
            }


            // Старую систему двух
            // вопросов клиента не ломаем.
            if (ClientQuestionDialogueController
                .AnyQuestionDialogueOpen)
            {
                return false;
            }


            // Входящий штрафной звонок
            // всегда имеет приоритет.
            if (phoneController != null &&
                phoneController
                    .PenaltyCallPendingOrActive)
            {
                return false;
            }


            return true;
        }
    }


    // =====================================================
    // RUNTIME
    // =====================================================

    private int selectedContactIndex;


    private bool phoneOpen;
    private bool sequenceBusy;
    private bool outgoingCallActive;


    private Coroutine sequenceCoroutine;


    private PhoneContact currentContact;

    private PhoneCallData currentCallData;

    private string currentScenarioId;


    private bool currentCallChoiceResolved;


    private AudioSource
        previousDefaultVoiceSource;

    private bool voiceOverrideActive;


    /*
     * contactId -> scenarioId
     *
     * Например:
     *
     * security -> woman_gray_coat
     */
    private readonly Dictionary<
        string,
        string>
            activeScenarios =
                new Dictionary<
                    string,
                    string>();

    private readonly List<
        DialogueManager.DialogueLine>
            choiceOpeningPrefix =
                new List<
                    DialogueManager.DialogueLine>();

    // =====================================================
    // UNITY
    // =====================================================

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
            StopCoroutine(
                sequenceCoroutine
            );

            sequenceCoroutine = null;
        }


        StopPhoneSfx();


        if (choiceController != null)
        {
            choiceController
                .HideChoices(this);
        }


        RestoreDialogueVoice();

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


    // =====================================================
    // ОТКРЫТИЕ ТЕЛЕФОНА
    // =====================================================

    public bool TryOpenPhone()
    {
        FindReferences();


        if (!CanStartManualUse)
            return false;


        if (phoneController == null)
            return false;


        if (sequenceCoroutine != null)
            return false;


        phoneOpen = true;

        sequenceBusy = true;

        AnyManualPhoneOpen = true;


        selectedContactIndex = 0;


        currentContact = null;

        currentCallData = null;

        currentScenarioId = null;

        currentCallChoiceResolved = false;


        SetPhoneButtons(
            false,
            false,
            false
        );


        sequenceCoroutine =
            StartCoroutine(
                OpenPhoneRoutine()
            );


        return true;
    }

    private void PlayPhoneButtonClick()
    {
        if (phoneButtonAudioSource == null ||
            phoneButtonClickClip == null)
        {
            return;
        }


        phoneButtonAudioSource.PlayOneShot(
            phoneButtonClickClip
        );
    }

    private IEnumerator OpenPhoneRoutine()
    {
        FindReferences();


        if (phoneController == null)
        {
            FinishForcedClose();

            yield break;
        }


        // -------------------------------------------------
        // TAKE PHONE
        // -------------------------------------------------

        yield return
            phoneController
                .PlayManualTakeAnimation();


        // -------------------------------------------------
        // CAMERA HOLD
        // -------------------------------------------------

        /*
         * Используется тот же
         * PhoneHoldAnchor, что и
         * у входящего звонка босса.
         *
         * Конечная мировая поза
         * TakePhone сохраняется.
         */
        phoneController
            .AttachPhoneForManualUse();


        // -------------------------------------------------
        // PHONE UI
        // -------------------------------------------------

        ShowSelectedContact();


        if (phoneCanvas != null)
        {
            phoneCanvas.SetActive(true);
        }


        // Звук включения появляется
        // только ПОСЛЕ TakePhone.
        PlayPowerOnSound();


        sequenceBusy = false;


        SetPhoneButtons(
            true,
            true,
            true
        );


        sequenceCoroutine = null;
    }

    private IEnumerator PlayOpeningDialogueRoutine()
    {
        if (currentCallData == null ||
            currentCallData.OpeningDialogue ==
                null ||
            currentCallData.OpeningDialogue.Count ==
                0 ||
            dialogueManager == null)
        {
            yield break;
        }


        List<DialogueManager.DialogueLine>
            lines =
                currentCallData
                    .OpeningDialogue;


        bool needsChoices =
            currentCallData.HasChoices &&
            choiceController != null;


        // =====================================================
        // ОБЫЧНЫЙ ДИАЛОГ БЕЗ ВАРИАНТОВ
        // =====================================================

        if (!needsChoices)
        {
            while (DialogueManager
                .AnyDialogueActive)
            {
                yield return null;
            }


            dialogueManager.StartDialogue(
                lines,
                false
            );


            while (dialogueManager != null &&
                   dialogueManager.DialogueActive)
            {
                yield return null;
            }


            yield break;
        }


        // =====================================================
        // ДИАЛОГ С ДВУМЯ ВАРИАНТАМИ
        // =====================================================

        int finalIndex =
            FindLastValidDialogueLineIndex(
                lines
            );


        if (finalIndex < 0)
            yield break;


        choiceOpeningPrefix.Clear();


        /*
         * Все реплики ДО последней
         * проигрываем как обычный диалог.
         */
        for (int i = 0;
             i < finalIndex;
             i++)
        {
            if (lines[i] != null)
            {
                choiceOpeningPrefix.Add(
                    lines[i]
                );
            }
        }


        if (choiceOpeningPrefix.Count > 0)
        {
            while (DialogueManager
                .AnyDialogueActive)
            {
                yield return null;
            }


            /*
             * true:
             * после окончания этого списка
             * DialoguePanel остаётся видимой.
             */
            dialogueManager.StartDialogue(
                choiceOpeningPrefix,
                false,
                true
            );


            while (dialogueManager != null &&
                   dialogueManager.DialogueActive)
            {
                yield return null;
            }
        }


        DialogueManager.DialogueLine
            finalPrompt =
                lines[finalIndex];


        while (DialogueManager
            .AnyDialogueActive)
        {
            yield return null;
        }


        /*
         * ПОСЛЕДНЯЯ реплика становится
         * настоящим ChoicePrompt.
         *
         * Она:
         * - печатается полностью;
         * - остаётся на DialoguePanel;
         * - не реагирует на LMB;
         * - не реагирует на Space.
         */
        dialogueManager.ShowChoicePrompt(
            finalPrompt,
            false
        );


        while (dialogueManager != null &&
               dialogueManager.DialogueActive &&
               !dialogueManager
                   .ChoicePromptReady)
        {
            yield return null;
        }
    }


    private int FindLastValidDialogueLineIndex(
        List<DialogueManager.DialogueLine>
            lines)
    {
        if (lines == null)
            return -1;


        for (int i = lines.Count - 1;
             i >= 0;
             i--)
        {
            if (lines[i] != null)
            {
                return i;
            }
        }


        return -1;
    }

    // =====================================================
    // CONTACT NAVIGATION
    // =====================================================

    private void PreviousContact()
    {
        if (!CanNavigateContacts())
            return;


        if (contacts == null ||
            contacts.Count == 0)
        {
            return;
        }

        PlayPhoneButtonClick();
        selectedContactIndex--;


        if (selectedContactIndex < 0)
        {
            selectedContactIndex =
                contacts.Count - 1;
        }


        ShowSelectedContact();
    }


    private void NextContact()
    {
        if (!CanNavigateContacts())
            return;


        if (contacts == null ||
            contacts.Count == 0)
        {
            return;
        }

        PlayPhoneButtonClick();
        selectedContactIndex++;


        if (selectedContactIndex >=
            contacts.Count)
        {
            selectedContactIndex = 0;
        }


        ShowSelectedContact();
    }


    private bool CanNavigateContacts()
    {
        return
            phoneOpen &&
            !sequenceBusy &&
            !outgoingCallActive;
    }


    private void ShowSelectedContact()
    {
        if (contacts == null)
            return;


        for (int i = 0;
             i < contacts.Count;
             i++)
        {
            PhoneContact contact =
                contacts[i];


            if (contact == null ||
                contact.ScreenObject == null)
            {
                continue;
            }


            contact.ScreenObject
                .SetActive(
                    i ==
                    selectedContactIndex
                );
        }
    }


    private PhoneContact
        GetSelectedContact()
    {
        if (contacts == null ||
            contacts.Count == 0)
        {
            return null;
        }


        if (selectedContactIndex < 0 ||
            selectedContactIndex >=
                contacts.Count)
        {
            selectedContactIndex = 0;
        }


        return contacts[
            selectedContactIndex
        ];
    }


    // =====================================================
    // OUTGOING CALL
    // =====================================================

    private void CallSelectedContact()
    {
        if (!phoneOpen ||
            sequenceBusy ||
            outgoingCallActive)
        {
            return;
        }


        FindReferences();


        PhoneContact contact =
            GetSelectedContact();


        if (contact == null)
            return;

        PlayPhoneButtonClick();
        currentContact =
            contact;


        currentScenarioId =
            GetActiveScenarioId(
                contact.ContactId
            );


        currentCallData =
            contact.ResolveCall(
                currentScenarioId
            );


        currentCallChoiceResolved =
            false;


        sequenceBusy = true;

        outgoingCallActive = true;


        SetPhoneButtons(
            false,
            false,
            false
        );


        if (choiceController != null)
        {
            choiceController
                .HideChoices(this);
        }


        sequenceCoroutine =
            StartCoroutine(
                CallContactRoutine(
                    contact
                )
            );
    }


    private IEnumerator CallContactRoutine(
        PhoneContact contact)
    {
        // -------------------------------------------------
        // ГУДОК
        // -------------------------------------------------

        PlayDialTone();


        float minimum =
            Mathf.Min(
                minimumAnswerDelay,
                maximumAnswerDelay
            );


        float maximum =
            Mathf.Max(
                minimumAnswerDelay,
                maximumAnswerDelay
            );


        float answerDelay =
            UnityEngine.Random.Range(
                minimum,
                maximum
            );


        yield return
            new WaitForSecondsRealtime(
                answerDelay
            );


        // Человек ответил:
        // гудок обрывается резко.
        StopPhoneSfx();


        // -------------------------------------------------
        // VOICE
        // -------------------------------------------------

        ConfigureContactVoice(
            contact
        );


        // -------------------------------------------------
        // OPENING DIALOGUE
        // -------------------------------------------------

        if (currentCallData != null &&
            currentCallData
                .OpeningDialogue != null &&
            currentCallData
                .OpeningDialogue.Count > 0 &&
            dialogueManager != null)
        {
            while (DialogueManager
                .AnyDialogueActive)
            {
                yield return null;
            }


            yield return StartCoroutine(
                PlayOpeningDialogueRoutine()
            );


            while (dialogueManager != null &&
                   dialogueManager
                       .DialogueActive)
            {
                yield return null;
            }
        }


        sequenceBusy = false;


        /*
         * После ответа уже нельзя
         * листать контакты и начинать
         * второй звонок.
         *
         * Красная трубка доступна.
         */
        SetPhoneButtons(
            false,
            false,
            true
        );


        // -------------------------------------------------
        // TWO CHOICES
        // -------------------------------------------------

        ShowCurrentChoices();


        sequenceCoroutine = null;
    }


    // =====================================================
    // GLOBAL TWO-CHOICE UI
    // =====================================================

    private void ShowCurrentChoices()
    {
        if (currentCallData == null ||
            !currentCallData.HasChoices)
        {
            return;
        }


        FindReferences();


        if (choiceController == null)
            return;


        PhoneChoiceData first =
            currentCallData
                .FirstChoice;


        PhoneChoiceData second =
            currentCallData
                .SecondChoice;


        string firstText =
            first != null
                ? first.ButtonText
                : "";


        string secondText =
            second != null
                ? second.ButtonText
                : "";


        choiceController.ShowChoices(
            this,
            firstText,
            secondText,
            HandleDialogueChoice
        );
    }


    private void HandleDialogueChoice(
        int index)
    {
        if (!phoneOpen ||
            sequenceBusy ||
            currentCallData == null)
        {
            return;
        }


        PhoneChoiceData choice =
            currentCallData
                .GetChoice(index);


        if (choice == null ||
            !choice.HasButton)
        {
            return;
        }


        sequenceBusy = true;


        SetPhoneButtons(
            false,
            false,
            false
        );


        sequenceCoroutine =
            StartCoroutine(
                PlayChoiceRoutine(
                    choice
                )
            );
    }


    private IEnumerator PlayChoiceRoutine(
        PhoneChoiceData choice)
    {
        // -------------------------------------------------
        // RESPONSE DIALOGUE
        // -------------------------------------------------

        if (choice.ResponseDialogue != null &&
            choice.ResponseDialogue.Count > 0 &&
            dialogueManager != null)
        {
            while (DialogueManager
                .AnyDialogueActive)
            {
                yield return null;
            }


            dialogueManager.StartDialogue(
                choice.ResponseDialogue,
                false
            );


            while (dialogueManager != null &&
                   dialogueManager
                       .DialogueActive)
            {
                yield return null;
            }
        }


        string resolvedContactId =
            currentContact != null
                ? currentContact.ContactId
                : null;


        string resolvedScenarioId =
            currentScenarioId;


        string resolvedChoiceId =
            choice.ChoiceId;


        currentCallChoiceResolved =
            true;


        // -------------------------------------------------
        // AUTO HANG UP
        // -------------------------------------------------

        if (choice.HangUpAfterResponse)
        {
            /*
             * Сначала полностью кладём телефон.
             *
             * И только ПОСЛЕ PutPhone
             * сюжет получает результат выбора.
             *
             * Для женщины это важно:
             * задержание начнётся уже после звонка.
             */
            yield return
                ClosePhoneRoutine(
                    false
                );


            PhoneChoiceResolved?.Invoke(
                resolvedContactId,
                resolvedScenarioId,
                resolvedChoiceId
            );


            sequenceCoroutine = null;

            yield break;
        }


        // -------------------------------------------------
        // CALL CONTINUES
        // -------------------------------------------------

        PhoneChoiceResolved?.Invoke(
            resolvedContactId,
            resolvedScenarioId,
            resolvedChoiceId
        );


        sequenceBusy = false;


        SetPhoneButtons(
            false,
            false,
            true
        );


        // Если разговор продолжается,
        // возвращаем те же две плашки.
        ShowCurrentChoices();


        sequenceCoroutine = null;
    }


    // =====================================================
    // RED PHONE BUTTON
    // =====================================================

    private void HangUpPressed()
    {
        if (!phoneOpen ||
            sequenceBusy)
        {
            return;
        }

        PlayPhoneButtonClick();
        sequenceBusy = true;


        if (choiceController != null)
        {
            choiceController
                .HideChoices(this);
        }


        SetPhoneButtons(
            false,
            false,
            false
        );


        sequenceCoroutine =
            StartCoroutine(
                ClosePhoneRoutine(
                    true
                )
            );
    }


    private IEnumerator ClosePhoneRoutine(
        bool notifyCancellation)
    {
        string cancelledContactId =
            currentContact != null
                ? currentContact.ContactId
                : null;


        string cancelledScenarioId =
            currentScenarioId;


        bool hadActiveCall =
            outgoingCallActive;


        bool choiceWasResolved =
            currentCallChoiceResolved;


        StopPhoneSfx();


        if (choiceController != null)
        {
            choiceController
                .HideChoices(this);
        }


        RestoreDialogueVoice();


        if (phoneCanvas != null)
        {
            phoneCanvas.SetActive(false);
        }


        outgoingCallActive = false;


        if (phoneController != null)
        {
            // Сначала возвращаем телефон
            // из Camera Hold
            // в исходную иерархию.
            phoneController
                .DetachPhoneForManualUse();


            // Затем Animator выполняет PutPhone.
            yield return
                phoneController
                    .PlayManualPutAnimation();
        }


        phoneOpen = false;

        sequenceBusy = false;

        AnyManualPhoneOpen = false;


        currentContact = null;

        currentCallData = null;

        currentScenarioId = null;

        currentCallChoiceResolved = false;


        sequenceCoroutine = null;


        /*
         * Игрок уже дозвонился,
         * но просто положил трубку
         * без вариативного решения.
         */
        if (notifyCancellation &&
            hadActiveCall &&
            !choiceWasResolved)
        {
            PhoneCallCancelled?.Invoke(
                cancelledContactId,
                cancelledScenarioId
            );
        }
    }


    private void FinishForcedClose()
    {
        StopPhoneSfx();


        if (choiceController != null)
        {
            choiceController
                .HideChoices(this);
        }


        RestoreDialogueVoice();


        if (phoneController != null)
        {
            phoneController
                .DetachPhoneForManualUse();
        }


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


    // =====================================================
    // ACTIVE SCENARIOS
    // =====================================================

    public bool SetContactScenario(
        string contactId,
        string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(
                contactId) ||
            string.IsNullOrWhiteSpace(
                scenarioId))
        {
            return false;
        }


        PhoneContact contact =
            FindContact(
                contactId
            );


        if (contact == null)
            return false;


        activeScenarios[
            contactId
        ] =
            scenarioId;


        return true;
    }


    public void ClearContactScenario(
        string contactId)
    {
        if (string.IsNullOrWhiteSpace(
                contactId))
        {
            return;
        }


        activeScenarios.Remove(
            contactId
        );
    }


    private string GetActiveScenarioId(
        string contactId)
    {
        if (string.IsNullOrWhiteSpace(
                contactId))
        {
            return null;
        }


        string scenarioId;


        if (activeScenarios.TryGetValue(
                contactId,
                out scenarioId))
        {
            return scenarioId;
        }


        return null;
    }


    private PhoneContact FindContact(
        string contactId)
    {
        if (contacts == null)
            return null;


        for (int i = 0;
             i < contacts.Count;
             i++)
        {
            PhoneContact contact =
                contacts[i];


            if (contact == null)
                continue;


            if (contact.ContactId ==
                contactId)
            {
                return contact;
            }
        }


        return null;
    }


    // =====================================================
    // CONTACT VOICE
    // =====================================================

    private void ConfigureContactVoice(
        PhoneContact contact)
    {
        if (dialogueManager == null ||
            contactVoiceAudioSource == null)
        {
            return;
        }


        /*
         * Запоминаем предыдущий
         * default voice только один раз
         * за текущий ручной телефон.
         */
        if (!voiceOverrideActive)
        {
            previousDefaultVoiceSource =
                dialogueManager
                    .defaultVoiceAudioSource;


            voiceOverrideActive = true;
        }


        dialogueManager
            .ResetVoiceAudioSource(
                contactVoiceAudioSource
            );


        contactVoiceAudioSource.clip =
            contact != null
                ? contact.VoiceClip
                : null;


        dialogueManager
            .defaultVoiceAudioSource =
                contactVoiceAudioSource;
    }


    private void RestoreDialogueVoice()
    {
        if (!voiceOverrideActive)
            return;


        if (dialogueManager != null)
        {
            dialogueManager
                .ResetVoiceAudioSource(
                    contactVoiceAudioSource
                );


            dialogueManager
                .defaultVoiceAudioSource =
                    previousDefaultVoiceSource;
        }


        previousDefaultVoiceSource =
            null;


        voiceOverrideActive =
            false;
    }


    // =====================================================
    // PHONE SFX
    // =====================================================

    private void PlayPowerOnSound()
    {
        if (phoneSfxAudioSource == null ||
            phonePowerOnClip == null)
        {
            return;
        }


        phoneSfxAudioSource.Stop();

        phoneSfxAudioSource.loop =
            false;


        phoneSfxAudioSource.PlayOneShot(
            phonePowerOnClip
        );
    }


    private void PlayDialTone()
    {
        if (phoneSfxAudioSource == null ||
            dialToneClip == null)
        {
            return;
        }


        phoneSfxAudioSource.Stop();


        phoneSfxAudioSource.clip =
            dialToneClip;


        phoneSfxAudioSource.loop =
            true;


        phoneSfxAudioSource.Play();
    }


    private void StopPhoneSfx()
    {
        if (phoneSfxAudioSource == null)
            return;


        phoneSfxAudioSource.Stop();

        phoneSfxAudioSource.loop =
            false;
    }


    // =====================================================
    // PHONE BUTTONS
    // =====================================================

    private void AddButtonListeners()
    {
        if (leftButton != null)
        {
            leftButton.onClick
                .RemoveListener(
                    PreviousContact
                );

            leftButton.onClick
                .AddListener(
                    PreviousContact
                );
        }


        if (rightButton != null)
        {
            rightButton.onClick
                .RemoveListener(
                    NextContact
                );

            rightButton.onClick
                .AddListener(
                    NextContact
                );
        }


        if (callButton != null)
        {
            callButton.onClick
                .RemoveListener(
                    CallSelectedContact
                );

            callButton.onClick
                .AddListener(
                    CallSelectedContact
                );
        }


        if (hangUpButton != null)
        {
            hangUpButton.onClick
                .RemoveListener(
                    HangUpPressed
                );

            hangUpButton.onClick
                .AddListener(
                    HangUpPressed
                );
        }
    }


    private void RemoveButtonListeners()
    {
        if (leftButton != null)
        {
            leftButton.onClick
                .RemoveListener(
                    PreviousContact
                );
        }


        if (rightButton != null)
        {
            rightButton.onClick
                .RemoveListener(
                    NextContact
                );
        }


        if (callButton != null)
        {
            callButton.onClick
                .RemoveListener(
                    CallSelectedContact
                );
        }


        if (hangUpButton != null)
        {
            hangUpButton.onClick
                .RemoveListener(
                    HangUpPressed
                );
        }
    }


    private void SetPhoneButtons(
        bool navigation,
        bool call,
        bool hangUp)
    {
        if (leftButton != null)
        {
            leftButton.interactable =
                navigation;
        }


        if (rightButton != null)
        {
            rightButton.interactable =
                navigation;
        }


        if (callButton != null)
        {
            callButton.interactable =
                call;
        }


        if (hangUpButton != null)
        {
            hangUpButton.interactable =
                hangUp;
        }
    }


    // =====================================================
    // INITIAL UI
    // =====================================================

    private void HidePhoneUIImmediately()
    {
        if (phoneCanvas != null)
        {
            phoneCanvas.SetActive(false);
        }


        if (contacts != null)
        {
            for (int i = 0;
                 i < contacts.Count;
                 i++)
            {
                PhoneContact contact =
                    contacts[i];


                if (contact != null &&
                    contact.ScreenObject != null)
                {
                    contact.ScreenObject
                        .SetActive(false);
                }
            }
        }


        if (choiceController != null)
        {
            choiceController
                .HideChoices(this);
        }
    }


    // =====================================================
    // REFERENCES
    // =====================================================

    private void FindReferences()
    {
        // -------------------------------------------------
        // PHONE
        // -------------------------------------------------

        if (phoneController == null)
        {
            phoneController =
                GetComponent<
                    WorkPhonePenaltyController>();
        }


        // -------------------------------------------------
        // DIALOGUE MANAGER
        // -------------------------------------------------

        if (dialogueManager == null)
        {
            GameObject dialogueObject =
                GameObject.Find(
                    "DialogueManager"
                );


            if (dialogueObject != null)
            {
                dialogueManager =
                    dialogueObject
                        .GetComponent<
                            DialogueManager>();
            }
        }


        if (dialogueManager == null)
        {
            dialogueManager =
                FindFirstObjectByType<
                    DialogueManager>(
                        FindObjectsInactive
                            .Include
                    );
        }


        // -------------------------------------------------
        // GLOBAL DIALOGUE CHOICE
        // -------------------------------------------------

        /*
         * Никакого Instance.
         *
         * Глобальный объект живёт
         * в persistent-группе.
         *
         * Ищем только если ссылка
         * ещё отсутствует, после чего
         * сохраняем её.
         */
        if (choiceController == null)
        {
            choiceController =
                FindFirstObjectByType<
                    DialogueChoiceController>(
                        FindObjectsInactive
                            .Include
                    );
        }
    }


    // =====================================================
    // INSPECTOR
    // =====================================================

    private void OnValidate()
    {
        minimumAnswerDelay =
            Mathf.Max(
                0f,
                minimumAnswerDelay
            );


        maximumAnswerDelay =
            Mathf.Max(
                minimumAnswerDelay,
                maximumAnswerDelay
            );
    }
}
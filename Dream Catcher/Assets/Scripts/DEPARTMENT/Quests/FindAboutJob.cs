using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FindAboutJob : MonoBehaviour, IInteractable
{
    [Header("Quest Settings")]
    public string questIdToComplete = "find_about_job";
    public string questIdToAddAfterSecondDialogue = "find_workplace";

    [Header("Activation")]
    public float activationDelay = 2f;

    [Header("Dialogues")]
    public List<DialogueManager.DialogueLine> firstDialogueLines;
    public List<DialogueManager.DialogueLine> secondDialogueLines;

    [Header("Receptionist Animation")]
    public Animator receptionistAnimator;
    public bool autoFindAnimatorInChildren = true;

    [Header("Animator Triggers")]
    public string talkTriggerName = "Talk";
    public string givesKeyTriggerName = "GivesKey";
    public string stopTalkTriggerName = "StopTalk";
    public bool resetOtherTriggersBeforeSet = true;

    [Header("Give Key Audio")]
    [Tooltip("AudioSource со звуком выдачи ключей. Clip назначается прямо в AudioSource.")]
    [SerializeField] private AudioSource giveKeyAudioSource;

    [Tooltip("Название Animator State анимации выдачи ключей.")]
    [SerializeField] private string giveKeyStateName = "giveKey";

    [Tooltip("Кадр state giveKey, на котором должен проиграться звук.")]
    [SerializeField] private int giveKeyAudioFrame = 148;

    [Header("Keys Swap By Time")]
    [Tooltip("Ключи, которые участвуют в анимации секретутки. Они пропадут в нужный момент.")]
    public GameObject animatedKeysObject;

    [Tooltip("Ключи в мире на столе. Этот объект потом назначается в FindWorkplace как Keys Object To Hide.")]
    public GameObject worldKeysObject;

    [Tooltip("Спрятать worldKeysObject на старте, если задание find_workplace ещё не активно/не завершено.")]
    public bool hideWorldKeysOnStart = true;

    [Tooltip("Включи, если хочешь указывать момент не секундами, а кадром клипа.")]
    public bool useFrameNumberForKeysSwap = true;

    [Tooltip("Кадр, на котором надо заменить анимированные ключи на world keys.")]
    public int keysSwapFrame = 165;

    [Tooltip("FPS анимации. Если Maya/Unity клип 30 fps, оставь 30.")]
    public float animationFrameRate = 30f;

    [Tooltip("Если useFrameNumberForKeysSwap выключен, используется это время в секундах.")]
    public float keysSwapDelaySeconds = 5.5f;

    [Tooltip("После появления world keys секретутка снова станет доступна для второго диалога.")]
    public bool unlockSecondDialogueWhenKeysSwapped = true;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string questUIManagerObjectName = "QuestUIManager";
    public string dialogueManagerObjectName = "DialogueManager";
    public string interactionDotObjectName = "InteractionDot";

    private bool isCompleted = false;
    private bool firstDialogueShown = false;
    private bool secondDialogueShown = false;
    private bool secondDialogueUnlocked = false;
    private bool givesKeyAnimationTriggered = false;
    private bool keysSwapped = false;

    private Collider objectCollider;
    private int defaultLayer;
    private int interactableLayer;

    private QuestUIManager questManager;
    private DialogueManager dialogueManager;
    private Image interactionDot;

    private Coroutine givesKeySequenceCoroutine;

    private void Start()
    {
        objectCollider = GetComponent<Collider>();

        defaultLayer = LayerMask.NameToLayer("Default");
        interactableLayer = LayerMask.NameToLayer("Interactable");

        DisableInteraction();

        FindReferences();
        PrepareWorldKeysOnStart();

        StartCoroutine(ActivationRoutine());
    }

    private IEnumerator ActivationRoutine()
    {
        yield return new WaitForSeconds(activationDelay);

        while (!isCompleted)
        {
            FindReferences();

            if (questManager != null && questManager.IsQuestActive(questIdToComplete))
            {
                EnableInteraction();

                Debug.Log(
                    $"FindAboutJob: объект {gameObject.name} активирован для задания {questIdToComplete}",
                    this
                );

                yield break;
            }

            yield return new WaitForSeconds(2f);
        }
    }

    public void Interact()
    {
        FindReferences();

        if (isCompleted)
            return;

        if (questManager == null || dialogueManager == null)
            return;

        if (dialogueManager.DialogueActive)
            return;

        if (!questManager.IsQuestActive(questIdToComplete))
        {
            Debug.Log(
                $"FindAboutJob: задание '{questIdToComplete}' не активно или уже завершено.",
                this
            );

            return;
        }

        if (!firstDialogueShown)
        {
            StartFirstDialogue();
            return;
        }

        if (!secondDialogueShown)
        {
            if (!secondDialogueUnlocked)
            {
                HideInteractionDot();
                return;
            }

            StartSecondDialogue();
        }
    }

    private void StartFirstDialogue()
    {
        if (firstDialogueLines == null || firstDialogueLines.Count == 0)
            return;

        firstDialogueShown = true;
        secondDialogueUnlocked = false;

        HideInteractionDot();
        DisableInteraction();

        PlayTalkAnimation();

        dialogueManager.StartDialogue(firstDialogueLines, true);

        StartCoroutine(WaitForFirstDialogueAndStartGivesKey());
    }

    private IEnumerator WaitForFirstDialogueAndStartGivesKey()
    {
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        HideInteractionDot();
        DisableInteraction();

        TriggerGivesKeyAnimation();
    }

    private void TriggerGivesKeyAnimation()
    {
        if (givesKeyAnimationTriggered)
            return;

        givesKeyAnimationTriggered = true;

        if (givesKeySequenceCoroutine != null)
            StopCoroutine(givesKeySequenceCoroutine);

        givesKeySequenceCoroutine = StartCoroutine(GivesKeySequence());
    }

    private IEnumerator GivesKeySequence()
    {
        HideInteractionDot();
        DisableInteraction();

        if (receptionistAnimator == null)
        {
            Debug.LogWarning(
                $"FindAboutJob: Animator не назначен на объекте {gameObject.name}. Ключи будут показаны без анимации.",
                this
            );

            SwapAnimatedKeysToWorldKeys();
            yield break;
        }

        PlayGivesKeyAnimation();

        // Отдельно ждём 148-й кадр state giveKey
        // и проигрываем Clip, назначенный в AudioSource.
        StartCoroutine(PlayGiveKeyAudioAtFrame());

        Debug.Log(
            $"FindAboutJob: запущен Trigger {givesKeyTriggerName}",
            this
        );

        float delay = GetKeysSwapDelay();

        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        else
            yield return null;

        SwapAnimatedKeysToWorldKeys();
    }

    private IEnumerator PlayGiveKeyAudioAtFrame()
    {
        if (receptionistAnimator == null)
            yield break;

        if (giveKeyAudioSource == null)
            yield break;

        if (giveKeyAudioSource.clip == null)
            yield break;

        if (string.IsNullOrEmpty(giveKeyStateName))
            yield break;

        if (animationFrameRate <= 0f)
            yield break;

        int stateHash = Animator.StringToHash(giveKeyStateName);

        // Ждём, пока Animator реально войдёт в state giveKey.
        while (true)
        {
            AnimatorStateInfo stateInfo =
                receptionistAnimator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.shortNameHash == stateHash)
                break;

            yield return null;
        }

        float targetTime = giveKeyAudioFrame / animationFrameRate;

        // Считаем время именно внутри state giveKey,
        // а не от момента установки Trigger.
        while (true)
        {
            AnimatorStateInfo stateInfo =
                receptionistAnimator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.shortNameHash != stateHash)
                yield break;

            float currentTime = stateInfo.normalizedTime * stateInfo.length;

            if (currentTime >= targetTime)
                break;

            yield return null;
        }

        giveKeyAudioSource.Play();
    }

    private float GetKeysSwapDelay()
    {
        if (!useFrameNumberForKeysSwap)
            return Mathf.Max(0f, keysSwapDelaySeconds);

        if (animationFrameRate <= 0f)
            return Mathf.Max(0f, keysSwapDelaySeconds);

        return Mathf.Max(0f, keysSwapFrame / animationFrameRate);
    }

    private void SwapAnimatedKeysToWorldKeys()
    {
        if (keysSwapped)
            return;

        keysSwapped = true;

        if (animatedKeysObject != null)
            animatedKeysObject.SetActive(false);

        if (worldKeysObject != null)
            worldKeysObject.SetActive(true);

        if (unlockSecondDialogueWhenKeysSwapped)
            UnlockSecondDialogueInteraction();

        Debug.Log(
            "FindAboutJob: анимированные ключи скрыты, world keys показаны.",
            this
        );
    }

    private void UnlockSecondDialogueInteraction()
    {
        secondDialogueUnlocked = true;

        EnableInteraction();
        ShowInteractionDot();

        Debug.Log(
            "FindAboutJob: второй диалог теперь доступен после появления ключей.",
            this
        );
    }

    private void StartSecondDialogue()
    {
        if (secondDialogueLines == null || secondDialogueLines.Count == 0)
            return;

        secondDialogueShown = true;

        HideInteractionDot();
        DisableInteraction();

        PlayTalkAnimation();

        dialogueManager.StartDialogue(secondDialogueLines, true);

        StartCoroutine(WaitForSecondDialogueAndCompleteQuest());
    }

    private IEnumerator WaitForSecondDialogueAndCompleteQuest()
    {
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        HideInteractionDot();

        StopTalkAnimation();

        FindReferences();

        if (questManager != null && !string.IsNullOrEmpty(questIdToComplete))
        {
            if (questManager.IsQuestActive(questIdToComplete))
            {
                questManager.CompleteQuest(questIdToComplete);

                Debug.Log(
                    $"FindAboutJob: задание '{questIdToComplete}' завершено после второго диалога.",
                    this
                );
            }
        }

        if (questManager != null &&
            !string.IsNullOrEmpty(questIdToAddAfterSecondDialogue))
        {
            if (!questManager.IsQuestActive(questIdToAddAfterSecondDialogue) &&
                !questManager.IsQuestCompleted(questIdToAddAfterSecondDialogue))
            {
                questManager.AddQuest(questIdToAddAfterSecondDialogue);

                Debug.Log(
                    $"FindAboutJob: добавлено новое задание: {questIdToAddAfterSecondDialogue}",
                    this
                );
            }
        }

        isCompleted = true;
        DisableInteraction();

        Debug.Log(
            $"FindAboutJob: второй диалог для {questIdToComplete} завершён. Объект больше не интерактивен.",
            this
        );
    }

    private void PlayTalkAnimation()
    {
        if (receptionistAnimator == null)
            return;

        if (resetOtherTriggersBeforeSet)
        {
            if (!string.IsNullOrEmpty(givesKeyTriggerName))
                receptionistAnimator.ResetTrigger(givesKeyTriggerName);

            if (!string.IsNullOrEmpty(stopTalkTriggerName))
                receptionistAnimator.ResetTrigger(stopTalkTriggerName);
        }

        if (!string.IsNullOrEmpty(talkTriggerName))
            receptionistAnimator.SetTrigger(talkTriggerName);
    }

    private void PlayGivesKeyAnimation()
    {
        if (receptionistAnimator == null)
            return;

        if (resetOtherTriggersBeforeSet)
        {
            if (!string.IsNullOrEmpty(talkTriggerName))
                receptionistAnimator.ResetTrigger(talkTriggerName);

            if (!string.IsNullOrEmpty(stopTalkTriggerName))
                receptionistAnimator.ResetTrigger(stopTalkTriggerName);
        }

        if (!string.IsNullOrEmpty(givesKeyTriggerName))
            receptionistAnimator.SetTrigger(givesKeyTriggerName);
    }

    private void StopTalkAnimation()
    {
        if (receptionistAnimator == null)
            return;

        if (resetOtherTriggersBeforeSet)
        {
            if (!string.IsNullOrEmpty(talkTriggerName))
                receptionistAnimator.ResetTrigger(talkTriggerName);

            if (!string.IsNullOrEmpty(givesKeyTriggerName))
                receptionistAnimator.ResetTrigger(givesKeyTriggerName);
        }

        if (!string.IsNullOrEmpty(stopTalkTriggerName))
            receptionistAnimator.SetTrigger(stopTalkTriggerName);
    }

    private void PrepareWorldKeysOnStart()
    {
        if (!hideWorldKeysOnStart)
            return;

        if (worldKeysObject == null)
            return;

        if (ShouldKeepWorldKeysVisibleOnStart())
            return;

        worldKeysObject.SetActive(false);
    }

    private bool ShouldKeepWorldKeysVisibleOnStart()
    {
        FindReferences();

        if (questManager == null)
            return false;

        if (!string.IsNullOrEmpty(questIdToAddAfterSecondDialogue))
        {
            if (questManager.IsQuestActive(questIdToAddAfterSecondDialogue))
                return true;

            if (questManager.IsQuestCompleted(questIdToAddAfterSecondDialogue))
                return true;
        }

        return false;
    }

    private void EnableInteraction()
    {
        if (interactableLayer != -1)
            gameObject.layer = interactableLayer;

        if (objectCollider != null)
            objectCollider.enabled = true;
    }

    private void DisableInteraction()
    {
        if (defaultLayer != -1)
            gameObject.layer = defaultLayer;

        if (objectCollider != null)
            objectCollider.enabled = false;
    }

    private void HideInteractionDot()
    {
        FindReferences();

        if (interactionDot != null)
            interactionDot.enabled = false;
    }

    private void ShowInteractionDot()
    {
        FindReferences();

        if (interactionDot != null)
            interactionDot.enabled = true;
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (questManager == null)
            questManager = QuestUIManager.Instance;

        if (questManager == null)
        {
            GameObject obj = GameObject.Find(questUIManagerObjectName);

            if (obj != null)
                questManager = obj.GetComponent<QuestUIManager>();
        }

        if (questManager == null)
            questManager = FindObjectOfType<QuestUIManager>();

        if (dialogueManager == null ||
            dialogueManager.gameObject.name != dialogueManagerObjectName)
        {
            GameObject obj = GameObject.Find(dialogueManagerObjectName);

            if (obj != null)
                dialogueManager = obj.GetComponent<DialogueManager>();
        }

        if (dialogueManager == null)
        {
            DialogueManager[] managers =
                FindObjectsOfType<DialogueManager>();

            foreach (DialogueManager manager in managers)
            {
                if (manager.gameObject.name == dialogueManagerObjectName)
                {
                    dialogueManager = manager;
                    break;
                }
            }
        }

        if (interactionDot == null)
        {
            GameObject obj =
                GameObject.Find(interactionDotObjectName);

            if (obj != null)
                interactionDot = obj.GetComponent<Image>();
        }

        if (receptionistAnimator == null &&
            autoFindAnimatorInChildren)
        {
            receptionistAnimator =
                GetComponentInChildren<Animator>();
        }
    }
}
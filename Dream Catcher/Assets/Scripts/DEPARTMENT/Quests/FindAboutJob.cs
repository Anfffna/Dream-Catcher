using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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
    public string givesKeyTriggerName = "GivesKey";
    public bool autoFindAnimatorInChildren = true;

    [Header("Move With Chair Before GivesKey")]
    public Transform receptionistMoveRoot;
    public Vector3 givesKeyLocalOffset = new Vector3(0f, 0f, -0.35f);
    public float moveBackDuration = 0.35f;
    public bool moveBeforeGivesKeyTrigger = true;

    [Header("Return After GivesKey")]
    public bool returnAfterGivesKey = true;
    public float returnDelayAfterGivesKey = 2f;
    public float returnDuration = 0.35f;

    [Header("Second Dialogue Unlock")]
    public string givesKeyStateName = "GivesKey";
    public int givesKeyLayerIndex = 0;
    public float waitForGivesKeyEnterTimeout = 2f;

    [Header("Keys Transfer During GivesKey")]
    public GameObject keysInHandObject;
    public Transform keysWorldObject;
    public float keysTransferTimeInGivesKey = 4f;
    public float keysWorldAppearDuration = 0.4f;
    public bool hideKeysOnStart = true;

    private Vector3 keysWorldOriginalLocalScale;
    private bool keysWorldScaleSaved = false;
    private bool keysTransferDone = false;
    private Coroutine keysTransferCoroutine;

    private bool secondDialogueUnlocked = false;

    private Vector3 receptionistStartLocalPosition;
    private bool receptionistStartPositionSaved = false;
    private Coroutine givesKeySequenceCoroutine;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string questUIManagerObjectName = "QuestUIManager";
    public string dialogueManagerObjectName = "DialogueManager";
    public string interactionDotObjectName = "InteractionDot";

    private bool isCompleted = false;
    private bool firstDialogueShown = false;
    private bool secondDialogueShown = false;
    private bool givesKeyAnimationTriggered = false;

    private Collider objectCollider;
    private int defaultLayer;
    private int interactableLayer;
    private QuestUIManager questManager;
    private DialogueManager dialogueManager;
    private Image interactionDot;

    void Start()
    {
        objectCollider = GetComponent<Collider>();
        defaultLayer = LayerMask.NameToLayer("Default");
        interactableLayer = LayerMask.NameToLayer("Interactable");

        gameObject.layer = defaultLayer;
        if (objectCollider != null) objectCollider.enabled = false;

        FindReferences();
        PrepareKeysTransferObjects();
        StartCoroutine(ActivationRoutine());
    }

    private IEnumerator ActivationRoutine()
    {
        yield return new WaitForSeconds(activationDelay);

        FindReferences();

        if (questManager == null)
        {
            yield return new WaitForSeconds(2f);
            StartCoroutine(ActivationRoutine());
            yield break;
        }

        if (questManager.IsQuestActive(questIdToComplete))
        {
            gameObject.layer = interactableLayer;

            if (objectCollider != null)
                objectCollider.enabled = true;

            Debug.Log($"Объект {gameObject.name} активирован для задания {questIdToComplete}");
        }
        else
        {
            yield return new WaitForSeconds(2f);
            StartCoroutine(ActivationRoutine());
        }
    }

    private void EnsureDialogueManager()
    {
        FindReferences();
    }

    public void Interact()
    {
        FindReferences();

        if (isCompleted) return;

        EnsureDialogueManager();

        if (questManager == null || dialogueManager == null)
            return;

        if (!firstDialogueShown && !questManager.IsQuestActive(questIdToComplete))
        {
            Debug.Log($"Задание '{questIdToComplete}' не активно или уже завершено.");
            return;
        }

        if (dialogueManager.DialogueActive) return;

        if (firstDialogueShown && !secondDialogueShown && !secondDialogueUnlocked)
        {
            HideInteractionDot();
            return;
        }

        if (!firstDialogueShown)
        {
            if (firstDialogueLines != null && firstDialogueLines.Count > 0)
            {
                HideInteractionDot();

                dialogueManager.StartDialogue(firstDialogueLines, true);
                firstDialogueShown = true;

                StartCoroutine(WaitForFirstDialogueAndCompleteQuest());
            }
        }
        else if (!secondDialogueShown)
        {
            if (secondDialogueLines != null && secondDialogueLines.Count > 0)
            {
                HideInteractionDot();

                dialogueManager.StartDialogue(secondDialogueLines, true);
                secondDialogueShown = true;

                StartCoroutine(WaitForSecondDialogueAndDisableInteraction());
            }
        }
    }

    private IEnumerator ShowDotAfterDialogue()
    {
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        ShowInteractionDot();
    }

    private IEnumerator WaitForFirstDialogueAndCompleteQuest()
    {
        // Ждём, пока первый диалог полностью закончится
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        // Пока идёт GivesKey-анимация, второй интерактив запрещён
        secondDialogueUnlocked = false;
        HideInteractionDot();

        gameObject.layer = defaultLayer;

        if (objectCollider != null)
            objectCollider.enabled = false;

        // После полного окончания первого диалога запускаем анимацию GivesKey
        TriggerGivesKeyAnimation();

        FindReferences();

        if (questManager != null && questManager.IsQuestActive(questIdToComplete))
        {
            questManager.CompleteQuest(questIdToComplete);
            Debug.Log($"Задание '{questIdToComplete}' завершено после первого диалога.");
        }

        // ВАЖНО: второй диалог разблокируется не здесь,
        // а только после полного завершения GivesKeySequence().
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
        SaveReceptionistStartPosition();

        HideInteractionDot();

        gameObject.layer = defaultLayer;

        if (objectCollider != null)
            objectCollider.enabled = false;

        if (receptionistAnimator == null)
        {
            Debug.LogWarning($"Animator для анимации '{givesKeyTriggerName}' не назначен на объекте {gameObject.name}.");

            UnlockSecondDialogueInteraction();
            yield break;
        }

        if (receptionistMoveRoot != null && moveBeforeGivesKeyTrigger)
        {
            Vector3 targetPosition = receptionistStartLocalPosition + givesKeyLocalOffset;
            yield return MoveLocalPosition(receptionistMoveRoot, targetPosition, moveBackDuration);
        }

        receptionistAnimator.SetTrigger(givesKeyTriggerName);
        Debug.Log($"Запущен Animator Trigger: {givesKeyTriggerName}");
        StartKeysTransferCoroutine();

        if (receptionistMoveRoot != null && !moveBeforeGivesKeyTrigger)
        {
            Vector3 targetPosition = receptionistStartLocalPosition + givesKeyLocalOffset;
            yield return MoveLocalPosition(receptionistMoveRoot, targetPosition, moveBackDuration);
        }

        // Ждём, пока Animator реально проиграет состояние GivesKey
        yield return WaitForAnimatorStateComplete(givesKeyStateName, givesKeyLayerIndex);

        if (returnAfterGivesKey && receptionistMoveRoot != null)
        {
            yield return MoveLocalPosition(receptionistMoveRoot, receptionistStartLocalPosition, returnDuration);
        }

        // Только теперь второй диалог снова доступен
        UnlockSecondDialogueInteraction();
    }

    private void PrepareKeysTransferObjects()
    {
        if (keysInHandObject != null && hideKeysOnStart)
            keysInHandObject.SetActive(false);

        if (keysWorldObject != null)
        {
            keysWorldOriginalLocalScale = keysWorldObject.localScale;
            keysWorldScaleSaved = true;

            if (hideKeysOnStart)
            {
                keysWorldObject.localScale = Vector3.zero;
                keysWorldObject.gameObject.SetActive(false);
                SetObjectCollidersEnabled(keysWorldObject.gameObject, false);
            }
        }
    }

    private void StartKeysTransferCoroutine()
    {
        if (keysTransferDone)
            return;

        if (keysTransferCoroutine != null)
            StopCoroutine(keysTransferCoroutine);

        keysTransferCoroutine = StartCoroutine(KeysTransferRoutine());
    }

    private IEnumerator KeysTransferRoutine()
    {
        if (receptionistAnimator == null)
            yield break;

        int stateHash = Animator.StringToHash(givesKeyStateName);
        bool enteredState = false;
        float timer = 0f;

        // Ждём, пока Animator войдёт в состояние guestKey/GivesKey
        while (!enteredState && timer < waitForGivesKeyEnterTimeout)
        {
            AnimatorStateInfo currentInfo = receptionistAnimator.GetCurrentAnimatorStateInfo(givesKeyLayerIndex);
            AnimatorStateInfo nextInfo = receptionistAnimator.GetNextAnimatorStateInfo(givesKeyLayerIndex);

            enteredState =
                IsAnimatorState(currentInfo, givesKeyStateName, stateHash) ||
                IsAnimatorState(nextInfo, givesKeyStateName, stateHash);

            timer += Time.deltaTime;
            yield return null;
        }

        if (!enteredState)
        {
            Debug.LogWarning($"Ключи не показались в руке: Animator не вошёл в состояние '{givesKeyStateName}'.");
            yield break;
        }

        // Ждём, пока состояние станет текущим
        while (true)
        {
            AnimatorStateInfo currentInfo = receptionistAnimator.GetCurrentAnimatorStateInfo(givesKeyLayerIndex);

            if (IsAnimatorState(currentInfo, givesKeyStateName, stateHash))
                break;

            yield return null;
        }

        // В самом начале второй анимации показываем ключи в руке
        if (keysInHandObject != null)
            keysInHandObject.SetActive(true);

        // Ждём 4 секунды внутри этой анимации
        while (true)
        {
            AnimatorStateInfo currentInfo = receptionistAnimator.GetCurrentAnimatorStateInfo(givesKeyLayerIndex);

            if (!IsAnimatorState(currentInfo, givesKeyStateName, stateHash))
            {
                Debug.LogWarning($"Состояние '{givesKeyStateName}' закончилось раньше, чем наступила {keysTransferTimeInGivesKey} секунда.");
                yield break;
            }

            float secondsInState = currentInfo.normalizedTime * currentInfo.length;

            if (secondsInState >= keysTransferTimeInGivesKey)
                break;

            yield return null;
        }

        // На 4-й секунде ключи исчезают из руки
        if (keysInHandObject != null)
            keysInHandObject.SetActive(false);

        // И появляются там, где уже стоит keysWorldObject
        yield return RevealWorldKeysObject();

        keysTransferDone = true;
    }

    private IEnumerator RevealWorldKeysObject()
    {
        if (keysWorldObject == null)
            yield break;

        if (!keysWorldScaleSaved)
        {
            keysWorldOriginalLocalScale = keysWorldObject.localScale;
            keysWorldScaleSaved = true;
        }

        keysWorldObject.gameObject.SetActive(true);
        keysWorldObject.localScale = Vector3.zero;

        SetObjectCollidersEnabled(keysWorldObject.gameObject, false);

        float elapsed = 0f;

        while (elapsed < keysWorldAppearDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / keysWorldAppearDuration);
            float smoothT = t * t * (3f - 2f * t);

            keysWorldObject.localScale = Vector3.Lerp(Vector3.zero, keysWorldOriginalLocalScale, smoothT);

            yield return null;
        }

        keysWorldObject.localScale = keysWorldOriginalLocalScale;
        SetObjectCollidersEnabled(keysWorldObject.gameObject, false);

        Debug.Log("Ключи исчезли из руки и появились в мире, но пока не интерактивны.");
    }

    private bool IsAnimatorState(AnimatorStateInfo info, string stateName, int stateHash)
    {
        return info.shortNameHash == stateHash || info.IsName(stateName);
    }

    private void SetObjectCollidersEnabled(GameObject targetObject, bool enabled)
    {
        if (targetObject == null)
            return;

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            if (col != null)
                col.enabled = enabled;
        }
    }

    private IEnumerator WaitForAnimatorStateComplete(string stateName, int layerIndex)
    {
        if (receptionistAnimator == null)
            yield break;

        int stateHash = Animator.StringToHash(stateName);
        bool enteredState = false;
        float timer = 0f;

        // Ждём, пока Animator войдёт в состояние GivesKey
        while (!enteredState && timer < waitForGivesKeyEnterTimeout)
        {
            AnimatorStateInfo currentInfo = receptionistAnimator.GetCurrentAnimatorStateInfo(layerIndex);
            AnimatorStateInfo nextInfo = receptionistAnimator.GetNextAnimatorStateInfo(layerIndex);

            enteredState =
                currentInfo.shortNameHash == stateHash ||
                nextInfo.shortNameHash == stateHash ||
                currentInfo.IsName(stateName) ||
                nextInfo.IsName(stateName);

            timer += Time.deltaTime;
            yield return null;
        }

        if (!enteredState)
        {
            Debug.LogWarning($"Animator не вошёл в состояние '{stateName}'. Проверь имя State в Animator.");
            yield break;
        }

        // Если состояние пока только next state, ждём, пока оно станет current state
        while (true)
        {
            AnimatorStateInfo currentInfo = receptionistAnimator.GetCurrentAnimatorStateInfo(layerIndex);

            bool isCurrentState =
                currentInfo.shortNameHash == stateHash ||
                currentInfo.IsName(stateName);

            if (isCurrentState)
                break;

            yield return null;
        }

        // Ждём, пока состояние полностью проиграется и Animator выйдет из него
        while (true)
        {
            AnimatorStateInfo currentInfo = receptionistAnimator.GetCurrentAnimatorStateInfo(layerIndex);

            bool isCurrentState =
                currentInfo.shortNameHash == stateHash ||
                currentInfo.IsName(stateName);

            bool isTransitioning = receptionistAnimator.IsInTransition(layerIndex);

            if (!isCurrentState && !isTransitioning)
                break;

            if (isCurrentState && currentInfo.normalizedTime >= 1f && !isTransitioning)
                break;

            yield return null;
        }
    }

    private void UnlockSecondDialogueInteraction()
    {
        secondDialogueUnlocked = true;

        gameObject.layer = interactableLayer;

        if (objectCollider != null)
            objectCollider.enabled = true;

        ShowInteractionDot();

        Debug.Log("Второй диалог теперь доступен после завершения GivesKey-анимации.");
    }

    private void SaveReceptionistStartPosition()
    {
        if (receptionistStartPositionSaved)
            return;

        if (receptionistMoveRoot == null)
            return;

        receptionistStartLocalPosition = receptionistMoveRoot.localPosition;
        receptionistStartPositionSaved = true;
    }

    private IEnumerator MoveLocalPosition(Transform target, Vector3 endPosition, float duration)
    {
        if (target == null)
            yield break;

        Vector3 startPosition = target.localPosition;

        if (duration <= 0f)
        {
            target.localPosition = endPosition;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            // плавнее, чем просто линейно
            float smoothT = t * t * (3f - 2f * t);

            target.localPosition = Vector3.Lerp(startPosition, endPosition, smoothT);
            yield return null;
        }

        target.localPosition = endPosition;
    }

    private IEnumerator WaitForSecondDialogueAndDisableInteraction()
    {
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        HideInteractionDot();

        FindReferences();

        if (questManager != null && !string.IsNullOrEmpty(questIdToAddAfterSecondDialogue))
        {
            questManager.AddQuest(questIdToAddAfterSecondDialogue);
            Debug.Log($"Добавлено новое задание: {questIdToAddAfterSecondDialogue}");
        }

        isCompleted = true;
        gameObject.layer = defaultLayer;

        if (objectCollider != null)
            objectCollider.enabled = false;

        Debug.Log($"Второй диалог для {questIdToComplete} завершён. Объект больше не интерактивен.");
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

        // QuestUIManager
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

        // DialogueManager — ищем именно обычный DialogueManager, не LoadingDialogueManager
        if (dialogueManager == null || dialogueManager.gameObject.name != dialogueManagerObjectName)
        {
            GameObject obj = GameObject.Find(dialogueManagerObjectName);

            if (obj != null)
                dialogueManager = obj.GetComponent<DialogueManager>();
        }

        if (dialogueManager == null)
        {
            DialogueManager[] managers = FindObjectsOfType<DialogueManager>();

            foreach (DialogueManager manager in managers)
            {
                if (manager.gameObject.name == dialogueManagerObjectName)
                {
                    dialogueManager = manager;
                    break;
                }
            }
        }

        // InteractionDot
        if (interactionDot == null)
        {
            GameObject obj = GameObject.Find(interactionDotObjectName);

            if (obj != null)
                interactionDot = obj.GetComponent<Image>();
        }

        // Animator регистраторши
        if (receptionistAnimator == null && autoFindAnimatorInChildren)
        {
            receptionistAnimator = GetComponentInChildren<Animator>();
        }
    }
}
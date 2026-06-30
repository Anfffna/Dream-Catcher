using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class MailboxStartDay : MonoBehaviour, IInteractable
{
    [Header("UI Elements")]
    public RectTransform letterImage;          // спрайт письма (RectTransform)
    public CanvasGroup hintCanvasGroup;        // плашка с подсказкой

    [Header("Blur")]
    public Volume blurVolume;                  // глобальный Volume с блюром

    [Header("Quest")]
    public QuestUIManager questUIManager;
    public string questIdToComplete = "check_the_mailbox";

    [Header("Dialogue After Letter")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> afterLetterLines;
    public float dialogueDelay = 1f;

    [Header("Animation Timings")]
    public float slideDuration = 1f;           // время выдвижения/задвижения письма
    public float fadeDuration = 1f;            // время появления/исчезновения плашки

    [Header("Audio")]
    public AudioSource letterAudioSource;   // сюда перетащите AudioSource с клипом

    [Header("Player")]
    public PlayerController playerController;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string questUIManagerObjectName = "QuestUIManager";
    public string dialogueManagerObjectName = "DialogueManager";
    public string playerObjectName = "Player";

    private bool isReading = false;
    private bool isRead = false;
    private Vector2 startPos;   // Y = -990
    private Vector2 targetPos;  // Y = 0 (центр)
    private Coroutine currentCoroutine;
    private bool waitingForFirstClick = false;
    private bool waitingForSecondClick = false;

    void Start()
    {
        FindReferences();

        gameObject.layer = LayerMask.NameToLayer("Interactable");
        startPos = new Vector2(letterImage.anchoredPosition.x, -990f);
        targetPos = new Vector2(letterImage.anchoredPosition.x, 0f);

        letterImage.gameObject.SetActive(false);
        hintCanvasGroup.alpha = 0f;
        hintCanvasGroup.blocksRaycasts = false;
        hintCanvasGroup.interactable = false;
    }

    void Update()
    {
        if (waitingForFirstClick && Input.GetMouseButtonDown(0))
        {
            waitingForFirstClick = false;
            StartCoroutine(ShowHintRoutine());
        }
        else if (waitingForSecondClick && Input.GetMouseButtonDown(0))
        {
            waitingForSecondClick = false;
            StartCoroutine(HideAllRoutine());
        }
    }

    public void Interact()
    {
        FindReferences();

        if (isRead || isReading) return;
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(ShowLetterRoutine());
    }

    private IEnumerator ShowLetterRoutine()
    {
        FindReferences();
        isReading = true;

        // Блюр
        if (blurVolume != null)
        {
            blurVolume.weight = 1f;
            blurVolume.enabled = true;
        }

        // Блокируем движение на время показа письма
        if (playerController != null)
            playerController.canMove = false;

        // Выключаем звуки шагов
        if (playerController != null && playerController.footstepSource != null)
        {
            playerController.footstepSource.Stop();          // остановить, если играет
            playerController.footstepSource.enabled = false; // отключить источник
        }

        // Письмо выдвигается
        letterImage.gameObject.SetActive(true);
        letterImage.anchoredPosition = startPos;

        if (letterAudioSource != null)
            letterAudioSource.Play();

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            float smoothT = t * t * (3f - 2f * t);
            letterImage.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }
        letterImage.anchoredPosition = targetPos;

        // Скрыть обводку с почтового ящика
        InteractionOutline outline = GetComponent<InteractionOutline>();
        if (outline != null) outline.HideOutline();

        // Ожидание первого клика (плашка появится только после него)
        waitingForFirstClick = true;
        yield return new WaitUntil(() => !waitingForFirstClick);

        // Плашка уже показана корутиной ShowHintRoutine, но мы подождём, пока она появится
        yield return new WaitUntil(() => hintCanvasGroup.alpha >= 0.99f);

        // Ожидание второго клика
        waitingForSecondClick = true;
        yield return new WaitUntil(() => !waitingForSecondClick);

        // Всё скрыто корутиной HideAllRoutine
        // Ждём завершения анимации (проверяем, что письмо скрыто и плашка скрыта)
        yield return new WaitUntil(() => !letterImage.gameObject.activeSelf && hintCanvasGroup.alpha <= 0.01f);

        // Разблокируем движение (перед диалогом)
        if (playerController != null)
            playerController.canMove = true;

        // Включаем звуки шагов обратно
        if (playerController != null && playerController.footstepSource != null)
        {
            playerController.footstepSource.enabled = true;
            // Звук начнёт играть автоматически, когда игрок начнёт двигаться
        }

        FindReferences();
        // Завершаем задание
        if (questUIManager != null && !string.IsNullOrEmpty(questIdToComplete))
        {
            questUIManager.CompleteQuest(questIdToComplete);

            // Добавляем следующее задание
            questUIManager.AddQuest("go_to_depart");
        }

        // Смена слоя на Default, чтобы ящик больше не был интерактивным
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Задержка перед диалогом
        if (dialogueDelay > 0f)
            yield return new WaitForSeconds(dialogueDelay);

        FindReferences();
        // Запускаем диалог после письма
        if (dialogueManager != null && afterLetterLines != null && afterLetterLines.Count > 0)
        {
            dialogueManager.StartDialogue(afterLetterLines);
        }

        isReading = false;
        isRead = true;
        currentCoroutine = null;
    }

    private IEnumerator ShowHintRoutine()
    {
        hintCanvasGroup.alpha = 0f;
        hintCanvasGroup.blocksRaycasts = true;
        hintCanvasGroup.interactable = true;
        hintCanvasGroup.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            hintCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        hintCanvasGroup.alpha = 1f;
    }

    private IEnumerator HideAllRoutine()
    {
        // Плавно скрываем плашку
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            hintCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        hintCanvasGroup.alpha = 0f;
        hintCanvasGroup.blocksRaycasts = false;
        hintCanvasGroup.interactable = false;

        // Письмо задвигается вниз
        elapsed = 0f;
        Vector2 currentPos = letterImage.anchoredPosition;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            float smoothT = t * t * (3f - 2f * t);
            letterImage.anchoredPosition = Vector2.Lerp(targetPos, startPos, smoothT);
            yield return null;
        }
        letterImage.anchoredPosition = startPos;
        letterImage.gameObject.SetActive(false);

        // Блюр выключаем
        if (blurVolume != null)
        {
            blurVolume.weight = 0f;
            blurVolume.enabled = false;
        }
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        // QuestUIManager
        if (questUIManager == null)
            questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
        {
            GameObject obj = GameObject.Find(questUIManagerObjectName);

            if (obj != null)
                questUIManager = obj.GetComponent<QuestUIManager>();
        }

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        // DialogueManager — строго ищем объект с именем DialogueManager
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

        // PlayerController
        if (playerController == null)
        {
            GameObject obj = GameObject.Find(playerObjectName);

            if (obj != null)
                playerController = obj.GetComponent<PlayerController>();
        }

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
    }
}
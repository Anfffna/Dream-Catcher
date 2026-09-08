using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MailboxStartDay :
    MonoBehaviour,
    IInteractable
{
    [Header("Письмо")]

    [Tooltip("Изображение письма.")]
    public RectTransform letterImage;


    [Header("Подсказка")]

    [Tooltip(
        "Отдельный универсальный контроллер " +
        "подсказки для этого ящика."
    )]
    public ClickInteractionHint
        interactionHint;

    [Tooltip(
    "Текст подсказки, которая появляется " +
    "после первого клика по письму."
    )]
    [TextArea(2, 5)]
    public string interactionHintText =
    "Нажмите ЛКМ, чтобы закрыть письмо.";


    [Header("Blur")]

    public Volume blurVolume;


    [Header("Quest")]

    public QuestUIManager questUIManager;

    public string questIdToComplete =
        "check_the_mailbox";


    [Header("Dialogue After Letter")]

    public DialogueManager dialogueManager;

    public List<DialogueManager.DialogueLine>
        afterLetterLines;

    public float dialogueDelay =
        1f;


    [Header("Animation Timings")]

    [Tooltip(
        "Время выдвижения и задвижения письма."
    )]
    public float slideDuration =
        1f;


    [Header("Audio")]

    public AudioSource letterAudioSource;


    [Header("Player")]

    public PlayerController playerController;


    [Header("Auto Find")]

    public bool autoFindReferences =
        true;

    public string questUIManagerObjectName =
        "QuestUIManager";

    public string dialogueManagerObjectName =
        "DialogueManager";

    public string playerObjectName =
        "Player";


    private bool isReading;
    private bool isRead;

    private Vector2 startPos;
    private Vector2 targetPos;

    private Coroutine currentCoroutine;

    /*
     * Пока true —
     * ждём первый клик после того,
     * как письмо полностью появилось.
     */
    private bool waitingForFirstClick;


    // =====================================================
    // UNITY
    // =====================================================

    private void Start()
    {
        FindReferences();


        gameObject.layer =
            LayerMask.NameToLayer(
                "Interactable"
            );


        if (letterImage != null)
        {
            startPos =
                new Vector2(
                    letterImage
                        .anchoredPosition.x,
                    -990f
                );

            targetPos =
                new Vector2(
                    letterImage
                        .anchoredPosition.x,
                    0f
                );


            letterImage
                .gameObject
                .SetActive(false);
        }
    }


    private void Update()
    {
        if (!waitingForFirstClick)
            return;


        if (!Input.GetMouseButtonDown(0))
            return;


        /*
         * Этот клик означает:
         * игрок закончил рассматривать письмо.
         *
         * Дальнейшим кликом уже будет
         * заниматься ClickInteractionHint.
         */
        waitingForFirstClick =
            false;
    }


    // =====================================================
    // INTERACTION
    // =====================================================

    public void Interact()
    {
        FindReferences();


        if (isRead ||
            isReading)
        {
            return;
        }


        /*
         * Ящик больше не должен повторно
         * ловить взаимодействие.
         */
        BoxCollider boxCollider =
            GetComponent<BoxCollider>();


        if (boxCollider != null)
        {
            boxCollider.enabled =
                false;
        }


        if (currentCoroutine != null)
        {
            StopCoroutine(
                currentCoroutine
            );
        }


        currentCoroutine =
            StartCoroutine(
                ShowLetterRoutine()
            );
    }


    // =====================================================
    // ПОКАЗ ПИСЬМА
    // =====================================================

    private IEnumerator ShowLetterRoutine()
    {
        FindReferences();


        isReading =
            true;


        // =================================================
        // BLUR
        // =================================================

        if (blurVolume != null)
        {
            blurVolume.weight =
                1f;

            blurVolume.enabled =
                true;
        }


        // =================================================
        // PLAYER
        // =================================================

        if (playerController != null)
        {
            playerController.canMove =
                false;


            if (playerController
                    .footstepSource != null)
            {
                playerController
                    .footstepSource
                    .Stop();

                playerController
                    .footstepSource
                    .enabled =
                    false;
            }
        }


        // =================================================
        // ПИСЬМО ВЫЕЗЖАЕТ
        // =================================================

        if (letterImage != null)
        {
            letterImage
                .gameObject
                .SetActive(true);

            letterImage.anchoredPosition =
                startPos;


            if (letterAudioSource != null)
            {
                letterAudioSource.Play();
            }


            float elapsed =
                0f;


            while (elapsed <
                   slideDuration)
            {
                elapsed +=
                    Time.deltaTime;


                float t =
                    slideDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(
                            elapsed /
                            slideDuration
                        );


                float smoothT =
                    t * t *
                    (3f - 2f * t);


                letterImage
                    .anchoredPosition =
                    Vector2.Lerp(
                        startPos,
                        targetPos,
                        smoothT
                    );


                yield return null;
            }


            letterImage.anchoredPosition =
                targetPos;
        }


        // =================================================
        // OUTLINE
        // =================================================

        InteractionOutline outline =
            GetComponent<
                InteractionOutline
            >();


        if (outline != null)
        {
            if (!string.IsNullOrEmpty(
                    outline.outlineId))
            {
                InteractionOutlineRegistry
                    .Hide(
                        outline.outlineId
                    );
            }


            outline.HideOutline();
        }


        // =================================================
        // ПЕРВЫЙ КЛИК
        // =================================================

        /*
         * Игрок сначала просто читает письмо.
         *
         * Первый ЛКМ после появления письма
         * вызывает подсказку.
         */
        waitingForFirstClick =
            true;


        yield return new WaitUntil(
            () =>
                !waitingForFirstClick
        );


        // =================================================
        // ПОКАЗ ПОДСКАЗКИ
        // =================================================

        if (interactionHint != null)
        {
            interactionHint.Show(
                interactionHintText,
                true
            );


            /*
             * Show() сразу устанавливает
             * IsVisible = true.
             *
             * Если подсказка действительно
             * открылась — ждём следующего ЛКМ,
             * которым игрок её закрывает.
             */
            if (interactionHint.IsVisible)
            {
                yield return new WaitUntil(
                    () =>
                        interactionHint
                            .DismissRequested
                );
            }
        }


        // =================================================
        // ЗАКРЫВАЕМ ПИСЬМО
        // =================================================

        yield return StartCoroutine(
            HideLetterRoutine()
        );


        /*
         * Подсказка сама делает свой Fade Out.
         *
         * Если её Fade чуть длиннее
         * анимации письма —
         * дожидаемся окончания.
         */
        if (interactionHint != null &&
            interactionHint.IsVisible)
        {
            yield return new WaitUntil(
                () =>
                    !interactionHint
                        .IsVisible
            );
        }


        // =================================================
        // ВОЗВРАЩАЕМ PLAYER
        // =================================================

        if (playerController != null)
        {
            playerController.canMove =
                true;


            if (playerController
                    .footstepSource != null)
            {
                playerController
                    .footstepSource
                    .enabled =
                    true;
            }
        }


        // =================================================
        // QUEST
        // =================================================

        FindReferences();


        if (questUIManager != null &&
            !string.IsNullOrEmpty(
                questIdToComplete))
        {
            questUIManager
                .CompleteQuest(
                    questIdToComplete
                );


            questUIManager
                .AddQuest(
                    "go_to_depart"
                );
        }


        // =================================================
        // ЯЩИК БОЛЬШЕ НЕ ИНТЕРАКТИВЕН
        // =================================================

        gameObject.layer =
            LayerMask.NameToLayer(
                "Default"
            );


        // =================================================
        // ДИАЛОГ ПОСЛЕ ПИСЬМА
        // =================================================

        if (dialogueDelay > 0f)
        {
            yield return
                new WaitForSeconds(
                    dialogueDelay
                );
        }


        FindReferences();


        if (dialogueManager != null &&
            afterLetterLines != null &&
            afterLetterLines.Count > 0)
        {
            dialogueManager
                .StartDialogue(
                    afterLetterLines
                );
        }


        isReading =
            false;

        isRead =
            true;

        currentCoroutine =
            null;
    }


    // =====================================================
    // СКРЫТИЕ ПИСЬМА
    // =====================================================

    private IEnumerator HideLetterRoutine()
    {
        if (letterImage != null)
        {
            float elapsed =
                0f;


            Vector2 currentPos =
                letterImage
                    .anchoredPosition;


            while (elapsed <
                   slideDuration)
            {
                elapsed +=
                    Time.deltaTime;


                float t =
                    slideDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(
                            elapsed /
                            slideDuration
                        );


                float smoothT =
                    t * t *
                    (3f - 2f * t);


                letterImage
                    .anchoredPosition =
                    Vector2.Lerp(
                        currentPos,
                        startPos,
                        smoothT
                    );


                yield return null;
            }


            letterImage.anchoredPosition =
                startPos;


            letterImage
                .gameObject
                .SetActive(false);
        }


        // =================================================
        // BLUR OFF
        // =================================================

        if (blurVolume != null)
        {
            blurVolume.weight =
                0f;

            blurVolume.enabled =
                false;
        }
    }


    // =====================================================
    // REFERENCES
    // =====================================================

    private void FindReferences()
    {
        /*
         * Hint ищем только локально.
         * Никакого поиска по сцене.
         */
        if (interactionHint == null)
        {
            interactionHint =
                GetComponent<
                    ClickInteractionHint
                >();
        }


        if (!autoFindReferences)
            return;


        // =================================================
        // QUEST MANAGER
        // =================================================

        if (questUIManager == null)
        {
            questUIManager =
                QuestUIManager.Instance;
        }


        if (questUIManager == null)
        {
            GameObject obj =
                GameObject.Find(
                    questUIManagerObjectName
                );


            if (obj != null)
            {
                questUIManager =
                    obj.GetComponent<
                        QuestUIManager
                    >();
            }
        }


        if (questUIManager == null)
        {
            questUIManager =
                FindObjectOfType<
                    QuestUIManager
                >();
        }


        // =================================================
        // DIALOGUE MANAGER
        // =================================================

        if (dialogueManager == null ||
            dialogueManager
                .gameObject
                .name !=
            dialogueManagerObjectName)
        {
            GameObject obj =
                GameObject.Find(
                    dialogueManagerObjectName
                );


            if (obj != null)
            {
                dialogueManager =
                    obj.GetComponent<
                        DialogueManager
                    >();
            }
        }


        if (dialogueManager == null)
        {
            DialogueManager[] managers =
                FindObjectsOfType<
                    DialogueManager
                >();


            foreach (
                DialogueManager manager
                in managers)
            {
                if (manager
                        .gameObject
                        .name ==
                    dialogueManagerObjectName)
                {
                    dialogueManager =
                        manager;

                    break;
                }
            }
        }


        // =================================================
        // PLAYER
        // =================================================

        if (playerController == null)
        {
            GameObject obj =
                GameObject.Find(
                    playerObjectName
                );


            if (obj != null)
            {
                playerController =
                    obj.GetComponent<
                        PlayerController
                    >();
            }
        }


        if (playerController == null)
        {
            playerController =
                FindObjectOfType<
                    PlayerController
                >();
        }
    }
}
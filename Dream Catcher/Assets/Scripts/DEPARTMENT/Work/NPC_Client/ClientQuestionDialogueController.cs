using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientQuestionDialogueController :
    MonoBehaviour
{
    public static bool AnyQuestionDialogueOpen
    {
        get;
        private set;
    }

    public static int LastClosedFrame
    {
        get;
        private set;
    } = -1;

    [Header("Диалог")]
    [Tooltip("Существующий DialogueManager.")]
    [SerializeField]
    private DialogueManager dialogueManager;

    [Header("Блок вопросов")]
    [Tooltip("Общий родитель двух кнопок вопросов.")]
    [SerializeField]
    private GameObject questionChoicesRoot;

    [Tooltip("Кнопка первого вопроса.")]
    [SerializeField]
    private Button complaintsButton;

    [Tooltip("Текст первого вопроса.")]
    [SerializeField]
    private TMP_Text complaintsButtonText;

    [Tooltip("Кнопка второго вопроса.")]
    [SerializeField]
    private Button personalQuestionButton;

    [Tooltip("Текст второго вопроса.")]
    [SerializeField]
    private TMP_Text personalQuestionButtonText;

    [Header("Цвет вопросов")]
    [Tooltip("Цвет вопроса, который ещё не задавали.")]
    [SerializeField]
    private Color normalQuestionTextColor =
        Color.white;

    [Tooltip("Более тёмный цвет уже заданного вопроса.")]
    [SerializeField]
    private Color askedQuestionTextColor =
        new Color32(130, 130, 130, 255);

    private VisitorCaseData.VisitorCaseVariant
        currentVariant;

    private VisitorCaseData.QuestionDialogueData
        complaintsQuestion;

    private VisitorCaseData.QuestionDialogueData
        personalQuestion;

    private DialogueManager.DialogueLine
        currentIdleLine;

    private Coroutine dialogueRoutine;

    private bool complaintsAsked;
    private bool personalQuestionAsked;
    private bool answerRunning;
    private bool choicesVisible;

    public bool IsOpen
    {
        get;
        private set;
    }

    public bool SymptomsDiscussed =>
    complaintsAsked;

    private void Awake()
    {
        FindReferences();
        AddButtonListeners();
        SetChoicesVisible(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (answerRunning)
            return;

        if (!complaintsAsked)
            return;

        if (!choicesVisible)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        // Клик по кнопке жалоб позволяет закрыть окно.
        // Кнопка самостоятельно повторно запустит свой ответ.
        if (IsPointerOverQuestionButton())
            return;

        // После двух заданных вопросов
        // любой клик вне кнопок закрывает диалог.
        CloseDialogue();
    }

    private bool IsPointerOverQuestionButton()
    {
        return
            IsPointerInsideButton(
                complaintsButton
            ) ||
            IsPointerInsideButton(
                personalQuestionButton
            );
    }

    private bool IsPointerInsideButton(
        Button targetButton)
    {
        if (targetButton == null ||
            !targetButton.gameObject
                .activeInHierarchy)
        {
            return false;
        }

        RectTransform buttonRect =
            targetButton.transform
                as RectTransform;

        if (buttonRect == null)
            return false;

        Canvas parentCanvas =
            targetButton
                .GetComponentInParent<Canvas>();

        Camera eventCamera = null;

        if (parentCanvas != null &&
            parentCanvas.renderMode !=
                RenderMode.ScreenSpaceOverlay)
        {
            eventCamera =
                parentCanvas.worldCamera;
        }

        return RectTransformUtility
            .RectangleContainsScreenPoint(
                buttonRect,
                Input.mousePosition,
                eventCamera
            );
    }

    private void OnDisable()
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(
                dialogueRoutine
            );

            dialogueRoutine = null;
        }

        answerRunning = false;
        IsOpen = false;

        AnyQuestionDialogueOpen = false;
        LastClosedFrame = Time.frameCount;

        SetChoicesVisible(false);
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
    }

    public void Configure(
        VisitorCaseData.VisitorCaseVariant
            newVariant)
    {
        CloseDialogueImmediately();

        currentVariant = newVariant;

        complaintsQuestion =
            currentVariant != null
                ? currentVariant
                    .ComplaintsQuestion
                : null;

        personalQuestion =
            currentVariant != null
                ? currentVariant
                    .PersonalQuestion
                : null;

        currentIdleLine =
            currentVariant != null
                ? currentVariant
                    .QuestionMenuOpeningLine
                : null;

        complaintsAsked = false;
        personalQuestionAsked = false;
        answerRunning = false;

        RefreshQuestionTexts();
        RefreshQuestionColors();
    }

    public bool OpenDialogue()
    {
        if (IsOpen ||
            answerRunning ||
            currentVariant == null ||
            dialogueManager == null ||
            DialogueManager.AnyDialogueActive)
        {
            return false;
        }

        IsOpen = true;
        AnyQuestionDialogueOpen = true;
        SetChoicesVisible(false);

        if (dialogueRoutine != null)
        {
            StopCoroutine(
                dialogueRoutine
            );
        }

        dialogueRoutine =
            StartCoroutine(
                ShowIdleStateRoutine()
            );

        return true;
    }

    public bool CloseDialogue()
    {
        if (!IsOpen ||
            answerRunning)
        {
            return false;
        }

        if (dialogueManager != null &&
            dialogueManager.DialogueActive)
        {
            if (!dialogueManager
                .ChoicePromptReady)
            {
                return false;
            }

            dialogueManager
                .FinishChoicePrompt(false);
        }

        SetChoicesVisible(false);

        if (dialogueManager != null)
        {
            dialogueManager
                .HidePersistentDialogue();
        }

        IsOpen = false;

        AnyQuestionDialogueOpen = false;
        LastClosedFrame = Time.frameCount;

        return true;
    }

    private IEnumerator ShowIdleStateRoutine()
    {
        if (currentIdleLine == null)
        {
            SetChoicesVisible(true);
            dialogueRoutine = null;
            yield break;
        }

        dialogueManager.ShowChoicePrompt(
            currentIdleLine,
            false
        );

        while (IsOpen &&
               dialogueManager != null &&
               dialogueManager.DialogueActive &&
               !dialogueManager.ChoicePromptReady)
        {
            yield return null;
        }

        if (IsOpen &&
            !answerRunning)
        {
            SetChoicesVisible(true);
        }

        dialogueRoutine = null;
    }

    private void HandleComplaintsPressed()
    {
        StartQuestion(
            complaintsQuestion,
            true
        );
    }

    private void HandlePersonalQuestionPressed()
    {
        StartQuestion(
            personalQuestion,
            false
        );
    }

    private void StartQuestion(
        VisitorCaseData.QuestionDialogueData
            questionData,
        bool isComplaintsQuestion)
    {
        if (!IsOpen ||
            answerRunning ||
            questionData == null ||
            !questionData.HasDialogue ||
            dialogueManager == null)
        {
            return;
        }

        if (dialogueRoutine != null)
        {
            StopCoroutine(
                dialogueRoutine
            );

            dialogueRoutine = null;
        }

        dialogueRoutine =
            StartCoroutine(
                PlayQuestionRoutine(
                    questionData,
                    isComplaintsQuestion
                )
            );
    }

    private IEnumerator PlayQuestionRoutine(
        VisitorCaseData.QuestionDialogueData
            questionData,
        bool isComplaintsQuestion)
    {
        answerRunning = true;

        SetChoicesVisible(false);

        if (dialogueManager.DialogueActive &&
            dialogueManager.ChoicePromptReady)
        {
            dialogueManager
                .FinishChoicePrompt(true);
        }

        if (isComplaintsQuestion)
        {
            complaintsAsked = true;
        }
        else
        {
            personalQuestionAsked = true;
        }

        RefreshQuestionColors();

        yield return null;

        dialogueManager.StartDialogue(
            questionData.ResponseDialogue,
            false,
            true
        );

        while (dialogueManager != null &&
               dialogueManager.DialogueActive)
        {
            yield return null;
        }

        answerRunning = false;

        if (IsOpen)
        {
            RefreshQuestionColors();
            SetChoicesVisible(true);
        }

        dialogueRoutine = null;
    }

    private DialogueManager.DialogueLine
        FindLastLine(
            System.Collections.Generic
                .List<DialogueManager.DialogueLine>
                    lines)
    {
        if (lines == null)
            return null;

        for (int i = lines.Count - 1;
             i >= 0;
             i--)
        {
            if (lines[i] != null)
                return lines[i];
        }

        return null;
    }

    private void RefreshQuestionTexts()
    {
        if (complaintsButtonText != null)
        {
            complaintsButtonText.text =
                complaintsQuestion != null
                    ? complaintsQuestion
                        .QuestionText
                    : "";
        }

        if (personalQuestionButtonText != null)
        {
            personalQuestionButtonText.text =
                personalQuestion != null
                    ? personalQuestion
                        .QuestionText
                    : "";
        }
    }

    private void RefreshQuestionColors()
    {
        if (complaintsButtonText != null)
        {
            complaintsButtonText.color =
                complaintsAsked
                    ? askedQuestionTextColor
                    : normalQuestionTextColor;
        }

        if (personalQuestionButtonText != null)
        {
            personalQuestionButtonText.color =
                personalQuestionAsked
                    ? askedQuestionTextColor
                    : normalQuestionTextColor;
        }
    }

    private void SetChoicesVisible(
        bool visible)
    {
        choicesVisible = visible;
        if (questionChoicesRoot != null)
        {
            questionChoicesRoot
                .SetActive(visible);
        }

        bool firstAvailable =
            visible &&
            complaintsQuestion != null &&
            complaintsQuestion.HasDialogue;

        bool secondAvailable =
            visible &&
            personalQuestion != null &&
            personalQuestion.HasDialogue;

        if (complaintsButton != null)
        {
            complaintsButton.interactable =
                firstAvailable;
        }

        if (personalQuestionButton != null)
        {
            personalQuestionButton.interactable =
                secondAvailable;
        }
    }

    private void CloseDialogueImmediately()
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(
                dialogueRoutine
            );

            dialogueRoutine = null;
        }

        if (dialogueManager != null)
        {
            if (dialogueManager
                .ChoicePromptReady)
            {
                dialogueManager
                    .FinishChoicePrompt(false);
            }

            if (!dialogueManager
                .DialogueActive)
            {
                dialogueManager
                    .HidePersistentDialogue();
            }
        }

        SetChoicesVisible(false);

        IsOpen = false;
        answerRunning = false;

        AnyQuestionDialogueOpen = false;
        LastClosedFrame = Time.frameCount;
    }

    private void AddButtonListeners()
    {
        if (complaintsButton != null)
        {
            complaintsButton.onClick
                .RemoveListener(
                    HandleComplaintsPressed
                );

            complaintsButton.onClick
                .AddListener(
                    HandleComplaintsPressed
                );
        }

        if (personalQuestionButton != null)
        {
            personalQuestionButton.onClick
                .RemoveListener(
                    HandlePersonalQuestionPressed
                );

            personalQuestionButton.onClick
                .AddListener(
                    HandlePersonalQuestionPressed
                );
        }
    }

    private void RemoveButtonListeners()
    {
        if (complaintsButton != null)
        {
            complaintsButton.onClick
                .RemoveListener(
                    HandleComplaintsPressed
                );
        }

        if (personalQuestionButton != null)
        {
            personalQuestionButton.onClick
                .RemoveListener(
                    HandlePersonalQuestionPressed
                );
        }
    }

    private void FindReferences()
    {
        if (dialogueManager == null)
        {
            dialogueManager =
                FindFirstObjectByType
                    <DialogueManager>(
                        FindObjectsInactive
                            .Include
                    );
        }
    }
}
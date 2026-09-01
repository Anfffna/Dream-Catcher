using UnityEngine;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NewsDialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string text;
    }

    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Header("Start Day")]
    public StartDay startDay;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string questUIManagerObjectName = "QuestUIManager";
    public string taskPanelControllerObjectName = "TaskPanel";

    [Header("Timing")]
    public double firstLineTime = 4.0;
    public double firstPauseTime = 9.0;
    public double secondPauseTime = 18.0;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Dialogue Lines")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("GG Dialogue After Stand")]
    public List<DialogueLine> standDialogueLines = new List<DialogueLine>();
    public string standDialogueColorHex = "#A997C9";

    [Header("Quest")]
    public QuestUIManager questUIManager;

    [Header("Task Panel")]
    public TaskPanelController taskPanelController;

    [Header("Typewriter")]
    public float lettersPerSecond = 35f;
    public bool hidePanelOnStart = true;
    public bool hidePanelOnEnd = true;

    [Header("Auto Close Last Line")]

    [Tooltip(
    "Через сколько секунд последняя реплика каждого диалога " +
    "сама пропускается. Пауза игры в это время не учитывается. " +
    "0 = отключено."
    )]
    [Min(0f)]
    [SerializeField]
    private float lastLineAutoCloseDelay = 5f;

    private int currentLineIndex = 0;

    public static bool NewsBlocksPause { get; private set; }
    private bool firstLineStarted = false;
    private bool firstPauseTriggered = false;
    private bool secondLineStarted = false;
    private bool secondPauseTriggered = false;
    private bool thirdLineStarted = false;
    private bool standDialogueStarted = false;
    private bool allDialoguesFinished = false;

    private bool dialogueActive = false;
    private bool isTyping = false;
    private bool waitingForClickToResume = false;

    private Coroutine typingCoroutine;
    private Coroutine lastLineAutoCloseCoroutine;

    void Start()
    {
        FindReferences();

        if (hidePanelOnStart && dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (dialogueText != null)
            dialogueText.text = "";
    }

    void Update()
    {
        CheckVideoTiming();
        CheckInput();
    }

    void CheckVideoTiming()
    {
        if (allDialoguesFinished) return;
        if (videoPlayer == null) return;
        if (!videoPlayer.isPlaying) return;

        double currentTime = videoPlayer.time;

        if (!firstLineStarted && currentTime >= firstLineTime)
        {
            firstLineStarted = true;
            StartLine(0);
        }

        if (!firstPauseTriggered && currentTime >= firstPauseTime)
        {
            firstPauseTriggered = true;
            PauseVideoAndWaitForClick();
        }

        if (!secondPauseTriggered && currentTime >= secondPauseTime)
        {
            secondPauseTriggered = true;
            PauseVideoAndWaitForClick();
        }
    }

    void CheckInput()
    {
        if (!dialogueActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                SkipTyping();
                return;
            }

            if (waitingForClickToResume)
            {
                ContinueAfterPausedDialogue();
                return;
            }

            if (currentLineIndex == 2 && !standDialogueStarted)
            {
                EndDialogue();

                StartCoroutine(StartStandDialogueDelayed());
                return;
            }

            // Скип третьей реплики сразу после стэнд позиции
            if (standDialogueStarted)
            {
                EndDialogue();
                allDialoguesFinished = true;
                NewsBlocksPause = false;

                FindReferences();

                if (taskPanelController != null)
                    taskPanelController.panelUnlocked = true;

                if (questUIManager != null)
                    questUIManager.AddQuest("turn_on_light");

                return;
            }
        }
    }

    void StartLine(int lineIndex)
    {
        if (dialogueLines == null || dialogueLines.Count <= lineIndex) return;

        NewsBlocksPause = true;

        currentLineIndex = lineIndex;
        dialogueActive = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowLine(dialogueLines[currentLineIndex].text);
    }

    void StartStandDialogue()
    {
        if (standDialogueLines == null || standDialogueLines.Count == 0) return;

        NewsBlocksPause = true;

        currentLineIndex = 0;
        standDialogueStarted = true;
        dialogueActive = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        // Сразу применяем цвет к тексту перед печатью
        ShowLineColored(standDialogueLines[currentLineIndex].text, standDialogueColorHex);
    }

    void PauseVideoAndWaitForClick()
    {
        if (videoPlayer != null)
            videoPlayer.Pause();

        waitingForClickToResume = true;

        if (isTyping)
            SkipTyping();
    }

    void ContinueAfterPausedDialogue()
    {
        waitingForClickToResume = false;

        if (currentLineIndex == 0 && !secondLineStarted)
        {
            secondLineStarted = true;

            if (videoPlayer != null)
                videoPlayer.Play();

            StartLine(1);
            return;
        }

        if (currentLineIndex == 1 && !thirdLineStarted)
        {
            thirdLineStarted = true;

            if (startDay != null)
                startDay.BeginStandUp();

            if (videoPlayer != null)
                videoPlayer.Play();

            StartLine(2);
            return;
        }

        if (!standDialogueStarted)
        {
            // Запуск реплик ГГ после стэнд позиции через 2 секунды
            StartCoroutine(StartStandDialogueDelayed());
            return;
        }

        EndDialogue();
        allDialoguesFinished = true;
        NewsBlocksPause = false;

        if (videoPlayer != null)
            videoPlayer.Play();
    }

    IEnumerator StartStandDialogueDelayed()
    {
        yield return new WaitForSeconds(2f);
        StartStandDialogue();
    }

    void ShowLine(string line)
    {
        StopLastLineAutoClose();
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    void ShowLineColored(string line, string hexColor)
    {
        StopLastLineAutoClose();
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // Сразу формируем строку с тегом цвета перед печатью
        string coloredLine = $"<color={hexColor}>{line}</color>";
        typingCoroutine = StartCoroutine(TypeLine(coloredLine));
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;

        // Сразу присваиваем весь текст с тегами
        dialogueText.text = line;

        // Скрываем текст по количеству символов
        dialogueText.maxVisibleCharacters = 0;

        float delay = lettersPerSecond <= 0f ? 0f : 1f / lettersPerSecond;

        for (int i = 1; i <= line.Length; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            else
                yield return null;
        }

        isTyping = false;
        typingCoroutine = null;
        TryStartLastLineAutoClose();
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (dialogueText != null)
        {
            string fullText;

            if (standDialogueStarted && standDialogueLines != null && standDialogueLines.Count > currentLineIndex)
            {
                fullText = $"<color={standDialogueColorHex}>{standDialogueLines[currentLineIndex].text}</color>";
            }
            else if (dialogueLines != null && dialogueLines.Count > currentLineIndex)
            {
                fullText = dialogueLines[currentLineIndex].text;
            }
            else
            {
                fullText = "";
            }

            dialogueText.text = fullText;
            dialogueText.maxVisibleCharacters = fullText.Length;
        }

        isTyping = false;
        typingCoroutine = null;
        TryStartLastLineAutoClose();
    }

    private void TryStartLastLineAutoClose()
    {
        StopLastLineAutoClose();

        if (!dialogueActive)
            return;

        if (lastLineAutoCloseDelay <= 0f)
            return;

        if (!IsCurrentLineLast())
            return;

        lastLineAutoCloseCoroutine =
            StartCoroutine(
                LastLineAutoCloseRoutine()
            );
    }


    private bool IsCurrentLineLast()
    {
        /*
         * Если сейчас идёт диалог после вставания —
         * смотрим последний элемент standDialogueLines.
         */
        if (standDialogueStarted)
        {
            return
                standDialogueLines != null &&
                standDialogueLines.Count > 0 &&
                currentLineIndex >=
                    standDialogueLines.Count - 1;
        }

        /*
         * Иначе смотрим последний элемент
         * обычного dialogueLines.
         */
        return
            dialogueLines != null &&
            dialogueLines.Count > 0 &&
            currentLineIndex >=
                dialogueLines.Count - 1;
    }


    private IEnumerator LastLineAutoCloseRoutine()
    {
        /*
         * WaitForSeconds использует scaled time.
         *
         * Поэтому при Time.timeScale = 0
         * эти 5 секунд не проходят.
         */
        yield return new WaitForSeconds(
            lastLineAutoCloseDelay
        );

        lastLineAutoCloseCoroutine = null;


        if (!dialogueActive ||
            isTyping ||
            !IsCurrentLineLast())
        {
            yield break;
        }


        // =====================================================
        // ПОСЛЕДНЯЯ РЕПЛИКА ОСНОВНЫХ НОВОСТЕЙ
        // =====================================================

        if (!standDialogueStarted)
        {
            EndDialogue();

            StartCoroutine(
                StartStandDialogueDelayed()
            );

            yield break;
        }


        // =====================================================
        // ПОСЛЕДНЯЯ РЕПЛИКА ПОСЛЕ ВСТАВАНИЯ
        // =====================================================

        EndDialogue();

        allDialoguesFinished = true;
        NewsBlocksPause = false;

        FindReferences();

        if (taskPanelController != null)
        {
            taskPanelController.panelUnlocked =
                true;
        }

        if (questUIManager != null)
        {
            questUIManager.AddQuest(
                "turn_on_light"
            );
        }
    }


    private void StopLastLineAutoClose()
    {
        if (lastLineAutoCloseCoroutine == null)
            return;

        StopCoroutine(
            lastLineAutoCloseCoroutine
        );

        lastLineAutoCloseCoroutine = null;
    }

    void EndDialogue()
    {
        StopLastLineAutoClose();
        dialogueActive = false;
        isTyping = false;
        waitingForClickToResume = false;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;

        if (dialogueText != null)
            dialogueText.text = "";

        if (hidePanelOnEnd && dialoguePanel != null)
            dialoguePanel.SetActive(false);
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

        // TaskPanelController
        if (taskPanelController == null)
        {
            GameObject obj = GameObject.Find(taskPanelControllerObjectName);

            if (obj != null)
                taskPanelController = obj.GetComponent<TaskPanelController>();
        }

        if (taskPanelController == null)
            taskPanelController = FindObjectOfType<TaskPanelController>();
    }

    private void OnDisable()
    {
        NewsBlocksPause = false;
    }

    private void OnDestroy()
    {
        StopLastLineAutoClose();
        NewsBlocksPause = false;
    }
}
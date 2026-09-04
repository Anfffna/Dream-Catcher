using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager :
    MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string text;

        [Header("Color")]
        public bool useCustomColor = false;
        public string colorHex = "#A997C9";

        [Header("Voice Audio")]
        [Tooltip("Если включено, во время этой реплики будет играть зацикленная озвучка.")]
        public bool useAudio = false;

        [Tooltip("Отдельный AudioSource реплики. Для данных клиента обычно оставляется пустым.")]
        public AudioSource voiceAudioSource;

        [Header("After Click Pause")]
        [Tooltip("Задержка после клика перед следующей репликой.")]
        public float clickPauseDelay = 0f;
    }

    private enum DialogueRunMode
    {
        Normal,
        KeepLastLineVisible,
        ChoicePrompt
    }

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Dialogue Settings")]
    public List<DialogueLine> dialogueLines =
        new List<DialogueLine>();

    [Header("Auto Close Last Line")]

    [Tooltip(
    "Через сколько секунд последняя реплика обычного диалога " +
    "сама закроется. Время во время паузы игры не учитывается. " +
    "0 = отключено."
    )]
    [Min(0f)]
    [SerializeField]
    private float lastLineAutoCloseDelay = 8f;

    public float lettersPerSecond = 35f;
    public bool hidePanelOnStart = true;
    public bool hidePanelOnEnd = true;

    [Header("Voice Settings")]
    [Tooltip("Источник голоса, используемый, если в реплике отдельный источник не назначен.")]
    public AudioSource defaultVoiceAudioSource;

    [Tooltip("Принудительно включать зацикливание используемого голоса.")]
    public bool forceVoiceLoop = true;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string playerObjectName = "Player";

    private int currentLineIndex;
    private bool isTyping;
    private bool dialogueActive;

    public static int ActiveDialogueCount
    {
        get;
        private set;
    }

    public static bool AnyDialogueActive =>
        ActiveDialogueCount > 0;

    public int CurrentLineIndex =>
        currentLineIndex;

    public bool DialogueActive =>
        dialogueActive;

    public bool IsTyping =>
        isTyping;

    public bool ChoicePromptReady =>
        dialogueActive &&
        currentRunMode ==
            DialogueRunMode.ChoicePrompt &&
        choicePromptReady;

    private bool registeredAsActiveDialogue;
    private bool waitingForClickAfterTyping;
    private bool isWaitingForClickPause;
    private bool skipProtection;
    private bool choicePromptReady;

    private Coroutine typingCoroutine;
    private Coroutine pauseCoroutine;
    private Coroutine lastLineAutoCloseCoroutine;

    private PlayerController playerController;

    private bool wasMovementLocked;
    private bool originalCanMoveState;
    private bool blockMovementForCurrentDialogue;

    private AudioSource currentVoiceAudioSource;

    private readonly List<AudioSource>
        pausedByDialogueSources =
            new List<AudioSource>();

    private DialogueRunMode currentRunMode =
        DialogueRunMode.Normal;

    private void Start()
    {
        FindReferences();

        if (hidePanelOnStart &&
            dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
    }

    private void Update()
    {
        if (!dialogueActive)
            return;

        if (isWaitingForClickPause)
            return;

        if (skipProtection)
            return;

        bool skipPressed =
            Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Space);

        if (!skipPressed)
            return;

        // Исходная реплика меню вопросов
        // всегда печатается полностью.
        if (currentRunMode ==
            DialogueRunMode.ChoicePrompt)
        {
            return;
        }

        if (isTyping)
        {
            SkipTyping();
            return;
        }

        if (waitingForClickAfterTyping)
        {
            StartClickPauseAndNext();
        }
    }

    public void StartDialogue()
    {
        StartDialogue(
            dialogueLines,
            false
        );
    }

    public void StartDialogue(
        List<DialogueLine> lines,
        bool blockMovement = false)
    {
        StartDialogueInternal(
            lines,
            blockMovement,
            DialogueRunMode.Normal
        );
    }

    public void StartDialogue(
        List<DialogueLine> lines,
        bool blockMovement,
        bool keepLastLineVisible)
    {
        DialogueRunMode runMode =
            keepLastLineVisible
                ? DialogueRunMode
                    .KeepLastLineVisible
                : DialogueRunMode.Normal;

        StartDialogueInternal(
            lines,
            blockMovement,
            runMode
        );
    }

    public void ShowChoicePrompt(
        DialogueLine line,
        bool blockMovement = false)
    {
        if (line == null)
            return;

        List<DialogueLine> promptLines =
            new List<DialogueLine>
            {
                line
            };

        StartDialogueInternal(
            promptLines,
            blockMovement,
            DialogueRunMode.ChoicePrompt
        );
    }

    public void FinishChoicePrompt(
        bool keepPanelVisible)
    {
        if (!dialogueActive ||
            currentRunMode !=
            DialogueRunMode.ChoicePrompt)
        {
            return;
        }

        EndDialogueInternal(
            keepPanelVisible
        );
    }

    public void HidePersistentDialogue()
    {
        if (dialogueActive)
        {
            if (currentRunMode ==
                DialogueRunMode.ChoicePrompt)
            {
                FinishChoicePrompt(false);
            }
            else
            {
                return;
            }
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters =
                99999;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void StartDialogueInternal(
        List<DialogueLine> lines,
        bool blockMovement,
        DialogueRunMode runMode)
    {
        FindReferences();

        if (dialogueActive)
            return;

        if (lines == null ||
            lines.Count == 0)
        {
            return;
        }

        PauseCurrentVoiceAudio();

        dialogueLines = lines;
        currentRunMode = runMode;
        choicePromptReady = false;

        bool playerIsSeated =
            WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsSeated;

        blockMovementForCurrentDialogue =
            blockMovement &&
            !playerIsSeated;

        currentLineIndex = 0;
        dialogueActive = true;

        RegisterActiveDialogue();

        if (blockMovementForCurrentDialogue &&
            playerController != null)
        {
            originalCanMoveState =
                playerController.canMove;

            playerController
                .SetMovementEnabled(false);

            wasMovementLocked = true;
        }

        waitingForClickAfterTyping = false;

        ShowDialoguePanel(true);

        ShowLine(
            dialogueLines[currentLineIndex]
        );
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >=
            dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        waitingForClickAfterTyping = false;

        ShowDialoguePanel(true);

        ShowLine(
            dialogueLines[currentLineIndex]
        );
    }

    private void ShowLine(
        DialogueLine line)
    {
        StopLastLineAutoClose();

        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );
        }

        if (pauseCoroutine != null)
        {
            StopCoroutine(
                pauseCoroutine
            );
        }

        isWaitingForClickPause = false;
        waitingForClickAfterTyping = false;
        skipProtection = false;
        choicePromptReady = false;

        HandleVoiceForLine(line);

        string finalText =
            GetFinalText(line);

        typingCoroutine =
            StartCoroutine(
                TypeLine(finalText)
            );
    }

    private string GetFinalText(
        DialogueLine line)
    {
        if (line == null)
            return "";

        if (line.useCustomColor)
        {
            return
                "<color=" +
                line.colorHex +
                ">" +
                line.text +
                "</color>";
        }

        return line.text;
    }

    private IEnumerator TypeLine(
        string line)
    {
        isTyping = true;

        if (dialogueText != null)
        {
            dialogueText.text = line;
            dialogueText.maxVisibleCharacters =
                0;
        }

        float delay =
            lettersPerSecond <= 0f
                ? 0f
                : 1f / lettersPerSecond;

        for (int i = 1;
             i <= line.Length;
             i++)
        {
            if (dialogueText != null)
            {
                dialogueText
                    .maxVisibleCharacters = i;
            }

            if (delay > 0f)
            {
                yield return
                    new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }
        }

        isTyping = false;
        typingCoroutine = null;

        bool isLastLine =
            currentLineIndex >=
            dialogueLines.Count - 1;

        if (currentRunMode ==
                DialogueRunMode.ChoicePrompt &&
            isLastLine)
        {
            choicePromptReady = true;
            waitingForClickAfterTyping = false;

            // После печати меню вопросов уже не блокирует 3D-клик по самому клиенту.
            UnregisterActiveDialogue();

            yield break;
        }

        waitingForClickAfterTyping = true;

        if (isLastLine)
        {
            StartLastLineAutoClose();
        }
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );
        }

        if (dialogueText != null &&
            dialogueLines != null &&
            dialogueLines.Count >
                currentLineIndex)
        {
            string fullText =
                GetFinalText(
                    dialogueLines[
                        currentLineIndex
                    ]
                );

            dialogueText.text = fullText;

            dialogueText.maxVisibleCharacters =
                fullText.Length;
        }

        PauseCurrentVoiceAudio();

        isTyping = false;
        typingCoroutine = null;

        bool isLastLine =
            currentLineIndex >=
            dialogueLines.Count - 1;

        if (currentRunMode ==
                DialogueRunMode.ChoicePrompt &&
            isLastLine)
        {
            choicePromptReady = true;
            waitingForClickAfterTyping = false;

            UnregisterActiveDialogue();
        }

        else
        {
            waitingForClickAfterTyping = true;

            if (isLastLine)
            {
                StartLastLineAutoClose();
            }
        }

        if (!skipProtection)
        {
            StartCoroutine(
                SkipProtectionRoutine()
            );
        }
    }

    private IEnumerator SkipProtectionRoutine()
    {
        skipProtection = true;

        yield return
            new WaitForSeconds(0.2f);

        skipProtection = false;
    }

    private void StartClickPauseAndNext()
    {
        StopLastLineAutoClose();
        PauseCurrentVoiceAudio();

        float delay =
            dialogueLines[
                currentLineIndex
            ].clickPauseDelay;

        waitingForClickAfterTyping = false;

        if (delay > 0f)
        {
            isWaitingForClickPause = true;

            ShowDialoguePanel(false);

            pauseCoroutine =
                StartCoroutine(
                    ClickPauseRoutine(delay)
                );
        }
        else
        {
            ShowNextLine();
        }
    }

    private IEnumerator ClickPauseRoutine(
        float delay)
    {
        yield return
            new WaitForSeconds(delay);

        isWaitingForClickPause = false;
        pauseCoroutine = null;

        ShowNextLine();
    }

    private void ShowDialoguePanel(
        bool show)
    {
        if (!show)
        {
            PauseCurrentVoiceAudio();
        }

        if (dialoguePanel != null &&
            dialoguePanel.activeSelf != show)
        {
            dialoguePanel.SetActive(show);
        }
    }

    private void HandleVoiceForLine(
        DialogueLine line)
    {
        if (line == null ||
            !line.useAudio)
        {
            PauseCurrentVoiceAudio();
            return;
        }

        AudioSource source =
            line.voiceAudioSource;

        if (source == null)
        {
            source =
                defaultVoiceAudioSource;
        }

        if (source == null ||
            source.clip == null)
        {
            PauseCurrentVoiceAudio();
            return;
        }

        if (currentVoiceAudioSource != null &&
            currentVoiceAudioSource != source)
        {
            PauseVoiceAudio(
                currentVoiceAudioSource
            );
        }

        currentVoiceAudioSource =
            source;

        if (forceVoiceLoop)
        {
            currentVoiceAudioSource.loop =
                true;
        }

        PlayOrResumeVoiceAudio(
            currentVoiceAudioSource
        );
    }

    private void PlayOrResumeVoiceAudio(
        AudioSource source)
    {
        if (source == null ||
            source.clip == null ||
            source.isPlaying)
        {
            return;
        }

        if (pausedByDialogueSources
            .Contains(source))
        {
            source.UnPause();

            pausedByDialogueSources
                .Remove(source);
        }
        else
        {
            source.Play();
        }
    }

    private void PauseCurrentVoiceAudio()
    {
        if (currentVoiceAudioSource == null)
            return;

        PauseVoiceAudio(
            currentVoiceAudioSource
        );
    }

    private void PauseVoiceAudio(
        AudioSource source)
    {
        if (source == null ||
            source.clip == null)
        {
            return;
        }

        if (!source.isPlaying)
            return;

        source.Pause();

        if (!pausedByDialogueSources
            .Contains(source))
        {
            pausedByDialogueSources
                .Add(source);
        }
    }

    private void RegisterActiveDialogue()
    {
        if (registeredAsActiveDialogue)
            return;

        registeredAsActiveDialogue = true;
        ActiveDialogueCount++;
    }

    private void UnregisterActiveDialogue()
    {
        if (!registeredAsActiveDialogue)
            return;

        registeredAsActiveDialogue = false;

        ActiveDialogueCount =
            Mathf.Max(
                0,
                ActiveDialogueCount - 1
            );
    }

    private void StartLastLineAutoClose()
    {
        StopLastLineAutoClose();

        if (lastLineAutoCloseDelay <= 0f)
            return;

        /*
         * Автозакрытие только для обычных диалогов.
         *
         * ChoicePrompt должен ждать выбора игрока.
         * KeepLastLineVisible специально оставляет
         * последнюю реплику на экране.
         */
        if (currentRunMode != DialogueRunMode.Normal)
            return;

        if (dialogueLines == null ||
            currentLineIndex <
                dialogueLines.Count - 1)
        {
            return;
        }

        lastLineAutoCloseCoroutine =
            StartCoroutine(
                LastLineAutoCloseRoutine()
            );
    }


    private IEnumerator LastLineAutoCloseRoutine()
    {
        float elapsed = 0f;

        while (elapsed <
               lastLineAutoCloseDelay)
        {
            /*
             * Если диалог уже закончился
             * или игрок сам переключил реплику —
             * таймер больше не нужен.
             */
            if (!dialogueActive ||
                !waitingForClickAfterTyping)
            {
                lastLineAutoCloseCoroutine =
                    null;

                yield break;
            }

            /*
             * Time.deltaTime зависит от Time.timeScale.
             *
             * Поэтому при обычной игровой паузе
             * с Time.timeScale = 0
             * эти 8 секунд НЕ идут.
             */
            elapsed += Time.deltaTime;

            yield return null;
        }


        lastLineAutoCloseCoroutine = null;


        if (!dialogueActive ||
            !waitingForClickAfterTyping)
        {
            yield break;
        }


        waitingForClickAfterTyping = false;

        /*
         * Это настоящая последняя реплика списка,
         * поэтому просто заканчиваем диалог.
         */
        EndDialogue();
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

    private void EndDialogue()
    {
        bool keepPanelVisible =
            currentRunMode ==
            DialogueRunMode
                .KeepLastLineVisible;

        EndDialogueInternal(
            keepPanelVisible
        );
    }

    private void EndDialogueInternal(
        bool keepPanelVisible)
    {
        StopLastLineAutoClose();
        PauseCurrentVoiceAudio();

        dialogueActive = false;

        UnregisterActiveDialogue();

        if (wasMovementLocked &&
            playerController != null)
        {
            playerController
                .SetMovementEnabled(
                    originalCanMoveState
                );

            wasMovementLocked = false;
        }

        blockMovementForCurrentDialogue =
            false;

        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );
        }

        if (pauseCoroutine != null)
        {
            StopCoroutine(
                pauseCoroutine
            );
        }

        pauseCoroutine = null;
        typingCoroutine = null;

        isTyping = false;
        isWaitingForClickPause = false;
        waitingForClickAfterTyping = false;
        skipProtection = false;
        choicePromptReady = false;

        if (!keepPanelVisible)
        {
            if (dialogueText != null)
            {
                dialogueText.text = "";

                dialogueText
                    .maxVisibleCharacters =
                    99999;
            }

            if (hidePanelOnEnd &&
                dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        currentRunMode =
            DialogueRunMode.Normal;

        RestoreWorkState();
    }

    private void RestoreWorkState()
    {
        if (WorkSessionManager.Instance == null ||
            !WorkSessionManager.Instance.IsSeated)
        {
            return;
        }

        if (WorkSessionManager.Instance
            .seatController != null)
        {
            WorkSessionManager.Instance
                .seatController
                .RestoreWorkControlAfterPause();
        }

        if (WorkSessionManager.Instance
            .cursorController != null)
        {
            WorkSessionManager.Instance
                .cursorController
                .ShowWorkCursor();
        }
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (playerController == null)
        {
            GameObject playerObject =
                GameObject.Find(
                    playerObjectName
                );

            if (playerObject != null)
            {
                playerController =
                    playerObject
                        .GetComponent
                            <PlayerController>();
            }
        }

        if (playerController == null)
        {
            playerController =
                FindFirstObjectByType
                    <PlayerController>(
                        FindObjectsInactive
                            .Include
                    );
        }
    }

    private void OnDisable()
    {
        StopLastLineAutoClose();
        PauseCurrentVoiceAudio();

        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );
        }

        if (pauseCoroutine != null)
        {
            StopCoroutine(
                pauseCoroutine
            );
        }

        typingCoroutine = null;
        pauseCoroutine = null;
        dialogueActive = false;
        choicePromptReady = false;

        UnregisterActiveDialogue();
    }

    private void OnDestroy()
    {
        PauseCurrentVoiceAudio();

        if (dialogueActive)
        {
            dialogueActive = false;
        }

        UnregisterActiveDialogue();
    }
}
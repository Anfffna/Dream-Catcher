using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
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

        [Tooltip("AudioSource с бормотанием персонажа. Можно назначать только на тех репликах, где Use Audio включён.")]
        public AudioSource voiceAudioSource;

        [Header("After Click Pause")]
        [Tooltip("Задержка в секундах после клика (диалоговое окно исчезнет), затем появится следующая реплика. 0 = сразу. Клики во время паузы не работают.")]
        public float clickPauseDelay = 0f;
    }

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Dialogue Settings")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
    public float lettersPerSecond = 35f;
    public bool hidePanelOnStart = true;
    public bool hidePanelOnEnd = true;

    [Header("Voice Settings")]
    [Tooltip("Если у реплики Use Audio включён, но Voice Audio Source не назначен, будет использован этот AudioSource.")]
    public AudioSource defaultVoiceAudioSource;

    [Tooltip("Если включено, DialogueManager принудительно ставит loop = true на используемый AudioSource.")]
    public bool forceVoiceLoop = true;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string playerObjectName = "Player";

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;

    public static int ActiveDialogueCount { get; private set; }
    public static bool AnyDialogueActive => ActiveDialogueCount > 0;
    public int CurrentLineIndex => currentLineIndex;

    private bool registeredAsActiveDialogue = false;
    private bool waitingForClickAfterTyping = false;
    private Coroutine typingCoroutine;
    private Coroutine pauseCoroutine;
    private bool isWaitingForClickPause = false;
    private bool skipProtection = false;

    private PlayerController playerController;
    private bool wasMovementLocked = false;
    private bool originalCanMoveState = false;
    private bool blockMovementForCurrentDialogue = false;

    private AudioSource currentVoiceAudioSource;
    private readonly List<AudioSource> pausedByDialogueSources = new List<AudioSource>();

    public bool DialogueActive => dialogueActive;

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
        if (!dialogueActive) return;
        if (isWaitingForClickPause) return;
        if (skipProtection) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Если идёт печать — пропускаем печать и прерываем озвучку этой реплики.
            if (isTyping)
            {
                SkipTyping();
                return;
            }

            // Если текст полностью показан и ждём клик — запускаем паузу/следующую реплику.
            if (waitingForClickAfterTyping)
            {
                StartClickPauseAndNext();
            }
        }
    }

    public void StartDialogue()
    {
        StartDialogue(dialogueLines, false);
    }

    public void StartDialogue(List<DialogueLine> lines, bool blockMovement = false)
    {
        FindReferences();

        PauseCurrentVoiceAudio();

        dialogueLines = lines;
        /*Во время работы игрок уже сидит:
         * Поэтому DialogueManager не должен повторно блокировать управление через SetMovementEnabled*/
        bool playerIsSeated =
            WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsSeated;

        blockMovementForCurrentDialogue =
            blockMovement &&
            !playerIsSeated;

        if (dialogueLines == null || dialogueLines.Count == 0) return;

        currentLineIndex = 0;
        dialogueActive = true;
        RegisterActiveDialogue();

        // Блокируем движение, если требуется.
        if (blockMovementForCurrentDialogue && playerController != null)
        {
            originalCanMoveState = playerController.canMove;
            playerController.SetMovementEnabled(false);
            wasMovementLocked = true;
        }

        waitingForClickAfterTyping = false;
        ShowDialoguePanel(true);
        ShowLine(dialogueLines[currentLineIndex]);
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        waitingForClickAfterTyping = false;
        ShowDialoguePanel(true);
        ShowLine(dialogueLines[currentLineIndex]);
    }

    private void ShowLine(DialogueLine line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (pauseCoroutine != null)
            StopCoroutine(pauseCoroutine);

        isWaitingForClickPause = false;
        waitingForClickAfterTyping = false;
        skipProtection = false;

        HandleVoiceForLine(line);

        string finalText = GetFinalText(line);
        typingCoroutine = StartCoroutine(TypeLine(finalText));
    }

    private string GetFinalText(DialogueLine line)
    {
        if (line == null) return "";

        if (line.useCustomColor)
            return "<color=" + line.colorHex + ">" + line.text + "</color>";

        return line.text;
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = line;
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

        // Печать завершена, теперь ждём клик игрока.
        // Озвучку НЕ останавливаем тут: она продолжает бормотать, пока игрок не кликнет.
        waitingForClickAfterTyping = true;
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (dialogueText != null && dialogueLines != null && dialogueLines.Count > currentLineIndex)
        {
            string fullText = GetFinalText(dialogueLines[currentLineIndex]);
            dialogueText.text = fullText;
            dialogueText.maxVisibleCharacters = fullText.Length;
        }

        // При скипе печати озвучка этой реплики прерывается/ставится на паузу.
        PauseCurrentVoiceAudio();

        isTyping = false;
        typingCoroutine = null;
        waitingForClickAfterTyping = true;

        // Защита от двойного клика на 0.2 секунды.
        if (!skipProtection)
            StartCoroutine(SkipProtectionRoutine());
    }

    private IEnumerator SkipProtectionRoutine()
    {
        skipProtection = true;
        yield return new WaitForSeconds(0.2f);
        skipProtection = false;
    }

    private void StartClickPauseAndNext()
    {
        // При переходе с реплики озвучка прерывается.
        PauseCurrentVoiceAudio();

        float delay = dialogueLines[currentLineIndex].clickPauseDelay;
        waitingForClickAfterTyping = false;

        if (delay > 0f)
        {
            isWaitingForClickPause = true;
            ShowDialoguePanel(false);
            pauseCoroutine = StartCoroutine(ClickPauseRoutine(delay));
        }
        else
        {
            ShowNextLine();
        }
    }

    private IEnumerator ClickPauseRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        isWaitingForClickPause = false;
        pauseCoroutine = null;

        ShowNextLine();
    }

    private void ShowDialoguePanel(bool show)
    {
        if (!show)
            PauseCurrentVoiceAudio();

        if (dialoguePanel != null && dialoguePanel.activeSelf != show)
            dialoguePanel.SetActive(show);
    }

    private void HandleVoiceForLine(DialogueLine line)
    {
        if (line == null)
        {
            PauseCurrentVoiceAudio();
            return;
        }

        if (!line.useAudio)
        {
            PauseCurrentVoiceAudio();
            return;
        }

        AudioSource source = line.voiceAudioSource;

        if (source == null)
            source = defaultVoiceAudioSource;

        if (source == null)
        {
            PauseCurrentVoiceAudio();
            return;
        }

        if (source.clip == null)
        {
            PauseCurrentVoiceAudio();
            return;
        }

        if (currentVoiceAudioSource != null && currentVoiceAudioSource != source)
            PauseVoiceAudio(currentVoiceAudioSource);

        currentVoiceAudioSource = source;

        if (forceVoiceLoop)
            currentVoiceAudioSource.loop = true;

        PlayOrResumeVoiceAudio(currentVoiceAudioSource);
    }

    private void PlayOrResumeVoiceAudio(AudioSource source)
    {
        if (source == null)
            return;

        if (source.clip == null)
            return;

        if (source.isPlaying)
            return;

        if (pausedByDialogueSources.Contains(source))
        {
            source.UnPause();
            pausedByDialogueSources.Remove(source);
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

        PauseVoiceAudio(currentVoiceAudioSource);
    }

    private void PauseVoiceAudio(AudioSource source)
    {
        if (source == null)
            return;

        if (source.clip == null)
            return;

        if (source.isPlaying)
        {
            source.Pause();

            if (!pausedByDialogueSources.Contains(source))
                pausedByDialogueSources.Add(source);
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
        ActiveDialogueCount = Mathf.Max(0, ActiveDialogueCount - 1);
    }

    private void EndDialogue()
    {
        PauseCurrentVoiceAudio();

        dialogueActive = false;
        UnregisterActiveDialogue();

        if (wasMovementLocked && playerController != null)
        {
            playerController.SetMovementEnabled(originalCanMoveState);
            wasMovementLocked = false;
        }

        blockMovementForCurrentDialogue = false;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (pauseCoroutine != null)
            StopCoroutine(pauseCoroutine);

        pauseCoroutine = null;
        isWaitingForClickPause = false;
        waitingForClickAfterTyping = false;
        skipProtection = false;
        typingCoroutine = null;

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 99999;
        }

        if (hidePanelOnEnd && dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsSeated)
        {
            if (WorkSessionManager.Instance.seatController != null)
            {
                WorkSessionManager.Instance
                    .seatController
                    .RestoreWorkControlAfterPause();
            }

            if (WorkSessionManager.Instance.cursorController != null)
            {
                WorkSessionManager.Instance
                    .cursorController
                    .ShowWorkCursor();
            }
        }
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (playerController == null)
        {
            GameObject obj = GameObject.Find(playerObjectName);

            if (obj != null)
                playerController = obj.GetComponent<PlayerController>();
        }

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
    }

    private void OnDisable()
    {
        PauseCurrentVoiceAudio();

        if (dialogueActive)
        {
            dialogueActive = false;
            UnregisterActiveDialogue();
        }
    }

    private void OnDestroy()
    {
        PauseCurrentVoiceAudio();

        if (dialogueActive)
        {
            dialogueActive = false;
            UnregisterActiveDialogue();
        }
    }
}
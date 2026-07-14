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

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string playerObjectName = "Player";

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;
    public static int ActiveDialogueCount { get; private set; }
    public static bool AnyDialogueActive => ActiveDialogueCount > 0;
    private bool registeredAsActiveDialogue = false;
    private bool waitingForClickAfterTyping = false; // текст полностью показан, ждём клик для паузы
    private Coroutine typingCoroutine;
    private Coroutine pauseCoroutine;
    private bool isWaitingForClickPause = false;
    private bool skipProtection = false; // защита от двойного клика при пропуске печати
    private PlayerController playerController;
    private bool wasMovementLocked = false;
    private bool originalCanMoveState = false;
    private bool blockMovementForCurrentDialogue = false;

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
        if (isWaitingForClickPause) return; // во время паузы клики игнорируем
        if (skipProtection) return; // защита после скипа: кратковременно не принимаем клики

        if (Input.GetMouseButtonDown(0))
        {
            // Если идёт печать – пропускаем
            if (isTyping)
            {
                SkipTyping();
                return;
            }

            // Если текст полностью показан и ждём клик – запускаем паузу
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

        dialogueLines = lines;
        blockMovementForCurrentDialogue = blockMovement;

        if (dialogueLines == null || dialogueLines.Count == 0) return;

        currentLineIndex = 0;
        dialogueActive = true;
        RegisterActiveDialogue();

        // Блокируем движение, если требуется
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
        // Печать завершена, теперь ждём клик игрока для паузы
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

        isTyping = false;
        typingCoroutine = null;
        waitingForClickAfterTyping = true;

        // Включаем защиту от двойного клика на 0.2 секунды
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
        if (dialoguePanel != null && dialoguePanel.activeSelf != show)
            dialoguePanel.SetActive(show);
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
        if (dialogueActive)
        {
            dialogueActive = false;
            UnregisterActiveDialogue();
        }
    }

    private void OnDestroy()
    {
        if (dialogueActive)
        {
            dialogueActive = false;
            UnregisterActiveDialogue();
        }
    }
}
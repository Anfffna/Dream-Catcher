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
    }

    [Header("UI Elements")]
    public GameObject dialoguePanel;           // Ваша плашка PNG
    public TextMeshProUGUI dialogueText;       // Текст внутри плашки

    [Header("Dialogue Settings")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
    public float lettersPerSecond = 35f;
    public bool hidePanelOnStart = true;
    public bool hidePanelOnEnd = true;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private Coroutine typingCoroutine;

    void Start()
    {
        if (hidePanelOnStart && dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (dialogueText != null)
            dialogueText.text = "";
    }

    void Update()
    {
        if (!dialogueActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    /// <summary>
    /// Запускает диалог из инспектора
    /// </summary>
    public void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Count == 0) return;

        currentLineIndex = 0;
        dialogueActive = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowLine(dialogueLines[currentLineIndex].text);
    }

    /// <summary>
    /// Запуск диалога с новым списком реплик
    /// </summary>
    public void StartDialogue(List<DialogueLine> lines)
    {
        dialogueLines = lines;
        StartDialogue();
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        ShowLine(dialogueLines[currentLineIndex].text);
    }

    private void ShowLine(string line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        float delay = lettersPerSecond <= 0f ? 0f : 1f / lettersPerSecond;

        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            else
                yield return null;
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = dialogueLines[currentLineIndex].text;
        isTyping = false;
        typingCoroutine = null;
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        isTyping = false;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;

        if (dialogueText != null)
            dialogueText.text = "";

        if (hidePanelOnEnd && dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}
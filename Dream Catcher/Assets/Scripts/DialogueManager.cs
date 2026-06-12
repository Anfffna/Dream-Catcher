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
    }

    [Header("UI Elements")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Dialogue Settings")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
    public float lettersPerSecond = 35f;
    public bool hidePanelOnStart = true;
    public bool hidePanelOnEnd = true;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private Coroutine typingCoroutine;

    public bool DialogueActive => dialogueActive;

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

    public void StartDialogue()
    {
        if (dialogueLines == null || dialogueLines.Count == 0) return;

        currentLineIndex = 0;
        dialogueActive = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowLine(dialogueLines[currentLineIndex]);
    }

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

        ShowLine(dialogueLines[currentLineIndex]);
    }

    private void ShowLine(DialogueLine line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

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
    }

    private void EndDialogue()
    {
        dialogueActive = false;
        isTyping = false;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.maxVisibleCharacters = 99999;
        }

        if (hidePanelOnEnd && dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}
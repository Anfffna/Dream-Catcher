using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TalkNeighbour : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> dialogueLines;

    private bool hasTalked = false;

    void Start()
    {
        // Устанавливаем слой Interactable (если ещё не)
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    public void Interact()
    {
        if (hasTalked) return;

        if (dialogueManager != null && dialogueLines != null && dialogueLines.Count > 0)
        {
            dialogueManager.StartDialogue(dialogueLines);
            StartCoroutine(AfterDialogue());
        }
        else
        {
            Debug.LogWarning($"На {gameObject.name} не заданы диалоговые строки или DialogueManager");
        }
    }

    private IEnumerator AfterDialogue()
    {
        // Ждём, пока диалог не закончится
        while (dialogueManager.DialogueActive)
            yield return null;

        hasTalked = true;
        // Меняем слой на Default, чтобы больше нельзя было взаимодействовать
        gameObject.layer = LayerMask.NameToLayer("Default");
    }
}
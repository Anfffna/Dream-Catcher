using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(InteractionOutline))]
public class ItemInteraction : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> dialogueLines;

    private InteractionOutline outline;
    private bool outlineHidden = false;
    private static FirstInteractionHint hintManager;

    void Start()
    {
        outline = GetComponent<InteractionOutline>();
        if (hintManager == null)
            hintManager = FindObjectOfType<FirstInteractionHint>();
    }

    public void Interact()
    {
        if (dialogueManager != null && dialogueLines != null && dialogueLines.Count > 0)
        {
            // Запускаем диалог
            dialogueManager.StartDialogue(dialogueLines);

            // Скрываем обводку при первом взаимодействии
            if (!outlineHidden && outline != null)
            {
                outline.HideOutline();
                outlineHidden = true;
            }

            // Запускаем ожидание окончания диалога, затем показываем подсказку
            StartCoroutine(ShowHintAfterDialogue());
        }
        else
        {
            Debug.LogWarning($"На {gameObject.name} не заданы диалоговые строки или DialogueManager", this);
        }
    }

    private IEnumerator ShowHintAfterDialogue()
    {
        // Ждём, пока диалог не завершится
        while (dialogueManager.DialogueActive)
            yield return null;

        // Показываем подсказку (только один раз за всю игру, благодаря статическому флагу)
        if (hintManager != null)
            hintManager.TryShowHint();
    }
}
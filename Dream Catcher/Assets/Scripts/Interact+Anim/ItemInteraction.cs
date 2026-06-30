using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(InteractionOutline))]
public class ItemInteraction : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> dialogueLines;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string dialogueManagerObjectName = "DialogueManager";

    private InteractionOutline outline;
    private bool outlineHidden = false;
    private static FirstInteractionHint hintManager;

    void Start()
    {
        outline = GetComponent<InteractionOutline>();

        FindReferences();

        if (hintManager == null)
            hintManager = FindObjectOfType<FirstInteractionHint>();
    }

    public void Interact()
    {
        FindReferences();

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
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        // Показываем подсказку
        if (hintManager != null)
            hintManager.TryShowHint();
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        // DialogueManager — ВАЖНО: ищем именно обычный DialogueManager, не LoadingDialogueManager
        if (dialogueManager == null || dialogueManager.gameObject.name != dialogueManagerObjectName)
        {
            GameObject obj = GameObject.Find(dialogueManagerObjectName);

            if (obj != null)
                dialogueManager = obj.GetComponent<DialogueManager>();
        }

        // Запасной вариант: среди всех DialogueManager берём только тот, у которого имя DialogueManager
        if (dialogueManager == null)
        {
            DialogueManager[] managers = FindObjectsOfType<DialogueManager>();

            foreach (DialogueManager manager in managers)
            {
                if (manager.gameObject.name == dialogueManagerObjectName)
                {
                    dialogueManager = manager;
                    break;
                }
            }
        }
    }
}
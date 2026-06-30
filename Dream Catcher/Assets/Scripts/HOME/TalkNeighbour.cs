using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TalkNeighbour : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> dialogueLines;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string dialogueManagerObjectName = "DialogueManager";

    private bool hasTalked = false;

    void Start()
    {
        FindReferences();
        // Устанавливаем слой Interactable (если ещё не)
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    public void Interact()
    {
        FindReferences();
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

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        // Сначала ищем строго объект с именем DialogueManager
        if (dialogueManager == null || dialogueManager.gameObject.name != dialogueManagerObjectName)
        {
            GameObject obj = GameObject.Find(dialogueManagerObjectName);

            if (obj != null)
                dialogueManager = obj.GetComponent<DialogueManager>();
        }

        // Запасной вариант: ищем среди всех DialogueManager именно тот, у которого имя DialogueManager
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
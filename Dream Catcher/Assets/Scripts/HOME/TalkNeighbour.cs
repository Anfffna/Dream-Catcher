using UnityEngine;
using UnityEngine.UI;
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
    public string interactionDotObjectName = "InteractionDot";

    private bool hasTalked = false;
    private Image interactionDot;

    void Start()
    {
        FindReferences();
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    public void Interact()
    {
        FindReferences();

        if (hasTalked) return;
        if (dialogueManager == null) return;
        if (dialogueManager.DialogueActive) return;

        if (dialogueLines != null && dialogueLines.Count > 0)
        {
            HideInteractionDot();

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
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        ShowInteractionDot();

        hasTalked = true;
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void HideInteractionDot()
    {
        FindReferences();

        if (interactionDot != null)
            interactionDot.enabled = false;
    }

    private void ShowInteractionDot()
    {
        FindReferences();

        if (interactionDot != null)
            interactionDot.enabled = true;
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (dialogueManager == null || dialogueManager.gameObject.name != dialogueManagerObjectName)
        {
            GameObject obj = GameObject.Find(dialogueManagerObjectName);

            if (obj != null)
                dialogueManager = obj.GetComponent<DialogueManager>();
        }

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

        if (interactionDot == null)
        {
            GameObject obj = GameObject.Find(interactionDotObjectName);

            if (obj != null)
                interactionDot = obj.GetComponent<Image>();
        }
    }
}
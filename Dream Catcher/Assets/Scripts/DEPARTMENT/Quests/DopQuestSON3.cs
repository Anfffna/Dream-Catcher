using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DopQuestSON3 : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> dialogueLines = new List<DialogueManager.DialogueLine>();
    public bool blockMovementDuringDialogue = true;

    [Header("Quest")]
    public QuestUIManager questUIManager;
    public string questId = "call_son3";

    [Header("Interaction")]
    public bool interactOnlyOnce = true;
    public bool disableInteractionAfterClick = true;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string dialogueManagerObjectName = "DialogueManager";
    public string questUIManagerObjectName = "QuestUIManager";

    [Header("Layer")]
    public bool setLayerToInteractableOnStart = true;
    public bool setLayerRecursively = true;
    public string interactableLayerName = "Interactable";
    public string defaultLayerName = "Default";

    private bool hasInteracted = false;
    private bool routineStarted = false;

    private int interactableLayer;
    private int defaultLayer;

    private void Start()
    {
        interactableLayer = LayerMask.NameToLayer(interactableLayerName);
        defaultLayer = LayerMask.NameToLayer(defaultLayerName);

        FindReferences();

        if (setLayerToInteractableOnStart)
            SetObjectLayer(interactableLayer);
    }

    public void Interact()
    {
        if (routineStarted)
            return;

        if (interactOnlyOnce && hasInteracted)
            return;

        FindReferences();

        if (questUIManager == null)
        {
            Debug.LogWarning("DopQuestSON3: QuestUIManager не найден.");
            return;
        }

        if (dialogueManager == null)
        {
            Debug.LogWarning("DopQuestSON3: DialogueManager не найден.");
            return;
        }

        StartCoroutine(InteractionRoutine());
    }

    private IEnumerator InteractionRoutine()
    {
        routineStarted = true;
        hasInteracted = true;

        AddQuestIfNeeded();

        if (dialogueLines != null && dialogueLines.Count > 0)
        {
            dialogueManager.StartDialogue(dialogueLines, blockMovementDuringDialogue);

            while (dialogueManager != null && dialogueManager.DialogueActive)
                yield return null;
        }

        // ВАЖНО:
        // Квест call_son3 здесь НЕ завершается.
        // Он остаётся активным и висит в панели заданий.

        if (disableInteractionAfterClick)
            DisableInteraction();

        routineStarted = false;
    }

    private void AddQuestIfNeeded()
    {
        if (questUIManager == null)
            return;

        if (string.IsNullOrEmpty(questId))
            return;

        if (!questUIManager.IsQuestActive(questId) &&
            !questUIManager.IsQuestCompleted(questId))
        {
            questUIManager.AddQuest(questId);
            Debug.Log($"DopQuestSON3: добавлено дополнительное задание '{questId}'.");
        }
    }

    private void DisableInteraction()
    {
        SetObjectLayer(defaultLayer);
    }

    private void SetObjectLayer(int layer)
    {
        if (layer == -1)
            return;

        if (setLayerRecursively)
            SetLayerRecursive(transform, layer);
        else
            gameObject.layer = layer;
    }

    private void SetLayerRecursive(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursive(child, layer);
        }
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (questUIManager == null)
            questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
        {
            GameObject obj = GameObject.Find(questUIManagerObjectName);

            if (obj != null)
                questUIManager = obj.GetComponent<QuestUIManager>();
        }

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        if (dialogueManager == null)
        {
            GameObject obj = GameObject.Find(dialogueManagerObjectName);

            if (obj != null)
                dialogueManager = obj.GetComponent<DialogueManager>();
        }

        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>();
    }
}
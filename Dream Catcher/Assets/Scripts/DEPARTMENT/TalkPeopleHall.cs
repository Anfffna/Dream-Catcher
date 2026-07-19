using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TalkPeopleHall : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> dialogueLines = new List<DialogueManager.DialogueLine>();
    public bool blockMovementDuringDialogue = true;

    [Header("Hidden Save ID")]
    [Tooltip("Уникальный скрытый id разговора. НЕ добавлять в QuestUIManager.")]
    public string talkId = "talk_hall_person_01";

    [Header("Interaction Collider")]
    public Collider interactionCollider;
    public bool autoFindCollider = true;

    [Header("Layer")]
    public bool setLayerOnStart = true;
    public string interactableLayerName = "Interactable";
    public string defaultLayerName = "Default";

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string dialogueManagerObjectName = "DialogueManager";

    private bool dialogueRoutineStarted = false;

    private int interactableLayer;
    private int defaultLayer;

    private bool stateApplied = false;
    private bool lastAppliedTalkedState = false;

    private void Start()
    {
        interactableLayer = LayerMask.NameToLayer(interactableLayerName);
        defaultLayer = LayerMask.NameToLayer(defaultLayerName);

        if (autoFindCollider && interactionCollider == null)
            interactionCollider = GetComponent<Collider>();

        FindReferences();

        RefreshSavedState();
    }

    private void Update()
    {
        // Важно: при загрузке сейва ждём, пока SaveManager восстановит ItemInteractionState.
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoadingSave)
            return;

        RefreshSavedState();
    }

    public void Interact()
    {
        if (dialogueRoutineStarted)
            return;

        if (WasTalkedAlready())
            return;

        FindReferences();

        if (dialogueManager == null)
        {
            Debug.LogWarning("TalkPeopleHall: DialogueManager не найден.", this);
            return;
        }

        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            Debug.LogWarning("TalkPeopleHall: Dialogue Lines пустой.", this);
            return;
        }

        StartCoroutine(TalkRoutine());
    }

    private IEnumerator TalkRoutine()
    {
        dialogueRoutineStarted = true;

        dialogueManager.StartDialogue(dialogueLines, blockMovementDuringDialogue);

        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        MarkTalked();

        stateApplied = false;
        RefreshSavedState();

        dialogueRoutineStarted = false;

        Debug.Log($"TalkPeopleHall: разговор сохранён скрытым id: {talkId}", this);
    }

    private void RefreshSavedState()
    {
        bool talked = WasTalkedAlready();

        if (stateApplied && lastAppliedTalkedState == talked)
            return;

        stateApplied = true;
        lastAppliedTalkedState = talked;

        if (talked)
            DisableInteraction();
        else
            EnableInteraction();
    }

    private void EnableInteraction()
    {
        if (!setLayerOnStart)
            return;

        if (interactionCollider == null)
            return;

        if (interactableLayer != -1)
            interactionCollider.gameObject.layer = interactableLayer;
    }

    private void DisableInteraction()
    {
        if (interactionCollider == null)
            return;

        if (defaultLayer != -1)
            interactionCollider.gameObject.layer = defaultLayer;
    }

    private void MarkTalked()
    {
        if (string.IsNullOrEmpty(talkId))
        {
            Debug.LogWarning("TalkPeopleHall: talkId пустой, разговор не будет сохранён.", this);
            return;
        }

        ItemInteractionState.MarkInspected(talkId);
    }

    private bool WasTalkedAlready()
    {
        if (string.IsNullOrEmpty(talkId))
            return false;

        return ItemInteractionState.IsInspected(talkId);
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

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
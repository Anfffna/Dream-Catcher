using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TalkWorker : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> dialogueLines = new List<DialogueManager.DialogueLine>();
    public bool blockMovementDuringDialogue = true;

    [Header("Quest")]
    public QuestUIManager questUIManager;
    public string questIdToAddAfterDialogue = "walk_to_boss";

    [Header("Unblock Box After Dialogue")]
    public Collider unblockBoxAfterDialogue;
    public bool disableUnblockBoxGameObject = false;

    [Header("Optional Quest Requirement")]
    public bool requireQuestToTalk = false;
    public string requiredQuestId = "";

    [Header("Disappear Trigger")]
    public Collider disappearTrigger;
    public GameObject workerObjectToHide;
    public bool disappearOnlyAfterDialogueFinished = true;
    public bool disableDisappearTriggerAfterUse = true;
    public string playerTag = "Player";

    [Header("Disappear Save Marker")]
    public bool useDisappearSaveMarker = true;
    public string disappearedMarkerQuestId = "worker_disappeared_walk_to_boss";
    public bool completeDisappearedMarkerImmediately = true;

    [Header("Interaction")]
    public bool allowRepeatDialogue = false;
    public bool disableInteractionAfterDialogue = true;
    public bool setLayerOnStart = true;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string dialogueManagerObjectName = "DialogueManager";
    public string questUIManagerObjectName = "QuestUIManager";

    [Header("Layer")]
    public string defaultLayerName = "Default";
    public string interactableLayerName = "Interactable";
    public bool keepDisappearTriggerIgnoreRaycast = true;

    private bool hasTalked = false;
    private bool dialogueRoutineStarted = false;
    private bool dialogueFinished = false;
    private bool questAdded = false;
    private bool disappeared = false;

    private int defaultLayer;
    private int interactableLayer;
    private Collider objectCollider;

    private void Start()
    {
        defaultLayer = LayerMask.NameToLayer(defaultLayerName);
        interactableLayer = LayerMask.NameToLayer(interactableLayerName);

        objectCollider = GetComponent<Collider>();

        FindReferences();
        SetupDisappearTrigger();

        if (workerObjectToHide == null)
            workerObjectToHide = gameObject;

        if (setLayerOnStart)
            EnableInteraction();
    }

    public void Interact()
    {
        if (dialogueRoutineStarted)
            return;

        if (!allowRepeatDialogue && hasTalked)
            return;

        FindReferences();

        if (dialogueManager == null)
        {
            Debug.LogWarning("TalkWorker: DialogueManager не найден.");
            return;
        }

        if (questUIManager == null)
        {
            Debug.LogWarning("TalkWorker: QuestUIManager не найден.");
            return;
        }

        if (requireQuestToTalk && !string.IsNullOrEmpty(requiredQuestId))
        {
            if (!questUIManager.IsQuestActive(requiredQuestId))
            {
                Debug.Log($"TalkWorker: нельзя говорить, задание '{requiredQuestId}' не активно.");
                return;
            }
        }

        if (dialogueManager.DialogueActive)
            return;

        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            Debug.LogWarning("TalkWorker: Dialogue Lines пустой.");
            return;
        }

        StartCoroutine(TalkRoutine());
    }

    private IEnumerator TalkRoutine()
    {
        dialogueRoutineStarted = true;
        hasTalked = true;

        dialogueManager.StartDialogue(dialogueLines, blockMovementDuringDialogue);

        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        dialogueFinished = true;

        AddQuestAfterDialogue();
        DisableUnblockBoxAfterDialogue();

        if (disableInteractionAfterDialogue && !allowRepeatDialogue)
            DisableInteraction();

        dialogueRoutineStarted = false;
    }

    private void AddQuestAfterDialogue()
    {
        FindReferences();

        if (questUIManager == null)
            return;

        if (string.IsNullOrEmpty(questIdToAddAfterDialogue))
            return;

        if (!questUIManager.IsQuestActive(questIdToAddAfterDialogue) &&
            !questUIManager.IsQuestCompleted(questIdToAddAfterDialogue))
        {
            questUIManager.AddQuest(questIdToAddAfterDialogue);
            questAdded = true;

            Debug.Log($"TalkWorker: добавлено новое задание '{questIdToAddAfterDialogue}'.");
        }
    }

    private void DisableUnblockBoxAfterDialogue()
    {
        if (unblockBoxAfterDialogue == null)
            return;

        unblockBoxAfterDialogue.enabled = false;

        if (disableUnblockBoxGameObject)
            unblockBoxAfterDialogue.gameObject.SetActive(false);

        Debug.Log("TalkWorker: Unblock Box After Dialogue выключен после диалога.");
    }

    public void HandleDisappearTriggerEnter(Collider other)
    {
        if (disappeared)
            return;

        if (other == null)
            return;

        if (!IsPlayer(other))
            return;

        if (disappearOnlyAfterDialogueFinished && !CanDisappearNow())
            return;

        disappeared = true;

        MarkWorkerDisappearedForSave();

        if (workerObjectToHide == null)
            workerObjectToHide = gameObject;

        workerObjectToHide.SetActive(false);

        if (disableDisappearTriggerAfterUse && disappearTrigger != null)
            disappearTrigger.enabled = false;

        Debug.Log("TalkWorker: работник исчез после входа игрока в trigger box.");
    }

    private bool CanDisappearNow()
    {
        if (dialogueFinished)
            return true;

        FindReferences();

        if (questUIManager == null)
            return false;

        if (!string.IsNullOrEmpty(questIdToAddAfterDialogue))
        {
            if (questUIManager.IsQuestActive(questIdToAddAfterDialogue))
                return true;

            if (questUIManager.IsQuestCompleted(questIdToAddAfterDialogue))
                return true;
        }

        if (useDisappearSaveMarker && !string.IsNullOrEmpty(disappearedMarkerQuestId))
        {
            if (questUIManager.IsQuestActive(disappearedMarkerQuestId))
                return true;

            if (questUIManager.IsQuestCompleted(disappearedMarkerQuestId))
                return true;
        }

        return false;
    }

    private void MarkWorkerDisappearedForSave()
    {
        if (!useDisappearSaveMarker)
            return;

        FindReferences();

        if (questUIManager == null)
            return;

        if (string.IsNullOrEmpty(disappearedMarkerQuestId))
            return;

        if (!questUIManager.IsQuestActive(disappearedMarkerQuestId) &&
            !questUIManager.IsQuestCompleted(disappearedMarkerQuestId))
        {
            questUIManager.AddQuest(disappearedMarkerQuestId);
        }

        if (completeDisappearedMarkerImmediately &&
            !questUIManager.IsQuestCompleted(disappearedMarkerQuestId))
        {
            questUIManager.CompleteQuest(disappearedMarkerQuestId);
        }

        Debug.Log($"TalkWorker: сохранён маркер исчезновения работника: {disappearedMarkerQuestId}");
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        if (other.transform.root != null && other.transform.root.CompareTag(playerTag))
            return true;

        return false;
    }

    private void EnableInteraction()
    {
        if (interactableLayer != -1)
            gameObject.layer = interactableLayer;

        if (objectCollider != null)
            objectCollider.enabled = true;
    }

    private void DisableInteraction()
    {
        if (defaultLayer != -1)
            gameObject.layer = defaultLayer;

        if (objectCollider != null)
            objectCollider.enabled = false;
    }

    private void SetupDisappearTrigger()
    {
        if (disappearTrigger == null)
            return;

        disappearTrigger.enabled = true;
        disappearTrigger.isTrigger = true;

        if (keepDisappearTriggerIgnoreRaycast)
        {
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

            if (ignoreRaycastLayer != -1)
                disappearTrigger.gameObject.layer = ignoreRaycastLayer;
        }

        TalkWorkerDisappearTriggerProxy proxy =
            disappearTrigger.GetComponent<TalkWorkerDisappearTriggerProxy>();

        if (proxy == null)
            proxy = disappearTrigger.gameObject.AddComponent<TalkWorkerDisappearTriggerProxy>();

        proxy.owner = this;

        Rigidbody rb = disappearTrigger.GetComponent<Rigidbody>();

        if (rb == null)
            rb = disappearTrigger.gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
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
    }
}

public class TalkWorkerDisappearTriggerProxy : MonoBehaviour
{
    public TalkWorker owner;

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null)
            owner.HandleDisappearTriggerEnter(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (owner != null)
            owner.HandleDisappearTriggerEnter(other);
    }
}
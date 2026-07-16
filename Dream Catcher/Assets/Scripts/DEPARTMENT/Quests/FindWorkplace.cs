using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FindWorkplace : MonoBehaviour, IInteractable
{
    [Header("Quest Settings")]
    public QuestUIManager questUIManager;
    public string questId = "find_workplace";

    [Header("Key Object")]
    public GameObject keysObjectToHide;

    [Header("Turnstile")]
    public WorkplaceTurnstile workplaceTurnstile;
    public bool autoFindTurnstileIfMissing = true;

    [Header("Completion Trigger")]
    public Collider completionTrigger;
    public bool completeOnlyAfterKeysTaken = true;
    public bool completeQuestImmediatelyOnTrigger = true;
    public string playerTag = "Player";

    [Header("Completion Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> completionDialogueLines = new List<DialogueManager.DialogueLine>();
    public bool blockMovementDuringCompletionDialogue = true;

    [Header("Auto Find")]
    public bool autoFindQuestUIManager = true;
    public string questUIManagerObjectName = "QuestUIManager";
    public bool autoFindDialogueManager = true;
    public string dialogueManagerObjectName = "DialogueManager";

    [Header("Pickup")]
    public float disappearDuration = 0.4f;
    public bool deactivateAfterPickup = true;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Save State Marker")]
    public string keysTakenMarkerQuestId = "workplace_keys_taken";
    public bool completeKeysTakenMarkerImmediately = true;

    [Header("Layer")]
    public bool setLayerRecursively = true;

    private bool isAvailable = false;
    private bool isPickedUp = false;
    private bool pickupRoutineStarted = false;
    private bool completionStarted = false;

    private int defaultLayer;
    private int interactableLayer;

    private Collider[] allColliders;
    private Renderer[] allRenderers;

    private Vector3 keysOriginalLocalScale;
    private bool keysOriginalScaleSaved = false;

    void Start()
    {
        defaultLayer = LayerMask.NameToLayer("Default");
        interactableLayer = LayerMask.NameToLayer("Interactable");

        FindReferences();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        CacheKeyObjectParts();
        SaveKeysOriginalScale();

        SetupCompletionTrigger();
        DisableInteractionOnly();
        KeepCompletionTriggerNonInteractive();
    }

    void Update()
    {
        if (questUIManager == null || workplaceTurnstile == null || dialogueManager == null)
            FindReferences();

        if (isPickedUp)
            return;

        if (!isAvailable &&
            questUIManager != null &&
            questUIManager.IsQuestActive(questId))
        {
            EnableKeysInteraction();
        }
    }

    public void Interact()
    {
        if (!isAvailable || isPickedUp || pickupRoutineStarted)
            return;

        pickupRoutineStarted = true;

        if (audioSource != null)
            audioSource.Play();

        StartCoroutine(PickupRoutine());
    }

    public void HandleCompletionTriggerEnter(Collider other)
    {
        if (completionStarted)
            return;

        if (other == null)
            return;

        if (!IsPlayer(other))
            return;

        if (questUIManager == null)
            FindReferences();

        if (questUIManager == null)
            return;

        if (!questUIManager.IsQuestActive(questId))
            return;

        if (completeOnlyAfterKeysTaken && !AreKeysTaken())
            return;

        completionStarted = true;

        StartCoroutine(CompleteWorkplaceRoutine());
    }

    private IEnumerator CompleteWorkplaceRoutine()
    {
        FindReferences();

        if (completeQuestImmediatelyOnTrigger)
            CompleteWorkplaceQuest();

        if (dialogueManager != null &&
            completionDialogueLines != null &&
            completionDialogueLines.Count > 0)
        {
            dialogueManager.StartDialogue(completionDialogueLines, blockMovementDuringCompletionDialogue);

            while (dialogueManager != null && dialogueManager.DialogueActive)
                yield return null;
        }

        if (!completeQuestImmediatelyOnTrigger)
            CompleteWorkplaceQuest();

        if (completionTrigger != null)
            completionTrigger.enabled = false;
    }

    private void CompleteWorkplaceQuest()
    {
        FindReferences();

        if (questUIManager == null)
            return;

        if (!string.IsNullOrEmpty(questId) && questUIManager.IsQuestActive(questId))
        {
            questUIManager.CompleteQuest(questId);
            Debug.Log($"Задание '{questId}' завершено после входа в зону рабочего места.");
        }
    }

    private bool AreKeysTaken()
    {
        if (questUIManager == null)
            return false;

        if (string.IsNullOrEmpty(keysTakenMarkerQuestId))
            return isPickedUp;

        if (questUIManager.IsQuestActive(keysTakenMarkerQuestId))
            return true;

        if (questUIManager.IsQuestCompleted(keysTakenMarkerQuestId))
            return true;

        return isPickedUp;
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        if (other.transform.root != null && other.transform.root.CompareTag(playerTag))
            return true;

        return false;
    }

    private void EnableKeysInteraction()
    {
        isAvailable = true;

        SetLayer(interactableLayer);
        SetCollidersEnabled(true);
        KeepCompletionTriggerNonInteractive();

        Debug.Log($"Ключи активированы для задания: {questId}");
    }

    private void DisableInteractionOnly()
    {
        isAvailable = false;

        SetLayer(defaultLayer);
        SetCollidersEnabled(false);
    }

    private IEnumerator PickupRoutine()
    {
        isAvailable = false;
        isPickedUp = true;

        SetLayer(defaultLayer);
        SetCollidersEnabled(false);

        GameObject target = GetKeysTargetObject();

        if (target == null)
        {
            Debug.LogWarning("FindWorkplace: keysObjectToHide не назначен и объект ключей не найден.");
            yield break;
        }

        Transform targetTransform = target.transform;
        Vector3 startScale = targetTransform.localScale;

        float elapsed = 0f;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;

            float t = disappearDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / disappearDuration);

            float smoothT = t * t * (3f - 2f * t);

            targetTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothT);

            yield return null;
        }

        targetTransform.localScale = Vector3.zero;

        if (allRenderers != null)
        {
            foreach (Renderer rend in allRenderers)
            {
                if (rend != null)
                    rend.enabled = false;
            }
        }

        if (workplaceTurnstile != null)
        {
            workplaceTurnstile.UnlockTurnstile();
            Debug.Log("Ключи подобраны. Турникет разблокирован. Задание пока НЕ завершено.");
            MarkKeysAsTakenForSave();
        }
        else
        {
            Debug.LogWarning("Ключи подобраны, но WorkplaceTurnstile не назначен.");
        }

        if (deactivateAfterPickup)
            target.SetActive(false);
    }

    private void MarkKeysAsTakenForSave()
    {
        FindReferences();

        if (questUIManager == null)
            return;

        if (string.IsNullOrEmpty(keysTakenMarkerQuestId))
            return;

        if (!questUIManager.IsQuestActive(keysTakenMarkerQuestId) &&
            !questUIManager.IsQuestCompleted(keysTakenMarkerQuestId))
        {
            questUIManager.AddQuest(keysTakenMarkerQuestId);
        }

        if (completeKeysTakenMarkerImmediately &&
            !questUIManager.IsQuestCompleted(keysTakenMarkerQuestId))
        {
            questUIManager.CompleteQuest(keysTakenMarkerQuestId);
        }

        Debug.Log($"Маркер подобранных ключей записан: {keysTakenMarkerQuestId}");
    }

    private void SetupCompletionTrigger()
    {
        if (completionTrigger == null)
            return;

        completionTrigger.enabled = true;
        completionTrigger.isTrigger = true;

        KeepCompletionTriggerNonInteractive();

        FindWorkplaceCompletionTriggerProxy proxy =
            completionTrigger.GetComponent<FindWorkplaceCompletionTriggerProxy>();

        if (proxy == null)
            proxy = completionTrigger.gameObject.AddComponent<FindWorkplaceCompletionTriggerProxy>();

        proxy.owner = this;

        Rigidbody rb = completionTrigger.GetComponent<Rigidbody>();

        if (rb == null)
            rb = completionTrigger.gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void KeepCompletionTriggerNonInteractive()
    {
        if (completionTrigger == null)
            return;

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        if (ignoreRaycastLayer != -1)
            SetLayerRecursiveOnly(completionTrigger.transform, ignoreRaycastLayer);

        completionTrigger.enabled = true;
        completionTrigger.isTrigger = true;
    }

    private void SetLayerRecursiveOnly(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursiveOnly(child, layer);
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        CacheKeyObjectParts();

        if (allColliders == null)
            return;

        foreach (Collider col in allColliders)
        {
            if (col == null)
                continue;

            if (completionTrigger != null && col == completionTrigger)
                continue;

            col.enabled = enabled;
        }

        KeepCompletionTriggerNonInteractive();
    }

    private void SetLayer(int layer)
    {
        GameObject target = GetKeysTargetObject();

        if (target == null)
            return;

        if (!setLayerRecursively)
        {
            target.layer = layer;
            return;
        }

        SetLayerRecursive(target.transform, layer);
    }

    private void SetLayerRecursive(Transform root, int layer)
    {
        if (completionTrigger != null)
        {
            if (root == completionTrigger.transform || root.IsChildOf(completionTrigger.transform))
                return;
        }

        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursive(child, layer);
        }

        KeepCompletionTriggerNonInteractive();
    }

    private GameObject GetKeysTargetObject()
    {
        if (keysObjectToHide != null)
            return keysObjectToHide;

        return gameObject;
    }

    private void CacheKeyObjectParts()
    {
        GameObject target = GetKeysTargetObject();

        if (target == null)
        {
            allColliders = null;
            allRenderers = null;
            return;
        }

        allColliders = target.GetComponentsInChildren<Collider>(true);
        allRenderers = target.GetComponentsInChildren<Renderer>(true);
    }

    private void SaveKeysOriginalScale()
    {
        GameObject target = GetKeysTargetObject();

        if (target == null)
            return;

        keysOriginalLocalScale = target.transform.localScale;
        keysOriginalScaleSaved = true;
    }

    private void RestoreKeysScale()
    {
        GameObject target = GetKeysTargetObject();

        if (target == null)
            return;

        if (!keysOriginalScaleSaved)
            SaveKeysOriginalScale();

        target.transform.localScale = keysOriginalLocalScale;

        if (allRenderers != null)
        {
            foreach (Renderer rend in allRenderers)
            {
                if (rend != null)
                    rend.enabled = true;
            }
        }
    }

    private void FindReferences()
    {
        if (autoFindQuestUIManager)
        {
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

        if (autoFindTurnstileIfMissing && workplaceTurnstile == null)
            workplaceTurnstile = FindObjectOfType<WorkplaceTurnstile>();

        if (autoFindDialogueManager)
        {
            if (dialogueManager == null || dialogueManager.gameObject.name != dialogueManagerObjectName)
            {
                GameObject obj = GameObject.Find(dialogueManagerObjectName);

                if (obj != null)
                    dialogueManager = obj.GetComponent<DialogueManager>();
            }

            if (dialogueManager == null)
                dialogueManager = FindObjectOfType<DialogueManager>();
        }
    }
}

public class FindWorkplaceCompletionTriggerProxy : MonoBehaviour
{
    public FindWorkplace owner;

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null)
            owner.HandleCompletionTriggerEnter(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (owner != null)
            owner.HandleCompletionTriggerEnter(other);
    }
}
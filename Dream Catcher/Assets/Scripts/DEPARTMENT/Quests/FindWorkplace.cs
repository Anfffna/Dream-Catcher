using UnityEngine;
using System.Collections;

public class FindWorkplace : MonoBehaviour, IInteractable
{
    [Header("Quest Settings")]
    public QuestUIManager questUIManager;
    public string questId = "find_workplace";

    [Header("Turnstile")]
    public WorkplaceTurnstile workplaceTurnstile;
    public bool autoFindTurnstileIfMissing = true;

    [Header("Auto Find")]
    public bool autoFindQuestUIManager = true;
    public string questUIManagerObjectName = "QuestUIManager";

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

    private int defaultLayer;
    private int interactableLayer;

    private Collider[] allColliders;
    private Renderer[] allRenderers;

    void Start()
    {
        defaultLayer = LayerMask.NameToLayer("Default");
        interactableLayer = LayerMask.NameToLayer("Interactable");

        FindReferences();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        allColliders = GetComponentsInChildren<Collider>(true);
        allRenderers = GetComponentsInChildren<Renderer>(true);

        DisableInteractionOnly();
    }

    void Update()
    {
        if (questUIManager == null || workplaceTurnstile == null)
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

    private void EnableKeysInteraction()
    {
        isAvailable = true;

        SetLayer(interactableLayer);
        SetCollidersEnabled(true);

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

        Vector3 startScale = transform.localScale;

        float elapsed = 0f;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;

            float t = disappearDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / disappearDuration);

            float smoothT = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothT);

            yield return null;
        }

        transform.localScale = Vector3.zero;

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
            gameObject.SetActive(false);
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

    private void SetCollidersEnabled(bool enabled)
    {
        if (allColliders == null)
            allColliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider col in allColliders)
        {
            if (col != null)
                col.enabled = enabled;
        }
    }

    private void SetLayer(int layer)
    {
        if (!setLayerRecursively)
        {
            gameObject.layer = layer;
            return;
        }

        SetLayerRecursive(transform, layer);
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
    }
}
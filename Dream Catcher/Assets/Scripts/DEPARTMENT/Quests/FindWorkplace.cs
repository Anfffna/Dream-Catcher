using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FindWorkplace : MonoBehaviour, IInteractable
{
    [Header("Quest Settings")]
    public QuestUIManager questUIManager;
    public string questId = "find_workplace";
    public string nextQuestId = "";

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string questUIManagerObjectName = "QuestUIManager";

    [Header("Pickup")]
    public float disappearDuration = 0.4f;
    public bool deactivateAfterPickup = true;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Layer")]
    public bool setLayerRecursively = true;

    private bool isAvailable = false;
    private bool isPickedUp = false;

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

        // До старта квеста ключи могут быть видимыми,
        // но они не должны быть интерактивными.
        DisableInteractionOnly();
    }

    void Update()
    {
        if (questUIManager == null)
            FindReferences();

        if (isPickedUp)
            return;

        if (!isAvailable && questUIManager != null && questUIManager.IsQuestActive(questId))
        {
            EnableKeysInteraction();
        }
    }

    public void Interact()
    {
        if (!isAvailable || isPickedUp)
            return;

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

        FindReferences();

        if (questUIManager != null)
        {
            questUIManager.CompleteQuest(questId);

            if (!string.IsNullOrEmpty(nextQuestId))
                questUIManager.AddQuest(nextQuestId);
        }

        if (deactivateAfterPickup)
            gameObject.SetActive(false);

        Debug.Log($"Ключи подобраны. Задание завершено: {questId}");
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
    }
}
using UnityEngine;
using System.Collections.Generic;

public class DepartmentGoDoor : MonoBehaviour, IInteractable
{
    [Header("Quest")]
    public QuestUIManager questUIManager;
    public string requiredQuestId = "go_to_depart";

    [Header("Loading Settings")]
    public string sceneToLoad = "NextScene";
    public float showImageDelay = 1f;

    [Header("Loading Dialogue")]
    public DialogueManager loadingDialogueManager;
    public List<DialogueManager.DialogueLine> loadingDialogueLines;

    private bool isAvailable = false;
    private bool isInteracting = false;

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Default");
        isAvailable = false;
    }

    void Update()
    {
        if (!isAvailable && questUIManager != null && questUIManager.IsQuestActive(requiredQuestId))
        {
            isAvailable = true;
            gameObject.layer = LayerMask.NameToLayer("Interactable");
            InteractionOutline outline = GetComponent<InteractionOutline>();
            if (outline != null) outline.ShowOutline();
        }
    }

    public void Interact()
    {
        if (!isAvailable || isInteracting) return;

        isInteracting = true;

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.StartLoading(
                sceneToLoad,
                loadingDialogueManager,
                loadingDialogueLines,
                showImageDelay
            );
        }
        else
        {
            Debug.LogError("LoadingManager не найден!");
        }

        // isInteracting останется true, чтобы запретить повторное нажатие.
        // Объект будет уничтожен при смене сцены (он локальный).
    }
}
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

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string questUIManagerObjectName = "QuestUIManager";
    public string loadingDialogueManagerObjectName = "LoadingDialogueManager";

    private bool isAvailable = false;
    private bool isInteracting = false;

    void Start()
    {
        FindReferences();

        gameObject.layer = LayerMask.NameToLayer("Default");
        isAvailable = false;
    }

    void Update()
    {
        if (questUIManager == null)
            FindReferences();

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
        FindReferences();

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

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        // QuestUIManager
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

        // LoadingDialogueManager — ВАЖНО: ищем именно объект с именем LoadingDialogueManager
        if (loadingDialogueManager == null ||
            loadingDialogueManager.gameObject.name != loadingDialogueManagerObjectName)
        {
            GameObject obj = GameObject.Find(loadingDialogueManagerObjectName);

            if (obj != null)
                loadingDialogueManager = obj.GetComponent<DialogueManager>();
        }

        // Запасной вариант: среди всех DialogueManager ищем именно LoadingDialogueManager
        if (loadingDialogueManager == null)
        {
            DialogueManager[] managers = FindObjectsOfType<DialogueManager>();

            foreach (DialogueManager manager in managers)
            {
                if (manager.gameObject.name == loadingDialogueManagerObjectName)
                {
                    loadingDialogueManager = manager;
                    break;
                }
            }
        }
    }
}
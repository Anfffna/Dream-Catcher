using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class FindAboutJob : MonoBehaviour, IInteractable
{
    [Header("Quest Settings")]
    public string questIdToComplete = "find_about_job";

    [Header("Activation")]
    public float activationDelay = 2f;

    [Header("Dialogues")]
    public List<DialogueManager.DialogueLine> firstDialogueLines;
    public List<DialogueManager.DialogueLine> secondDialogueLines;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string questUIManagerObjectName = "QuestUIManager";
    public string dialogueManagerObjectName = "DialogueManager";
    public string interactionDotObjectName = "InteractionDot";

    private bool isCompleted = false;
    private bool firstDialogueShown = false;
    private bool secondDialogueShown = false;
    private Collider objectCollider;
    private int defaultLayer;
    private int interactableLayer;
    private QuestUIManager questManager;
    private DialogueManager dialogueManager; // теперь приватное, ищется по имени
    private Image interactionDot;

    void Start()
    {
        objectCollider = GetComponent<Collider>();
        defaultLayer = LayerMask.NameToLayer("Default");
        interactableLayer = LayerMask.NameToLayer("Interactable");

        gameObject.layer = defaultLayer;
        if (objectCollider != null) objectCollider.enabled = false;

        FindReferences();

        StartCoroutine(ActivationRoutine());
    }

    private IEnumerator ActivationRoutine()
    {
        yield return new WaitForSeconds(activationDelay);

        FindReferences();

        if (questManager == null)
        {
            yield return new WaitForSeconds(2f);
            StartCoroutine(ActivationRoutine());
            yield break;
        }

        if (questManager.IsQuestActive(questIdToComplete))
        {
            gameObject.layer = interactableLayer;
            if (objectCollider != null) objectCollider.enabled = true;
            Debug.Log($"Объект {gameObject.name} активирован для задания {questIdToComplete}");
        }
        else
        {
            yield return new WaitForSeconds(2f);
            StartCoroutine(ActivationRoutine());
        }
    }

    private void EnsureDialogueManager()
    {
        FindReferences();
    }

    public void Interact()
    {
        FindReferences();

        if (isCompleted) return;

        EnsureDialogueManager();

        if (questManager == null || dialogueManager == null)
        {
            return;
        }

        if (!firstDialogueShown && !questManager.IsQuestActive(questIdToComplete))
        {
            Debug.Log($"Задание '{questIdToComplete}' не активно или уже завершено.");
            return;
        }

        if (dialogueManager.DialogueActive) return;

        if (!firstDialogueShown)
        {
            if (firstDialogueLines != null && firstDialogueLines.Count > 0)
            {
                HideInteractionDot();

                dialogueManager.StartDialogue(firstDialogueLines, true);
                firstDialogueShown = true;

                StartCoroutine(WaitForFirstDialogueAndCompleteQuest()); //первый диалог запущен
            }
        }
        else if (!secondDialogueShown)
        {
            if (secondDialogueLines != null && secondDialogueLines.Count > 0)
            {
                HideInteractionDot();

                dialogueManager.StartDialogue(secondDialogueLines, true);
                secondDialogueShown = true;
                StartCoroutine(WaitForSecondDialogueAndDisableInteraction());
            }
        }
    }

    private IEnumerator ShowDotAfterDialogue()
    {
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        ShowInteractionDot();
    }

    private IEnumerator WaitForFirstDialogueAndCompleteQuest()
    {
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        ShowInteractionDot();
        FindReferences();

        if (questManager != null && questManager.IsQuestActive(questIdToComplete))
        {
            questManager.CompleteQuest(questIdToComplete);
            Debug.Log($"Задание '{questIdToComplete}' завершено после первого диалога.");
        }
        // ВАЖНО: Не ставим isCompleted = true, чтобы второй необязательный диалог ещё можно было вызвать.
    }

    private IEnumerator WaitForSecondDialogueAndDisableInteraction()
    {
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        ShowInteractionDot();

        isCompleted = true;
        gameObject.layer = defaultLayer;

        if (objectCollider != null)
            objectCollider.enabled = false;

        Debug.Log($"Второй диалог для {questIdToComplete} завершён. Объект больше не интерактивен.");
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

        // QuestUIManager
        if (questManager == null)
            questManager = QuestUIManager.Instance;

        if (questManager == null)
        {
            GameObject obj = GameObject.Find(questUIManagerObjectName);

            if (obj != null)
                questManager = obj.GetComponent<QuestUIManager>();
        }

        if (questManager == null)
            questManager = FindObjectOfType<QuestUIManager>();

        // DialogueManager — ВАЖНО: ищем именно обычный DialogueManager, не LoadingDialogueManager
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

        // InteractionDot
        if (interactionDot == null)
        {
            GameObject obj = GameObject.Find(interactionDotObjectName);

            if (obj != null)
                interactionDot = obj.GetComponent<Image>();
        }
    }
}
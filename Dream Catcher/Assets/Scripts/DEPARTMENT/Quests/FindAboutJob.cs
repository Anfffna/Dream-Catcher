using UnityEngine;
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

    private bool isCompleted = false;
    private bool firstDialogueShown = false;
    private bool secondDialogueShown = false;
    private Collider objectCollider;
    private int defaultLayer;
    private int interactableLayer;
    private QuestUIManager questManager;
    private DialogueManager dialogueManager; // теперь приватное, ищется по имени

    void Start()
    {
        objectCollider = GetComponent<Collider>();
        defaultLayer = LayerMask.NameToLayer("Default");
        interactableLayer = LayerMask.NameToLayer("Interactable");

        gameObject.layer = defaultLayer;
        if (objectCollider != null) objectCollider.enabled = false;

        // Ищем DialogueManager строго по имени объекта
        dialogueManager = FindDialogueManagerByName();

        questManager = FindObjectOfType<QuestUIManager>();

        StartCoroutine(ActivationRoutine());
    }

    private DialogueManager FindDialogueManagerByName()
    {
        // 1. Ищем объект по точному имени (без пробела)
        GameObject dmObj = GameObject.Find("DialogueManager");
        if (dmObj == null)
            dmObj = GameObject.Find("Dialogue Manager"); // если имя с пробелом

        if (dmObj != null)
            return dmObj.GetComponent<DialogueManager>();

        // 2. Если не найден – пробуем перебор всех диалоговых менеджеров и отфильтровать по имени
        DialogueManager[] all = FindObjectsOfType<DialogueManager>();
        foreach (var dm in all)
        {
            if (dm.name == "DialogueManager" || dm.name == "Dialogue Manager")
                return dm;
        }

        Debug.LogWarning("DialogueManager не найден по имени! Проверь, что объект с именем 'DialogueManager' существует в GlobalSystem.");
        return null;
    }

    private IEnumerator ActivationRoutine()
    {
        yield return new WaitForSeconds(activationDelay);

        if (questManager == null)
        {
            Debug.LogWarning("QuestUIManager not found!");
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
            Debug.Log($"Задание {questIdToComplete} ещё не активно, повторная проверка через 2 сек.");
            yield return new WaitForSeconds(2f);
            StartCoroutine(ActivationRoutine());
        }
    }

    private void EnsureDialogueManager()
    {
        if (dialogueManager == null)
            dialogueManager = FindDialogueManagerByName();

        // Если ссылки на UI сброшены – восстанавливаем
        if (dialogueManager != null && dialogueManager.dialoguePanel == null)
        {
            Transform panel = dialogueManager.transform.Find("DialoguePanel");
            if (panel != null)
            {
                dialogueManager.dialoguePanel = panel.gameObject;
                dialogueManager.dialogueText = panel.GetComponentInChildren<TextMeshProUGUI>();
                Debug.Log("Ссылки на диалоговую панель восстановлены.");
            }
            else
            {
                Debug.LogError("DialoguePanel не найден в иерархии DialogueManager!");
            }
        }
    }

    public void Interact()
    {
        if (isCompleted) return;

        EnsureDialogueManager();

        if (questManager == null || dialogueManager == null)
        {
            Debug.LogWarning("QuestManager или DialogueManager не найден!");
            return;
        }

        if (!questManager.IsQuestActive(questIdToComplete))
        {
            Debug.Log($"Задание '{questIdToComplete}' не активно или уже завершено.");
            return;
        }

        if (dialogueManager.DialogueActive) return;

        if (!firstDialogueShown)
        {
            if (firstDialogueLines != null && firstDialogueLines.Count > 0)
            {
                dialogueManager.StartDialogue(firstDialogueLines, true);
                firstDialogueShown = true;
                Debug.Log($"Запущен первый диалог для {questIdToComplete}");
            }
            else
            {
                Debug.LogWarning("Нет реплик для первого диалога!");
            }
        }
        else if (!secondDialogueShown)
        {
            if (secondDialogueLines != null && secondDialogueLines.Count > 0)
            {
                dialogueManager.StartDialogue(secondDialogueLines, true);
                secondDialogueShown = true;
                StartCoroutine(WaitForDialogueAndComplete());
                Debug.Log($"Запущен второй диалог для {questIdToComplete}");
            }
            else
            {
                Debug.LogWarning("Нет реплик для второго диалога!");
            }
        }
    }

    private IEnumerator WaitForDialogueAndComplete()
    {
        while (dialogueManager.DialogueActive)
            yield return null;

        if (questManager != null && questManager.IsQuestActive(questIdToComplete))
        {
            questManager.CompleteQuest(questIdToComplete);
            Debug.Log($"Задание '{questIdToComplete}' завершено после второго диалога.");
        }

        isCompleted = true;
        gameObject.layer = defaultLayer;
        if (objectCollider != null) objectCollider.enabled = false;

        Debug.Log($"Объект {gameObject.name} больше не интерактивен.");
    }
}
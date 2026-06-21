using UnityEngine;
using System.Collections;

public class FindAboutJob : MonoBehaviour, IInteractable
{
    [Header("Quest Settings")]
    public string questIdToComplete = "find_about_job";

    [Header("Activation")]
    public float activationDelay = 2f; // задержка перед активацией интерактивности

    [Header("Visual Feedback")]
    public GameObject visualObject;
    public float destroyDelay = 0.5f;

    private bool isCompleted = false;
    private Collider objectCollider;
    private int defaultLayer;
    private int interactableLayer;

    void Start()
    {
        // Запоминаем коллайдер и слои
        objectCollider = GetComponent<Collider>();
        defaultLayer = LayerMask.NameToLayer("Default");
        interactableLayer = LayerMask.NameToLayer("Interactable");

        // Изначально объект неинтерактивен (слой Default, коллайдер выключен)
        gameObject.layer = defaultLayer;
        if (objectCollider != null) objectCollider.enabled = false;

        // Запускаем проверку с задержкой
        StartCoroutine(ActivationRoutine());
    }

    private IEnumerator ActivationRoutine()
    {
        // Ждём указанную задержку (чтобы задание успело добавиться)
        yield return new WaitForSeconds(activationDelay);

        // Проверяем активность задания
        QuestUIManager questManager = FindObjectOfType<QuestUIManager>();
        if (questManager == null)
        {
            Debug.LogWarning("QuestUIManager not found!");
            yield break;
        }

        if (questManager.IsQuestActive(questIdToComplete))
        {
            // Задание активно – делаем объект интерактивным
            gameObject.layer = interactableLayer;
            if (objectCollider != null) objectCollider.enabled = true;
            Debug.Log($"Объект {gameObject.name} активирован для задания {questIdToComplete}");
        }
        else
        {
            // Задание не активно – можно либо повторить проверку позже, либо оставить неактивным
            // В данном случае повторяем проверку через 2 секунды (пока задание не появится)
            Debug.Log($"Задание {questIdToComplete} ещё не активно, повторная проверка через 2 сек.");
            yield return new WaitForSeconds(2f);
            // Рекурсивно запускаем повторную проверку (но аккуратно, чтобы не зациклить)
            StartCoroutine(ActivationRoutine());
        }
    }

    public void Interact()
    {
        if (isCompleted) return;

        QuestUIManager questManager = FindObjectOfType<QuestUIManager>();
        if (questManager == null)
        {
            Debug.LogWarning("QuestUIManager not found!");
            return;
        }

        if (!questManager.IsQuestActive(questIdToComplete))
        {
            Debug.Log($"Задание '{questIdToComplete}' не активно или уже завершено.");
            return;
        }

        // Завершаем задание
        questManager.CompleteQuest(questIdToComplete);
        isCompleted = true;
        Debug.Log($"Задание '{questIdToComplete}' завершено при взаимодействии с {gameObject.name}");

        // Визуальный фидбэк
        if (visualObject != null)
            visualObject.SetActive(false);
        else
            Destroy(gameObject, destroyDelay);
    }
}
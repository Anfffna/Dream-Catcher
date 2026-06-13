using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestingQuestHelper : MonoBehaviour
{
    [Header("Quest Manager")]
    public QuestUIManager questUIManager;

    [Header("Список ID заданий для теста (по порядку)")]
    public List<string> testQuestIds = new List<string>();

    [Header("Ручное управление (по индексу)")]
    [Range(0, 10)] public int selectedIndex = 0;

    // Контекстные меню (правой кнопкой по компоненту)
    [ContextMenu("Add Selected Quest")]
    void AddSelectedQuest()
    {
        if (questUIManager != null && testQuestIds.Count > selectedIndex)
            questUIManager.AddQuest(testQuestIds[selectedIndex]);
    }

    [ContextMenu("Complete Selected Quest")]
    void CompleteSelectedQuest()
    {
        if (questUIManager != null && testQuestIds.Count > selectedIndex)
            questUIManager.CompleteQuest(testQuestIds[selectedIndex]);
    }

    [ContextMenu("Add All Quests (in order)")]
    void AddAllQuests()
    {
        foreach (string id in testQuestIds)
            questUIManager?.AddQuest(id);
    }

    [ContextMenu("Complete All Quests (in order)")]
    void CompleteAllQuests()
    {
        foreach (string id in testQuestIds)
            questUIManager?.CompleteQuest(id);
    }

    // Для быстрого доступа через горячие клавиши (только в Play Mode)
    void Update()
    {
        if (!Application.isPlaying) return;

        // F1 - добавить выбранное задание
        if (Input.GetKeyDown(KeyCode.F1))
            AddSelectedQuest();

        // F2 - завершить выбранное задание
        if (Input.GetKeyDown(KeyCode.F2))
            CompleteSelectedQuest();

        // F3 - добавить все по порядку
        if (Input.GetKeyDown(KeyCode.F3))
            AddAllQuests();

        // F4 - завершить все по порядку
        if (Input.GetKeyDown(KeyCode.F4))
            CompleteAllQuests();
    }
}
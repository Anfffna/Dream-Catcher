using UnityEngine;
using System.Collections.Generic;

public class SceneQuestCompleter : MonoBehaviour
{
    [Header("Quests to complete on scene load")]
    public List<string> questIdsToComplete = new List<string>();

    [Header("Optional: complete only if quest is active")]
    public bool completeOnlyIfActive = true;

    private void Start()
    {
        // Ищем глобальный QuestUIManager
        QuestUIManager questManager = FindObjectOfType<QuestUIManager>();
        if (questManager == null)
        {
            Debug.LogWarning("QuestUIManager not found in scene!");
            return;
        }

        foreach (string questId in questIdsToComplete)
        {
            if (string.IsNullOrEmpty(questId)) continue;

            // Если требуется проверять активность
            if (completeOnlyIfActive && !questManager.IsQuestActive(questId))
                continue;

            questManager.CompleteQuest(questId);
            Debug.Log($"Задание '{questId}' завершено при входе в сцену {gameObject.scene.name}");
        }

        // Опционально: уничтожить объект после выполнения (чтобы не мешал)
        Destroy(gameObject, 0.1f);
    }
}
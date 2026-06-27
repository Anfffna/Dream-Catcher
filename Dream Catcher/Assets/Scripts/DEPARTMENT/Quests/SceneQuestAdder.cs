using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SceneQuestAdder : MonoBehaviour
{
    [Header("Quest Manager")]
    public QuestUIManager questUIManager; // если не назначен Ц найдЄт автоматически

    [Header("Quests to add on scene load")]
    public List<string> questIdsToAdd = new List<string>();

    [Header("Delay")]
    public float addDelay = 2f; // задержка перед добавлением заданий (в секундах)

    private void Start()
    {
        StartCoroutine(AddQuestsAfterDelay());
    }

    private IEnumerator AddQuestsAfterDelay()
    {
        yield return new WaitForSeconds(addDelay);

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        if (questUIManager == null)
        {
            yield break;
        }

        foreach (var q in questUIManager.quests)

        foreach (string questId in questIdsToAdd)
        {
            if (string.IsNullOrEmpty(questId))
            {
                continue;
            }

            bool isActive = questUIManager.IsQuestActive(questId);

            if (!isActive)
            {
                questUIManager.AddQuest(questId);
            }
        }
    }
}
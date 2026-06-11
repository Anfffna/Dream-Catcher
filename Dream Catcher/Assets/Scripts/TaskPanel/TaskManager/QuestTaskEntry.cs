using UnityEngine;
using TMPro;

public class QuestTaskEntry : MonoBehaviour
{
    private TextMeshProUGUI taskText;

    private QuestData questData;
    private QuestUIManager questUIManager;

    void Awake()
    {
        taskText = GetComponent<TextMeshProUGUI>();
    }

    public void Setup(QuestData newQuestData, QuestUIManager newQuestUIManager)
    {
        questData = newQuestData;
        questUIManager = newQuestUIManager;

        if (taskText != null)
            taskText.text = "• " + questData.title;
    }

    public void OnClick()
    {
        if (questUIManager != null && questData != null)
            questUIManager.ShowActiveQuestSummary(questData);
    }
}
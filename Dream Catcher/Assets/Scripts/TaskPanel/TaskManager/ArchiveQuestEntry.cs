using UnityEngine;
using TMPro;

public class ArchiveQuestEntry : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI archiveText;

    private QuestData questData;
    private QuestUIManager questUIManager;

    void Awake()
    {
        if (archiveText == null)
            archiveText = GetComponent<TextMeshProUGUI>();

        if (archiveText == null)
            archiveText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Setup(QuestData newQuestData, QuestUIManager newQuestUIManager)
    {
        questData = newQuestData;
        questUIManager = newQuestUIManager;

        if (archiveText == null)
        {
            return;
        }

        if (questData == null)
        {
            return;
        }

        archiveText.text = "<s>• " + questData.title + "</s>";
    }

    public void OnClick()
    {
        if (questUIManager != null && questData != null)
            questUIManager.ShowArchiveQuestSummary(questData);
    }
}
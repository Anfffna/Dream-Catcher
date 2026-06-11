using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestUIManager : MonoBehaviour
{
    [Header("Quest Database")]
    public List<QuestData> quests = new List<QuestData>();

    [Header("Active Tasks UI")]
    public Transform activeTasksContainer;
    public GameObject taskEntryPrefab;

    [Header("Archive UI")]
    public Transform archiveContent;
    public GameObject archiveTaskEntryPrefab;
    public TextMeshProUGUI archiveCountText;

    [Header("Summary UI")]
    public TextMeshProUGUI summaryDescriptionText;

    [Header("Task Update Toast")]
    public TaskUpdateToast taskUpdateToast;

    private Dictionary<string, QuestData> questById = new Dictionary<string, QuestData>();
    private Dictionary<string, GameObject> activeQuestObjects = new Dictionary<string, GameObject>();

    private enum SummarySource
    {
        None,
        Active,
        Archive
    }

    private SummarySource currentSummarySource = SummarySource.None;

    private int completedArchiveQuestCount = 0;

    void Awake()
    {
        questById.Clear();

        for (int i = 0; i < quests.Count; i++)
        {
            QuestData quest = quests[i];

            if (quest == null) continue;
            if (string.IsNullOrEmpty(quest.questId)) continue;

            if (!questById.ContainsKey(quest.questId))
                questById.Add(quest.questId, quest);
        }

        ClearSummary();
        UpdateArchiveCountText();
    }

    public void AddQuest(string questId)
    {
        if (!questById.ContainsKey(questId))
        {
            Debug.LogWarning("Задание не найдено: " + questId);
            return;
        }

        if (activeQuestObjects.ContainsKey(questId))
            return;

        QuestData quest = questById[questId];

        GameObject newTask = Instantiate(taskEntryPrefab, activeTasksContainer);
        newTask.transform.SetAsLastSibling();

        QuestTaskEntry taskEntry = newTask.GetComponent<QuestTaskEntry>();

        if (taskEntry != null)
            taskEntry.Setup(quest, this);

        activeQuestObjects.Add(questId, newTask);

        if (taskUpdateToast != null)
            taskUpdateToast.ShowToast();
    }

    public void CompleteQuest(string questId)
    {
        if (!activeQuestObjects.ContainsKey(questId))
        {
            Debug.LogWarning("Активное задание не найдено: " + questId);
            return;
        }

        QuestData quest = null;

        if (questById.ContainsKey(questId))
            quest = questById[questId];

        GameObject questObject = activeQuestObjects[questId];

        if (questObject != null)
            Destroy(questObject);

        activeQuestObjects.Remove(questId);

        ClearSummary();

        if (quest != null && quest.showInArchive)
        {
            AddQuestToArchive(quest);
        }

        if (taskUpdateToast != null)
            taskUpdateToast.ShowToast();
    }

    private void AddQuestToArchive(QuestData quest)
    {
        if (archiveContent == null) return;
        if (archiveTaskEntryPrefab == null) return;

        GameObject archiveObject = Instantiate(archiveTaskEntryPrefab, archiveContent);
        archiveObject.transform.SetAsLastSibling();

        ArchiveQuestEntry archiveEntry = archiveObject.GetComponent<ArchiveQuestEntry>();

        if (archiveEntry != null)
            archiveEntry.Setup(quest, this);

        completedArchiveQuestCount++;
        UpdateArchiveCountText();
    }

    private void UpdateArchiveCountText()
    {
        if (archiveCountText != null)
            archiveCountText.text = "(" + completedArchiveQuestCount + ")";
    }

    public void ShowActiveQuestSummary(QuestData quest)
    {
        ShowQuestSummaryInternal(quest, SummarySource.Active);
    }

    public void ShowArchiveQuestSummary(QuestData quest)
    {
        ShowQuestSummaryInternal(quest, SummarySource.Archive);
    }

    private void ShowQuestSummaryInternal(QuestData quest, SummarySource source)
    {
        if (quest == null) return;

        currentSummarySource = source;

        if (summaryDescriptionText != null)
            summaryDescriptionText.text = quest.description;
    }

    public void ClearSummary()
    {
        currentSummarySource = SummarySource.None;

        if (summaryDescriptionText != null)
            summaryDescriptionText.text = "";
    }

    public void ClearSummaryIfFromArchive()
    {
        if (currentSummarySource != SummarySource.Archive)
            return;

        ClearSummary();
    }
}
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestUIManager : MonoBehaviour
{
    public static QuestUIManager Instance { get; private set; }

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

    // UI-объекты активных заданий
    private Dictionary<string, GameObject> activeQuestObjects = new Dictionary<string, GameObject>();

    // UI-объекты архива
    private Dictionary<string, GameObject> archiveQuestObjects = new Dictionary<string, GameObject>();

    // Состояние заданий, которое будет сохраняться
    private List<string> activeQuestIds = new List<string>();
    private List<string> completedQuestIds = new List<string>();

    private enum SummarySource
    {
        None,
        Active,
        Archive
    }

    private SummarySource currentSummarySource = SummarySource.None;

    private int completedArchiveQuestCount = 0;

    private bool isRestoring = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // НЕ включаем DontDestroyOnLoad.
        // QuestUIManager обычно привязан к UI конкретной сцены.
        // Глобальное состояние будет храниться в SaveData / SaveManager / GameProgress.

        BuildQuestDatabase();

        ClearSummary();
        UpdateArchiveCountText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void BuildQuestDatabase()
    {
        questById.Clear();

        for (int i = 0; i < quests.Count; i++)
        {
            QuestData quest = quests[i];

            if (quest == null)
                continue;

            if (string.IsNullOrEmpty(quest.questId))
            {
                Debug.LogWarning("QuestUIManager: найдено задание с пустым questId.");
                continue;
            }

            if (!questById.ContainsKey(quest.questId))
            {
                questById.Add(quest.questId, quest);
            }
            else
            {
                Debug.LogWarning("QuestUIManager: повторяющийся questId: " + quest.questId);
            }
        }
    }

    public void AddQuest(string questId)
    {
        AddQuestInternal(questId, true);
    }

    private void AddQuestInternal(string questId, bool showToast)
    {
        if (string.IsNullOrEmpty(questId))
        {
            Debug.LogWarning("QuestUIManager: попытка добавить задание с пустым questId.");
            return;
        }

        // Если задание уже завершено, не добавляем его снова.
        if (completedQuestIds.Contains(questId))
            return;

        // Если оно уже активно, повторно не добавляем, но пробуем восстановить UI,
        // если вдруг ID есть, а объект UI ещё не создан.
        if (activeQuestIds.Contains(questId))
        {
            CreateActiveQuestObject(questId);
            return;
        }

        activeQuestIds.Add(questId);

        CreateActiveQuestObject(questId);

        if (taskUpdateToast != null && showToast && !isRestoring)
            taskUpdateToast.ShowToast();
        if (!isRestoring)
            QuestWorldStateApplier.ApplyAllInScene();
    }

    private void CreateActiveQuestObject(string questId)
    {
        if (activeQuestObjects.ContainsKey(questId))
            return;

        if (!questById.ContainsKey(questId))
        {
            Debug.LogWarning("QuestUIManager: задание не найдено в базе quests: " + questId);
            return;
        }

        if (activeTasksContainer == null)
        {
            Debug.LogError("QuestUIManager: activeTasksContainer не назначен. UI активного задания не создан.");
            return;
        }

        if (taskEntryPrefab == null)
        {
            Debug.LogError("QuestUIManager: taskEntryPrefab не назначен. UI активного задания не создан.");
            return;
        }

        QuestData quest = questById[questId];

        GameObject newTask = Instantiate(taskEntryPrefab, activeTasksContainer);
        newTask.transform.SetAsLastSibling();

        QuestTaskEntry taskEntry = newTask.GetComponent<QuestTaskEntry>();

        if (taskEntry != null)
            taskEntry.Setup(quest, this);

        activeQuestObjects.Add(questId, newTask);

        SetQuestOutlines(quest, true);
    }

    public void CompleteQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId))
        {
            Debug.LogWarning("QuestUIManager: попытка завершить задание с пустым questId.");
            return;
        }

        if (!activeQuestIds.Contains(questId))
        {
            if (completedQuestIds.Contains(questId))
                return;

            Debug.LogWarning("QuestUIManager: активное задание не найдено: " + questId);
            return;
        }

        QuestData quest = null;

        if (questById.ContainsKey(questId))
            quest = questById[questId];

        // Удаляем из активных
        activeQuestIds.Remove(questId);

        // Удаляем UI-объект активного задания
        if (activeQuestObjects.ContainsKey(questId))
        {
            GameObject questObject = activeQuestObjects[questId];

            if (questObject != null)
                Destroy(questObject);

            activeQuestObjects.Remove(questId);
        }

        if (quest != null)
            SetQuestOutlines(quest, false);

        // Запоминаем как завершённое
        if (!completedQuestIds.Contains(questId))
            completedQuestIds.Add(questId);

        ClearSummary();

        // В архив попадают только сюжетные задания
        if (quest != null && quest.tag == QuestTag.Сюжет)
        {
            AddQuestToArchive(quest);
        }

        // Toast при завершении показываем только для сюжетных, как у тебя было
        if (taskUpdateToast != null && quest != null && quest.tag == QuestTag.Сюжет && !isRestoring)
            taskUpdateToast.ShowToast();
        if (!isRestoring)
            QuestWorldStateApplier.ApplyAllInScene();
    }

    private void AddQuestToArchive(QuestData quest)
    {
        if (quest == null)
            return;

        if (string.IsNullOrEmpty(quest.questId))
            return;

        if (archiveQuestObjects.ContainsKey(quest.questId))
            return;

        if (archiveContent == null)
            return;

        if (archiveTaskEntryPrefab == null)
            return;

        GameObject archiveObject = Instantiate(archiveTaskEntryPrefab, archiveContent);
        archiveObject.transform.SetAsLastSibling();

        ArchiveQuestEntry archiveEntry = archiveObject.GetComponent<ArchiveQuestEntry>();

        if (archiveEntry != null)
            archiveEntry.Setup(quest, this);

        archiveQuestObjects.Add(quest.questId, archiveObject);

        completedArchiveQuestCount = archiveQuestObjects.Count;
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
        if (quest == null)
            return;

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

    private void SetQuestOutlines(QuestData quest, bool state)
    {
        if (quest == null)
            return;

        if (quest.outlineIds == null)
            return;

        for (int i = 0; i < quest.outlineIds.Length; i++)
        {
            string outlineId = quest.outlineIds[i];

            if (string.IsNullOrEmpty(outlineId))
                continue;

            if (state)
                InteractionOutlineRegistry.Show(outlineId);
            else
                InteractionOutlineRegistry.Hide(outlineId);
        }
    }

    public bool IsQuestActive(string questId)
    {
        return activeQuestIds.Contains(questId);
    }

    public bool IsQuestCompleted(string questId)
    {
        return completedQuestIds.Contains(questId);
    }

    public List<string> GetActiveQuestIds()
    {
        return new List<string>(activeQuestIds);
    }

    public List<string> GetCompletedQuestIds()
    {
        return new List<string>(completedQuestIds);
    }

    public void RestoreQuests(List<string> activeIds, List<string> completedIds)
    {
        isRestoring = true;

        ClearAllActiveQuestObjects();
        ClearAllArchiveQuestObjects();

        activeQuestIds.Clear();
        completedQuestIds.Clear();

        // Сначала восстанавливаем завершённые.
        // Это важно: если questId случайно есть и в active, и в completed,
        // completed считается главным.
        if (completedIds != null)
        {
            for (int i = 0; i < completedIds.Count; i++)
            {
                string questId = completedIds[i];

                if (string.IsNullOrEmpty(questId))
                    continue;

                if (!completedQuestIds.Contains(questId))
                    completedQuestIds.Add(questId);

                if (questById.ContainsKey(questId))
                {
                    QuestData quest = questById[questId];

                    SetQuestOutlines(quest, false);

                    if (quest.tag == QuestTag.Сюжет)
                        AddQuestToArchive(quest);
                }
                else
                {
                    Debug.LogWarning("QuestUIManager: завершённое задание есть в сохранении, но не найдено в quests: " + questId);
                }
            }
        }

        // Потом восстанавливаем активные.
        if (activeIds != null)
        {
            for (int i = 0; i < activeIds.Count; i++)
            {
                string questId = activeIds[i];

                if (string.IsNullOrEmpty(questId))
                    continue;

                if (completedQuestIds.Contains(questId))
                    continue;

                if (!activeQuestIds.Contains(questId))
                    activeQuestIds.Add(questId);

                CreateActiveQuestObject(questId);
            }
        }

        completedArchiveQuestCount = archiveQuestObjects.Count;
        UpdateArchiveCountText();
        ClearSummary();

        isRestoring = false;
    }

    private void ClearAllActiveQuestObjects()
    {
        foreach (KeyValuePair<string, GameObject> pair in activeQuestObjects)
        {
            string questId = pair.Key;
            GameObject questObject = pair.Value;

            if (questById.ContainsKey(questId))
                SetQuestOutlines(questById[questId], false);

            if (questObject != null)
                Destroy(questObject);
        }

        activeQuestObjects.Clear();
    }

    private void ClearAllArchiveQuestObjects()
    {
        foreach (KeyValuePair<string, GameObject> pair in archiveQuestObjects)
        {
            GameObject archiveObject = pair.Value;

            if (archiveObject != null)
                Destroy(archiveObject);
        }

        archiveQuestObjects.Clear();

        completedArchiveQuestCount = 0;
        UpdateArchiveCountText();
    }

    public void ClearAllQuests()
    {
        ClearAllActiveQuestObjects();
        ClearAllArchiveQuestObjects();

        activeQuestIds.Clear();
        completedQuestIds.Clear();

        ClearSummary();
        UpdateArchiveCountText();
    }
}
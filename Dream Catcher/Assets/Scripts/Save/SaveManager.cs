using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Settings")]
    public int maxSaves = 3;

    private List<SaveData> saves = new List<SaveData>();
    private string savePath;

    private SaveData pendingLoadData;

    public bool IsLoadingSave { get; private set; }

    private Coroutine finishLoadingCoroutine;

    [Header("Загрузка характеристик")]

    [Tooltip("Через сколько секунд после нажатия загрузки восстановить характеристики игрока.")]
    [SerializeField]
    private float playerStatsRestoreDelay = 1f;

    private Coroutine restorePlayerStatsCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        savePath = Path.Combine(Application.persistentDataPath, "saves.json");
        LoadFromFile();

        Debug.Log("SaveManager активен. Путь сохранений: " + savePath);
    }

    public List<SaveData> GetSaves()
    {
        if (saves == null)
            saves = new List<SaveData>();

        return saves;
    }

    public void CreateNewSave(string name)
    {
        if (saves == null)
            saves = new List<SaveData>();

        if (saves.Count >= maxSaves)
        {
            Debug.LogWarning("Максимум сохранений!");
            return;
        }

        SaveData newSave = new SaveData();

        if (!FillSaveDataFromCurrentGame(newSave, name))
            return;

        saves.Add(newSave);
        SaveToFile();
    }

    public void OverwriteSave(int index, string newName)
    {
        if (saves == null)
            saves = new List<SaveData>();

        if (index < 0 || index >= saves.Count)
            return;

        SaveData save = saves[index];

        if (!FillSaveDataFromCurrentGame(save, newName))
            return;

        // После перезаписи перемещаем это сохранение в конец списка.
        // SavePanelController и LoadPanelController показывают список наоборот,
        // поэтому это сохранение визуально окажется самым верхним.
        saves.RemoveAt(index);
        saves.Add(save);

        SaveToFile();
    }

    private bool FillSaveDataFromCurrentGame(SaveData save, string saveName)
    {
        if (save == null)
            return false;

        PlayerController player = FindObjectOfType<PlayerController>();

        if (player == null)
        {
            Debug.LogWarning("SaveManager: PlayerController не найден. Сохранение можно делать только в игровой сцене.");
            return false;
        }

        save.saveName = saveName;
        save.sceneName = SceneManager.GetActiveScene().name;

        save.posX = player.transform.position.x;
        save.posY = player.transform.position.y;
        save.posZ = player.transform.position.z;

        save.dateTime = DateTime.Now.ToString("dd.MM.yy / HH:mm");

        SaveQuestState(save);
        SaveItemInteractionState(save);

        if (!SavePlayerStats(save))
            return false;

        return true;
    }

    private bool SavePlayerStats(
    SaveData save)
    {
        if (save == null)
            return false;

        SessionStatsManager stats =
            SessionStatsManager.Instance;

        if (stats == null)
        {
            Debug.LogWarning(
                "SaveManager: SessionStatsManager не найден. " +
                "Характеристики игрока не сохранены."
            );

            return false;
        }

        save.playerStatsVersion =
            SessionStatsManager.SaveVersion;

        save.sanity =
            stats.GetSanityForSave();

        save.experience =
            stats.GetExperienceForSave();

        save.money =
            stats.GetMoneyForSave();

        return true;
    }

    private void SaveQuestState(SaveData save)
    {
        save.activeQuestIds = new List<string>();
        save.completedQuestIds = new List<string>();

        QuestUIManager questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        if (questUIManager == null)
        {
            Debug.LogWarning("SaveManager: QuestUIManager не найден. Задания не будут записаны в сохранение.");
            return;
        }

        save.activeQuestIds = new List<string>(questUIManager.GetActiveQuestIds());
        save.completedQuestIds = new List<string>(questUIManager.GetCompletedQuestIds());
    }

    private void SaveItemInteractionState(SaveData save)
    {
        if (save == null)
            return;

        save.inspectedItemIds = ItemInteractionState.GetInspectedItemIds();
    }

    public void LoadSave(int index)
    {
        if (saves == null || index < 0 || index >= saves.Count)
        {
            Debug.LogWarning("SaveManager: неверный индекс сохранения.");
            return;
        }

        pendingLoadData = CloneSaveData(saves[index]);

        if (pendingLoadData == null || string.IsNullOrEmpty(pendingLoadData.sceneName))
        {
            Debug.LogWarning("SaveManager: сохранение повреждено или в нём нет sceneName.");
            return;
        }

        NormalizeSaveData(pendingLoadData);

        Time.timeScale = 1f;
        IsLoadingSave = true;

        if (restorePlayerStatsCoroutine != null)
        {
            StopCoroutine(
                restorePlayerStatsCoroutine
            );
        }

        restorePlayerStatsCoroutine =
            StartCoroutine(
                RestorePlayerStatsDelayed(
                    pendingLoadData
                )
            );

        SceneManager.sceneLoaded -= OnSceneLoadedAfterSaveLoad;
        SceneManager.sceneLoaded += OnSceneLoadedAfterSaveLoad;

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.StartLoading(
                pendingLoadData.sceneName
            );
        }
        else
        {
            SceneManager.LoadScene(
                pendingLoadData.sceneName
            );
        }
    }

    private void RestorePlayerStats(
    SaveData data)
    {
        if (data == null)
            return;

        SessionStatsManager stats =
            SessionStatsManager.Instance;

        if (stats == null)
            return;

        // Совсем старый сейв,
        // созданный до появления характеристик.
        if (data.playerStatsVersion <= 0)
        {
            stats.ResetForNewGame();
            return;
        }

        // Версия 1 существовала до денег.
        // Такие сейвы получают стартовые 100 рублей.
        int savedMoney =
            data.playerStatsVersion >= 2
                ? data.money
                : 100;

        stats.RestoreFromSave(
            data.sanity,
            data.experience,
            savedMoney
        );
    }

    private IEnumerator RestorePlayerStatsDelayed(
    SaveData data)
    {
        if (playerStatsRestoreDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                playerStatsRestoreDelay
            );
        }

        // На случай, если persistent-объекты
        // ещё не успели окончательно определиться.
        while (SessionStatsManager.Instance == null)
        {
            yield return null;
        }

        RestorePlayerStats(data);

        restorePlayerStatsCoroutine = null;
    }

    private void OnSceneLoadedAfterSaveLoad(Scene scene, LoadSceneMode mode)
    {
        if (pendingLoadData == null)
            return;

        if (scene.name != pendingLoadData.sceneName)
            return;

        SceneManager.sceneLoaded -= OnSceneLoadedAfterSaveLoad;

        if (finishLoadingCoroutine != null)
            StopCoroutine(finishLoadingCoroutine);

        finishLoadingCoroutine = StartCoroutine(ApplyLoadedSaveAfterSceneReady(pendingLoadData));
    }

    private IEnumerator ApplyLoadedSaveAfterSceneReady(SaveData data)
    {
        // Ждём, чтобы:
        // 1. PersistentObject успел удалить дубликат Player из новой сцены.
        // 2. Start/Awake у объектов сцены успели отработать.
        yield return null;
        yield return null;

        // Сначала восстанавливаем квесты и состояние мира,
        // чтобы нужные коллайдеры/объекты уже были в правильном состоянии.
        InteractionOutlineRegistry.ClearAllVisible();
        ItemInteractionState.Restore(data.inspectedItemIds);

        RestoreQuestState(data);
        QuestWorldStateApplier.ApplyAllInScene();

        yield return null;

        // Синхронизируем физику после включения/выключения объектов и коллайдеров.
        Physics.SyncTransforms();

        // Теперь ставим настоящего оставшегося Player в сохранённую позицию.
        RestorePlayerPosition(data);

        // Обязательно сбрасываем временные игровые режимы.
        // Они не должны переноситься через загрузку сохранения.
        ResetRuntimeGameplayStateAfterLoad();

        yield return null;

        Physics.SyncTransforms();

        // После переноса игрока можно перерисовать outline,
        // чтобы камера и UI уже были в правильном месте.
        InteractionOutlineRegistry.RedrawVisibleOutlines();

        if (PauseManager.Instance != null)
            PauseManager.Instance.EnableGameplayAfterLoading();

        pendingLoadData = null;

        IsLoadingSave = false;
        finishLoadingCoroutine = null;
    }

    private void ResetRuntimeGameplayStateAfterLoad()
    {
        PlayerController player =
            FindObjectOfType<PlayerController>();

        if (player != null)
            player.ForceResetToNormalGameplayAfterLoad();

        if (WorkSessionManager.Instance != null)
            WorkSessionManager.Instance.ResetAfterLoad();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log(
            "SaveManager: Player и временные игровые режимы сброшены после загрузки."
        );
    }

    private IEnumerator FinishLoadingFlagNextFrame()
    {
        // Держим IsLoadingSave включённым ещё один кадр,
        // чтобы Start() у StartDay и других сценовых скриптов понял,
        // что это загрузка сейва, а не новая игра.
        yield return null;

        IsLoadingSave = false;
        finishLoadingCoroutine = null;
    }

    private void RestoreQuestState(SaveData data)
    {
        QuestUIManager questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        if (questUIManager == null)
        {
            Debug.LogWarning("SaveManager: QuestUIManager не найден в загруженной сцене. Задания не восстановлены.");
            return;
        }

        questUIManager.RestoreQuests(data.activeQuestIds, data.completedQuestIds);
    }

    private void RestorePlayerPosition(SaveData data)
    {
        PlayerController player = FindObjectOfType<PlayerController>();

        if (player == null)
        {
            Debug.LogWarning("SaveManager: PlayerController не найден после загрузки сцены.");
            return;
        }

        // На время телепорта лучше не давать управлению/гравитации вмешаться.
        player.canControl = false;

        Vector3 loadedPosition = new Vector3(
            data.posX,
            data.posY,
            data.posZ
        );

        CharacterController characterController = player.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
            player.transform.position = loadedPosition;
            Physics.SyncTransforms();
            characterController.enabled = true;
        }
        else
        {
            player.transform.position = loadedPosition;
            Physics.SyncTransforms();
        }

        player.ResetMovementAfterTeleport();
    }

    public bool HasAnySaves()
    {
        return saves != null && saves.Count > 0;
    }

    public void LoadLatestSave()
    {
        if (saves == null || saves.Count == 0)
        {
            Debug.LogWarning("SaveManager: нет сохранений для продолжения.");
            return;
        }

        // Последнее сохранение хранится в конце списка.
        int latestIndex = saves.Count - 1;

        LoadSave(latestIndex);
    }

    private void SaveToFile()
    {
        if (saves == null)
            saves = new List<SaveData>();

        for (int i = 0; i < saves.Count; i++)
            NormalizeSaveData(saves[i]);

        SaveWrapper wrapper = new SaveWrapper();
        wrapper.saves = saves;

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(savePath, json);

        Debug.Log("Сохранения записаны в файл.");
    }

    private void LoadFromFile()
    {
        if (!File.Exists(savePath))
        {
            saves = new List<SaveData>();
            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);

            if (wrapper != null && wrapper.saves != null)
                saves = wrapper.saves;
            else
                saves = new List<SaveData>();

            NormalizeAllLoadedSaves();
        }
        catch (Exception e)
        {
            Debug.LogWarning("SaveManager: не удалось прочитать файл сохранений. Будет создан пустой список. Ошибка: " + e.Message);
            saves = new List<SaveData>();
        }
    }

    private void NormalizeAllLoadedSaves()
    {
        if (saves == null)
        {
            saves = new List<SaveData>();
            return;
        }

        for (int i = saves.Count - 1; i >= 0; i--)
        {
            if (saves[i] == null)
            {
                saves.RemoveAt(i);
                continue;
            }

            NormalizeSaveData(saves[i]);
        }
    }

    private void NormalizeSaveData(
    SaveData save)
    {
        if (save == null)
            return;

        if (save.activeQuestIds == null)
            save.activeQuestIds =
                new List<string>();

        if (save.completedQuestIds == null)
            save.completedQuestIds =
                new List<string>();

        if (save.inspectedItemIds == null)
            save.inspectedItemIds =
                new List<string>();

        // Сейвы версии 1 были созданы
        // до появления денег.
        if (save.playerStatsVersion == 1)
        {
            save.money = 100;
        }
    }

    private SaveData CloneSaveData(
    SaveData source)
    {
        if (source == null)
            return null;

        NormalizeSaveData(source);

        SaveData clone = new SaveData();

        clone.saveName = source.saveName;

        clone.sceneName = source.sceneName;

        clone.posX = source.posX;

        clone.posY = source.posY;

        clone.posZ = source.posZ;

        clone.dateTime = source.dateTime;

        clone.playerStatsVersion = source.playerStatsVersion;

        clone.sanity = source.sanity;

        clone.experience = source.experience;

        clone.money = source.money;

        clone.activeQuestIds =
            new List<string>(
                source.activeQuestIds
            );

        clone.completedQuestIds =
            new List<string>(
                source.completedQuestIds
            );

        clone.inspectedItemIds =
            new List<string>(
                source.inspectedItemIds
            );

        return clone;
    }

    public void PrepareNewGame()
    {
        pendingLoadData = null;
        IsLoadingSave = false;

        SceneManager.sceneLoaded -= OnSceneLoadedAfterSaveLoad;

        if (finishLoadingCoroutine != null)
        {
            StopCoroutine(finishLoadingCoroutine);
            finishLoadingCoroutine = null;
        }

        Time.timeScale = 1f;

        // ВАЖНО:
        // Новая игра не должна наследовать задания/обводки от ранее загруженного сейва.
        InteractionOutlineRegistry.ClearAllVisible();
        ItemInteractionState.Clear();

        QuestUIManager questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>(true);

        if (questUIManager != null)
            questUIManager.ClearAllQuests();

        TaskPanelController taskPanelController = TaskPanelController.Instance;

        if (taskPanelController == null)
            taskPanelController = FindObjectOfType<TaskPanelController>(true);

        if (taskPanelController != null)
            taskPanelController.ResetForNewGame();

        if (SessionStatsManager.Instance != null)
        {
            SessionStatsManager.Instance
                .ResetForNewGame();
        }

        Debug.Log("SaveManager: подготовка новой игры завершена.");
    }

    public void DeleteAllSaves()
    {
        if (saves == null)
            saves = new List<SaveData>();

        saves.Clear();

        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Все сохранения удалены. Файл удалён: " + savePath);
        }
        else
        {
            Debug.Log("Сохранений для удаления нет.");
        }
    }

    [System.Serializable]
    private class SaveWrapper
    {
        public List<SaveData> saves;
    }
}
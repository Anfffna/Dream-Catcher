using UnityEngine;
using UnityEngine.SceneManagement;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

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
        if (index < 0 || index >= saves.Count)
            return;

        SaveData save = saves[index];

        save.saveName = newName;
        save.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            save.posX = player.transform.position.x;
            save.posY = player.transform.position.y;
            save.posZ = player.transform.position.z;
        }

        save.dateTime = DateTime.Now.ToString("dd.MM.yy / HH:mm");

        // После перезаписи перемещаем это сохранение в конец списка.
        // Так как SavePanelController показывает список в обратном порядке,
        // это сохранение визуально окажется самым верхним.
        saves.RemoveAt(index);
        saves.Add(save);

        SaveToFile();
    }

    private bool FillSaveDataFromCurrentGame(SaveData save, string saveName)
    {
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

        return true;
    }

    public void LoadSave(int index)
    {
        if (index < 0 || index >= saves.Count)
        {
            Debug.LogWarning("SaveManager: неверный индекс сохранения.");
            return;
        }

        pendingLoadData = saves[index];

        if (pendingLoadData == null || string.IsNullOrEmpty(pendingLoadData.sceneName))
        {
            Debug.LogWarning("SaveManager: сохранение повреждено или в нём нет sceneName.");
            return;
        }

        Time.timeScale = 1f;

        SceneManager.sceneLoaded -= OnSceneLoadedAfterSaveLoad;
        SceneManager.sceneLoaded += OnSceneLoadedAfterSaveLoad;

        if (GlobalLoadingManager.Instance != null)
        {
            GlobalLoadingManager.Instance.StartLoading(pendingLoadData.sceneName);
        }
        else
        {
            SceneManager.LoadScene(pendingLoadData.sceneName);
        }
    }

    private void OnSceneLoadedAfterSaveLoad(Scene scene, LoadSceneMode mode)
    {
        if (pendingLoadData == null)
            return;

        if (scene.name != pendingLoadData.sceneName)
            return;

        SceneManager.sceneLoaded -= OnSceneLoadedAfterSaveLoad;

        PlayerController player = FindObjectOfType<PlayerController>();

        if (player != null)
        {
            Vector3 loadedPosition = new Vector3(
                pendingLoadData.posX,
                pendingLoadData.posY,
                pendingLoadData.posZ
            );

            CharacterController characterController = player.GetComponent<CharacterController>();

            if (characterController != null)
            {
                characterController.enabled = false;
                player.transform.position = loadedPosition;
                characterController.enabled = true;
            }
            else
            {
                player.transform.position = loadedPosition;
            }
        }
        else
        {
            Debug.LogWarning("SaveManager: после загрузки сцены PlayerController не найден.");
        }

        pendingLoadData = null;
    }

    private void SaveToFile()
    {
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

        string json = File.ReadAllText(savePath);
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);

        if (wrapper != null && wrapper.saves != null)
            saves = wrapper.saves;
        else
            saves = new List<SaveData>();
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
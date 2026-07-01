using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InitializerMainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Global Gameplay Canvas")]
    public GameObject globalCanvas;

    [Header("Optional Gameplay Managers")]
    public PauseManager pauseManager;
    public TaskPanelController taskPanelController;

    [Header("Cursor")]
    public bool showCursorInMainMenu = true;
    public bool hideCursorInGameplay = true;

    [Header("Debug")]
    public bool debugLogs = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(ApplyModeNextFrames());
    }

    private void Start()
    {
        StartCoroutine(ApplyModeNextFrames());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyModeNextFrames());
    }

    private IEnumerator ApplyModeNextFrames()
    {
        ApplyMode();

        yield return null;
        ApplyMode();

        yield return null;
        ApplyMode();
    }

    private void ApplyMode()
    {
        FindReferences();

        bool isMainMenu = SceneManager.GetActiveScene().name == mainMenuSceneName;

        if (isMainMenu)
            ApplyMainMenuMode();
        else
            ApplyGameplayMode();
    }

    private void ApplyMainMenuMode()
    {
        if (debugLogs)
            Debug.Log("InitializerMainMenu: MainMenu mode");

        Time.timeScale = 1f;

        // В главном меню весь игровой UI выключен.
        if (globalCanvas != null)
            globalCanvas.SetActive(false);

        // Пауза и панель заданий в меню не должны реагировать.
        if (pauseManager != null)
            pauseManager.enabled = false;

        if (taskPanelController != null)
            taskPanelController.enabled = false;

        if (showCursorInMainMenu)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        DestroyPlayersInMainMenu();
    }

    private void ApplyGameplayMode()
    {
        if (debugLogs)
            Debug.Log("InitializerMainMenu: Gameplay mode");

        // В игре возвращаем игровой UI.
        if (globalCanvas != null)
            globalCanvas.SetActive(true);

        if (pauseManager != null)
            pauseManager.enabled = true;

        if (taskPanelController != null)
            taskPanelController.enabled = true;

        if (hideCursorInGameplay)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void FindReferences()
    {
        if (globalCanvas == null)
        {
            GameObject obj = GameObject.Find("GlobalCanvas");

            if (obj != null)
                globalCanvas = obj;
        }

        if (pauseManager == null)
            pauseManager = FindObjectOfType<PauseManager>(true);

        if (taskPanelController == null)
            taskPanelController = FindObjectOfType<TaskPanelController>(true);
    }

    private void DestroyPlayersInMainMenu()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>(true);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
                Destroy(players[i].gameObject);
        }
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class LastGameplaySceneMemory : MonoBehaviour
{
    public static LastGameplaySceneMemory Instance;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";

    [Header("PlayerPrefs Key")]
    public string lastGameplaySceneKey = "LastGameplaySceneName";

    [Header("Debug")]
    public bool debugLogs = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
            return;

        PlayerPrefs.SetString(lastGameplaySceneKey, scene.name);
        PlayerPrefs.Save();

        if (debugLogs)
            Debug.Log($"LastGameplaySceneMemory: последн€€ игрова€ сцена = {scene.name}", this);
    }
}
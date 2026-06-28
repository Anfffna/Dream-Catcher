using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Sliders")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;

    private bool isSubscribed = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        FindSlidersInScene();
        LoadSettings();
        SubscribeToSliders();

        // Подписываемся на смену сцены
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Вызывается при загрузке любой сцены
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Сбрасываем старые ссылки и ищем заново в новой сцене
        volumeSlider = null;
        sensitivitySlider = null;
        isSubscribed = false;

        FindSlidersInScene();
        LoadSettings();
        SubscribeToSliders();

        Debug.Log($"SettingsManager: слайдеры обновлены для сцены {scene.name}");
    }

    public void RefreshSliders()
    {
        volumeSlider = null;
        sensitivitySlider = null;
        isSubscribed = false;

        FindSlidersInScene();
        LoadSettings();
        SubscribeToSliders();
    }

    private void FindSlidersInScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Slider[] allSliders = FindObjectsOfType<Slider>(true);

        foreach (Slider s in allSliders)
        {
            // Для MainMenu — ищем ТОЛЬКО локальные слайдеры (не внутри GlobalSystem)
            if (sceneName == "MainMenu")
            {
                Transform parent = s.transform.parent;
                bool isInGlobalSystem = false;
                while (parent != null)
                {
                    if (parent.name == "GlobalSystem" || parent.name == "DontDestroyOnLoad")
                    {
                        isInGlobalSystem = true;
                        break;
                    }
                    parent = parent.parent;
                }

                if (isInGlobalSystem) continue; // пропускаем глобальные слайдеры
            }

            if (s.name == "VolumeSlider" && volumeSlider == null)
                volumeSlider = s;
            if (s.name == "SensitivitySlider" && sensitivitySlider == null)
                sensitivitySlider = s;
        }

        if (volumeSlider == null)
            Debug.LogWarning($"VolumeSlider не найден в сцене {sceneName}!");
        if (sensitivitySlider == null)
            Debug.LogWarning($"SensitivitySlider не найден в сцене {sceneName}!");
    }

    private void SubscribeToSliders()
    {
        if (isSubscribed) return;

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        isSubscribed = true;
    }

    public void LoadSettings()
    {
        float volume = PlayerPrefs.GetFloat("Volume", 0.5f);
        float sensitivity = PlayerPrefs.GetFloat("Sensitivity", 150f);

        if (volumeSlider != null)
            volumeSlider.value = volume;

        if (sensitivitySlider != null)
            sensitivitySlider.value = sensitivity;

        ApplyVolume(volume);
        ApplySensitivity(sensitivity);
    }

    private void OnVolumeChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
    }

    private void OnSensitivityChanged(float value)
    {
        ApplySensitivity(value);
        PlayerPrefs.SetFloat("Sensitivity", value);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = value;
    }

    private void ApplySensitivity(float value)
    {
        GameSettings.MouseSensitivity = value;
    }
}
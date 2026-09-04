using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance
    {
        get;
        private set;
    }


    [Header("Sliders")]

    public Slider volumeSlider;

    public Slider sensitivitySlider;

    public Slider brightnessSlider;

    public Slider grainSlider;


    [Header("Глобальные визуальные эффекты")]

    [Tooltip(
        "Глобальный Volume Noise+ColorEff. " +
        "Назначается вручную в Inspector."
    )]
    [SerializeField]
    private Volume visualEffectsVolume;


    private ColorAdjustments
        colorAdjustments;

    private FilmGrain
        filmGrain;


    private bool isSubscribed = false;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeVisualEffects();
    }


    private void Start()
    {
        FindSlidersInScene();

        LoadSettings();

        SubscribeToSliders();

        SceneManager.sceneLoaded +=
            OnSceneLoaded;
    }


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -=
            OnSceneLoaded;
    }


    // =====================================================
    // СМЕНА СЦЕНЫ
    // =====================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        volumeSlider = null;
        sensitivitySlider = null;
        brightnessSlider = null;
        grainSlider = null;

        isSubscribed = false;

        FindSlidersInScene();

        LoadSettings();

        SubscribeToSliders();
    }


    public void RefreshSliders()
    {
        volumeSlider = null;
        sensitivitySlider = null;
        brightnessSlider = null;
        grainSlider = null;

        isSubscribed = false;

        FindSlidersInScene();

        LoadSettings();

        SubscribeToSliders();
    }


    // =====================================================
    // ПОИСК СЛАЙДЕРОВ
    // =====================================================

    private void FindSlidersInScene()
    {
        string sceneName =
            SceneManager
                .GetActiveScene()
                .name;

        Slider[] allSliders =
            FindObjectsOfType<Slider>(
                true
            );


        foreach (Slider slider in allSliders)
        {
            /*
             * В MainMenu берём только
             * локальные слайдеры самой сцены,
             * а не элементы внутри GlobalSystem.
             */
            if (sceneName == "MainMenu")
            {
                Transform parent =
                    slider.transform.parent;

                bool isInGlobalSystem =
                    false;


                while (parent != null)
                {
                    if (parent.name ==
                            "GlobalSystem" ||
                        parent.name ==
                            "DontDestroyOnLoad")
                    {
                        isInGlobalSystem =
                            true;

                        break;
                    }

                    parent =
                        parent.parent;
                }


                if (isInGlobalSystem)
                    continue;
            }


            if (slider.name ==
                    "VolumeSlider" &&
                volumeSlider == null)
            {
                volumeSlider =
                    slider;
            }


            if (slider.name ==
                    "SensitivitySlider" &&
                sensitivitySlider == null)
            {
                sensitivitySlider =
                    slider;
            }


            if (slider.name ==
                    "BrightnessSlider" &&
                brightnessSlider == null)
            {
                brightnessSlider =
                    slider;
            }


            if (slider.name ==
                    "GrainSlider" &&
                grainSlider == null)
            {
                grainSlider =
                    slider;
            }
        }
    }


    // =====================================================
    // ПОДПИСКА НА SLIDER
    // =====================================================

    private void SubscribeToSliders()
    {
        if (isSubscribed)
            return;


        if (volumeSlider != null)
        {
            volumeSlider
                .onValueChanged
                .AddListener(
                    OnVolumeChanged
                );
        }


        if (sensitivitySlider != null)
        {
            sensitivitySlider
                .onValueChanged
                .AddListener(
                    OnSensitivityChanged
                );
        }


        if (brightnessSlider != null)
        {
            brightnessSlider
                .onValueChanged
                .AddListener(
                    OnBrightnessChanged
                );
        }


        if (grainSlider != null)
        {
            grainSlider
                .onValueChanged
                .AddListener(
                    OnGrainChanged
                );
        }


        isSubscribed = true;
    }


    // =====================================================
    // ЗАГРУЗКА НАСТРОЕК
    // =====================================================

    public void LoadSettings()
    {
        float volume =
            PlayerPrefs.GetFloat(
                "Volume",
                0.5f
            );

        float sensitivity =
            PlayerPrefs.GetFloat(
                "Sensitivity",
                150f
            );

        float brightness =
            PlayerPrefs.GetFloat(
                "Brightness",
                0f
            );

        float grain =
            PlayerPrefs.GetFloat(
                "GrainIntensity",
                0.8f
            );


        if (volumeSlider != null)
        {
            volumeSlider.value =
                volume;
        }


        if (sensitivitySlider != null)
        {
            sensitivitySlider.value =
                sensitivity;
        }


        if (brightnessSlider != null)
        {
            brightnessSlider.value =
                brightness;
        }


        if (grainSlider != null)
        {
            grainSlider.value =
                grain;
        }


        ApplyVolume(
            volume
        );

        ApplySensitivity(
            sensitivity
        );

        ApplyBrightness(
            brightness
        );

        ApplyGrain(
            grain
        );
    }


    // =====================================================
    // ИЗМЕНЕНИЯ SLIDER
    // =====================================================

    private void OnVolumeChanged(
        float value)
    {
        ApplyVolume(
            value
        );

        PlayerPrefs.SetFloat(
            "Volume",
            value
        );

        PlayerPrefs.Save();
    }


    private void OnSensitivityChanged(
        float value)
    {
        ApplySensitivity(
            value
        );

        PlayerPrefs.SetFloat(
            "Sensitivity",
            value
        );

        PlayerPrefs.Save();
    }


    private void OnBrightnessChanged(
        float value)
    {
        ApplyBrightness(
            value
        );

        PlayerPrefs.SetFloat(
            "Brightness",
            value
        );

        PlayerPrefs.Save();
    }


    private void OnGrainChanged(
        float value)
    {
        ApplyGrain(
            value
        );

        PlayerPrefs.SetFloat(
            "GrainIntensity",
            value
        );

        PlayerPrefs.Save();
    }


    // =====================================================
    // ПРИМЕНЕНИЕ НАСТРОЕК
    // =====================================================

    private void ApplyVolume(
        float value)
    {
        AudioListener.volume =
            value;
    }


    private void ApplySensitivity(
        float value)
    {
        GameSettings.MouseSensitivity =
            value;
    }


    private void ApplyBrightness(
        float value)
    {
        if (colorAdjustments == null)
            return;

        colorAdjustments
            .postExposure
            .overrideState = true;

        colorAdjustments
            .postExposure
            .value = value;
    }


    private void ApplyGrain(
        float value)
    {
        if (filmGrain == null)
            return;

        filmGrain
            .intensity
            .overrideState = true;

        filmGrain
            .intensity
            .value =
            Mathf.Clamp01(
                value
            );
    }


    // =====================================================
    // GLOBAL VOLUME
    // =====================================================

    private void InitializeVisualEffects()
    {
        colorAdjustments = null;
        filmGrain = null;


        if (visualEffectsVolume == null)
            return;


        VolumeProfile profile =
            visualEffectsVolume.profile;


        if (profile == null)
            return;


        profile.TryGet(
            out colorAdjustments
        );

        profile.TryGet(
            out filmGrain
        );
    }
}
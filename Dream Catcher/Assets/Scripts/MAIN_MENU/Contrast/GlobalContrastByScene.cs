using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GlobalContrastByScene : MonoBehaviour
{
    [Header("Volume")]
    [Tooltip("Глобальный Volume с Color Adjustments.")]
    [SerializeField] private Volume volume;


    [Header("Контраст")]
    [Tooltip("Обычный контраст во всех сценах.")]
    [SerializeField] private float defaultContrast = 23f;

    [Tooltip("Контраст только в сцене MainMenu.")]
    [SerializeField] private float mainMenuContrast = 8f;


    private ColorAdjustments colorAdjustments;


    private void Awake()
    {
        if (volume == null)
            return;

        VolumeProfile profile = volume.profile;

        if (profile == null)
            return;

        profile.TryGet(out colorAdjustments);
    }


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void Start()
    {
        ApplyContrast(
            SceneManager.GetActiveScene().name
        );
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        ApplyContrast(scene.name);
    }


    private void ApplyContrast(string sceneName)
    {
        if (colorAdjustments == null)
            return;

        colorAdjustments.contrast.overrideState = true;

        colorAdjustments.contrast.value =
            sceneName == "MainMenu"
                ? mainMenuContrast
                : defaultContrast;
    }
}
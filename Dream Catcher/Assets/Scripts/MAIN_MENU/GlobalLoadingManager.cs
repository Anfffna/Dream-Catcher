using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GlobalLoadingManager : MonoBehaviour
{
    public static GlobalLoadingManager Instance { get; private set; }

    [Header("UI Elements")]
    public CanvasGroup fadeCanvasGroup;
    public LoadingSpinnerController loadingSpinner;

    [Header("Fade")]
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad обеспечивается PersistentObject

        // Инициализация: скрываем всё, но объекты активны
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;

        // Спиннер уже скрыт благодаря alpha = 0, ничего не вызываем
        // Не вызываем loadingSpinner.Hide(), так как он уже в начальном состоянии
    }

    public void StartLoading(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Скрываем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Показываем затемнение
        yield return StartCoroutine(FadeCanvas(fadeCanvasGroup, 0f, 1f, fadeDuration));
        if (loadingSpinner != null)
            loadingSpinner.Show();

        // Запускаем асинхронную загрузку
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Ждём, пока прогресс не достигнет 0.9 (почти загружено)
        while (asyncLoad.progress < 0.9f)
            yield return null;

        // Активируем сцену
        asyncLoad.allowSceneActivation = true;

        // Ждём, пока сцена полностью загрузится (isDone == true)
        while (!asyncLoad.isDone)
            yield return null;

        // Теперь сцена полностью загружена – скрываем загрузочный экран
        if (loadingSpinner != null)
            loadingSpinner.HideSmooth();
        yield return StartCoroutine(FadeCanvas(fadeCanvasGroup, 1f, 0f, fadeDuration));
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}
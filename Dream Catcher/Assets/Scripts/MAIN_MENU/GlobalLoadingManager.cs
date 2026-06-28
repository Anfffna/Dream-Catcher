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

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;
    }

    public void StartLoading(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Показываем затемнение (используем unscaledDeltaTime)
        yield return StartCoroutine(FadeCanvas(fadeCanvasGroup, 0f, 1f, fadeDuration));
        if (loadingSpinner != null)
            loadingSpinner.Show();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
            yield return null;

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
            yield return null;

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
            t += Time.unscaledDeltaTime; // <-- ключевая правка
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}
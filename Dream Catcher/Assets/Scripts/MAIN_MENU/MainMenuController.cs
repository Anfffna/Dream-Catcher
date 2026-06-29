using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    public string firstSceneName = "House";

    [Header("Testing")]
    public bool TestAgain = false;

    [Header("Panels")]
    public GameObject quitPanel;
    public GameObject settingsPanel;

    [Header("Load Panel")]
    public GameObject loadPanel;
    public LoadPanelController loadPanelController;

    [Header("Fade Settings")]
    public float fadeDuration = 0.3f;

    private CanvasGroup quitCG;
    private CanvasGroup settingsCG;
    private CanvasGroup loadCG;

    private Coroutine quitFadeCoroutine;
    private Coroutine settingsFadeCoroutine;
    private Coroutine loadFadeCoroutine;

    private void Start()
    {
        if (quitPanel != null)
        {
            quitCG = quitPanel.GetComponent<CanvasGroup>();

            if (quitCG != null)
            {
                HidePanelInstantly(quitPanel, quitCG);
            }
            else
            {
                Debug.LogWarning("quitPanel не имеет CanvasGroup! Анимация не будет работать.");
                quitPanel.SetActive(false);
            }
        }

        if (settingsPanel != null)
        {
            settingsCG = settingsPanel.GetComponent<CanvasGroup>();

            if (settingsCG != null)
            {
                HidePanelInstantly(settingsPanel, settingsCG);
            }
            else
            {
                Debug.LogWarning("settingsPanel не имеет CanvasGroup! Анимация не будет работать.");
                settingsPanel.SetActive(false);
            }
        }

        if (loadPanel != null)
        {
            loadCG = loadPanel.GetComponent<CanvasGroup>();

            if (loadCG != null)
            {
                HidePanelInstantly(loadPanel, loadCG);
            }
            else
            {
                Debug.LogWarning("loadPanel не имеет CanvasGroup! Добавь CanvasGroup на LoadPanel для плавного скрытия.");
                loadPanel.SetActive(false);
            }
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.PrepareNewGame();

            if (TestAgain)
            {
                SaveManager.Instance.DeleteAllSaves();

                if (loadPanelController != null)
                    loadPanelController.PrepareLoadPanel();
            }
        }

        if (GlobalLoadingManager.Instance != null)
            GlobalLoadingManager.Instance.StartLoading(firstSceneName);
        else
            SceneManager.LoadScene(firstSceneName);
    }

    public void OnContinueButton()
    {
        Time.timeScale = 1f;

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("MainMenuController: SaveManager.Instance == null. Продолжить невозможно.");
            return;
        }

        if (!SaveManager.Instance.HasAnySaves())
        {
            Debug.LogWarning("MainMenuController: сохранений нет. Продолжить невозможно.");
            return;
        }

        SaveManager.Instance.LoadLatestSave();
    }

    public void OnLoadButton()
    {
        if (loadPanel == null || loadCG == null)
            return;

        // Если панель загрузки уже открыта — второе нажатие плавно закрывает её.
        if (loadPanel.activeSelf)
        {
            StartFadeOut(ref loadFadeCoroutine, loadPanel, loadCG);
            return;
        }

        // Перед открытием загрузки закрываем всё остальное,
        // чтобы панели не перекрывали друг друга.
        CloseSettingsPanelIfOpen();
        CloseQuitPanelIfOpen();

        StartFadeIn(ref loadFadeCoroutine, loadPanel, loadCG);

        if (loadPanelController != null)
            loadPanelController.PrepareLoadPanel();
    }

    public void OnSettingsButton()
    {
        if (settingsPanel == null || settingsCG == null)
            return;

        // Если настройки уже открыты — второе нажатие плавно закрывает их.
        if (settingsPanel.activeSelf)
        {
            StartFadeOut(ref settingsFadeCoroutine, settingsPanel, settingsCG);
            return;
        }

        // Перед открытием настроек закрываем всё остальное.
        CloseLoadPanelIfOpen();
        CloseQuitPanelIfOpen();

        StartFadeIn(ref settingsFadeCoroutine, settingsPanel, settingsCG);

        SettingsManager.Instance?.RefreshSliders();
    }

    public void OnQuitButton()
    {
        if (quitPanel == null || quitCG == null)
            return;

        // Если панель выхода уже открыта — второе нажатие плавно закрывает её.
        if (quitPanel.activeSelf)
        {
            StartFadeOut(ref quitFadeCoroutine, quitPanel, quitCG);
            return;
        }

        // Перед открытием выхода закрываем всё остальное.
        CloseLoadPanelIfOpen();
        CloseSettingsPanelIfOpen();

        StartFadeIn(ref quitFadeCoroutine, quitPanel, quitCG);
    }

    public void ConfirmQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CancelQuit()
    {
        CloseQuitPanelIfOpen();
    }

    private void CloseLoadPanelIfOpen()
    {
        if (loadPanel != null && loadCG != null && loadPanel.activeSelf)
        {
            StartFadeOut(ref loadFadeCoroutine, loadPanel, loadCG);
        }
    }

    private void CloseSettingsPanelIfOpen()
    {
        if (settingsPanel != null && settingsCG != null && settingsPanel.activeSelf)
        {
            StartFadeOut(ref settingsFadeCoroutine, settingsPanel, settingsCG);
        }
    }

    private void CloseQuitPanelIfOpen()
    {
        if (quitPanel != null && quitCG != null && quitPanel.activeSelf)
        {
            StartFadeOut(ref quitFadeCoroutine, quitPanel, quitCG);
        }
    }

    private void HidePanelInstantly(GameObject panel, CanvasGroup cg)
    {
        if (panel == null || cg == null)
            return;

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        panel.SetActive(false);
    }

    private void StartFadeIn(ref Coroutine coroutine, GameObject panel, CanvasGroup cg)
    {
        if (panel == null || cg == null)
            return;

        if (coroutine != null)
            StopCoroutine(coroutine);

        coroutine = StartCoroutine(FadeInPanel(panel, cg));
    }

    private void StartFadeOut(ref Coroutine coroutine, GameObject panel, CanvasGroup cg)
    {
        if (panel == null || cg == null)
            return;

        if (coroutine != null)
            StopCoroutine(coroutine);

        coroutine = StartCoroutine(FadeOutPanel(panel, cg));
    }

    private IEnumerator FadeInPanel(GameObject panel, CanvasGroup cg)
    {
        if (panel == null || cg == null)
            yield break;

        panel.SetActive(true);

        cg.interactable = true;
        cg.blocksRaycasts = true;

        float timer = 0f;
        float startAlpha = cg.alpha;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 1f, timer / fadeDuration);
            yield return null;
        }

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private IEnumerator FadeOutPanel(GameObject panel, CanvasGroup cg)
    {
        if (panel == null || cg == null || !panel.activeSelf)
            yield break;

        // Сразу отключаем клики, чтобы закрывающаяся панель ничего не перекрывала.
        cg.interactable = false;
        cg.blocksRaycasts = false;

        float timer = 0f;
        float startAlpha = cg.alpha;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        panel.SetActive(false);
    }
}
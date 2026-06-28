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

    [Header("Fade Settings")]
    public float fadeDuration = 0.3f;

    private CanvasGroup quitCG;
    private CanvasGroup settingsCG;

    private void Start()
    {
        if (quitPanel != null)
        {
            quitCG = quitPanel.GetComponent<CanvasGroup>();
            if (quitCG != null)
            {
                quitCG.alpha = 0f;
                quitPanel.SetActive(false);
            }
            else
            {
                Debug.LogWarning("quitPanel не имеет CanvasGroup! Анимация не будет работать.");
            }
        }

        if (settingsPanel != null)
        {
            settingsCG = settingsPanel.GetComponent<CanvasGroup>();
            if (settingsCG != null)
            {
                settingsCG.alpha = 0f;
                settingsPanel.SetActive(false);
            }
            else
            {
                Debug.LogWarning("settingsPanel не имеет CanvasGroup! Анимация не будет работать.");
            }
        }
    }

    public void StartGame()
    {
        // Тестовая очистка сохранений перед новой игрой.
        // Работает только если в инспекторе включена галочка TestAgain.
        if (TestAgain)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteAllSaves();
            }
            else
            {
                Debug.LogWarning("MainMenuController: TestAgain включен, но SaveManager.Instance == null. Сохранения не очищены.");
            }
        }

        if (GlobalLoadingManager.Instance != null)
            GlobalLoadingManager.Instance.StartLoading(firstSceneName);
        else
            SceneManager.LoadScene(firstSceneName);
    }

    public void OnSettingsButton()
    {
        if (settingsCG == null || settingsPanel == null) return;
        if (!settingsPanel.activeSelf)
            StartCoroutine(FadeInPanel(settingsPanel, settingsCG));
        else
            StartCoroutine(FadeOutPanel(settingsPanel, settingsCG));
    }

    public void OnQuitButton()
    {
        if (quitCG == null || quitPanel == null) return;
        if (!quitPanel.activeSelf)
            StartCoroutine(FadeInPanel(quitPanel, quitCG));
        else
            StartCoroutine(FadeOutPanel(quitPanel, quitCG));
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
        if (quitCG != null && quitPanel != null)
            StartCoroutine(FadeOutPanel(quitPanel, quitCG));
    }

    private IEnumerator FadeInPanel(GameObject panel, CanvasGroup cg)
    {
        if (panel == null || cg == null) yield break;

        panel.SetActive(true);
        cg.alpha = 0f;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator FadeOutPanel(GameObject panel, CanvasGroup cg)
    {
        if (panel == null || cg == null || !panel.activeSelf) yield break;

        float timer = 0f;
        float startAlpha = cg.alpha;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }
        cg.alpha = 0f;
        panel.SetActive(false);
    }
}
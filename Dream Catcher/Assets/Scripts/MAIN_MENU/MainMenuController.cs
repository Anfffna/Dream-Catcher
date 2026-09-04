using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    public string firstSceneName = "House";

    [Header("Testing")]
    public bool TestAgain = false;

    [Header("Audio")]
    public AudioClip[] buttonClickSounds;
    private AudioSource buttonAudioSource;
    private AudioClip lastButtonClickSound;

    [Header("Panels")]
    public GameObject quitPanel;
    public GameObject settingsPanel;

    [Header("Load Panel")]
    public GameObject loadPanel;
    public LoadPanelController loadPanelController;

    [Header("Fade Settings")]
    public float fadeDuration = 0.3f;

    [Header("Cursor")]

    [Tooltip(
    "Родитель, внутри которого находятся " +
    "кнопки главного меню."
    )]
    [SerializeField]
    private Transform cursorButtonsRoot;

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

        StartCoroutine(
            SetupMainMenuCursor()
        );
    }

    public void StartGame()
    {
        PlayButtonSound();
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

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.StartLoading(
                firstSceneName
            );
        }
        else
        {
            SceneManager.LoadScene(
                firstSceneName
            );
        }
    }

    public void OnContinueButton()
    {
        PlayButtonSound();
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

    // =========================================================
    // ПЕРЕХОД ИЗ АНИМИРОВАННОГО ПУЛЬТА
    // =========================================================

    public void PlayTransitionButtonSound()
    {
        /*
         * Используем тот же самый звук и тот же AudioSource,
         * что и обычные кнопки MainMenu.
         *
         * Этот метод вызывается непосредственно
         * в момент Animation Event нажатия пальцем.
         */
        PlayButtonSound();
    }


    public void StartNewGameWithoutLoadingScreen()
    {
        /*
         * ВАЖНО:
         * здесь НЕТ PlayButtonSound().
         *
         * Звук уже был проигран раньше,
         * в момент физического нажатия кнопки.
         */

        Time.timeScale = 1f;


        /*
         * Полностью сохраняем существующую
         * подготовку новой игры.
         */
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.PrepareNewGame();


            if (TestAgain)
            {
                SaveManager.Instance.DeleteAllSaves();


                if (loadPanelController != null)
                {
                    loadPanelController
                        .PrepareLoadPanel();
                }
            }
        }


        /*
         * Главное отличие от обычного StartGame():
         *
         * LoadingManager НЕ вызываем.
         *
         * Загружаем House напрямую.
         */
        SceneManager.LoadSceneAsync(
            firstSceneName
        );
    }


    public void ContinueWithoutButtonSound()
    {
        /*
         * Здесь тоже нет PlayButtonSound(),
         * потому что он уже прозвучал
         * в момент Animation Event.
         */

        Time.timeScale = 1f;


        if (SaveManager.Instance == null)
        {
            Debug.LogWarning(
                "MainMenuController: SaveManager.Instance == null. " +
                "Продолжить невозможно."
            );

            return;
        }


        if (!SaveManager.Instance.HasAnySaves())
        {
            Debug.LogWarning(
                "MainMenuController: сохранений нет. " +
                "Продолжить невозможно."
            );

            return;
        }


        /*
         * Это всё ещё обычная загрузка сохранения.
         *
         * Поэтому привычный LoadingManager
         * продолжит работать как раньше.
         */
        SaveManager.Instance.LoadLatestSave();
    }

    public void OnLoadButton()
    {
        PlayButtonSound();
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
        PlayButtonSound();
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
        PlayButtonSound();
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

    private void PlayButtonSound()
    {
        if (buttonClickSounds == null || buttonClickSounds.Length == 0)
            return;

        // Берём существующий AudioSource или создаём автоматически
        if (buttonAudioSource == null)
        {
            buttonAudioSource = GetComponent<AudioSource>();

            if (buttonAudioSource == null)
            {
                buttonAudioSource = gameObject.AddComponent<AudioSource>();
                buttonAudioSource.playOnAwake = false;
                buttonAudioSource.loop = false;
                buttonAudioSource.spatialBlend = 0f;
            }
        }

        // Считаем доступные клипы,
        // исключая null и только что проигранный
        int availableCount = 0;

        for (int i = 0; i < buttonClickSounds.Length; i++)
        {
            if (buttonClickSounds[i] != null &&
                buttonClickSounds[i] != lastButtonClickSound)
            {
                availableCount++;
            }
        }

        AudioClip selectedClip = null;

        // Есть хотя бы один другой звук
        if (availableCount > 0)
        {
            int randomIndex = Random.Range(0, availableCount);
            int currentIndex = 0;

            for (int i = 0; i < buttonClickSounds.Length; i++)
            {
                AudioClip clip = buttonClickSounds[i];

                if (clip == null || clip == lastButtonClickSound)
                    continue;

                if (currentIndex == randomIndex)
                {
                    selectedClip = clip;
                    break;
                }

                currentIndex++;
            }
        }
        else
        {
            // Если в списке фактически только один рабочий звук,
            // разрешаем использовать его снова
            for (int i = 0; i < buttonClickSounds.Length; i++)
            {
                if (buttonClickSounds[i] != null)
                {
                    selectedClip = buttonClickSounds[i];
                    break;
                }
            }
        }

        if (selectedClip == null)
            return;

        lastButtonClickSound = selectedClip;
        buttonAudioSource.PlayOneShot(selectedClip);
    }

    private IEnumerator SetupMainMenuCursor()
    {
        // Пока MainMenu готовится, курсору показываться нельзя.
        if (PauseManager.Instance != null)
            PauseManager.Instance.SetCursorBlocked(true);

        while (LoadingManager.IsLoadingScreenBlockingPause())
            yield return null;

        // Loading уже закончен, но даём его последнему
        // полупрозрачному кадру полностью исчезнуть.
        yield return new WaitForSecondsRealtime(0.23f);

        if (PauseManager.Instance == null)
            yield break;

        PauseManager.Instance.SetCursorBlocked(false);
        PauseManager.Instance.ShowUICursor();

        PauseManager.Instance.AddCursorEventsToButtons(
            cursorButtonsRoot
        );
    }

    public void ConfirmQuit()
    {
        MainMenuBlinkTransition blinkTransition =
            FindFirstObjectByType<MainMenuBlinkTransition>(
                FindObjectsInactive.Include
            );

        if (blinkTransition == null)
        {
            Debug.LogWarning(
                "MainMenuController: MainMenuBlinkTransition не найден."
            );

            return;
        }

        blinkTransition.PlayQuitAndExit();
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
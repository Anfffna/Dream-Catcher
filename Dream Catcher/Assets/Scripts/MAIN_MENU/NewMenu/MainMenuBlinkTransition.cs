using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuBlinkTransition : MonoBehaviour
{
    [Header("Веки")]

    [Tooltip("Верхнее веко.")]
    [SerializeField]
    private RectTransform topEyelid;

    [Tooltip("Нижнее веко.")]
    [SerializeField]
    private RectTransform bottomEyelid;

    [Tooltip("CanvasGroup общего Canvas с веками.")]
    [SerializeField]
    private CanvasGroup blinkCanvasGroup;


    [Header("Закрытое положение")]

    [Tooltip("Pos Y верхнего века при полностью закрытых глазах.")]
    [SerializeField]
    private float topClosedY = 180f;

    [Tooltip("Pos Y нижнего века при полностью закрытых глазах.")]
    [SerializeField]
    private float bottomClosedY = -180f;


    [Header("Размытие")]

    [Tooltip(
        "Отдельный Global Volume для моргания. " +
        "Назначается вручную в Inspector."
    )]
    [SerializeField]
    private Volume blurVolume;

    [Tooltip(
        "На какой части закрывания начинает появляться размытие. " +
        "0 = с самого начала."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float blurStartNormalized = 0f;

    [Tooltip(
        "Aperture в начале. " +
        "Большое значение = почти нет размытия."
    )]
    [Range(1f, 32f)]
    [SerializeField]
    private float sharpAperture = 32f;

    [Tooltip(
        "Aperture в конце закрывания. " +
        "Маленькое значение = сильное размытие."
    )]
    [Range(1f, 32f)]
    [SerializeField]
    private float blurredAperture = 1f;


    [Header("Скорость")]

    [Tooltip("Сколько секунд закрываются глаза.")]
    [SerializeField]
    private float closeDuration = 0.42f;

    [Tooltip(
        "За сколько секунд закрытые веки растворяются " +
        "над Loading Screen или ScreenSaver."
    )]
    [SerializeField]
    private float fadeAwayDuration = 0.5f;


    [Header("Меню")]

    [Tooltip("MainMenuController текущей сцены.")]
    [SerializeField]
    private MainMenuController mainMenuController;


    private DepthOfField blinkDepthOfField;

    private bool transitionRunning;

    private float topStartY;
    private float bottomStartY;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Canvas.ForceUpdateCanvases();

        CaptureInitialState();

        InitializeBlur();

        SetEyesOpenImmediate();

        if (blinkCanvasGroup != null)
        {
            blinkCanvasGroup.alpha = 0f;
            blinkCanvasGroup.interactable = false;
            blinkCanvasGroup.blocksRaycasts = false;
        }
    }


    // =========================================================
    // НАСТРОЙКА BLUR
    // =========================================================

    private void InitializeBlur()
    {
        blinkDepthOfField = null;

        if (blurVolume == null)
            return;

        /*
         * Берём runtime-копию назначенного Volume Profile.
         *
         * Никаких FindObject здесь нет.
         * Volume уже назначен руками в Inspector.
         *
         * TryGet выполняется только один раз при Start().
         */
        VolumeProfile runtimeProfile =
            blurVolume.profile;

        if (runtimeProfile == null)
            return;

        runtimeProfile.TryGet(
            out blinkDepthOfField
        );

        if (blinkDepthOfField != null)
        {
            blinkDepthOfField
                .aperture
                .overrideState = true;

            blinkDepthOfField
                .aperture
                .value = sharpAperture;
        }

        /*
         * Пока моргания нет,
         * наш отдельный Volume ничего не делает.
         */
        blurVolume.weight = 0f;
    }


    // =========================================================
    // НОВАЯ ИГРА
    // =========================================================

    public void PlayNewGame()
    {
        if (transitionRunning)
            return;

        if (!FindMainMenuController())
            return;


        mainMenuController
            .PlayTransitionButtonSound();


        StartCoroutine(
            BlinkAndLoad(false)
        );
    }


    // =========================================================
    // ПРОДОЛЖИТЬ
    // =========================================================

    public void PlayContinue()
    {
        if (transitionRunning)
            return;

        if (!FindMainMenuController())
            return;


        /*
         * Если сохранений нет,
         * моргание не запускаем.
         */
        if (SaveManager.Instance == null ||
            !SaveManager.Instance.HasAnySaves())
        {
            mainMenuController
                .OnContinueButton();

            return;
        }


        mainMenuController
            .PlayTransitionButtonSound();


        StartCoroutine(
            BlinkAndLoad(true)
        );
    }


    // =========================================================
    // ВЫХОД ИЗ ИГРЫ
    // =========================================================

    public void PlayQuitAndExit()
    {
        if (transitionRunning)
            return;


        StartCoroutine(
            QuitBlinkRoutine()
        );
    }


    // =========================================================
    // ПОДГОТОВКА ПЕРЕХОДА
    // =========================================================

    private void PrepareTransition()
    {
        transitionRunning = true;


        /*
         * Каждый новый переход начинаем
         * из настоящего открытого положения.
         */
        SetEyesOpenImmediate();


        if (blinkCanvasGroup != null)
        {
            blinkCanvasGroup.alpha = 1f;
            blinkCanvasGroup.interactable = false;
            blinkCanvasGroup.blocksRaycasts = true;
        }


        if (PauseManager.Instance != null)
        {
            PauseManager.Instance
                .SetCursorBlocked(true);
        }


        /*
         * Ставим Aperture в резкое состояние,
         * а сам Volume включаем сразу.
         *
         * Weight больше НЕ анимируется.
         */
        SetBlurApertureImmediate(
            sharpAperture
        );


        if (blurVolume != null)
        {
            blurVolume.weight = 1f;
        }
    }


    // =========================================================
    // NEW GAME / CONTINUE
    // =========================================================

    private IEnumerator BlinkAndLoad(
        bool continueGame)
    {
        PrepareTransition();


        // =====================================================
        // 1. ЗАКРЫВАЕМ ГЛАЗА
        // =====================================================

        yield return CloseEyes();


        // =====================================================
        // CONTINUE
        // =====================================================

        if (continueGame)
        {
            mainMenuController
                .ContinueWithoutButtonSound();


            /*
             * Ждём запуска LoadingManager.
             */
            float loadingStartTimeout = 5f;


            while (loadingStartTimeout > 0f)
            {
                if (LoadingManager.Instance != null &&
                    LoadingManager.Instance.IsLoading)
                {
                    break;
                }


                loadingStartTimeout -=
                    Time.unscaledDeltaTime;


                yield return null;
            }


            /*
             * Ждём, пока loading background
             * полностью закроет экран.
             */
            if (LoadingManager.Instance != null &&
                LoadingManager.Instance.IsLoading)
            {
                float backgroundTimeout = 5f;


                while (
                    !LoadingManager.Instance
                        .IsLoadingBackgroundReady &&
                    backgroundTimeout > 0f)
                {
                    backgroundTimeout -=
                        Time.unscaledDeltaTime;


                    yield return null;
                }
            }


            /*
             * Loading Screen уже под веками.
             * Растворяем веки и убираем наш blur.
             */
            yield return
                FadeClosedEyesAway();


            transitionRunning = false;

            yield break;
        }


        // =====================================================
        // NEW GAME
        // =====================================================

        string oldSceneName =
            SceneManager
                .GetActiveScene()
                .name;


        /*
         * New Game загружает House напрямую,
         * без обычного Loading Screen.
         */
        mainMenuController
            .StartNewGameWithoutLoadingScreen();


        // =====================================================
        // ЖДЁМ HOUSE
        // =====================================================

        float sceneTimeout = 30f;


        while (
            SceneManager
                .GetActiveScene()
                .name ==
            oldSceneName)
        {
            sceneTimeout -=
                Time.unscaledDeltaTime;


            if (sceneTimeout <= 0f)
            {
                yield return
                    RecoverAfterFailedLoad();

                yield break;
            }


            yield return null;
        }


        // =====================================================
        // ИЩЕМ SCREEN SAVER
        // =====================================================

        ScreenSaver screenSaver = null;

        float screenSaverTimeout = 10f;


        while (
            screenSaver == null &&
            screenSaverTimeout > 0f)
        {
            screenSaver =
                FindFirstObjectByType<ScreenSaver>(
                    FindObjectsInactive.Include
                );


            if (screenSaver != null)
                break;


            screenSaverTimeout -=
                Time.unscaledDeltaTime;


            yield return null;
        }


        // =====================================================
        // ЖДЁМ ФОН SCREEN SAVER
        // =====================================================

        if (screenSaver != null)
        {
            float backgroundTimeout = 5f;


            while (
                !screenSaver.IsBackgroundReady &&
                backgroundTimeout > 0f)
            {
                backgroundTimeout -=
                    Time.unscaledDeltaTime;


                yield return null;
            }
        }


        /*
         * Даём House реально отрисовать
         * хотя бы один кадр под веками.
         */
        yield return
            new WaitForEndOfFrame();


        // =====================================================
        // РАСТВОРЯЕМ ВЕКИ
        // =====================================================

        yield return
            FadeClosedEyesForNewGame();


        /*
         * New Game не использует обычный LoadingManager,
         * поэтому снимаем блокировку сами.
         */
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance
                .SetCursorBlocked(false);

            PauseManager.Instance
                .HideGameplayCursor();
        }


        transitionRunning = false;
    }


    // =========================================================
    // QUIT
    // =========================================================

    private IEnumerator QuitBlinkRoutine()
    {
        PrepareTransition();


        yield return CloseEyes();


        yield return
            new WaitForEndOfFrame();


#if UNITY_EDITOR

        UnityEditor.EditorApplication
            .isPlaying = false;

#else

        Application.Quit();

#endif
    }


    // =========================================================
    // ЗАКРЫВАНИЕ ГЛАЗ
    // =========================================================

    private IEnumerator CloseEyes()
    {
        if (topEyelid == null ||
            bottomEyelid == null)
        {
            yield break;
        }


        float timer = 0f;


        float startTopY =
            topStartY;

        float startBottomY =
            bottomStartY;


        if (closeDuration <= 0f)
        {
            SetEyesClosedImmediate();

            SetBlurApertureImmediate(
                blurredAperture
            );

            yield break;
        }


        while (timer < closeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    closeDuration
                );


            float smoothEyesT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            // =================================================
            // ВЕРХНЕЕ ВЕКО
            // =================================================

            SetTopY(
                Mathf.Lerp(
                    startTopY,
                    topClosedY,
                    smoothEyesT
                )
            );


            // =================================================
            // НИЖНЕЕ ВЕКО
            // =================================================

            SetBottomY(
                Mathf.Lerp(
                    startBottomY,
                    bottomClosedY,
                    smoothEyesT
                )
            );


            // =================================================
            // BLUR
            // =================================================

            if (t >= blurStartNormalized)
            {
                float blurT =
                    Mathf.InverseLerp(
                        blurStartNormalized,
                        1f,
                        t
                    );


                /*
                 * ВАЖНО:
                 * здесь больше не меняется Volume Weight.
                 *
                 * Focus Distance и Focal Length
                 * остаются такими, какими ты их
                 * настроила в Volume Profile.
                 *
                 * Двигается только Aperture.
                 */
                float aperture =
                    Mathf.Lerp(
                        sharpAperture,
                        blurredAperture,
                        blurT
                    );


                SetBlurApertureImmediate(
                    aperture
                );
            }


            yield return null;
        }


        /*
         * Гарантируем точное
         * конечное состояние.
         */
        SetEyesClosedImmediate();


        SetBlurApertureImmediate(
            blurredAperture
        );
    }


    // =========================================================
    // FADE ДЛЯ CONTINUE
    // =========================================================

    private IEnumerator FadeClosedEyesAway()
    {
        float timer = 0f;


        SetEyesClosedImmediate();


        if (fadeAwayDuration <= 0f)
        {
            HideBlinkCanvasImmediate();
            DisableBlinkBlurImmediate();
            SetEyesOpenImmediate();

            yield break;
        }


        while (timer < fadeAwayDuration)
        {
            float frameDelta =
                Mathf.Min(
                    Time.unscaledDeltaTime,
                    0.05f
                );


            timer += frameDelta;


            float t =
                Mathf.Clamp01(
                    timer /
                    fadeAwayDuration
                );


            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            /*
             * Веки физически остаются закрытыми.
             */
            SetEyesClosedImmediate();


            /*
             * Растворяем Canvas век.
             */
            if (blinkCanvasGroup != null)
            {
                blinkCanvasGroup.alpha =
                    Mathf.Lerp(
                        1f,
                        0f,
                        smoothT
                    );
            }


            /*
             * Одновременно возвращаем
             * Aperture к резкому состоянию.
             */
            SetBlurApertureImmediate(
                Mathf.Lerp(
                    blurredAperture,
                    sharpAperture,
                    smoothT
                )
            );


            yield return null;
        }


        HideBlinkCanvasImmediate();

        DisableBlinkBlurImmediate();

        /*
         * Canvas уже полностью прозрачный,
         * поэтому незаметно возвращаем веки
         * за экран.
         */
        SetEyesOpenImmediate();
    }


    // =========================================================
    // FADE ДЛЯ NEW GAME
    // =========================================================

    private IEnumerator FadeClosedEyesForNewGame()
    {
        float timer = 0f;


        SetEyesClosedImmediate();


        if (fadeAwayDuration <= 0f)
        {
            HideBlinkCanvasImmediate();
            DisableBlinkBlurImmediate();
            SetEyesOpenImmediate();

            yield break;
        }


        while (timer < fadeAwayDuration)
        {
            float frameDelta =
                Mathf.Min(
                    Time.unscaledDeltaTime,
                    0.05f
                );


            timer += frameDelta;


            float t =
                Mathf.Clamp01(
                    timer /
                    fadeAwayDuration
                );


            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            /*
             * Веки не открываются физически.
             */
            SetEyesClosedImmediate();


            /*
             * Только растворяем их
             * над уже готовым ScreenSaver.
             */
            if (blinkCanvasGroup != null)
            {
                blinkCanvasGroup.alpha =
                    Mathf.Lerp(
                        1f,
                        0f,
                        smoothT
                    );
            }


            /*
             * Наш MainMenu blur одновременно
             * плавно исчезает.
             */
            SetBlurApertureImmediate(
                Mathf.Lerp(
                    blurredAperture,
                    sharpAperture,
                    smoothT
                )
            );


            yield return null;
        }


        HideBlinkCanvasImmediate();

        DisableBlinkBlurImmediate();

        SetEyesOpenImmediate();
    }


    // =========================================================
    // BLINK CANVAS
    // =========================================================

    private void HideBlinkCanvasImmediate()
    {
        if (blinkCanvasGroup == null)
            return;


        blinkCanvasGroup.alpha = 0f;
        blinkCanvasGroup.interactable = false;
        blinkCanvasGroup.blocksRaycasts = false;
    }


    // =========================================================
    // ИСХОДНОЕ СОСТОЯНИЕ ВЕК
    // =========================================================

    private void CaptureInitialState()
    {
        if (topEyelid != null)
        {
            topStartY =
                topEyelid
                    .anchoredPosition
                    .y;
        }


        if (bottomEyelid != null)
        {
            bottomStartY =
                bottomEyelid
                    .anchoredPosition
                    .y;
        }
    }


    // =========================================================
    // ПОЗИЦИИ ВЕК
    // =========================================================

    private void SetEyesClosedImmediate()
    {
        SetTopY(
            topClosedY
        );

        SetBottomY(
            bottomClosedY
        );
    }


    private void SetEyesOpenImmediate()
    {
        SetTopY(
            topStartY
        );

        SetBottomY(
            bottomStartY
        );
    }


    private void SetTopY(
        float y)
    {
        if (topEyelid == null)
            return;


        Vector2 position =
            topEyelid
                .anchoredPosition;


        position.y = y;


        topEyelid.anchoredPosition =
            position;
    }


    private void SetBottomY(
        float y)
    {
        if (bottomEyelid == null)
            return;


        Vector2 position =
            bottomEyelid
                .anchoredPosition;


        position.y = y;


        bottomEyelid.anchoredPosition =
            position;
    }


    // =========================================================
    // BLUR
    // =========================================================

    private void SetBlurApertureImmediate(
        float aperture)
    {
        if (blinkDepthOfField == null)
            return;


        blinkDepthOfField
            .aperture
            .value =
            Mathf.Clamp(
                aperture,
                1f,
                32f
            );
    }


    private void DisableBlinkBlurImmediate()
    {
        SetBlurApertureImmediate(
            sharpAperture
        );


        if (blurVolume != null)
        {
            blurVolume.weight = 0f;
        }
    }


    // =========================================================
    // MAIN MENU CONTROLLER
    // =========================================================

    private bool FindMainMenuController()
    {
        /*
         * Этот поиск нужен из-за того,
         * что BlinkTransition persistent,
         * а MainMenuController создаётся
         * заново при каждом входе в MainMenu.
         *
         * Он выполняется только при нажатии
         * New Game / Continue, не в Update.
         */
        if (mainMenuController != null)
            return true;


        mainMenuController =
            FindFirstObjectByType<MainMenuController>(
                FindObjectsInactive.Include
            );


        return mainMenuController != null;
    }


    // =========================================================
    // ЕСЛИ NEW GAME НЕ ЗАГРУЗИЛСЯ
    // =========================================================

    private IEnumerator RecoverAfterFailedLoad()
    {
        yield return
            FadeClosedEyesAway();


        if (PauseManager.Instance != null)
        {
            PauseManager.Instance
                .SetCursorBlocked(false);

            PauseManager.Instance
                .ShowUICursor();
        }


        transitionRunning = false;
    }
}
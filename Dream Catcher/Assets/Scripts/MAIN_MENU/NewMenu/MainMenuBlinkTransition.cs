using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuBlinkTransition : MonoBehaviour
{
    [Header("Веки")]
    [Tooltip("Верхнее веко.")]
    [SerializeField] private RectTransform topEyelid;

    [Tooltip("Нижнее веко.")]
    [SerializeField] private RectTransform bottomEyelid;

    [Tooltip("CanvasGroup общего Canvas с веками.")]
    [SerializeField] private CanvasGroup blinkCanvasGroup;


    [Header("Закрытое положение")]
    [Tooltip("Pos Y верхнего века при полностью закрытых глазах.")]
    [SerializeField] private float topClosedY = 180f;

    [Tooltip("Pos Y нижнего века при полностью закрытых глазах.")]
    [SerializeField] private float bottomClosedY = -180f;


    [Header("Размытие")]
    [Tooltip(
        "Рабочий Global Volume с блюром. " +
        "Назначь тот же Volume, который используется паузой."
    )]
    [SerializeField] private Volume blurVolume;

    [Tooltip(
        "На какой части закрывания начинает появляться блюр. " +
        "Например 0.6 = блюр начинает усиливаться после 60% закрывания."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float blurStartNormalized = 0.6f;

    [Tooltip("Максимальный Weight блюра.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxBlurWeight = 1f;


    [Header("Скорость")]
    [Tooltip("Сколько секунд закрываются глаза.")]
    [SerializeField] private float closeDuration = 0.42f;

    [Tooltip(
        "За сколько секунд закрытые веки растворяются " +
        "над загрузочным экраном или ScreenSaver."
    )]
    [SerializeField] private float fadeAwayDuration = 0.5f;


    [Header("Меню")]
    [Tooltip("Существующий MainMenuController.")]
    [SerializeField] private MainMenuController mainMenuController;


    private bool transitionRunning;
    private bool madePersistent;

    private float topStartY;
    private float bottomStartY;

    private float initialBlurWeight;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Canvas.ForceUpdateCanvases();

        CaptureInitialState();

        if (blinkCanvasGroup != null)
        {
            blinkCanvasGroup.alpha = 1f;
            blinkCanvasGroup.interactable = false;
            blinkCanvasGroup.blocksRaycasts = false;
        }
    }


    // =========================================================
    // НОВАЯ ИГРА
    // =========================================================

    public void PlayNewGame()
    {
        if (transitionRunning)
            return;

        if (mainMenuController == null)
            return;


        /*
         * Звук происходит сразу:
         * этот метод вызывается Animation Event'ом
         * в момент физического нажатия кнопки пульта.
         */
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

        if (mainMenuController == null)
            return;


        /*
         * Если сохранений нет,
         * не запускаем моргание.
         */
        if (SaveManager.Instance == null ||
            !SaveManager.Instance.HasAnySaves())
        {
            mainMenuController
                .OnContinueButton();

            return;
        }


        /*
         * Звук — в момент физического
         * нажатия пальцем.
         */
        mainMenuController
            .PlayTransitionButtonSound();


        StartCoroutine(
            BlinkAndLoad(true)
        );
    }


    // =========================================================
    // ПОДТВЕРЖДЕНИЕ ВЫХОДА
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
    // ОБЩАЯ ПОДГОТОВКА
    // =========================================================

    private void PrepareTransition()
    {
        transitionRunning = true;


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
         * Просто запоминаем текущее состояние
         * назначенного Global Volume.
         *
         * Никаких поисков Volume здесь больше нет.
         */
        initialBlurWeight =
            blurVolume != null
                ? blurVolume.weight
                : 0f;
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


        /*
         * Здесь:
         *
         * Top = 180
         * Bottom = -180
         * CanvasGroup Alpha = 1
         * Blur = maxBlurWeight
         */


        /*
         * Веки должны пережить
         * смену сцены.
         */
        MakePersistent();


        // =====================================================
        // ПРОДОЛЖИТЬ
        // =====================================================

        if (continueGame)
        {
            /*
             * Звук здесь уже НЕ проигрываем.
             *
             * Он прозвучал раньше,
             * в момент Animation Event.
             */
            mainMenuController
                .ContinueWithoutButtonSound();


            /*
             * Ждём, пока обычный LoadingManager
             * реально начнёт загрузку.
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
             * Ждём, пока его background
             * полностью проявится.
             *
             * До этого веки остаются
             * полностью непрозрачными.
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
             * Теперь под веками уже
             * готов обычный Loading Screen.
             */
            yield return
                FadeClosedEyesAway();


            transitionRunning = false;


            /*
             * LoadingManager продолжает
             * свою загрузку самостоятельно.
             */
            Destroy(gameObject);

            yield break;
        }


        // =====================================================
        // НОВАЯ ИГРА
        // =====================================================

        string oldSceneName =
            SceneManager
                .GetActiveScene()
                .name;


        /*
         * Только New Game:
         *
         * House загружается напрямую,
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


        /*
         * House уже активен,
         * но веки всё ещё полностью закрыты.
         */


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
         * Даём Unity реально отрисовать
         * ScreenSaver хотя бы один кадр.
         */
        yield return
            new WaitForEndOfFrame();


        // =====================================================
        // РАСТВОРЯЕМ ВЕКИ НАД SCREEN SAVER
        // =====================================================

        yield return
            FadeClosedEyesForNewGame(
                screenSaver
            );


        /*
         * В New Game LoadingManager
         * вообще не запускался.
         *
         * Поэтому нашу блокировку курсора
         * снимаем самостоятельно.
         */
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance
                .SetCursorBlocked(false);

            PauseManager.Instance
                .HideGameplayCursor();
        }


        transitionRunning = false;

        Destroy(gameObject);
    }


    // =========================================================
    // QUIT
    // =========================================================

    private IEnumerator QuitBlinkRoutine()
    {
        PrepareTransition();


        /*
         * Просто закрываем глаза.
         *
         * Никакого LoadingManager
         * при выходе больше нет.
         */
        yield return CloseEyes();


        /*
         * Даём полностью закрытому экрану
         * реально отрисоваться.
         */
        yield return
            new WaitForEndOfFrame();


        /*
         * И только после этого
         * закрываем игру.
         */
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


        /*
         * Берём именно исходные позиции,
         * в которых веки стояли в сцене.
         */
        float startTopY =
            topStartY;

        float startBottomY =
            bottomStartY;


        /*
         * На случай, если Weight
         * кто-то поменял после Start().
         */
        if (blurVolume != null)
        {
            initialBlurWeight =
                blurVolume.weight;
        }


        if (closeDuration <= 0f)
        {
            SetEyesClosedImmediate();

            SetBlurImmediate(
                maxBlurWeight
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


            float smoothT =
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
                    smoothT
                )
            );


            // =================================================
            // НИЖНЕЕ ВЕКО
            // =================================================

            SetBottomY(
                Mathf.Lerp(
                    startBottomY,
                    bottomClosedY,
                    smoothT
                )
            );


            // =================================================
            // BLUR
            // =================================================

            if (blurVolume != null &&
                t >= blurStartNormalized)
            {
                /*
                 * Превращаем участок
                 * blurStartNormalized -> 1
                 *
                 * снова в диапазон 0 -> 1.
                 */
                float blurT =
                    Mathf.InverseLerp(
                        blurStartNormalized,
                        1f,
                        t
                    );


                float smoothBlurT =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        blurT
                    );


                blurVolume.weight =
                    Mathf.Lerp(
                        initialBlurWeight,
                        maxBlurWeight,
                        smoothBlurT
                    );
            }


            yield return null;
        }


        /*
         * Гарантируем точное
         * конечное положение.
         */
        SetEyesClosedImmediate();

        SetBlurImmediate(
            maxBlurWeight
        );
    }


    // =========================================================
    // FADE ЗАКРЫТЫХ ВЕК ДЛЯ CONTINUE
    // =========================================================

    private IEnumerator FadeClosedEyesAway()
    {
        float timer = 0f;


        float startBlurWeight =
            blurVolume != null
                ? blurVolume.weight
                : 0f;


        /*
         * Веки физически НЕ открываются.
         */
        SetEyesClosedImmediate();


        if (fadeAwayDuration <= 0f)
        {
            if (blinkCanvasGroup != null)
            {
                blinkCanvasGroup.alpha = 0f;
                blinkCanvasGroup.blocksRaycasts = false;
            }


            SetBlurImmediate(
                initialBlurWeight
            );


            yield break;
        }


        while (timer < fadeAwayDuration)
        {
            /*
             * После тяжёлого кадра загрузки
             * не разрешаем одному огромному
             * deltaTime проглотить весь fade.
             */
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
             * Веки всё время остаются
             * в закрытом положении.
             */
            SetEyesClosedImmediate();


            /*
             * Растворяем только CanvasGroup.
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
             * И одновременно возвращаем
             * наш blur к исходному состоянию.
             */
            if (blurVolume != null)
            {
                blurVolume.weight =
                    Mathf.Lerp(
                        startBlurWeight,
                        initialBlurWeight,
                        smoothT
                    );
            }


            yield return null;
        }


        if (blinkCanvasGroup != null)
        {
            blinkCanvasGroup.alpha = 0f;
            blinkCanvasGroup.blocksRaycasts = false;
        }


        SetBlurImmediate(
            initialBlurWeight
        );
    }


    // =========================================================
    // FADE ДЛЯ NEW GAME
    // =========================================================

    private IEnumerator FadeClosedEyesForNewGame(
        ScreenSaver screenSaver)
    {
        float timer = 0f;


        /*
         * В House ScreenSaver сам управляет
         * своим wake-up blur.
         *
         * Если это тот же самый Volume,
         * мы после загрузки House
         * больше Weight не трогаем.
         */
        bool screenSaverOwnsSameBlur =
            screenSaver != null &&
            screenSaver.wakeUpBlurVolume != null &&
            screenSaver.wakeUpBlurVolume ==
                blurVolume;


        float startBlurWeight =
            blurVolume != null
                ? blurVolume.weight
                : 0f;


        SetEyesClosedImmediate();


        if (fadeAwayDuration <= 0f)
        {
            if (blinkCanvasGroup != null)
            {
                blinkCanvasGroup.alpha = 0f;
                blinkCanvasGroup.blocksRaycasts = false;
            }


            /*
             * Если ScreenSaver использует
             * другой Volume,
             * возвращаем наш обратно.
             *
             * Если тот же —
             * теперь им владеет ScreenSaver.
             */
            if (!screenSaverOwnsSameBlur)
            {
                SetBlurImmediate(
                    initialBlurWeight
                );
            }


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
             * Веки НЕ открываются.
             */
            SetEyesClosedImmediate();


            /*
             * Только растворяем их.
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
             * Если ScreenSaver уже управляет
             * этим же Volume —
             * вообще не вмешиваемся.
             */
            if (!screenSaverOwnsSameBlur &&
                blurVolume != null)
            {
                blurVolume.weight =
                    Mathf.Lerp(
                        startBlurWeight,
                        initialBlurWeight,
                        smoothT
                    );
            }


            yield return null;
        }


        if (blinkCanvasGroup != null)
        {
            blinkCanvasGroup.alpha = 0f;
            blinkCanvasGroup.blocksRaycasts = false;
        }


        if (!screenSaverOwnsSameBlur)
        {
            SetBlurImmediate(
                initialBlurWeight
            );
        }
    }


    // =========================================================
    // ИСХОДНОЕ СОСТОЯНИЕ
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


        initialBlurWeight =
            blurVolume != null
                ? blurVolume.weight
                : 0f;
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

    private void SetBlurImmediate(
        float weight)
    {
        if (blurVolume == null)
            return;


        blurVolume.weight =
            Mathf.Clamp01(weight);
    }


    // =========================================================
    // ЕСЛИ NEW GAME НЕ ЗАГРУЗИЛСЯ
    // =========================================================

    private IEnumerator RecoverAfterFailedLoad()
    {
        /*
         * Даже при ошибке веки назад
         * физически не открываем.
         *
         * Просто растворяем слой.
         */
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

        Destroy(gameObject);
    }


    // =========================================================
    // ПЕРЕЖИВАЕМ СМЕНУ СЦЕНЫ
    // =========================================================

    private void MakePersistent()
    {
        if (madePersistent)
            return;


        madePersistent = true;


        /*
         * DontDestroyOnLoad корректно работает
         * с root GameObject.
         */
        if (transform.parent != null)
        {
            transform.SetParent(
                null,
                true
            );
        }


        DontDestroyOnLoad(
            gameObject
        );
    }
}
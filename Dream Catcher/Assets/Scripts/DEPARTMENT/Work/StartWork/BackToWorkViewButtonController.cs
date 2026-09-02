using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class BackToWorkViewButtonController :
    MonoBehaviour
{
    [Header("Компоненты")]
    [Tooltip("Кнопка экранной стрелки.")]
    [SerializeField] private Button button;

    [Tooltip("Canvas Group экранной стрелки.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Связи")]
    [Tooltip("Страница второй вкладки — электронного направления.")]
    [SerializeField] private GameObject electronicDirectionPage;

    [Tooltip("Контроллер приближения к компьютеру.")]
    [SerializeField] private ZoomComputerWork zoomComputerWork;

    [Tooltip("Навигация интерфейса компьютера.")]
    [SerializeField]
    private ComputerInterfaceNavigation
        computerNavigation;

    [Header("Плавное появление и исчезновение")]
    [Tooltip("Использовать длительность движения камеры.")]
    [SerializeField]
    private bool useZoomDuration =
        true;

    [Tooltip("Своя длительность анимации, если настройка выше выключена.")]
    [SerializeField]
    private float fadeDuration =
        0.7f;

    [Header("Мигание предупреждения")]

    [Tooltip("Минимальная прозрачность стрелки во время мигания.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float warningBlinkMinAlpha = 0.6f;

    [Tooltip("Скорость плавного мигания.")]
    [SerializeField]
    private float warningBlinkSpeed = 1.5f;

    private Coroutine warningBlinkCoroutine;
    private bool warningBlinkRequested;

    private Coroutine fadeCoroutine;

    private bool returnInProgress;
    private bool targetVisible;

    private void Awake()
    {
        FindReferences();

        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleButtonPressed
            );

            button.onClick.AddListener(
                HandleButtonPressed
            );
        }

        SetVisibleInstantly(false);
    }

    private void Update()
    {
        if (returnInProgress)
            return;

        bool directionPageOpen =
            electronicDirectionPage != null &&
            electronicDirectionPage.activeInHierarchy;

        bool computerZoomed =
            zoomComputerWork != null &&
            zoomComputerWork.IsZoomedIn;

        bool shouldBeVisible =
            directionPageOpen &&
            computerZoomed;

        // При открытии второй вкладки
        // стрелка плавно появляется.
        if (shouldBeVisible)
        {
            if (!targetVisible)
            {
                StartVisibilityTransition(
                    true
                );
            }

            return;
        }

        // При переходе на первую вкладку
        // стрелка исчезает мгновенно.
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }

        SetVisibleInstantly(
            false
        );
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }

        returnInProgress = false;

        SetVisibleInstantly(false);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleButtonPressed
            );
        }
    }

    private void HandleButtonPressed()
    {
        if (returnInProgress ||
            zoomComputerWork == null ||
            !zoomComputerWork.IsZoomedIn)
        {
            return;
        }

        //if (computerNavigation != null)
        //{
        //    computerNavigation
        //        .CloseAllPopups();
        //}

        bool returnStarted =
            zoomComputerWork
                .ReturnToWorkView();

        if (!returnStarted)
            return;

        returnInProgress = true;
        targetVisible = false;

        if (button != null)
        {
            button.interactable =
                false;
        }

        if (canvasGroup != null)
        {
            // Сразу запрещаем повторный клик,
            // но сама стрелка исчезает плавно.
            canvasGroup.interactable =
                false;

            canvasGroup.blocksRaycasts =
                false;
        }

        StartFade(
            false,
            GetFadeDuration()
        );
    }

    private void StartVisibilityTransition(
        bool visible)
    {
        targetVisible = visible;

        StartFade(
            visible,
            GetFadeDuration()
        );
    }

    private void StartFade(
    bool visible,
    float duration)
    {
        StopWarningBlinkRoutine();

        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }

        fadeCoroutine =
            StartCoroutine(
                FadeRoutine(
                    visible,
                    duration
                )
            );
    }

    public void StartWarningBlink()
    {
        warningBlinkRequested = true;

        /*
         * Если стрелка уже полностью показана,
         * сразу начинаем мигание.
         *
         * Если она ещё появляется,
         * мигание запустится после fade.
         */
        if (targetVisible &&
            fadeCoroutine == null &&
            canvasGroup != null &&
            canvasGroup.alpha > 0.99f)
        {
            StartWarningBlinkRoutine();
        }
    }


    public void StopWarningBlink()
    {
        warningBlinkRequested = false;

        StopWarningBlinkRoutine();

        /*
         * Если стрелка сейчас просто видима,
         * возвращаем обычную прозрачность.
         */
        if (targetVisible &&
            fadeCoroutine == null &&
            canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void StartWarningBlinkRoutine()
    {
        if (warningBlinkCoroutine != null)
            return;

        if (canvasGroup == null)
            return;

        warningBlinkCoroutine =
            StartCoroutine(
                WarningBlinkRoutine()
            );
    }


    private void StopWarningBlinkRoutine()
    {
        if (warningBlinkCoroutine == null)
            return;

        StopCoroutine(
            warningBlinkCoroutine
        );

        warningBlinkCoroutine = null;
    }


    private IEnumerator WarningBlinkRoutine()
    {
        float elapsed = 0f;

        while (warningBlinkRequested &&
               targetVisible)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float wave =
                (Mathf.Sin(
                    elapsed *
                    warningBlinkSpeed *
                    Mathf.PI * 2f
                ) + 1f) * 0.5f;

            canvasGroup.alpha =
                Mathf.Lerp(
                    warningBlinkMinAlpha,
                    1f,
                    wave
                );

            yield return null;
        }

        warningBlinkCoroutine = null;

        if (targetVisible &&
            canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private IEnumerator FadeRoutine(
        bool visible,
        float duration)
    {
        if (canvasGroup == null)
        {
            targetVisible = visible;
            returnInProgress = false;
            fadeCoroutine = null;

            yield break;
        }

        float startAlpha =
            canvasGroup.alpha;

        float targetAlpha =
            visible
                ? 1f
                : 0f;

        // Пока стрелка появляется,
        // она ещё не должна принимать клики.
        canvasGroup.interactable =
            false;

        canvasGroup.blocksRaycasts =
            false;

        if (button != null)
        {
            button.interactable =
                false;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha =
                targetAlpha;

            CompleteFade(
                visible
            );

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    smoothT
                );

            yield return null;
        }

        canvasGroup.alpha =
            targetAlpha;

        CompleteFade(
            visible
        );
    }

    private void CompleteFade(
        bool visible)
    {
        targetVisible = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                visible
                    ? 1f
                    : 0f;

            canvasGroup.interactable =
                visible;

            canvasGroup.blocksRaycasts =
                visible;
        }

        if (button != null)
        {
            button.interactable =
                visible;
        }

        if (!visible)
        {
            returnInProgress =
                false;
        }

        if (visible &&
            warningBlinkRequested)
        {
            StartWarningBlinkRoutine();
        }

        fadeCoroutine = null;
    }

    private float GetFadeDuration()
    {
        if (useZoomDuration &&
            zoomComputerWork != null)
        {
            return zoomComputerWork
                .TransitionDuration;
        }

        return Mathf.Max(
            0f,
            fadeDuration
        );
    }

    private void SetVisibleInstantly(
        bool visible)
    {
        targetVisible = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                visible
                    ? 1f
                    : 0f;

            canvasGroup.interactable =
                visible;

            canvasGroup.blocksRaycasts =
                visible;
        }

        if (button != null)
        {
            button.interactable =
                visible;
        }
    }

    private void FindReferences()
    {
        if (button == null)
        {
            button =
                GetComponent<Button>();
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (zoomComputerWork == null)
        {
            zoomComputerWork =
                FindFirstObjectByType
                    <ZoomComputerWork>(
                        FindObjectsInactive.Include
                    );
        }

        if (computerNavigation == null)
        {
            computerNavigation =
                FindFirstObjectByType
                    <ComputerInterfaceNavigation>(
                        FindObjectsInactive.Include
                    );
        }
    }

    private void OnValidate()
    {
        fadeDuration =
            Mathf.Max(
                0f,
                fadeDuration
            );
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SanityHUDController :
    MonoBehaviour
{
    [Header("Шкала рассудка")]

    [SerializeField]
    private Slider sanitySlider;

    [Tooltip("Время плавного изменения шкалы.")]
    [SerializeField]
    private float changeDuration = 0.3f;

    [Header("Шум")]

    [Tooltip("Движущаяся текстура шума.")]
    [SerializeField]
    private RawImage noiseImage;

    [Tooltip("Canvas Group объекта Noise для плавного появления.")]
    [SerializeField]
    private CanvasGroup noiseCanvasGroup;

    [Tooltip("При каком уровне рассудка начинает появляться шум.")]
    [SerializeField]
    [Range(0f, 100f)]
    private float noiseThreshold = 95f;

    [Tooltip("Длительность появления и исчезновения шума.")]
    [SerializeField]
    private float noiseFadeDuration = 0.4f;

    [Tooltip("Скорость движения текстуры шума.")]
    [SerializeField]
    private Vector2 noiseScrollSpeed =
        new Vector2(0.12f, 0.04f);

    private SessionStatsManager statsManager;

    private Coroutine valueCoroutine;
    private Coroutine noiseFadeCoroutine;
    private Coroutine findManagerCoroutine;

    private bool noiseShouldBeVisible;

    private void OnEnable()
    {
        TryConnectToStats();
    }

    private void OnDisable()
    {
        DisconnectFromStats();

        if (valueCoroutine != null)
        {
            StopCoroutine(valueCoroutine);
            valueCoroutine = null;
        }

        if (noiseFadeCoroutine != null)
        {
            StopCoroutine(noiseFadeCoroutine);
            noiseFadeCoroutine = null;
        }

        if (findManagerCoroutine != null)
        {
            StopCoroutine(findManagerCoroutine);
            findManagerCoroutine = null;
        }
    }

    private void Update()
    {
        if (noiseImage == null)
            return;

        if (noiseCanvasGroup != null &&
            noiseCanvasGroup.alpha <= 0f)
        {
            return;
        }

        Rect uv =
            noiseImage.uvRect;

        uv.x +=
            noiseScrollSpeed.x *
            Time.unscaledDeltaTime;

        uv.y +=
            noiseScrollSpeed.y *
            Time.unscaledDeltaTime;

        noiseImage.uvRect = uv;
    }

    private void TryConnectToStats()
    {
        if (SessionStatsManager.Instance != null)
        {
            ConnectToStats(
                SessionStatsManager.Instance
            );

            return;
        }

        if (findManagerCoroutine == null)
        {
            findManagerCoroutine =
                StartCoroutine(
                    WaitForStatsManager()
                );
        }
    }

    private IEnumerator WaitForStatsManager()
    {
        while (SessionStatsManager.Instance == null)
        {
            yield return null;
        }

        findManagerCoroutine = null;

        ConnectToStats(
            SessionStatsManager.Instance
        );
    }

    private void ConnectToStats(
        SessionStatsManager manager)
    {
        DisconnectFromStats();

        statsManager = manager;

        if (statsManager == null)
            return;

        statsManager.SanityChanged +=
            HandleSanityChanged;

        if (sanitySlider != null)
        {
            sanitySlider.minValue = 0f;

            sanitySlider.maxValue =
                statsManager.MaxSanity;

            sanitySlider.wholeNumbers =
                false;

            sanitySlider.value =
                statsManager.CurrentSanity;
        }

        // При первом подключении просто выставляем
        // правильное состояние без анимации.
        SetNoiseImmediate(
            statsManager.CurrentSanity <=
            noiseThreshold
        );
    }

    private void DisconnectFromStats()
    {
        if (statsManager == null)
            return;

        statsManager.SanityChanged -=
            HandleSanityChanged;

        statsManager = null;
    }

    private void HandleSanityChanged(
        int oldValue,
        int newValue)
    {
        if (sanitySlider == null)
            return;

        if (valueCoroutine != null)
        {
            StopCoroutine(valueCoroutine);
        }

        valueCoroutine =
            StartCoroutine(
                AnimateValue(newValue)
            );
    }

    private IEnumerator AnimateValue(
        float targetValue)
    {
        float startValue =
            sanitySlider.value;

        if (changeDuration <= 0f)
        {
            sanitySlider.value =
                targetValue;

            UpdateNoiseState(
                targetValue
            );

            valueCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < changeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    changeDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            sanitySlider.value =
                Mathf.Lerp(
                    startValue,
                    targetValue,
                    smoothT
                );

            UpdateNoiseState(
                sanitySlider.value
            );

            yield return null;
        }

        sanitySlider.value =
            targetValue;

        UpdateNoiseState(
            targetValue
        );

        valueCoroutine = null;
    }

    private void UpdateNoiseState(
        float sanityValue)
    {
        bool shouldBeVisible =
            sanityValue <=
            noiseThreshold;

        if (shouldBeVisible ==
            noiseShouldBeVisible)
        {
            return;
        }

        noiseShouldBeVisible =
            shouldBeVisible;

        StartNoiseFade(
            shouldBeVisible ? 1f : 0f
        );
    }

    private void StartNoiseFade(
        float targetAlpha)
    {
        if (noiseCanvasGroup == null)
            return;

        if (noiseFadeCoroutine != null)
        {
            StopCoroutine(
                noiseFadeCoroutine
            );
        }

        noiseFadeCoroutine =
            StartCoroutine(
                NoiseFadeRoutine(
                    targetAlpha
                )
            );
    }

    private IEnumerator NoiseFadeRoutine(
        float targetAlpha)
    {
        float startAlpha =
            noiseCanvasGroup.alpha;

        if (noiseFadeDuration <= 0f)
        {
            noiseCanvasGroup.alpha =
                targetAlpha;

            noiseFadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < noiseFadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    noiseFadeDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            noiseCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    smoothT
                );

            yield return null;
        }

        noiseCanvasGroup.alpha =
            targetAlpha;

        noiseFadeCoroutine = null;
    }

    private void SetNoiseImmediate(
        bool visible)
    {
        noiseShouldBeVisible =
            visible;

        if (noiseFadeCoroutine != null)
        {
            StopCoroutine(
                noiseFadeCoroutine
            );

            noiseFadeCoroutine = null;
        }

        if (noiseCanvasGroup != null)
        {
            noiseCanvasGroup.alpha =
                visible ? 1f : 0f;

            noiseCanvasGroup.interactable =
                false;

            noiseCanvasGroup.blocksRaycasts =
                false;
        }
    }

    private void OnValidate()
    {
        changeDuration =
            Mathf.Max(
                0f,
                changeDuration
            );

        noiseFadeDuration =
            Mathf.Max(
                0f,
                noiseFadeDuration
            );
    }
}
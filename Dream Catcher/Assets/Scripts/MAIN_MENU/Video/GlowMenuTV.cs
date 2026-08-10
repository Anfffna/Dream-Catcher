using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Rendering;
using Coffee.UIEffects;
using System.Collections;
using System.Collections.Generic;

public class GlowMenuTV : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;

    [Tooltip("Общий родитель всех четырёх Glow Image.")]
    public GameObject glowRoot;

    [Tooltip("CanvasGroup на Glow Root.")]
    public CanvasGroup glowCanvasGroup;

    [Tooltip("Главный UIEffect. Остальные Glow используют UIEffectReplica.")]
    public UIEffect glowEffect;


    [Header("Glow Colors")]
    [Tooltip("Цвета, между которыми будет случайно переключаться подсветка.")]
    public List<Color> glowColors = new List<Color>()
    {
        new Color(0.55f, 0.70f, 1.00f, 1f),
        new Color(0.45f, 0.90f, 1.00f, 1f),
        new Color(0.60f, 1.00f, 0.70f, 1f),
        new Color(1.00f, 0.85f, 0.45f, 1f),
        new Color(1.00f, 0.55f, 0.35f, 1f),
        new Color(1.00f, 0.40f, 0.45f, 1f)
    };


    [Header("Random Color Timing")]
    [Tooltip("Минимальное время до следующего цвета.")]
    public float minColorInterval = 0.5f;

    [Tooltip("Максимальное время до следующего цвета.")]
    public float maxColorInterval = 1.5f;

    [Tooltip("Насколько плавно один цвет переходит в другой.")]
    public float colorTransitionDuration = 0.18f;


    [Header("Random Glow Intensity")]
    [Tooltip("Минимальная прозрачность Shadow Color.")]
    [Range(0f, 1f)]
    public float minGlowAlpha = 0.25f;

    [Tooltip("Максимальная прозрачность Shadow Color.")]
    [Range(0f, 1f)]
    public float maxGlowAlpha = 0.60f;


    [Header("Glow Root Fade")]
    public float glowFadeDuration = 0.3f;


    [Header("Black Screen Detection")]
    [Tooltip("Как часто проверяем, является ли видео чёрным.")]
    public float blackCheckInterval = 0.15f;

    [Range(4, 32)]
    public int sampleWidth = 12;

    [Range(4, 32)]
    public int sampleHeight = 7;

    [Tooltip("Пиксель темнее этого значения считается чёрным.")]
    [Range(0f, 0.2f)]
    public float blackPixelBrightness = 0.04f;

    [Tooltip("Какая часть кадра должна быть чёрной для отключения Glow.")]
    [Range(0.5f, 1f)]
    public float blackPixelRatio = 0.93f;


    [Header("Debug")]
    public bool debugLogs = false;


    private RenderTexture sampleRT;

    private bool readbackPending;
    private float nextBlackCheckTime;

    private bool screenIsBlack = true;
    private bool glowVisible = false;

    private int currentColorIndex = -1;

    private Coroutine fadeCoroutine;
    private Coroutine colorCoroutine;
    private Coroutine colorChangeLoop;


    private void Awake()
    {
        CreateSampleTexture();

        if (glowCanvasGroup == null && glowRoot != null)
            glowCanvasGroup = glowRoot.GetComponent<CanvasGroup>();

        // Никакой вспышки в начале сцены.
        if (glowRoot != null)
            glowRoot.SetActive(true);

        if (glowCanvasGroup != null)
        {
            glowCanvasGroup.alpha = 0f;
            glowCanvasGroup.interactable = false;
            glowCanvasGroup.blocksRaycasts = false;
        }

        glowVisible = false;
        screenIsBlack = true;
    }


    private void Start()
    {
        colorChangeLoop = StartCoroutine(RandomColorLoop());
    }


    private void Update()
    {
        if (videoPlayer == null)
            return;

        if (!videoPlayer.isPrepared)
            return;

        if (readbackPending)
            return;

        if (Time.unscaledTime < nextBlackCheckTime)
            return;

        Texture videoTexture = GetVideoTexture();

        if (videoTexture == null)
            return;

        nextBlackCheckTime =
            Time.unscaledTime + blackCheckInterval;

        Graphics.Blit(videoTexture, sampleRT);

        readbackPending = true;

        AsyncGPUReadback.Request(
            sampleRT,
            0,
            TextureFormat.RGBA32,
            OnFrameReadback
        );
    }


    // =========================================================
    // RANDOM COLOR
    // =========================================================

    private IEnumerator RandomColorLoop()
    {
        while (true)
        {
            // Пока телевизор чёрный — цвет не крутим.
            while (screenIsBlack)
                yield return null;

            // При первом появлении изображения
            // сразу выбираем цвет.
            ChangeToRandomColor();

            float waitTime =
                Random.Range(
                    minColorInterval,
                    maxColorInterval
                );

            yield return new WaitForSecondsRealtime(waitTime);
        }
    }


    private void ChangeToRandomColor()
    {
        if (glowEffect == null)
            return;

        if (glowColors == null || glowColors.Count == 0)
            return;


        int newIndex;

        // Если цветов больше одного —
        // гарантируем, что следующий будет ДРУГИМ.
        if (glowColors.Count > 1)
        {
            do
            {
                newIndex =
                    Random.Range(0, glowColors.Count);
            }
            while (newIndex == currentColorIndex);
        }
        else
        {
            newIndex = 0;
        }


        currentColorIndex = newIndex;

        Color targetColor =
            glowColors[newIndex];

        // Случайная сила свечения.
        targetColor.a =
            Random.Range(
                minGlowAlpha,
                maxGlowAlpha
            );


        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        colorCoroutine =
            StartCoroutine(
                FadeGlowColor(targetColor)
            );


        if (debugLogs)
        {
            Debug.Log(
                $"GlowMenuTV: новый Glow Color = " +
                $"{ColorUtility.ToHtmlStringRGB(targetColor)}, " +
                $"Alpha = {targetColor.a:F2}",
                this
            );
        }
    }


    private IEnumerator FadeGlowColor(
        Color targetColor)
    {
        if (glowEffect == null)
            yield break;


        Color startColor =
            glowEffect.shadowColor;


        if (colorTransitionDuration <= 0f)
        {
            glowEffect.shadowColor =
                targetColor;

            yield break;
        }


        float elapsed = 0f;


        while (elapsed < colorTransitionDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    colorTransitionDuration
                );


            // SmoothStep, чтобы переход не выглядел линейным.
            float smoothT =
                t * t * (3f - 2f * t);


            glowEffect.shadowColor =
                Color.Lerp(
                    startColor,
                    targetColor,
                    smoothT
                );


            yield return null;
        }


        glowEffect.shadowColor =
            targetColor;

        colorCoroutine = null;
    }


    // =========================================================
    // BLACK SCREEN
    // =========================================================

    private Texture GetVideoTexture()
    {
        if (videoPlayer.targetTexture != null)
            return videoPlayer.targetTexture;

        return videoPlayer.texture;
    }


    private void OnFrameReadback(
        AsyncGPUReadbackRequest request)
    {
        readbackPending = false;

        if (request.hasError)
            return;


        var pixels =
            request.GetData<Color32>();


        if (pixels.Length == 0)
            return;


        int blackPixels = 0;


        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 p = pixels[i];

            float r = p.r / 255f;
            float g = p.g / 255f;
            float b = p.b / 255f;


            float brightness =
                0.299f * r +
                0.587f * g +
                0.114f * b;


            if (brightness <= blackPixelBrightness)
                blackPixels++;
        }


        float ratio =
            (float)blackPixels /
            pixels.Length;


        bool isBlack =
            ratio >= blackPixelRatio;


        // Состояние не поменялось.
        if (isBlack == screenIsBlack)
            return;


        screenIsBlack = isBlack;


        if (screenIsBlack)
        {
            HideGlow();
        }
        else
        {
            // СНАЧАЛА задаём свежий цвет,
            // чтобы Glow не появился со старым цветом.
            ChangeToRandomColor();

            ShowGlow();
        }


        if (debugLogs)
        {
            Debug.Log(
                screenIsBlack
                    ? "GlowMenuTV: BLACK → Fade OUT"
                    : "GlowMenuTV: VIDEO → Fade IN",
                this
            );
        }
    }


    // =========================================================
    // CANVAS GROUP FADE
    // =========================================================

    private void ShowGlow()
    {
        if (glowRoot == null ||
            glowCanvasGroup == null)
            return;


        glowRoot.SetActive(true);


        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);


        fadeCoroutine =
            StartCoroutine(
                FadeGlowRoot(
                    glowCanvasGroup.alpha,
                    1f,
                    false
                )
            );
    }


    private void HideGlow()
    {
        if (glowRoot == null ||
            glowCanvasGroup == null)
            return;


        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);


        fadeCoroutine =
            StartCoroutine(
                FadeGlowRoot(
                    glowCanvasGroup.alpha,
                    0f,
                    true
                )
            );
    }


    private IEnumerator FadeGlowRoot(
        float from,
        float to,
        bool disableAfterFade)
    {
        glowRoot.SetActive(true);


        float elapsed = 0f;


        if (glowFadeDuration <= 0f)
        {
            glowCanvasGroup.alpha = to;
        }
        else
        {
            while (elapsed < glowFadeDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;


                float t =
                    Mathf.Clamp01(
                        elapsed /
                        glowFadeDuration
                    );


                float smoothT =
                    t * t * (3f - 2f * t);


                glowCanvasGroup.alpha =
                    Mathf.Lerp(
                        from,
                        to,
                        smoothT
                    );


                yield return null;
            }


            glowCanvasGroup.alpha = to;
        }


        glowVisible =
            to > 0.5f;


        if (disableAfterFade)
        {
            glowCanvasGroup.interactable = false;
            glowCanvasGroup.blocksRaycasts = false;

            // После плавного исчезновения
            // объект реально отключаем.
            glowRoot.SetActive(false);
        }
        else
        {
            glowCanvasGroup.interactable = false;
            glowCanvasGroup.blocksRaycasts = false;
        }


        fadeCoroutine = null;
    }


    // =========================================================
    // RENDER TEXTURE
    // =========================================================

    private void CreateSampleTexture()
    {
        sampleRT =
            new RenderTexture(
                sampleWidth,
                sampleHeight,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB
            );


        sampleRT.name =
            "GlowMenuTV_BlackCheck";


        sampleRT.filterMode =
            FilterMode.Bilinear;

        sampleRT.wrapMode =
            TextureWrapMode.Clamp;


        sampleRT.Create();
    }


    private void OnDestroy()
    {
        if (sampleRT != null)
        {
            sampleRT.Release();
            Destroy(sampleRT);
        }
    }
}
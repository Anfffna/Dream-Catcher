using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Rendering;

public class ScreenSaver : MonoBehaviour
{
    [Header("Background")]
    public CanvasGroup backgroundGroup;

    [Header("Loading")]
    public bool skipWhenLoadingSave = true;

    [Header("Text 1")]
    public CanvasGroup textGroup1;

    [Header("Text 2")]
    public CanvasGroup textGroup2;
    public TextMeshProUGUI text2;

    [Header("Text 3")]
    public CanvasGroup textGroup3;

    [Header("References")]
    public StartDay startDay;

    [Header("Wake Up Blur")]
    public Volume wakeUpBlurVolume;

    [Range(0f, 1f)]
    public float blurStartWeight = 1f;

    [Tooltip("Сколько секунд блюр держится ПОСЛЕ исчезновения заставки")]
    public float blurHoldTime = 2f;

    [Tooltip("За сколько секунд блюр плавно исчезает")]
    public float blurFadeDuration = 1.5f;

    public bool disableBlurAfterFade = true;

    [Header("Texts")]
    public string secondTextReplace = "";

    [Header("Timing")]
    public float delayBeforeText = 2f;
    public float delayBeforeThirdText = 1f;

    public float fadeDuration = 1f;

    public float delayBetweenTexts = 1f;
    public float delayBeforeReplace = 0.5f;
    public float delayBeforeTextFadeOut = 1f;
    public float delayBeforeBackgroundFade = 0.5f;

    private bool canInteract = false;
    private bool isRunning = false;
    private Coroutine blurRoutine;

    public bool IsBackgroundReady
    {
        get
        {
            return
                backgroundGroup != null &&
                backgroundGroup.alpha >= 0.999f &&
                gameObject.activeInHierarchy;
        }
    }

    void Start()
    {
        if (skipWhenLoadingSave &&
            SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSave)
        {
            ApplySkippedState();
            return;
        }

        backgroundGroup.alpha = 1f;

        textGroup1.alpha = 0f;
        textGroup2.alpha = 0f;
        textGroup3.alpha = 0f;

        EnableBlurImmediately();

        StartCoroutine(Startup());
    }

    void EnableBlurImmediately()
    {
        if (wakeUpBlurVolume == null) return;

        wakeUpBlurVolume.gameObject.SetActive(true);
        wakeUpBlurVolume.weight = blurStartWeight;
    }

    IEnumerator Startup()
    {
        yield return Fade(textGroup1, 0f, 1f);

        yield return new WaitForSeconds(delayBeforeThirdText);
        yield return Fade(textGroup3, 0f, 1f);

        canInteract = true;
    }

    void Update()
    {
        if (!canInteract || isRunning) return;

        if (Input.anyKeyDown)
        {
            StartCoroutine(Sequence());
        }
    }

    IEnumerator Sequence()
    {
        isRunning = true;
        canInteract = false;

        yield return FadeTwo(textGroup1, textGroup3, 0f);

        yield return new WaitForSeconds(delayBetweenTexts);

        yield return Fade(textGroup2, 0f, 1f);

        yield return new WaitForSeconds(delayBeforeReplace);

        text2.text = secondTextReplace;

        yield return new WaitForSeconds(delayBeforeTextFadeOut);

        yield return Fade(textGroup2, 1f, 0f);

        yield return new WaitForSeconds(delayBeforeBackgroundFade);

        yield return Fade(backgroundGroup, 1f, 0f);

        StartBlurFadeAfterScreenSaver();

        if (startDay != null)
            startDay.OnScreenSaverFinished();
    }

    void StartBlurFadeAfterScreenSaver()
    {
        if (wakeUpBlurVolume == null) return;

        wakeUpBlurVolume.gameObject.SetActive(true);
        wakeUpBlurVolume.weight = blurStartWeight;

        if (blurRoutine != null)
            StopCoroutine(blurRoutine);

        blurRoutine = StartCoroutine(BlurFadeRoutine());
    }

    IEnumerator BlurFadeRoutine()
    {
        yield return new WaitForSeconds(blurHoldTime);

        float t = 0f;
        float startWeight = wakeUpBlurVolume.weight;

        while (t < blurFadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / blurFadeDuration);

            wakeUpBlurVolume.weight = Mathf.Lerp(startWeight, 0f, k);

            yield return null;
        }

        wakeUpBlurVolume.weight = 0f;

        if (disableBlurAfterFade)
            wakeUpBlurVolume.gameObject.SetActive(false);
    }

    IEnumerator Fade(CanvasGroup g, float from, float to)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);

            g.alpha = Mathf.Lerp(from, to, k);

            yield return null;
        }

        g.alpha = to;
    }

    IEnumerator FadeTwo(CanvasGroup a, CanvasGroup b, float to)
    {
        float t = 0f;

        float aStart = a.alpha;
        float bStart = b.alpha;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);

            a.alpha = Mathf.Lerp(aStart, to, k);
            b.alpha = Mathf.Lerp(bStart, to, k);

            yield return null;
        }

        a.alpha = to;
        b.alpha = to;
    }

    void ApplySkippedState()
    {
        canInteract = false;
        isRunning = true;

        if (backgroundGroup != null)
        {
            backgroundGroup.alpha = 0f;
            backgroundGroup.interactable = false;
            backgroundGroup.blocksRaycasts = false;
        }

        if (textGroup1 != null)
        {
            textGroup1.alpha = 0f;
            textGroup1.interactable = false;
            textGroup1.blocksRaycasts = false;
        }

        if (textGroup2 != null)
        {
            textGroup2.alpha = 0f;
            textGroup2.interactable = false;
            textGroup2.blocksRaycasts = false;
        }

        if (textGroup3 != null)
        {
            textGroup3.alpha = 0f;
            textGroup3.interactable = false;
            textGroup3.blocksRaycasts = false;
        }

        if (text2 != null)
            text2.text = "";

        if (wakeUpBlurVolume != null)
        {
            wakeUpBlurVolume.weight = 0f;

            if (disableBlurAfterFade)
                wakeUpBlurVolume.gameObject.SetActive(false);
        }

        Debug.Log("ScreenSaver: пропущен, потому что загружается сейв.");
    }
}
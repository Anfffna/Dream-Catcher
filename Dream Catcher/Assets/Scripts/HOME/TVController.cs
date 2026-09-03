using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class TVController : MonoBehaviour
{
    [Header("TV")]
    public VideoPlayer videoPlayer;
    public AudioSource tvAudioSource;

    [Header("TV Screen Auto Setup")]
    public bool autoFindScreenRenderer = true;
    public string screenObjectName = "Quad";
    public Renderer screenRenderer;

    [Header("Noise")]
    public AudioClip tvNoiseClip;

    [Range(0f, 1f)]
    public float noiseStartVolume = 0f;

    [Range(0f, 1f)]
    public float noiseTargetVolume = 0.35f;

    public float noiseFadeInDuration = 2f;

    [Header("Noise Fade Out")]
    public float noiseFadeOutDuration = 0.75f;

    [Header("News")]
    [Range(0f, 1f)]
    public float newsVolume = 0.6f;

    private Coroutine noiseFadeCoroutine;
    private bool newsAlreadyStarted = false;

    private RenderTexture targetTexture;
    private Material screenMaterialInstance;

    void Awake()
    {
        FindReferences();
        SetupScreenMaterial();

        // Изначально экран новостей скрыт.
        SetScreenVisible(false);
    }

    void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnNewsVideoFinished;
            videoPlayer.loopPointReached += OnNewsVideoFinished;
        }
    }

    void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnNewsVideoFinished;
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnNewsVideoFinished;
    }

    void Start()
    {
        FindReferences();
        SetupScreenMaterial();

        if (SaveManager.Instance != null && SaveManager.Instance.IsLoadingSave)
        {
            ApplyLoadedSaveState();
            return;
        }

        StartNoise();
    }

    public void StartNoise()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoadingSave)
            return;

        FindReferences();
        SetupScreenMaterial();

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.time = 0;
        }

        // Пока только шум — Quad с новостями скрыт.
        SetScreenVisible(false);

        if (tvAudioSource == null || tvNoiseClip == null)
            return;

        if (noiseFadeCoroutine != null)
            StopCoroutine(noiseFadeCoroutine);

        tvAudioSource.Stop();
        tvAudioSource.clip = tvNoiseClip;
        tvAudioSource.loop = true;
        tvAudioSource.volume = noiseStartVolume;
        tvAudioSource.Play();

        noiseFadeCoroutine = StartCoroutine(FadeNoiseIn());
    }

    public void PlayNewsVideo()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoadingSave)
            return;

        FindReferences();
        SetupScreenMaterial();

        if (videoPlayer == null)
            return;

        if (newsAlreadyStarted)
            return;

        newsAlreadyStarted = true;

        if (noiseFadeCoroutine != null)
            StopCoroutine(noiseFadeCoroutine);

        noiseFadeCoroutine = StartCoroutine(FadeNoiseOutThenPlayVideo());
    }

    // На будущее для 7 дней.
    // DayManager сможет просто вызвать этот метод и подставить нужный клип дня.
    public void SetNewsClip(VideoClip clip)
    {
        FindReferences();

        newsAlreadyStarted = false;

        if (videoPlayer == null)
            return;

        videoPlayer.Stop();
        videoPlayer.time = 0;
        videoPlayer.clip = clip;

        // Новый клип установлен, но пока не запущен.
        SetScreenVisible(false);
    }

    private IEnumerator FadeNoiseIn()
    {
        if (tvAudioSource == null)
            yield break;

        float t = 0f;
        float startVolume = noiseStartVolume;

        while (t < noiseFadeInDuration)
        {
            t += Time.deltaTime;

            float k = noiseFadeInDuration <= 0f
                ? 1f
                : Mathf.Clamp01(t / noiseFadeInDuration);

            tvAudioSource.volume = Mathf.Lerp(
                startVolume,
                noiseTargetVolume,
                k
            );

            yield return null;
        }

        tvAudioSource.volume = noiseTargetVolume;
        noiseFadeCoroutine = null;
    }

    private IEnumerator FadeNoiseOutThenPlayVideo()
    {
        if (tvAudioSource != null)
        {
            float t = 0f;
            float startVolume = tvAudioSource.volume;

            while (t < noiseFadeOutDuration)
            {
                t += Time.deltaTime;

                float k = noiseFadeOutDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(t / noiseFadeOutDuration);

                tvAudioSource.volume = Mathf.Lerp(
                    startVolume,
                    0f,
                    k
                );

                yield return null;
            }

            tvAudioSource.Stop();
            tvAudioSource.clip = null;
            tvAudioSource.loop = false;
            tvAudioSource.volume = newsVolume;
        }

        if (videoPlayer == null)
            yield break;

        videoPlayer.Stop();
        videoPlayer.time = 0;

        // Quad всё ещё скрыт, пока видео готовится.
        SetScreenVisible(false);

        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        // Запускаем видео.
        videoPlayer.Play();

        // И в этот же момент показываем Quad.
        SetScreenVisible(true);

        noiseFadeCoroutine = null;
    }

    private void OnNewsVideoFinished(VideoPlayer vp)
    {
        // Новости закончились — просто скрываем Quad.
        SetScreenVisible(false);
    }

    private void SetScreenVisible(bool visible)
    {
        if (screenRenderer != null)
            screenRenderer.enabled = visible;
    }

    private void SetupScreenMaterial()
    {
        if (videoPlayer != null)
            targetTexture = videoPlayer.targetTexture;

        if (targetTexture == null)
            return;

        if (screenRenderer == null)
            return;

        // Отдельный экземпляр материала только для телевизора.
        // Цвета и Emission здесь НЕ меняются.
        if (screenMaterialInstance == null)
            screenMaterialInstance = screenRenderer.material;

        screenMaterialInstance.mainTexture = targetTexture;

        if (screenMaterialInstance.HasProperty("_BaseMap"))
            screenMaterialInstance.SetTexture(
                "_BaseMap",
                targetTexture
            );

        if (screenMaterialInstance.HasProperty("_EmissionMap"))
            screenMaterialInstance.SetTexture(
                "_EmissionMap",
                targetTexture
            );
    }

    private void FindReferences()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (tvAudioSource == null)
            tvAudioSource = GetComponent<AudioSource>();

        if (videoPlayer != null)
            targetTexture = videoPlayer.targetTexture;

        if (!autoFindScreenRenderer)
            return;

        if (screenRenderer != null)
            return;

        Transform screen = transform.Find(screenObjectName);

        if (screen != null)
        {
            screenRenderer = screen.GetComponent<Renderer>();
            return;
        }

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            if (renderers[i].gameObject.name == screenObjectName)
            {
                screenRenderer = renderers[i];
                return;
            }
        }

        if (renderers.Length == 1)
            screenRenderer = renderers[0];
    }

    private void ApplyLoadedSaveState()
    {
        newsAlreadyStarted = true;

        if (noiseFadeCoroutine != null)
        {
            StopCoroutine(noiseFadeCoroutine);
            noiseFadeCoroutine = null;
        }

        if (tvAudioSource != null)
        {
            tvAudioSource.Stop();
            tvAudioSource.clip = null;
            tvAudioSource.loop = false;
            tvAudioSource.volume = 0f;
        }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.time = 0;
        }

        // При загрузке сейва новости уже пропущены,
        // поэтому Quad скрыт.
        SetScreenVisible(false);

        Debug.Log(
            "TVController: пропущен, потому что загружается сейв."
        );
    }
}
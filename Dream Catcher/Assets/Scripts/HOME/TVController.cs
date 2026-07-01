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

    [Header("Render Texture Cleanup")]
    public bool clearScreenOnStart = true;
    public bool clearScreenBeforeNoise = true;
    public bool clearScreenBeforeNews = true;
    public bool clearScreenAfterNewsEnd = true;
    public Color clearColor = Color.black;

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

        if (clearScreenOnStart)
            ClearTVScreen();
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

        if (clearScreenBeforeNoise)
            ClearTVScreen();

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

        ClearTVScreen();
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

            tvAudioSource.volume = Mathf.Lerp(startVolume, noiseTargetVolume, k);

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

                tvAudioSource.volume = Mathf.Lerp(startVolume, 0f, k);

                yield return null;
            }

            tvAudioSource.Stop();
            tvAudioSource.clip = null;
            tvAudioSource.loop = false;
            tvAudioSource.volume = newsVolume;
        }

        if (videoPlayer == null)
            yield break;

        if (clearScreenBeforeNews)
            ClearTVScreen();

        videoPlayer.Stop();
        videoPlayer.time = 0;

        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
            yield return null;

        videoPlayer.Play();

        noiseFadeCoroutine = null;
    }

    private void OnNewsVideoFinished(VideoPlayer vp)
    {
        if (clearScreenAfterNewsEnd)
            ClearTVScreen();
    }

    public void ClearTVScreen()
    {
        FindReferences();
        SetupScreenMaterial();

        if (targetTexture == null)
            return;

        if (!targetTexture.IsCreated())
            targetTexture.Create();

        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = targetTexture;
        GL.Clear(true, true, clearColor);

        RenderTexture.active = previous;
    }

    private void SetupScreenMaterial()
    {
        if (videoPlayer != null)
            targetTexture = videoPlayer.targetTexture;

        if (targetTexture == null)
            return;

        if (screenRenderer == null)
            return;

        // Важно: .material создаёт отдельный экземпляр материала только для телевизора.
        // Так телевизор не будет случайно делить материал с постерами.
        if (screenMaterialInstance == null)
            screenMaterialInstance = screenRenderer.material;

        screenMaterialInstance.mainTexture = targetTexture;

        if (screenMaterialInstance.HasProperty("_BaseMap"))
            screenMaterialInstance.SetTexture("_BaseMap", targetTexture);

        if (screenMaterialInstance.HasProperty("_EmissionMap"))
            screenMaterialInstance.SetTexture("_EmissionMap", targetTexture);
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

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

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

        ClearTVScreen();

        Debug.Log("TVController: пропущен, потому что загружается сейв.");
    }
}
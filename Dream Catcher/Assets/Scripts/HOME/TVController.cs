using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class TVController : MonoBehaviour
{
    [Header("TV")]
    public VideoPlayer videoPlayer;
    public AudioSource tvAudioSource;

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

    void Start()
    {
        StartNoise();
    }

    public void StartNoise()
    {
        if (tvAudioSource == null || tvNoiseClip == null) return;

        if (videoPlayer != null)
            videoPlayer.Stop();

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
        if (videoPlayer == null) return;

        if (newsAlreadyStarted) return;
        newsAlreadyStarted = true;

        if (noiseFadeCoroutine != null)
            StopCoroutine(noiseFadeCoroutine);

        noiseFadeCoroutine = StartCoroutine(FadeNoiseOutThenPlayVideo());
    }

    private IEnumerator FadeNoiseIn()
    {
        if (tvAudioSource == null) yield break;

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

        videoPlayer.time = 0;
        videoPlayer.Play();

        noiseFadeCoroutine = null;
    }
}
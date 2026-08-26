using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class DrunkVisionEffect : MonoBehaviour
{
    [Header("Volume")]

    [Tooltip("Отдельный Global Volume с эффектом пьяного зрения.")]
    [SerializeField]
    private Volume drunkVolume;


    [Header("Время")]

    [Tooltip("Общая длительность эффекта.")]
    [SerializeField]
    private float duration = 3f;

    [Tooltip("Скорость появления эффекта.")]
    [SerializeField]
    private float fadeInDuration = 0.35f;

    [Tooltip("Скорость исчезновения эффекта.")]
    [SerializeField]
    private float fadeOutDuration = 0.7f;

    [Header("Звук")]

    [Tooltip("AudioSource для звука эффекта.")]
    [SerializeField]
    private AudioSource effectAudioSource;

    [Tooltip("Нарастающий звук при появлении эффекта.")]
    [SerializeField]
    private AudioClip effectSound;

    [Tooltip("Максимальная громкость звука.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float maxSoundVolume = 0.7f;


    private Coroutine effectRoutine;


    private void Awake()
    {
        if (drunkVolume != null)
        {
            drunkVolume.weight = 0f;
        }
    }


    public void PlayEffect()
    {
        if (drunkVolume == null)
            return;


        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
        }


        effectRoutine =
            StartCoroutine(
                EffectRoutine()
            );
    }


    public void StopEffectImmediate()
    {
        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            effectRoutine = null;
        }


        if (drunkVolume != null)
        {
            drunkVolume.weight = 0f;
        }

        if (effectAudioSource != null)
        {
            effectAudioSource.Stop();
        }
    }


    private IEnumerator EffectRoutine()
    {
        if (effectAudioSource != null &&
            effectSound != null)
        {
            effectAudioSource.clip =
                effectSound;

            effectAudioSource.loop =
                false;

            effectAudioSource.volume =
                0f;

            effectAudioSource.Play();
        }

        // Плавно появляется.
        float elapsed = 0f;


        while (elapsed < fadeInDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeInDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            drunkVolume.weight =
                smoothT;

            if (effectAudioSource != null)
            {
                effectAudioSource.volume =
                    Mathf.Lerp(
                        0f,
                        maxSoundVolume,
                        smoothT
                    );
            }


            yield return null;
        }


        drunkVolume.weight = 1f;


        // Держится.
        float holdDuration =
            Mathf.Max(
                0f,
                duration -
                fadeInDuration -
                fadeOutDuration
            );


        if (holdDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    holdDuration
                );
        }


        // Плавно исчезает.
        elapsed = 0f;


        while (elapsed < fadeOutDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeOutDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            drunkVolume.weight =
                Mathf.Lerp(
                    1f,
                    0f,
                    smoothT
                );

            if (effectAudioSource != null)
            {
                effectAudioSource.volume =
                    Mathf.Lerp(
                        maxSoundVolume,
                        0f,
                        smoothT
                    );
            }


            yield return null;
        }


        drunkVolume.weight = 0f;

        effectRoutine = null;

        if (effectAudioSource != null)
        {
            effectAudioSource.Stop();
            effectAudioSource.volume = maxSoundVolume;
        }
    }
}
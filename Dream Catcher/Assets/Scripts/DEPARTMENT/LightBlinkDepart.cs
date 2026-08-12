using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightBlinkDepart : MonoBehaviour
{
    [Header("Лампы")]

    [Tooltip(
        "Realtime-лампы, которые могут иногда мигать."
    )]
    [SerializeField]
    private List<Light> lights =
        new List<Light>();


    [Header("Пауза между миганиями")]

    [Tooltip(
        "Минимальное время до следующего мигания."
    )]
    [SerializeField]
    private float minPause = 7f;

    [Tooltip(
        "Максимальное время до следующего мигания."
    )]
    [SerializeField]
    private float maxPause = 20f;


    [Header("Само мигание")]

    [Tooltip(
        "Минимальное количество провалов света."
    )]
    [SerializeField]
    private int minBlinks = 1;

    [Tooltip(
        "Максимальное количество провалов света."
    )]
    [SerializeField]
    private int maxBlinks = 2;


    [Header("Плавность")]

    [Tooltip(
        "Минимальное время плавного затухания."
    )]
    [SerializeField]
    private float minFadeOutTime = 0.08f;

    [Tooltip(
        "Максимальное время плавного затухания."
    )]
    [SerializeField]
    private float maxFadeOutTime = 0.16f;

    [Tooltip(
        "Минимальное время возврата света."
    )]
    [SerializeField]
    private float minFadeInTime = 0.10f;

    [Tooltip(
        "Максимальное время возврата света."
    )]
    [SerializeField]
    private float maxFadeInTime = 0.22f;


    [Header("Насколько сильно гаснет")]

    [Tooltip(
        "Минимальная доля исходной яркости."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float minIntensityMultiplier = 0.08f;

    [Tooltip(
        "Максимальная доля исходной яркости."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float maxIntensityMultiplier = 0.35f;


    [Header("Пауза внутри серии")]

    [Tooltip(
        "Минимальная пауза между двумя провалами."
    )]
    [SerializeField]
    private float minBetweenBlinks = 0.05f;

    [Tooltip(
        "Максимальная пауза между двумя провалами."
    )]
    [SerializeField]
    private float maxBetweenBlinks = 0.14f;


    [Header("Мигать только когда игрок смотрит")]

    [Tooltip(
        "Если включено, выбираются только лампы, " +
        "находящиеся примерно в направлении взгляда."
    )]
    [SerializeField]
    private bool onlyWhenLookingAt = true;

    [Tooltip(
        "Камера игрока. Если пусто — попробует найти Main Camera."
    )]
    [SerializeField]
    private Camera playerCamera;

    [Tooltip(
        "Ширина области взгляда в градусах."
    )]
    [Range(10f, 180f)]
    [SerializeField]
    private float lookAngle = 80f;

    [Tooltip(
        "Дальше этой дистанции лампа не считается видимой."
    )]
    [SerializeField]
    private float maxLookDistance = 30f;


    private Coroutine blinkRoutine;

    private Light activeBlinkLight;
    private float activeOriginalIntensity;


    private void OnEnable()
    {
        FindCamera();

        blinkRoutine =
            StartCoroutine(BlinkLoop());
    }


    private void OnDisable()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        // Если объект выключили прямо во время мигания,
        // обязательно возвращаем лампе её исходную яркость.
        RestoreActiveLight();
    }


    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(
                    minPause,
                    maxPause
                )
            );

            Light selectedLight =
                GetRandomSuitableLight();

            if (selectedLight == null)
                continue;

            activeBlinkLight =
                selectedLight;

            activeOriginalIntensity =
                selectedLight.intensity;

            int blinkCount =
                Random.Range(
                    minBlinks,
                    maxBlinks + 1
                );

            for (int i = 0;
                 i < blinkCount;
                 i++)
            {
                if (selectedLight == null)
                    break;

                float originalIntensity =
                    activeOriginalIntensity;

                float lowIntensity =
                    originalIntensity *
                    Random.Range(
                        minIntensityMultiplier,
                        maxIntensityMultiplier
                    );

                float fadeOutTime =
                    Random.Range(
                        minFadeOutTime,
                        maxFadeOutTime
                    );

                float fadeInTime =
                    Random.Range(
                        minFadeInTime,
                        maxFadeInTime
                    );

                // Плавно тускнеет.
                yield return FadeIntensity(
                    selectedLight,
                    originalIntensity,
                    lowIntensity,
                    fadeOutTime
                );

                // Плавно возвращается.
                yield return FadeIntensity(
                    selectedLight,
                    lowIntensity,
                    originalIntensity,
                    fadeInTime
                );

                if (i < blinkCount - 1)
                {
                    yield return new WaitForSeconds(
                        Random.Range(
                            minBetweenBlinks,
                            maxBetweenBlinks
                        )
                    );
                }
            }

            RestoreActiveLight();
        }
    }


    private IEnumerator FadeIntensity(
        Light targetLight,
        float from,
        float to,
        float duration)
    {
        if (targetLight == null)
            yield break;

        if (duration <= 0f)
        {
            targetLight.intensity = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            if (targetLight == null)
                yield break;

            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / duration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            targetLight.intensity =
                Mathf.Lerp(
                    from,
                    to,
                    smoothProgress
                );

            yield return null;
        }

        if (targetLight != null)
        {
            targetLight.intensity = to;
        }
    }


    private Light GetRandomSuitableLight()
    {
        if (lights == null ||
            lights.Count == 0)
        {
            return null;
        }

        FindCamera();

        int startIndex =
            Random.Range(
                0,
                lights.Count
            );

        for (int i = 0;
             i < lights.Count;
             i++)
        {
            int index =
                (startIndex + i) %
                lights.Count;

            Light candidate =
                lights[index];

            if (candidate == null)
                continue;

            if (!candidate.enabled)
                continue;

            if (!candidate.gameObject.activeInHierarchy)
                continue;

            if (onlyWhenLookingAt &&
                !IsLookingAtLight(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }


    private bool IsLookingAtLight(
        Light targetLight)
    {
        if (targetLight == null)
            return false;

        if (playerCamera == null)
            return false;

        Vector3 toLight =
            targetLight.transform.position -
            playerCamera.transform.position;

        float distance =
            toLight.magnitude;

        if (distance >
            maxLookDistance)
        {
            return false;
        }

        if (distance <= 0.001f)
            return true;

        toLight /= distance;

        float dot =
            Vector3.Dot(
                playerCamera.transform.forward,
                toLight
            );

        float minimumDot =
            Mathf.Cos(
                lookAngle *
                0.5f *
                Mathf.Deg2Rad
            );

        return dot >= minimumDot;
    }


    private void FindCamera()
    {
        if (playerCamera != null)
            return;

        playerCamera =
            Camera.main;
    }


    private void RestoreActiveLight()
    {
        if (activeBlinkLight != null)
        {
            activeBlinkLight.intensity =
                activeOriginalIntensity;
        }

        activeBlinkLight = null;
    }


    private void OnValidate()
    {
        minPause =
            Mathf.Max(
                0.1f,
                minPause
            );

        maxPause =
            Mathf.Max(
                minPause,
                maxPause
            );

        minBlinks =
            Mathf.Max(
                1,
                minBlinks
            );

        maxBlinks =
            Mathf.Max(
                minBlinks,
                maxBlinks
            );

        minFadeOutTime =
            Mathf.Max(
                0.01f,
                minFadeOutTime
            );

        maxFadeOutTime =
            Mathf.Max(
                minFadeOutTime,
                maxFadeOutTime
            );

        minFadeInTime =
            Mathf.Max(
                0.01f,
                minFadeInTime
            );

        maxFadeInTime =
            Mathf.Max(
                minFadeInTime,
                maxFadeInTime
            );

        maxIntensityMultiplier =
            Mathf.Max(
                minIntensityMultiplier,
                maxIntensityMultiplier
            );

        minBetweenBlinks =
            Mathf.Max(
                0.01f,
                minBetweenBlinks
            );

        maxBetweenBlinks =
            Mathf.Max(
                minBetweenBlinks,
                maxBetweenBlinks
            );

        maxLookDistance =
            Mathf.Max(
                0.1f,
                maxLookDistance
            );
    }
}
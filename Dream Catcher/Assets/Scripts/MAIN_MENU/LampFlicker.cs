using UnityEngine;
using System.Collections;

public class LampFlicker : MonoBehaviour
{
    [Header("Лампа")]
    [Tooltip("Light, интенсивность которого будет меняться.")]
    [SerializeField] private Light targetLight;


    [Header("Обычное мерцание")]
    [Tooltip(
        "Минимальная интенсивность относительно исходной. " +
        "Например 0.82 = 82% от исходной яркости."
    )]
    [Range(0f, 1.5f)]
    [SerializeField] private float minIntensityMultiplier = 0.82f;

    [Tooltip(
        "Максимальная интенсивность относительно исходной."
    )]
    [Range(0f, 2f)]
    [SerializeField] private float maxIntensityMultiplier = 1.05f;


    [Header("Периодичность")]
    [Tooltip("Минимальная пауза между изменениями яркости.")]
    [SerializeField] private float minInterval = 0.08f;

    [Tooltip("Максимальная пауза между изменениями яркости.")]
    [SerializeField] private float maxInterval = 0.35f;


    [Header("Плавность")]
    [Tooltip("Минимальное время изменения интенсивности.")]
    [SerializeField] private float minTransitionDuration = 0.03f;

    [Tooltip("Максимальное время изменения интенсивности.")]
    [SerializeField] private float maxTransitionDuration = 0.12f;


    [Header("Редкое сильное мигание")]
    [Tooltip(
        "Вероятность короткого сильного провала яркости. " +
        "0 = никогда, 1 = каждый раз."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float deepFlickerChance = 0.07f;

    [Tooltip(
        "Минимальная яркость во время редкого сильного провала."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float deepFlickerMinMultiplier = 0.08f;

    [Tooltip(
        "Максимальная яркость во время редкого сильного провала."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float deepFlickerMaxMultiplier = 0.35f;

    [Tooltip("Минимальная длительность сильного провала.")]
    [SerializeField] private float deepFlickerMinDuration = 0.025f;

    [Tooltip("Максимальная длительность сильного провала.")]
    [SerializeField] private float deepFlickerMaxDuration = 0.08f;


    [Header("Поведение")]
    [Tooltip(
        "Вернуть исходную интенсивность, " +
        "если объект или скрипт отключается."
    )]
    [SerializeField] private bool restoreIntensityOnDisable = true;


    private float originalIntensity;
    private Coroutine flickerCoroutine;


    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight != null)
            originalIntensity = targetLight.intensity;
    }


    private void OnEnable()
    {
        if (targetLight == null)
            return;

        /*
         * Если компонент включили повторно,
         * начинаем от текущей базовой интенсивности.
         */
        if (originalIntensity <= 0f)
            originalIntensity = targetLight.intensity;

        flickerCoroutine = StartCoroutine(FlickerRoutine());
    }


    private void OnDisable()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        if (targetLight != null &&
            restoreIntensityOnDisable)
        {
            targetLight.intensity = originalIntensity;
        }
    }


    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(
                Mathf.Max(0.01f, minInterval),
                Mathf.Max(minInterval, maxInterval)
            );

            yield return new WaitForSeconds(waitTime);


            /*
             * Иногда делаем короткий заметный провал.
             */
            if (Random.value < deepFlickerChance)
            {
                float deepMultiplier = Random.Range(
                    deepFlickerMinMultiplier,
                    deepFlickerMaxMultiplier
                );

                float deepIntensity =
                    originalIntensity *
                    deepMultiplier;

                float deepDuration = Random.Range(
                    deepFlickerMinDuration,
                    deepFlickerMaxDuration
                );

                yield return ChangeIntensity(
                    deepIntensity,
                    deepDuration
                );


                /*
                 * После сильного моргания лампа
                 * возвращается не идеально в исходную
                 * яркость, а в слегка случайную.
                 */
                float recoveryMultiplier = Random.Range(
                    minIntensityMultiplier,
                    maxIntensityMultiplier
                );

                float recoveryIntensity =
                    originalIntensity *
                    recoveryMultiplier;

                float recoveryDuration = Random.Range(
                    minTransitionDuration,
                    maxTransitionDuration
                );

                yield return ChangeIntensity(
                    recoveryIntensity,
                    recoveryDuration
                );

                continue;
            }


            /*
             * Обычное небольшое нестабильное
             * изменение яркости.
             */
            float multiplier = Random.Range(
                minIntensityMultiplier,
                maxIntensityMultiplier
            );

            float targetIntensity =
                originalIntensity *
                multiplier;

            float transitionDuration = Random.Range(
                minTransitionDuration,
                maxTransitionDuration
            );

            yield return ChangeIntensity(
                targetIntensity,
                transitionDuration
            );
        }
    }


    private IEnumerator ChangeIntensity(
        float targetIntensity,
        float duration)
    {
        if (targetLight == null)
            yield break;

        if (duration <= 0f)
        {
            targetLight.intensity =
                targetIntensity;

            yield break;
        }

        float startIntensity =
            targetLight.intensity;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            /*
             * Небольшое сглаживание,
             * чтобы изменения не выглядели
             * чисто цифровыми.
             */
            float smoothT =
                t * t * (3f - 2f * t);

            targetLight.intensity =
                Mathf.Lerp(
                    startIntensity,
                    targetIntensity,
                    smoothT
                );

            yield return null;
        }

        targetLight.intensity =
            targetIntensity;
    }
}
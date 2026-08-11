using System.Collections;
using TMPro;
using UnityEngine;

public class AnimatedHUDValue :
    MonoBehaviour
{
    public enum ValueDisplayMode
    {
        Integer,
        TimeMinutes
    }

    [Header("Текст")]

    [Tooltip(
        "TMP-текст, значение которого будет анимироваться."
    )]
    [SerializeField]
    private TMP_Text valueText;

    [Header("Формат")]

    [Tooltip(
        "Integer — обычное число, например 150. " +
        "Time Minutes — минуты как время, например 6:40."
    )]
    [SerializeField]
    private ValueDisplayMode displayMode =
        ValueDisplayMode.Integer;

    [Header("Анимация")]

    [Tooltip(
        "Сколько секунд занимает переход " +
        "от текущего значения к новому."
    )]
    [SerializeField]
    private float animationDuration =
        0.7f;

    [Tooltip(
        "Использовать время, не зависящее от Time.timeScale. " +
        "Для HUD обычно лучше оставить включённым."
    )]
    [SerializeField]
    private bool useUnscaledTime =
        true;

    [Header("Звук начала анимации")]

    [Tooltip(
        "Необязательно. Если AudioSource или клип " +
        "не назначены, анимация будет без звука."
    )]
    [SerializeField]
    private AudioSource animationAudioSource;

    [SerializeField]
    private AudioClip animationStartClip;

    [SerializeField]
    [Range(0f, 1f)]
    private float animationSoundVolume =
        0.7f;

    private Coroutine animationCoroutine;

    private int displayedValue;
    private bool hasDisplayedValue;

    public int DisplayedValue =>
        displayedValue;

    public bool IsAnimating =>
        animationCoroutine != null;

    // =====================================================
    // МГНОВЕННАЯ УСТАНОВКА
    // =====================================================

    public void SetImmediate(
        int value)
    {
        StopCurrentAnimation();

        displayedValue =
            Mathf.Max(
                0,
                value
            );

        hasDisplayedValue =
            true;

        RefreshText();
    }

    // =====================================================
    // ПЛАВНАЯ АНИМАЦИЯ
    // =====================================================

    public void AnimateTo(
        int targetValue)
    {
        targetValue =
            Mathf.Max(
                0,
                targetValue
            );

        // Если значение ещё ни разу
        // не было установлено,
        // сначала принимаем текущее
        // целевое значение мгновенно.
        if (!hasDisplayedValue)
        {
            SetImmediate(
                targetValue
            );

            return;
        }

        StopCurrentAnimation();

        PlayAnimationSound();

        animationCoroutine =
            StartCoroutine(
                AnimateRoutine(
                    targetValue
                )
            );
    }

    private IEnumerator AnimateRoutine(
        int targetValue)
    {
        int startValue =
            displayedValue;

        if (animationDuration <= 0f)
        {
            displayedValue =
                targetValue;

            RefreshText();

            animationCoroutine =
                null;

            yield break;
        }

        float elapsed =
            0f;

        while (elapsed <
               animationDuration)
        {
            elapsed +=
                useUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    animationDuration
                );

            // Плавный старт и
            // плавное торможение.
            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            displayedValue =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        startValue,
                        targetValue,
                        smoothT
                    )
                );

            RefreshText();

            yield return null;
        }

        displayedValue =
            targetValue;

        RefreshText();

        animationCoroutine =
            null;
    }

    // =====================================================
    // ТЕКСТ
    // =====================================================

    private void RefreshText()
    {
        if (valueText == null)
            return;

        switch (displayMode)
        {
            case ValueDisplayMode.Integer:

                valueText.text =
                    displayedValue
                        .ToString();

                break;

            case ValueDisplayMode.TimeMinutes:

                int hours =
                    displayedValue /
                    60;

                int minutes =
                    displayedValue %
                    60;

                valueText.text =
                    hours.ToString() +
                    ":" +
                    minutes.ToString("00");

                break;
        }
    }

    // =====================================================
    // ЗВУК
    // =====================================================

    private void PlayAnimationSound()
    {
        if (animationAudioSource == null ||
            animationStartClip == null)
        {
            return;
        }

        animationAudioSource.PlayOneShot(
            animationStartClip,
            animationSoundVolume
        );
    }

    // =====================================================
    // STOP
    // =====================================================

    private void StopCurrentAnimation()
    {
        if (animationCoroutine == null)
            return;

        StopCoroutine(
            animationCoroutine
        );

        animationCoroutine =
            null;
    }

    private void OnDisable()
    {
        StopCurrentAnimation();
    }

    private void OnValidate()
    {
        animationDuration =
            Mathf.Max(
                0f,
                animationDuration
            );
    }
}
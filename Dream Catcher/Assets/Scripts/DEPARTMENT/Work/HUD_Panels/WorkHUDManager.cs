using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class WorkHUDManager : MonoBehaviour
{
    [Header("HUD")]
    public CanvasGroup canvasGroup;

    [Header("Fade")]
    public float fadeDuration = 0.35f;

    private Coroutine fadeCoroutine;

    // Настоящее состояние HUD,
    // которым управляют рабочие скрипты, зум и т.д.
    private bool requestedVisible = false;

    // Временные блокировки поверх настоящего состояния.
    private bool pauseBlocked = false;
    private bool taskPanelBlocked = false;


    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        requestedVisible = false;

        ApplyInstant(0f);
    }


    // Обычная логика игры:
    // "HUD сейчас должен быть виден".
    public void Show()
    {
        requestedVisible = true;
        RefreshVisibility();
    }


    // Обычная логика игры:
    // "HUD сейчас должен быть скрыт".
    public void Hide()
    {
        requestedVisible = false;
        RefreshVisibility();
    }


    public void HideInstant()
    {
        requestedVisible = false;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        ApplyInstant(0f);
    }


    // PauseManager вызывает только это.
    public void SetPauseBlocked(bool blocked)
    {
        if (pauseBlocked == blocked)
            return;

        pauseBlocked = blocked;
        RefreshVisibility();
    }


    // TaskPanelController вызывает только это.
    public void SetTaskPanelBlocked(bool blocked)
    {
        if (taskPanelBlocked == blocked)
            return;

        taskPanelBlocked = blocked;
        RefreshVisibility();
    }


    private void RefreshVisibility()
    {
        bool temporarilyBlocked =
            pauseBlocked ||
            taskPanelBlocked;

        bool shouldActuallyBeVisible =
            requestedVisible &&
            !temporarilyBlocked;

        StartFade(
            shouldActuallyBeVisible
                ? 1f
                : 0f
        );
    }


    private void StartFade(float targetAlpha)
    {
        if (canvasGroup == null)
            return;

        gameObject.SetActive(true);

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // Если уже на нужном значении,
        // лишнюю корутину не запускаем.
        if (Mathf.Approximately(
            canvasGroup.alpha,
            targetAlpha))
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        fadeCoroutine =
            StartCoroutine(
                FadeRoutine(targetAlpha)
            );
    }


    private IEnumerator FadeRoutine(
        float targetAlpha)
    {
        float startAlpha =
            canvasGroup.alpha;

        float timer = 0f;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (timer < fadeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                fadeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        timer /
                        fadeDuration
                    );

            float smoothT =
                t * t *
                (3f - 2f * t);

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    smoothT
                );

            yield return null;
        }

        canvasGroup.alpha =
            targetAlpha;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        fadeCoroutine = null;
    }


    private void ApplyInstant(float alpha)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
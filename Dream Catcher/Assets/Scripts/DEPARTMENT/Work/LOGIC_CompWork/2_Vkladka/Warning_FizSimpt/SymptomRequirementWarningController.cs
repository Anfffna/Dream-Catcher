using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class SymptomRequirementWarningController :
    MonoBehaviour
{
    [Header("Компоненты")]

    [Tooltip("Прямоугольник сообщения, которое выезжает из скрытой области.")]
    [SerializeField]
    private RectTransform messageRect;

    [Tooltip("Canvas Group сообщения.")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [Header("Движение")]

    [Tooltip("Смещение сообщения в скрытом состоянии.")]
    [SerializeField]
    private Vector2 hiddenOffset =
        new Vector2(0f, -70f);

    [Tooltip("Длительность появления.")]
    [SerializeField]
    private float showDuration =
        0.25f;

    [Tooltip("Сколько сообщение остаётся видимым.")]
    [SerializeField]
    private float visibleDuration =
        2.5f;

    [Tooltip("Длительность исчезновения.")]
    [SerializeField]
    private float hideDuration =
        0.25f;

    private Vector2 visiblePosition;
    private Vector2 hiddenPosition;

    private Coroutine warningRoutine;

    private void Awake()
    {
        if (messageRect == null)
        {
            messageRect =
                transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (messageRect != null)
        {
            visiblePosition =
                messageRect.anchoredPosition;

            hiddenPosition =
                visiblePosition +
                hiddenOffset;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable =
                false;

            canvasGroup.blocksRaycasts =
                false;
        }

        SetState(
            hiddenPosition,
            0f
        );
    }

    private void OnDisable()
    {
        if (warningRoutine != null)
        {
            StopCoroutine(
                warningRoutine
            );

            warningRoutine = null;
        }

        SetState(
            hiddenPosition,
            0f
        );
    }

    public void ShowWarning()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (warningRoutine != null)
        {
            StopCoroutine(
                warningRoutine
            );
        }

        warningRoutine =
            StartCoroutine(
                WarningRoutine()
            );
    }

    public void HideImmediately()
    {
        if (warningRoutine != null)
        {
            StopCoroutine(
                warningRoutine
            );

            warningRoutine = null;
        }

        SetState(
            hiddenPosition,
            0f
        );
    }

    private IEnumerator WarningRoutine()
    {
        yield return AnimateTo(
            visiblePosition,
            1f,
            showDuration
        );

        if (visibleDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                visibleDuration
            );
        }

        yield return AnimateTo(
            hiddenPosition,
            0f,
            hideDuration
        );

        warningRoutine = null;
    }

    private IEnumerator AnimateTo(
        Vector2 targetPosition,
        float targetAlpha,
        float duration)
    {
        if (messageRect == null ||
            canvasGroup == null)
        {
            yield break;
        }

        Vector2 startPosition =
            messageRect.anchoredPosition;

        float startAlpha =
            canvasGroup.alpha;

        if (duration <= 0f)
        {
            SetState(
                targetPosition,
                targetAlpha
            );

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            messageRect.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    smoothT
                );

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    smoothT
                );

            yield return null;
        }

        SetState(
            targetPosition,
            targetAlpha
        );
    }

    private void SetState(
        Vector2 position,
        float alpha)
    {
        if (messageRect != null)
        {
            messageRect.anchoredPosition =
                position;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                alpha;
        }
    }

    private void OnValidate()
    {
        showDuration =
            Mathf.Max(
                0f,
                showDuration
            );

        visibleDuration =
            Mathf.Max(
                0f,
                visibleDuration
            );

        hideDuration =
            Mathf.Max(
                0f,
                hideDuration
            );
    }
}
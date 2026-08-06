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

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        HideInstant();
    }

    public void Show()
    {
        StartFade(1f);
    }

    public void Hide()
    {
        StartFade(0f);
    }

    public void HideInstant()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void StartFade(float targetAlpha)
    {
        if (canvasGroup == null)
            return;

        gameObject.SetActive(true);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine =
            StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = fadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(timer / fadeDuration);

            float smoothT = t * t * (3f - 2f * t);

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    smoothT
                );

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        fadeCoroutine = null;
    }
}
using UnityEngine;
using System.Collections;

public class TaskUpdateToast : MonoBehaviour
{
    [Header("Toast")]
    public RectTransform toastTransform;

    [Header("Positions")]
    public float hiddenX = -500f;
    public float visibleX = 40f;

    [Header("Timing")]
    public float slideDuration = 0.5f;
    public float visibleTime = 2f;

    public bool IsShowing { get; private set; } = false;

    private Coroutine toastCoroutine;

    void Awake()
    {
        if (toastTransform != null)
        {
            Vector2 pos = toastTransform.anchoredPosition;
            pos.x = hiddenX;
            toastTransform.anchoredPosition = pos;
        }

        IsShowing = false;
    }

    public void ShowToast()
    {
        if (toastCoroutine != null)
            StopCoroutine(toastCoroutine);

        toastCoroutine = StartCoroutine(ToastRoutine());
    }

    public void HideToastNow()
    {
        if (toastCoroutine != null)
            StopCoroutine(toastCoroutine);

        toastCoroutine = StartCoroutine(HideRoutine());
    }

    IEnumerator ToastRoutine()
    {
        IsShowing = true;

        yield return MoveToX(visibleX);

        yield return new WaitForSeconds(visibleTime);

        yield return MoveToX(hiddenX);

        IsShowing = false;
        toastCoroutine = null;
    }

    IEnumerator HideRoutine()
    {
        yield return MoveToX(hiddenX);

        IsShowing = false;
        toastCoroutine = null;
    }

    IEnumerator MoveToX(float targetX)
    {
        if (toastTransform == null) yield break;

        Vector2 startPos = toastTransform.anchoredPosition;
        Vector2 targetPos = startPos;
        targetPos.x = targetX;

        float timer = 0f;

        while (timer < slideDuration)
        {
            timer += Time.deltaTime;

            float t = slideDuration <= 0f
                ? 1f
                : Mathf.Clamp01(timer / slideDuration);

            float smoothT = t * t * (3f - 2f * t);

            toastTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);

            yield return null;
        }

        toastTransform.anchoredPosition = targetPos;
    }
}
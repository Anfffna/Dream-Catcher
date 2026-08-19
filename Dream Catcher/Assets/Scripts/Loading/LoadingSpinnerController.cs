using UnityEngine;
using System.Collections;

public class LoadingSpinnerController : MonoBehaviour
{
    [Header("References")]
    public RectTransform outerCircle;
    public RectTransform innerDot;

    [Header("Settings")]
    public float orbitRadius = 30f;
    public float speed = 180f;

    [Header("Appearance")]
    public float fadeDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private float currentAngle = 0f;
    private Vector2 centerPosition;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Изначально скрыт (прозрачен)
        canvasGroup.alpha = 0f;
        // Не выключаем объект, чтобы корутины работали

        // Вычисляем радиус, если есть ссылки
        if (outerCircle != null)
        {
            float outerRadius = outerCircle.rect.width * 0.5f;
            float innerRadius = innerDot != null ? innerDot.rect.width * 0.5f : 0f;
            orbitRadius = outerRadius - innerRadius - 2f;
            centerPosition = outerCircle.anchoredPosition;
        }
        else
        {
            centerPosition = GetComponent<RectTransform>().anchoredPosition;
        }
    }

    public void Show()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(0f, 1f, fadeDuration));
    }

    public void Hide()
    {
        StopAllCoroutines();
        // Мгновенно скрываем без корутины, чтобы избежать ошибок при вызове до инициализации
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void HideSmooth()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(1f, 0f, fadeDuration));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    void Update()
    {
        if (canvasGroup == null || canvasGroup.alpha <= 0.01f) return;
        if (innerDot == null) return;

        currentAngle += speed * Time.deltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;
        float rad = currentAngle * Mathf.Deg2Rad;
        float x = centerPosition.x + orbitRadius * Mathf.Cos(rad);
        float y = centerPosition.y + orbitRadius * Mathf.Sin(rad);
        innerDot.anchoredPosition = new Vector2(x, y);
    }
}
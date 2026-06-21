using UnityEngine;

public class LoadingSpinnerController : MonoBehaviour
{
    [Header("References")]
    public RectTransform outerCircle;   // внешний круг (дл€ определени€ радиуса)
    public RectTransform innerDot;      // маленький кружок

    [Header("Settings")]
    public float orbitRadius = 30f;     // радиус орбиты в пиксел€х
    public float speed = 180f;          // градусов в секунду (полный оборот за 2 сек)

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
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);

        // ≈сли внешний круг задан, можно вычислить радиус автоматически
        if (outerCircle != null)
        {
            float outerRadius = outerCircle.rect.width * 0.5f;
            float innerRadius = innerDot.rect.width * 0.5f;
            orbitRadius = outerRadius - innerRadius - 2f; // минус небольшой отступ
        }

        // «апоминаем центр (это центр внешнего круга)
        if (outerCircle != null)
            centerPosition = outerCircle.anchoredPosition;
        else
            centerPosition = GetComponent<RectTransform>().anchoredPosition; // или Vector2.zero
    }

    public void Show()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        currentAngle = 0f; // начальный угол (можно задать случайный)
        StopAllCoroutines();
        StartCoroutine(Fade(0f, 1f, fadeDuration));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(1f, 0f, fadeDuration, () => gameObject.SetActive(false)));
    }

    private System.Collections.IEnumerator Fade(float from, float to, float duration, System.Action onComplete = null)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
        onComplete?.Invoke();
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        // ќбновл€ем угол
        currentAngle += speed * Time.deltaTime;
        if (currentAngle > 360f) currentAngle -= 360f;

        // ¬ычисл€ем позицию на окружности
        float rad = currentAngle * Mathf.Deg2Rad;
        float x = centerPosition.x + orbitRadius * Mathf.Cos(rad);
        float y = centerPosition.y + orbitRadius * Mathf.Sin(rad);

        innerDot.anchoredPosition = new Vector2(x, y);
    }

    // ќпционально: можно мен€ть направление (по часовой / против)
    public void SetClockwise(bool clockwise)
    {
        speed = clockwise ? Mathf.Abs(speed) : -Mathf.Abs(speed);
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class IndicatorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Indicator")]
    public RawImage indicator;

    [Header("Colors")]
    public Color normalColor = Color.gray;
    public Color activeColor = Color.red;

    [Header("Transition")]
    public float transitionSpeed = 5f;

    private Coroutine colorCoroutine;
    private static IndicatorHover selectedInstance = null;

    void Start()
    {
        // Устанавливаем нормальный цвет
        indicator.color = normalColor;

        // Проверяем размер, чтобы индикатор был виден
        if (indicator.rectTransform.sizeDelta.magnitude < 1f)
            indicator.rectTransform.sizeDelta = new Vector2(25, 10);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (indicator == null) return;
        if (selectedInstance == this) return;
        SetColor(activeColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (indicator == null) return;
        if (selectedInstance == this)
        {
            SetColor(activeColor);
            return;
        }
        SetColor(normalColor);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Select();
    }

    public void Select()
    {
        if (selectedInstance == this) return;

        if (selectedInstance != null)
            selectedInstance.Deselect();

        selectedInstance = this;
        if (indicator != null)
            SetColor(activeColor);
    }

    public void Deselect()
    {
        if (indicator != null)
            SetColor(normalColor);
    }

    private void SetColor(Color targetColor)
    {
        // Если объект неактивен – выходим, не запускаем корутину
        if (!gameObject.activeInHierarchy) return;

        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(ChangeColor(indicator.color, targetColor));
    }

    private IEnumerator ChangeColor(Color from, Color to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * transitionSpeed; // <-- ключевая правка
            indicator.color = Color.Lerp(from, to, t);
            yield return null;
        }
        indicator.color = to;
        colorCoroutine = null;
    }

    public static void ClearSelection()
    {
        if (selectedInstance != null)
        {
            selectedInstance.Deselect();
            selectedInstance = null;
        }
    }

    public static void ResetSelection()
    {
        if (selectedInstance != null)
        {
            selectedInstance.Deselect();
            selectedInstance = null;
        }
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI; // для Image

public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI text;          // опционально
    public Image imageToScale;            // опционально (добавлено)
    public float scaleMultiplier = 1.1f;
    public float duration = 0.2f;

    private Vector3 originalTextScale;
    private Vector3 originalImageScale;

    void Start()
    {
        if (text == null) text = GetComponentInChildren<TextMeshProUGUI>();
        if (imageToScale == null) imageToScale = GetComponentInChildren<Image>();

        if (text != null)
            originalTextScale = text.transform.localScale;
        if (imageToScale != null)
            originalImageScale = imageToScale.transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (text != null)
            text.transform.localScale = originalTextScale * scaleMultiplier;
        if (imageToScale != null)
            imageToScale.transform.localScale = originalImageScale * scaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (text != null)
            text.transform.localScale = originalTextScale;
        if (imageToScale != null)
            imageToScale.transform.localScale = originalImageScale;
    }
}
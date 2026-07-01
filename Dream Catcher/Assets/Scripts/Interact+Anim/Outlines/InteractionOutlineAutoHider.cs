using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InteractionOutlineAutoHider : MonoBehaviour
{
    [Header("Search Mode")]
    public bool scanAllActiveCanvases = true;

    [Tooltip("Если scanAllActiveCanvases выключен, проверяется только этот корень UI.")]
    public RectTransform uiRoot;

    [Header("Ignore")]
    [Tooltip("UI-объекты, которые нужно игнорировать. Сам InteractionOutlineCanvas игнорируется автоматически.")]
    public RectTransform[] ignoredRoots;

    [Header("Large UI Detection")]
    public float minWidthPixels = 400f;
    public float minHeightPixels = 400f;

    [Range(0f, 1f)]
    public float alphaThreshold = 0.05f;

    [Tooltip("Если включено, крупный RectTransform считается UI только если внутри есть видимая графика: Image, Text, TMP и т.п.")]
    public bool requireVisibleGraphic = true;

    [Header("Outline")]
    public bool redrawWhenShown = true;

    [Header("Debug")]
    public bool debugLogBlocker = false;
    [SerializeField] private string currentBlockerName = "";

    private RectTransform ownRect;
    private CanvasGroup ownCanvasGroup;
    private bool isHidden = false;

    private void Awake()
    {
        ownRect = GetComponent<RectTransform>();

        if (uiRoot == null && transform.parent != null)
            uiRoot = transform.parent as RectTransform;

        EnsureCanvasGroup();
        ApplyVisibility(false);
    }

    private void OnEnable()
    {
        EnsureCanvasGroup();
        ApplyVisibility(false);
    }

    private void LateUpdate()
    {
        bool shouldHide = HasLargeVisibleUI();
        ApplyVisibility(shouldHide);
    }

    private void EnsureCanvasGroup()
    {
        if (ownCanvasGroup == null)
            ownCanvasGroup = GetComponent<CanvasGroup>();

        if (ownCanvasGroup == null)
            ownCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        ownCanvasGroup.interactable = false;
        ownCanvasGroup.blocksRaycasts = false;
    }

    private bool HasLargeVisibleUI()
    {
        currentBlockerName = "";

        if (scanAllActiveCanvases)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];

                if (canvas == null)
                    continue;

                if (!canvas.gameObject.activeInHierarchy)
                    continue;

                RectTransform canvasRect = canvas.GetComponent<RectTransform>();

                if (canvasRect == null)
                    continue;

                if (CheckRoot(canvasRect))
                    return true;
            }

            return false;
        }

        if (uiRoot == null)
            return false;

        return CheckRoot(uiRoot);
    }

    private bool CheckRoot(RectTransform root)
    {
        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);

        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];

            if (rect == null)
                continue;

            if (!rect.gameObject.activeInHierarchy)
                continue;

            if (IsOwnOutlineCanvas(rect))
                continue;

            if (IsIgnored(rect))
                continue;

            if (!IsVisibleByCanvasGroups(rect.transform))
                continue;

            // Важно:
            // Сам корень Canvas часто размером на весь экран.
            // Но это не значит, что открыт большой UI.
            // Поэтому пустой Canvas-root без Graphic на себе пропускаем.
            Canvas canvasOnThisObject = rect.GetComponent<Canvas>();
            Graphic graphicOnThisObject = rect.GetComponent<Graphic>();

            if (canvasOnThisObject != null && graphicOnThisObject == null)
                continue;

            Vector2 screenSize = GetScreenSize(rect);

            if (screenSize.x < minWidthPixels)
                continue;

            if (screenSize.y < minHeightPixels)
                continue;

            if (requireVisibleGraphic && !HasVisibleGraphic(rect))
                continue;

            currentBlockerName = rect.name;

            if (debugLogBlocker)
                Debug.Log("InteractionOutlineAutoHider: outline скрыт из-за крупного UI: " + rect.name);

            return true;
        }

        return false;
    }

    private bool IsOwnOutlineCanvas(RectTransform rect)
    {
        if (ownRect == null)
            return false;

        return rect == ownRect || rect.transform.IsChildOf(ownRect);
    }

    private bool IsIgnored(RectTransform rect)
    {
        if (ignoredRoots == null)
            return false;

        for (int i = 0; i < ignoredRoots.Length; i++)
        {
            RectTransform ignored = ignoredRoots[i];

            if (ignored == null)
                continue;

            if (rect == ignored || rect.transform.IsChildOf(ignored))
                return true;
        }

        return false;
    }

    private bool IsVisibleByCanvasGroups(Transform target)
    {
        CanvasGroup[] groups = target.GetComponentsInParent<CanvasGroup>(true);

        for (int i = 0; i < groups.Length; i++)
        {
            CanvasGroup group = groups[i];

            if (group == null)
                continue;

            // Свой CanvasGroup не учитываем,
            // иначе скрипт мог бы сам себя считать скрытым.
            if (group == ownCanvasGroup)
                continue;

            if (group.alpha <= alphaThreshold)
                return false;
        }

        return true;
    }

    private bool HasVisibleGraphic(RectTransform root)
    {
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];

            if (graphic == null)
                continue;

            if (!graphic.gameObject.activeInHierarchy)
                continue;

            if (!graphic.enabled)
                continue;

            if (graphic.color.a <= alphaThreshold)
                continue;

            if (IsOwnOutlineCanvas(graphic.rectTransform))
                continue;

            if (IsIgnored(graphic.rectTransform))
                continue;

            if (!IsVisibleByCanvasGroups(graphic.transform))
                continue;

            return true;
        }

        return false;
    }

    private Vector2 GetScreenSize(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Canvas parentCanvas = rect.GetComponentInParent<Canvas>();
        Camera uiCamera = null;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = parentCanvas.worldCamera;

        Vector2 min = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
        Vector2 max = min;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);

            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        return max - min;
    }

    private void ApplyVisibility(bool shouldHide)
    {
        EnsureCanvasGroup();

        if (isHidden == shouldHide)
            return;

        isHidden = shouldHide;

        ownCanvasGroup.alpha = isHidden ? 0f : 1f;
        ownCanvasGroup.interactable = false;
        ownCanvasGroup.blocksRaycasts = false;

        if (!isHidden && redrawWhenShown)
            InteractionOutlineRegistry.RedrawVisibleOutlines();
    }
}
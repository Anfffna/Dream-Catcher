using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InteractionOutline : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("UI Parent")]
    public RectTransform outlineCanvasParent;

    [Header("Auto Find")]
    public string playerCameraObjectName = "Camera";
    public string outlineCanvasParentObjectName = "InteractionOutlineCanvas";
    public bool autoFindReferences = true;

    [Header("Quest Outline ID")]
    public string outlineId;

    [Header("Outline Look")]
    public Color outlineColor = new Color(0.745f, 0.831f, 0.663f, 1f);
    public float lineThicknessPixels = 4f;
    public float screenPaddingPixels = 3f;

    [Header("Line Texture")]
    public Sprite lineSprite;
    public Image.Type lineImageType = Image.Type.Tiled;

    [Header("Occlusion")]
    public bool hideWhenOccluded = true;
    public LayerMask occlusionMask = ~0;

    [Header("Settings")]
    public bool includeChildren = false;
    public bool hideOnStart = true;
    public bool updateEveryFrame = true;

    private MeshFilter[] meshFilters;
    private Renderer[] renderers;

    private readonly List<Image> lineImages = new List<Image>();
    private Canvas parentCanvas;
    private bool isVisible = false;
    private RectTransform currentLineParent;

    private struct ScreenPoint
    {
        public Vector2 position;

        public ScreenPoint(Vector2 newPosition)
        {
            position = newPosition;
        }
    }

    void Awake()
    {
        ResolveReferences();

        meshFilters = includeChildren
            ? GetComponentsInChildren<MeshFilter>(true)
            : GetComponents<MeshFilter>();

        renderers = includeChildren
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();
    }

    void Start()
    {
        ResolveReferences();

        if (InteractionOutlineRegistry.ShouldBeVisible(outlineId))
        {
            ShowOutline();
            return;
        }

        if (hideOnStart)
            HideOutline();
        else
            ShowOutline();
    }

    void LateUpdate()
    {
        if (!isVisible) return;
        if (!updateEveryFrame) return;

        DrawOutline();
    }

    public void ShowOutline()
    {
        ResolveReferences();

        isVisible = true;
        DrawOutline();
    }

    public void HideOutline()
    {
        isVisible = false;
        ClearLineImages();
    }

    private void OnEnable()
    {
        ResolveReferences();
        InteractionOutlineRegistry.Register(outlineId, this);
    }

    private void OnDisable()
    {
        InteractionOutlineRegistry.Unregister(outlineId, this);
        ClearLineImages();
    }

    public void ForceRedrawOutline()
    {
        ClearLineImages();
        ShowOutline();
    }

    private void OnDestroy()
    {
        InteractionOutlineRegistry.Unregister(outlineId, this);
        ClearLineImages();
    }

    private void DrawOutline()
    {
        ResolveReferences();

        if (playerCamera == null) return;
        if (outlineCanvasParent == null) return;

        List<ScreenPoint> screenPoints = new List<ScreenPoint>();

        CollectMeshScreenPoints(screenPoints);

        if (screenPoints.Count < 3)
        {
            HideAllLines();
            return;
        }

        List<ScreenPoint> hull = BuildConvexHull(screenPoints);

        if (hull.Count < 3)
        {
            HideAllLines();
            return;
        }

        Vector2 center = Vector2.zero;

        for (int i = 0; i < hull.Count; i++)
            center += hull[i].position;

        center /= hull.Count;

        List<Vector2> paddedHull = new List<Vector2>();

        for (int i = 0; i < hull.Count; i++)
        {
            Vector2 point = hull[i].position;
            Vector2 direction = point - center;

            if (direction.sqrMagnitude > 0.001f)
                point += direction.normalized * screenPaddingPixels;

            paddedHull.Add(point);
        }

        if (hideWhenOccluded && IsObjectOccluded())
        {
            HideAllLines();
            return;
        }

        DrawUILines(paddedHull);
    }

    private void ResolveReferences()
    {
        if (!autoFindReferences)
            return;

        if (playerCamera == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                playerCamera = mainCamera;
            }
            else if (!string.IsNullOrEmpty(playerCameraObjectName))
            {
                GameObject cameraObject = GameObject.Find(playerCameraObjectName);

                if (cameraObject != null)
                    playerCamera = cameraObject.GetComponent<Camera>();
            }
        }

        if (outlineCanvasParent == null && !string.IsNullOrEmpty(outlineCanvasParentObjectName))
        {
            GameObject canvasParentObject = GameObject.Find(outlineCanvasParentObjectName);

            if (canvasParentObject != null)
                outlineCanvasParent = canvasParentObject.GetComponent<RectTransform>();
        }

        if (outlineCanvasParent != currentLineParent)
        {
            ClearLineImages();
            currentLineParent = outlineCanvasParent;
        }

        if (outlineCanvasParent != null)
            parentCanvas = outlineCanvasParent.GetComponentInParent<Canvas>();
    }

    private void CollectMeshScreenPoints(List<ScreenPoint> screenPoints)
    {
        if (meshFilters == null) return;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];

            if (meshFilter == null) continue;
            if (meshFilter.sharedMesh == null) continue;

            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices;

            try
            {
                vertices = mesh.vertices;
            }
            catch
            {
                continue;
            }

            Transform meshTransform = meshFilter.transform;

            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 worldPoint = meshTransform.TransformPoint(vertices[v]);
                Vector3 screenPoint = playerCamera.WorldToScreenPoint(worldPoint);

                if (screenPoint.z > 0f)
                    screenPoints.Add(new ScreenPoint(new Vector2(screenPoint.x, screenPoint.y)));
            }
        }
    }

    private void DrawUILines(List<Vector2> screenPoints)
    {
        int count = screenPoints.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 screenA = screenPoints[i];
            Vector2 screenB = screenPoints[(i + 1) % count];

            Vector2 localA;
            Vector2 localB;

            Camera uiCamera = null;

            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = parentCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                outlineCanvasParent,
                screenA,
                uiCamera,
                out localA
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                outlineCanvasParent,
                screenB,
                uiCamera,
                out localB
            );

            Image line = GetLineImage(i);

            if (line == null)
                continue;

            line.gameObject.SetActive(true);

            Vector2 direction = localB - localA;
            float length = direction.magnitude;

            RectTransform rect = line.rectTransform;
            rect.anchoredPosition = (localA + localB) / 2f;
            rect.sizeDelta = new Vector2(length, lineThicknessPixels);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);

            ApplyLineVisual(line);
        }

        for (int i = count; i < lineImages.Count; i++)
        {
            if (lineImages[i] != null)
                lineImages[i].gameObject.SetActive(false);
        }
    }

    private Image GetLineImage(int index)
    {
        if (outlineCanvasParent == null)
            return null;

        while (lineImages.Count <= index)
            lineImages.Add(null);

        if (lineImages[index] == null)
        {
            GameObject lineObject = new GameObject("InteractionOutline_UI_Line");
            lineObject.transform.SetParent(outlineCanvasParent, false);

            Image image = lineObject.AddComponent<Image>();
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            ApplyLineVisual(image);

            lineObject.SetActive(false);
            lineImages[index] = image;
        }

        return lineImages[index];
    }

    private void ApplyLineVisual(Image image)
    {
        if (image == null) return;

        image.color = outlineColor;

        if (lineSprite != null)
        {
            image.sprite = lineSprite;
            image.type = lineImageType;
        }
        else
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
        }
    }

    private void HideAllLines()
    {
        for (int i = 0; i < lineImages.Count; i++)
        {
            if (lineImages[i] != null)
                lineImages[i].gameObject.SetActive(false);
        }
    }

    private void ClearLineImages()
    {
        for (int i = 0; i < lineImages.Count; i++)
        {
            if (lineImages[i] != null)
                Destroy(lineImages[i].gameObject);
        }

        lineImages.Clear();
    }

    private List<ScreenPoint> BuildConvexHull(List<ScreenPoint> points)
    {
        points.Sort((a, b) =>
        {
            int compareX = a.position.x.CompareTo(b.position.x);

            if (compareX == 0)
                return a.position.y.CompareTo(b.position.y);

            return compareX;
        });

        List<ScreenPoint> hull = new List<ScreenPoint>();

        for (int i = 0; i < points.Count; i++)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2].position, hull[hull.Count - 1].position, points[i].position) <= 0f)
                hull.RemoveAt(hull.Count - 1);

            hull.Add(points[i]);
        }

        int lowerCount = hull.Count;

        for (int i = points.Count - 2; i >= 0; i--)
        {
            while (hull.Count > lowerCount && Cross(hull[hull.Count - 2].position, hull[hull.Count - 1].position, points[i].position) <= 0f)
                hull.RemoveAt(hull.Count - 1);

            hull.Add(points[i]);
        }

        if (hull.Count > 1)
            hull.RemoveAt(hull.Count - 1);

        return hull;
    }

    private float Cross(Vector2 origin, Vector2 a, Vector2 b)
    {
        return (a.x - origin.x) * (b.y - origin.y) - (a.y - origin.y) * (b.x - origin.x);
    }

    private bool IsObjectOccluded()
    {
        if (playerCamera == null) return false;

        // Вычисляем общий bounds объекта (учитываем все рендереры)
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (!hasBounds)
            return false;

        // Точки для проверки: центр и 8 углов ограничивающего параллелепипеда
        Vector3[] points = new Vector3[]
        {
        bounds.center,
        bounds.min,
        bounds.max,
        new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
        new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
        new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
        new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
        new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
        new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)
        };

        Vector3 cameraPosition = playerCamera.transform.position;

        foreach (Vector3 point in points)
        {
            Vector3 direction = point - cameraPosition;
            float distance = direction.magnitude;
            if (distance <= 0.01f) continue;

            Ray ray = new Ray(cameraPosition, direction.normalized);
            if (Physics.Raycast(ray, out RaycastHit hit, distance, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                // Если луч попал в сам объект или его дочернюю часть – не считаем перекрытием
                if (hit.collider.transform == transform) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;

                // Любое другое препятствие – объект перекрыт
                return true;
            }
        }

        return false;
    }
}
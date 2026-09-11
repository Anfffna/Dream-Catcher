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
    public string outlineCanvasParentObjectName =
        "InteractionOutlineCanvas";

    public bool autoFindReferences = true;


    [Header("Quest Outline ID")]
    public string outlineId;


    [Header("Outline Look")]
    public Color outlineColor =
        new Color(
            0.745f,
            0.831f,
            0.663f,
            1f
        );

    public float lineThicknessPixels = 4f;
    public float screenPaddingPixels = 3f;


    [Header("Small Object Outline Smoothing")]
    public bool simplifySmallOutlines = true;

    [Tooltip(
        "Объекты меньше этого размера на экране " +
        "будут иметь упрощённый контур."
    )]
    public float smallOutlineMaxSizePixels = 120f;

    [Range(4, 12)]
    [Tooltip(
        "Максимальное количество точек контура " +
        "у маленьких объектов."
    )]
    public int smallOutlinePoints = 8;


    [Header("Line Texture")]
    public Sprite lineSprite;
    public Image.Type lineImageType =
        Image.Type.Tiled;


    [Header("Occlusion")]

    [Tooltip(
        "Полностью скрывать обводку, " +
        "когда объект почти перекрыт другими объектами."
    )]
    public bool hideWhenOccluded = true;

    [Tooltip(
        "Какие слои могут закрывать объект. " +
        "Обычно можно оставить Everything."
    )]
    public LayerMask occlusionMask = ~0;

    [Range(0.05f, 0.95f)]
    [Tooltip(
        "Минимальная видимая доля объекта. " +
        "0.20 = пока видно примерно 20% объекта, " +
        "обводка остаётся. Если видно меньше — скрывается."
    )]
    public float minimumVisibleRatio = 0.20f;


    [Header("Settings")]
    public bool includeChildren = false;
    public bool hideOnStart = true;


    // =========================================================
    // GEOMETRY CACHE
    // =========================================================

    private MeshFilter[] meshFilters;
    private Renderer[] renderers;
    private Vector3[][] cachedVertices;

    private bool geometryCacheReady;


    // =========================================================
    // UI
    // =========================================================

    private readonly List<Image> lineImages =
        new List<Image>();

    private Canvas parentCanvas;
    private RectTransform currentLineParent;


    // =========================================================
    // STATE
    // =========================================================

    private bool isVisible;

    private bool initializedView;

    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;
    private float lastCameraFieldOfView;
    private float lastCameraOrthographicSize;
    private bool lastCameraOrthographic;

    private int lastCameraPixelWidth;
    private int lastCameraPixelHeight;


    // =========================================================
    // OCCLUSION
    // =========================================================

    private bool cachedOccluded;

    private float lastOcclusionCheck =
        float.NegativeInfinity;

    /*
     * Проверяем перекрытие не каждый кадр.
     *
     * 0.15 = примерно 6–7 проверок в секунду.
     * На одну обводку это максимум 9 Raycast
     * за одну проверку.
     */
    private const float occlusionInterval =
        0.15f;

    /*
     * Маленький гистерезис предотвращает
     * дрожание на самой границе видимости.
     *
     * Он практически не меняет порог 20%.
     */
    private const float visibilityHysteresis =
        0.015f;

    /*
     * Центр + 8 углов Bounds.
     *
     * Массив создаётся один раз,
     * а не при каждой проверке.
     */
    private readonly Vector3[] occlusionPoints =
        new Vector3[9];


    // =========================================================
    // REUSABLE BUFFERS
    // =========================================================

    private readonly List<ScreenPoint>
        cachedScreenPoints =
            new List<ScreenPoint>(256);

    private readonly List<ScreenPoint>
        cachedHull =
            new List<ScreenPoint>(128);

    private readonly List<ScreenPoint>
        cachedResampledHull =
            new List<ScreenPoint>(16);

    private readonly List<float>
        cachedEdgeLengths =
            new List<float>(64);

    /*
     * Вот эти мировые точки являются
     * уже готовой границей объекта.
     *
     * При FOV Zoom мы используем только их,
     * а не тысячи вершин Mesh.
     */
    private readonly List<Vector3>
        cachedOutlineWorldPoints =
            new List<Vector3>(64);

    private readonly List<Vector2>
        cachedProjectedHull =
            new List<Vector2>(64);

    private readonly List<Vector2>
        cachedPaddedHull =
            new List<Vector2>(64);


    private struct ScreenPoint
    {
        public Vector2 position;
        public Vector3 worldPosition;


        public ScreenPoint(
            Vector2 newPosition,
            Vector3 newWorldPosition)
        {
            position = newPosition;
            worldPosition = newWorldPosition;
        }
    }


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        /*
         * Здесь больше не копируем Mesh.vertices
         * у всех InteractionOutline сцены.
         *
         * Это будет сделано только тогда,
         * когда конкретная обводка реально
         * впервые понадобится.
         */
        ResolveReferences();
    }


    private void Start()
    {
        ResolveReferences();


        if (InteractionOutlineRegistry
            .ShouldBeVisible(outlineId))
        {
            ShowOutline();
            return;
        }


        if (hideOnStart)
        {
            HideOutline();
        }
        else
        {
            ShowOutline();
        }
    }


    private void OnEnable()
    {
        ResolveReferences();

        InteractionOutlineRegistry.Register(
            outlineId,
            this
        );
    }


    private void OnDisable()
    {
        InteractionOutlineRegistry.Unregister(
            outlineId,
            this
        );

        ClearLineImages();
    }


    private void OnDestroy()
    {
        InteractionOutlineRegistry.Unregister(
            outlineId,
            this
        );

        ClearLineImages();
    }


    private void LateUpdate()
    {
        if (!isVisible)
            return;


        if (playerCamera == null)
        {
            ResolveReferences();

            if (playerCamera == null)
                return;
        }


        Transform cameraTransform =
            playerCamera.transform;


        bool positionChanged =
            (
                cameraTransform.position -
                lastCameraPosition
            ).sqrMagnitude >
            0.0000001f;


        bool rotationChanged =
            Quaternion.Angle(
                cameraTransform.rotation,
                lastCameraRotation
            ) >
            0.001f;


        bool projectionChanged =
            HasProjectionChanged();


        /*
         * Позиция или Rotation камеры реально
         * изменились.
         *
         * Только здесь делаем настоящий
         * Mesh -> Screen -> Convex Hull.
         */
        if (!initializedView ||
            positionChanged ||
            rotationChanged)
        {
            RebuildOutlineGeometry();

            SaveCameraState();

            return;
        }


        /*
         * ВАЖНО:
         *
         * Если изменился только FOV,
         * НЕ перебираем вершины Mesh,
         * НЕ сортируем их,
         * НЕ строим Convex Hull.
         *
         * Просто заново проецируем несколько
         * уже известных точек контура.
         *
         * Поэтому Zoom может обновляться
         * каждый кадр совершенно плавно.
         */
        if (projectionChanged)
        {
            DrawCachedOutline();

            SaveCameraState();
        }


        /*
         * Даже при неподвижной камере
         * препятствие может измениться.
         *
         * Поэтому occlusion проверяется
         * отдельно, но редко.
         */
        if (UpdateOcclusionState(false))
        {
            if (cachedOccluded)
            {
                HideAllLines();
            }
            else
            {
                DrawCachedOutline();
            }
        }
    }


    // =========================================================
    // PUBLIC
    // =========================================================

    public void ShowOutline()
    {
        ResolveReferences();

        isVisible = true;

        /*
         * При первом показе сразу проверяем
         * стену/мебель.
         *
         * Поэтому объект не должен сначала
         * на мгновение просветить через стену.
         */
        lastOcclusionCheck =
            float.NegativeInfinity;

        RebuildOutlineGeometry();

        SaveCameraState();
    }


    public void HideOutline()
    {
        isVisible = false;

        HideAllLines();
    }


    public void ForceRedrawOutline()
    {
        ResolveReferences();

        isVisible = true;

        lastOcclusionCheck =
            float.NegativeInfinity;

        RebuildOutlineGeometry();

        SaveCameraState();
    }


    // =========================================================
    // CAMERA STATE
    // =========================================================

    private bool HasProjectionChanged()
    {
        if (playerCamera == null)
            return false;


        if (playerCamera.orthographic !=
            lastCameraOrthographic)
        {
            return true;
        }


        if (playerCamera.pixelWidth !=
            lastCameraPixelWidth)
        {
            return true;
        }


        if (playerCamera.pixelHeight !=
            lastCameraPixelHeight)
        {
            return true;
        }


        if (playerCamera.orthographic)
        {
            return
                Mathf.Abs(
                    playerCamera.orthographicSize -
                    lastCameraOrthographicSize
                ) >
                0.0001f;
        }


        return
            Mathf.Abs(
                playerCamera.fieldOfView -
                lastCameraFieldOfView
            ) >
            0.0001f;
    }


    private void SaveCameraState()
    {
        if (playerCamera == null)
        {
            initializedView = false;
            return;
        }


        Transform cameraTransform =
            playerCamera.transform;


        lastCameraPosition =
            cameraTransform.position;

        lastCameraRotation =
            cameraTransform.rotation;

        lastCameraFieldOfView =
            playerCamera.fieldOfView;

        lastCameraOrthographicSize =
            playerCamera.orthographicSize;

        lastCameraOrthographic =
            playerCamera.orthographic;

        lastCameraPixelWidth =
            playerCamera.pixelWidth;

        lastCameraPixelHeight =
            playerCamera.pixelHeight;


        initializedView = true;
    }


    // =========================================================
    // FULL OUTLINE REBUILD
    // =========================================================

    private void RebuildOutlineGeometry()
    {
        ResolveReferences();


        if (playerCamera == null ||
            outlineCanvasParent == null)
        {
            HideAllLines();
            return;
        }


        EnsureGeometryCache();


        if (meshFilters == null ||
            meshFilters.Length == 0)
        {
            HideAllLines();
            return;
        }


        cachedScreenPoints.Clear();


        CollectMeshScreenPoints(
            cachedScreenPoints
        );


        if (cachedScreenPoints.Count < 3)
        {
            cachedOutlineWorldPoints.Clear();

            HideAllLines();

            return;
        }


        List<ScreenPoint> hull =
            BuildConvexHull(
                cachedScreenPoints
            );


        if (hull.Count < 3)
        {
            cachedOutlineWorldPoints.Clear();

            HideAllLines();

            return;
        }


        if (simplifySmallOutlines)
        {
            hull =
                SimplifySmallHull(
                    hull
                );
        }


        CacheOutlineWorldPoints(
            hull
        );


        if (cachedOutlineWorldPoints.Count < 3)
        {
            HideAllLines();
            return;
        }


        /*
         * После создания нового контура
         * сразу проверяем перекрытие.
         */
        UpdateOcclusionState(true);


        DrawCachedOutline();
    }


    // =========================================================
    // LAZY GEOMETRY CACHE
    // =========================================================

    private void EnsureGeometryCache()
    {
        if (geometryCacheReady)
            return;


        geometryCacheReady = true;


        meshFilters =
            includeChildren
                ? GetComponentsInChildren
                    <MeshFilter>(true)
                : GetComponents<MeshFilter>();


        renderers =
            includeChildren
                ? GetComponentsInChildren
                    <Renderer>(true)
                : GetComponents<Renderer>();


        if (meshFilters == null)
        {
            cachedVertices =
                new Vector3[0][];

            return;
        }


        cachedVertices =
            new Vector3[
                meshFilters.Length
            ][];


        for (int i = 0;
             i < meshFilters.Length;
             i++)
        {
            MeshFilter meshFilter =
                meshFilters[i];


            if (meshFilter == null)
                continue;


            Mesh mesh =
                meshFilter.sharedMesh;


            if (mesh == null)
                continue;


            /*
             * Копируется только один раз
             * за жизнь этой обводки.
             */
            cachedVertices[i] =
                mesh.vertices;
        }
    }


    // =========================================================
    // COLLECT MESH SCREEN POINTS
    // =========================================================

    private void CollectMeshScreenPoints(
        List<ScreenPoint> screenPoints)
    {
        if (meshFilters == null ||
            cachedVertices == null)
        {
            return;
        }


        for (int i = 0;
             i < meshFilters.Length;
             i++)
        {
            MeshFilter meshFilter =
                meshFilters[i];


            if (meshFilter == null)
                continue;


            Vector3[] vertices =
                cachedVertices[i];


            if (vertices == null)
                continue;


            Transform meshTransform =
                meshFilter.transform;


            for (int v = 0;
                 v < vertices.Length;
                 v++)
            {
                Vector3 worldPoint =
                    meshTransform.TransformPoint(
                        vertices[v]
                    );


                Vector3 screenPoint =
                    playerCamera.WorldToScreenPoint(
                        worldPoint
                    );


                if (screenPoint.z <= 0f)
                    continue;


                screenPoints.Add(
                    new ScreenPoint(
                        new Vector2(
                            screenPoint.x,
                            screenPoint.y
                        ),
                        worldPoint
                    )
                );
            }
        }
    }


    // =========================================================
    // CONVEX HULL
    // =========================================================

    private List<ScreenPoint> BuildConvexHull(
        List<ScreenPoint> points)
    {
        points.Sort(
            CompareScreenPoints
        );


        cachedHull.Clear();


        for (int i = 0;
             i < points.Count;
             i++)
        {
            while (
                cachedHull.Count >= 2 &&
                Cross(
                    cachedHull[
                        cachedHull.Count - 2
                    ].position,
                    cachedHull[
                        cachedHull.Count - 1
                    ].position,
                    points[i].position
                ) <= 0f)
            {
                cachedHull.RemoveAt(
                    cachedHull.Count - 1
                );
            }


            cachedHull.Add(
                points[i]
            );
        }


        int lowerCount =
            cachedHull.Count;


        for (int i = points.Count - 2;
             i >= 0;
             i--)
        {
            while (
                cachedHull.Count >
                    lowerCount &&
                Cross(
                    cachedHull[
                        cachedHull.Count - 2
                    ].position,
                    cachedHull[
                        cachedHull.Count - 1
                    ].position,
                    points[i].position
                ) <= 0f)
            {
                cachedHull.RemoveAt(
                    cachedHull.Count - 1
                );
            }


            cachedHull.Add(
                points[i]
            );
        }


        if (cachedHull.Count > 1)
        {
            cachedHull.RemoveAt(
                cachedHull.Count - 1
            );
        }


        return cachedHull;
    }


    private static int CompareScreenPoints(
        ScreenPoint a,
        ScreenPoint b)
    {
        int compareX =
            a.position.x.CompareTo(
                b.position.x
            );


        if (compareX != 0)
            return compareX;


        return
            a.position.y.CompareTo(
                b.position.y
            );
    }


    private static float Cross(
        Vector2 origin,
        Vector2 a,
        Vector2 b)
    {
        return
            (a.x - origin.x) *
            (b.y - origin.y) -
            (a.y - origin.y) *
            (b.x - origin.x);
    }


    // =========================================================
    // SMALL OUTLINE
    // =========================================================

    private List<ScreenPoint> SimplifySmallHull(
        List<ScreenPoint> hull)
    {
        if (hull == null ||
            hull.Count <= smallOutlinePoints)
        {
            return hull;
        }


        float minX =
            hull[0].position.x;

        float maxX =
            minX;

        float minY =
            hull[0].position.y;

        float maxY =
            minY;


        for (int i = 1;
             i < hull.Count;
             i++)
        {
            Vector2 point =
                hull[i].position;


            minX =
                Mathf.Min(
                    minX,
                    point.x
                );

            maxX =
                Mathf.Max(
                    maxX,
                    point.x
                );

            minY =
                Mathf.Min(
                    minY,
                    point.y
                );

            maxY =
                Mathf.Max(
                    maxY,
                    point.y
                );
        }


        float width =
            maxX - minX;

        float height =
            maxY - minY;


        float maxSize =
            Mathf.Max(
                width,
                height
            );


        /*
         * Большие объекты сохраняют
         * полный оригинальный силуэт.
         */
        if (maxSize >
            smallOutlineMaxSizePixels)
        {
            return hull;
        }


        int targetCount =
            Mathf.Clamp(
                smallOutlinePoints,
                4,
                hull.Count
            );


        return ResampleClosedHull(
            hull,
            targetCount
        );
    }


    private List<ScreenPoint> ResampleClosedHull(
        List<ScreenPoint> hull,
        int targetCount)
    {
        cachedResampledHull.Clear();


        if (hull == null ||
            hull.Count < 3)
        {
            return cachedResampledHull;
        }


        if (targetCount >= hull.Count)
        {
            for (int i = 0;
                 i < hull.Count;
                 i++)
            {
                cachedResampledHull.Add(
                    hull[i]
                );
            }


            return cachedResampledHull;
        }


        cachedEdgeLengths.Clear();


        float perimeter = 0f;


        for (int i = 0;
             i < hull.Count;
             i++)
        {
            Vector2 a =
                hull[i].position;

            Vector2 b =
                hull[
                    (i + 1) %
                    hull.Count
                ].position;


            float length =
                Vector2.Distance(
                    a,
                    b
                );


            cachedEdgeLengths.Add(
                length
            );


            perimeter +=
                length;
        }


        if (perimeter <= 0.001f)
            return cachedResampledHull;


        float spacing =
            perimeter /
            targetCount;


        int edgeIndex = 0;

        float edgeStartDistance = 0f;


        for (int sample = 0;
             sample < targetCount;
             sample++)
        {
            float targetDistance =
                sample *
                spacing;


            while (
                edgeIndex <
                    hull.Count - 1 &&
                edgeStartDistance +
                    cachedEdgeLengths[
                        edgeIndex
                    ] <
                    targetDistance)
            {
                edgeStartDistance +=
                    cachedEdgeLengths[
                        edgeIndex
                    ];

                edgeIndex++;
            }


            ScreenPoint a =
                hull[edgeIndex];

            ScreenPoint b =
                hull[
                    (edgeIndex + 1) %
                    hull.Count
                ];


            float edgeLength =
                cachedEdgeLengths[
                    edgeIndex
                ];


            float t = 0f;


            if (edgeLength > 0.001f)
            {
                t =
                    (
                        targetDistance -
                        edgeStartDistance
                    ) /
                    edgeLength;
            }


            t =
                Mathf.Clamp01(t);


            cachedResampledHull.Add(
                new ScreenPoint(
                    Vector2.Lerp(
                        a.position,
                        b.position,
                        t
                    ),
                    Vector3.Lerp(
                        a.worldPosition,
                        b.worldPosition,
                        t
                    )
                )
            );
        }


        return cachedResampledHull;
    }


    // =========================================================
    // CACHE FINAL OUTLINE
    // =========================================================

    private void CacheOutlineWorldPoints(
        List<ScreenPoint> hull)
    {
        cachedOutlineWorldPoints.Clear();


        if (hull == null)
            return;


        for (int i = 0;
             i < hull.Count;
             i++)
        {
            cachedOutlineWorldPoints.Add(
                hull[i].worldPosition
            );
        }
    }


    // =========================================================
    // FAST PROJECTION
    // =========================================================

    private void DrawCachedOutline()
    {
        if (!isVisible)
            return;


        if (hideWhenOccluded &&
            cachedOccluded)
        {
            HideAllLines();
            return;
        }


        if (playerCamera == null ||
            outlineCanvasParent == null)
        {
            HideAllLines();
            return;
        }


        int count =
            cachedOutlineWorldPoints.Count;


        if (count < 3)
        {
            HideAllLines();
            return;
        }


        cachedProjectedHull.Clear();


        Vector2 center =
            Vector2.zero;


        /*
         * На Zoom здесь обычно работает
         * всего несколько / несколько десятков
         * точек вместо всех вершин Mesh.
         */
        for (int i = 0;
             i < count;
             i++)
        {
            Vector3 screenPoint =
                playerCamera.WorldToScreenPoint(
                    cachedOutlineWorldPoints[i]
                );


            if (screenPoint.z <= 0f)
            {
                HideAllLines();
                return;
            }


            Vector2 point =
                new Vector2(
                    screenPoint.x,
                    screenPoint.y
                );


            cachedProjectedHull.Add(
                point
            );


            center +=
                point;
        }


        center /=
            count;


        cachedPaddedHull.Clear();


        for (int i = 0;
             i < count;
             i++)
        {
            Vector2 point =
                cachedProjectedHull[i];


            Vector2 direction =
                point - center;


            if (direction.sqrMagnitude >
                0.001f)
            {
                point +=
                    direction.normalized *
                    screenPaddingPixels;
            }


            cachedPaddedHull.Add(
                point
            );
        }


        DrawUILines(
            cachedPaddedHull
        );
    }


    // =========================================================
    // OCCLUSION
    // =========================================================

    private bool UpdateOcclusionState(
        bool force)
    {
        bool previousState =
            cachedOccluded;


        if (!hideWhenOccluded)
        {
            cachedOccluded = false;

            return
                previousState !=
                cachedOccluded;
        }


        if (playerCamera == null)
        {
            cachedOccluded = false;

            return
                previousState !=
                cachedOccluded;
        }


        float now =
            Time.unscaledTime;


        if (!force &&
            now - lastOcclusionCheck <
            occlusionInterval)
        {
            return false;
        }


        lastOcclusionCheck =
            now;


        float visibleRatio =
            CalculateVisibleRatio();


        float minimumRatio =
            Mathf.Clamp01(
                minimumVisibleRatio
            );


        /*
         * Когда объект уже спрятан,
         * даём совсем крошечный запас,
         * чтобы контур не мигал туда-сюда
         * на самой границе препятствия.
         */
        if (cachedOccluded)
        {
            float showRatio =
                Mathf.Clamp01(
                    minimumRatio +
                    visibilityHysteresis
                );


            cachedOccluded =
                visibleRatio <
                showRatio;
        }
        else
        {
            /*
             * Например minimumVisibleRatio = 0.20:
             *
             * >= 20% видно -> outline остаётся.
             * < 20% видно  -> outline скрывается.
             */
            cachedOccluded =
                visibleRatio <
                minimumRatio;
        }


        return
            previousState !=
            cachedOccluded;
    }


    private float CalculateVisibleRatio()
    {
        if (renderers == null ||
            renderers.Length == 0)
        {
            return 1f;
        }


        Bounds bounds =
            new Bounds(
                transform.position,
                Vector3.zero
            );


        bool hasBounds =
            false;


        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer currentRenderer =
                renderers[i];


            if (currentRenderer == null)
                continue;


            if (!currentRenderer.enabled)
                continue;


            if (!currentRenderer
                .gameObject
                .activeInHierarchy)
            {
                continue;
            }


            if (!hasBounds)
            {
                bounds =
                    currentRenderer.bounds;

                hasBounds =
                    true;
            }
            else
            {
                bounds.Encapsulate(
                    currentRenderer.bounds
                );
            }
        }


        if (!hasBounds)
            return 1f;


        Vector3 min =
            bounds.min;

        Vector3 max =
            bounds.max;


        /*
         * Центр.
         */
        occlusionPoints[0] =
            bounds.center;


        /*
         * Восемь углов Bounds.
         */
        occlusionPoints[1] =
            new Vector3(
                min.x,
                min.y,
                min.z
            );

        occlusionPoints[2] =
            new Vector3(
                max.x,
                min.y,
                min.z
            );

        occlusionPoints[3] =
            new Vector3(
                min.x,
                max.y,
                min.z
            );

        occlusionPoints[4] =
            new Vector3(
                max.x,
                max.y,
                min.z
            );

        occlusionPoints[5] =
            new Vector3(
                min.x,
                min.y,
                max.z
            );

        occlusionPoints[6] =
            new Vector3(
                max.x,
                min.y,
                max.z
            );

        occlusionPoints[7] =
            new Vector3(
                min.x,
                max.y,
                max.z
            );

        occlusionPoints[8] =
            new Vector3(
                max.x,
                max.y,
                max.z
            );


        Vector3 cameraPosition =
            playerCamera
                .transform.position;


        int testedPoints = 0;
        int visiblePoints = 0;


        for (int i = 0;
             i < occlusionPoints.Length;
             i++)
        {
            Vector3 direction =
                occlusionPoints[i] -
                cameraPosition;


            float distance =
                direction.magnitude;


            if (distance <= 0.01f)
                continue;


            testedPoints++;


            Ray ray =
                new Ray(
                    cameraPosition,
                    direction / distance
                );


            bool blocked =
                false;


            if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                distance,
                occlusionMask,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null)
                {
                    Transform hitTransform =
                        hit.collider.transform;


                    bool hitOwnObject =
                        hitTransform ==
                            transform ||
                        hitTransform
                            .IsChildOf(
                                transform
                            );


                    if (!hitOwnObject)
                    {
                        blocked =
                            true;
                    }
                }
            }


            if (!blocked)
            {
                visiblePoints++;
            }
        }


        if (testedPoints <= 0)
            return 1f;


        return
            (float)visiblePoints /
            testedPoints;
    }


    // =========================================================
    // UI LINES
    // =========================================================

    private void DrawUILines(
        List<Vector2> screenPoints)
    {
        int count =
            screenPoints.Count;


        Camera uiCamera =
            null;


        if (parentCanvas != null &&
            parentCanvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                parentCanvas.worldCamera;
        }


        for (int i = 0;
             i < count;
             i++)
        {
            Vector2 screenA =
                screenPoints[i];

            Vector2 screenB =
                screenPoints[
                    (i + 1) %
                    count
                ];


            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    outlineCanvasParent,
                    screenA,
                    uiCamera,
                    out Vector2 localA
                );


            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    outlineCanvasParent,
                    screenB,
                    uiCamera,
                    out Vector2 localB
                );


            Image line =
                GetLineImage(i);


            if (line == null)
                continue;


            Vector2 direction =
                localB -
                localA;


            float length =
                direction.magnitude;


            if (length <= 0.01f)
            {
                if (line.gameObject.activeSelf)
                {
                    line.gameObject.SetActive(
                        false
                    );
                }

                continue;
            }


            if (!line.gameObject.activeSelf)
            {
                line.gameObject.SetActive(
                    true
                );
            }


            RectTransform rect =
                line.rectTransform;


            rect.anchoredPosition =
                (localA + localB) *
                0.5f;


            rect.sizeDelta =
                new Vector2(
                    length,
                    lineThicknessPixels
                );


            float angle =
                Mathf.Atan2(
                    direction.y,
                    direction.x
                ) *
                Mathf.Rad2Deg;


            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );
        }


        /*
         * Если раньше контур состоял
         * из большего числа линий,
         * лишние просто выключаются.
         */
        for (int i = count;
             i < lineImages.Count;
             i++)
        {
            Image line =
                lineImages[i];


            if (line == null)
                continue;


            if (line.gameObject.activeSelf)
            {
                line.gameObject.SetActive(
                    false
                );
            }
        }
    }


    private Image GetLineImage(
        int index)
    {
        if (outlineCanvasParent == null)
            return null;


        while (
            lineImages.Count <=
            index)
        {
            lineImages.Add(
                null
            );
        }


        if (lineImages[index] == null)
        {
            GameObject lineObject =
                new GameObject(
                    "InteractionOutline_UI_Line"
                );


            lineObject.transform.SetParent(
                outlineCanvasParent,
                false
            );


            Image image =
                lineObject
                    .AddComponent<Image>();


            image.raycastTarget =
                false;


            RectTransform rect =
                image.rectTransform;


            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            ApplyLineVisual(
                image
            );


            lineObject.SetActive(
                false
            );


            lineImages[index] =
                image;
        }


        return lineImages[index];
    }


    private void ApplyLineVisual(
        Image image)
    {
        if (image == null)
            return;


        image.color =
            outlineColor;


        if (lineSprite != null)
        {
            image.sprite =
                lineSprite;

            image.type =
                lineImageType;
        }
        else
        {
            image.sprite =
                null;

            image.type =
                Image.Type.Simple;
        }
    }


    private void HideAllLines()
    {
        for (int i = 0;
             i < lineImages.Count;
             i++)
        {
            Image line =
                lineImages[i];


            if (line == null)
                continue;


            /*
             * Не дёргаем SetActive(false)
             * снова и снова.
             */
            if (line.gameObject.activeSelf)
            {
                line.gameObject.SetActive(
                    false
                );
            }
        }
    }


    private void ClearLineImages()
    {
        for (int i = 0;
             i < lineImages.Count;
             i++)
        {
            if (lineImages[i] != null)
            {
                Destroy(
                    lineImages[i]
                        .gameObject
                );
            }
        }


        lineImages.Clear();
    }


    // =========================================================
    // REFERENCES
    // =========================================================

    private void ResolveReferences()
    {
        if (autoFindReferences)
        {
            if (playerCamera == null)
            {
                Camera mainCamera =
                    Camera.main;


                if (mainCamera != null)
                {
                    playerCamera =
                        mainCamera;
                }
                else if (
                    !string.IsNullOrEmpty(
                        playerCameraObjectName
                    ))
                {
                    GameObject cameraObject =
                        GameObject.Find(
                            playerCameraObjectName
                        );


                    if (cameraObject != null)
                    {
                        playerCamera =
                            cameraObject
                                .GetComponent
                                    <Camera>();
                    }
                }
            }


            if (outlineCanvasParent == null &&
                !string.IsNullOrEmpty(
                    outlineCanvasParentObjectName
                ))
            {
                GameObject canvasObject =
                    GameObject.Find(
                        outlineCanvasParentObjectName
                    );


                if (canvasObject != null)
                {
                    outlineCanvasParent =
                        canvasObject
                            .GetComponent
                                <RectTransform>();
                }
            }
        }


        /*
         * Parent Canvas ищем только тогда,
         * когда сама ссылка реально поменялась.
         */
        if (outlineCanvasParent !=
            currentLineParent)
        {
            ClearLineImages();


            currentLineParent =
                outlineCanvasParent;


            parentCanvas =
                outlineCanvasParent != null
                    ? outlineCanvasParent
                        .GetComponentInParent
                            <Canvas>()
                    : null;
        }
        else if (
            parentCanvas == null &&
            outlineCanvasParent != null)
        {
            parentCanvas =
                outlineCanvasParent
                    .GetComponentInParent
                        <Canvas>();
        }
    }
}
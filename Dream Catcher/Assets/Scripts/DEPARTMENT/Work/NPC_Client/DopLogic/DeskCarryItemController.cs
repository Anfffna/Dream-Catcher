using System.Collections;
using UnityEngine;

public class DeskCarryItemController :
    MonoBehaviour,
    IInteractable
{
    public static bool AnyCarryInteractionActive =>
        carryInteractionOwner != null;

    public static bool AnyItemHeld =>
        carryInteractionOwner != null &&
        carryInteractionOwner.isHeld;


    [Header("Взаимодействие")]

    [Tooltip("Collider самого предмета.")]
    [SerializeField]
    private Collider interactionCollider;

    [Tooltip("Камера игрока.")]
    [SerializeField]
    private Camera playerCamera;

    [Tooltip(
        "Collider поверхности стола, " +
        "по которой предмет следует за мышью."
    )]
    [SerializeField]
    private Collider placementSurface;


    [Header("Начальное состояние")]

    [Tooltip(
        "Скрывать предмет до момента, " +
        "когда клиент реально начинает его отдавать."
    )]
    [SerializeField]
    private bool hideUntilClientGivesItem =
        true;


    [Header("Блокировка рабочего UI")]

    [Tooltip(
        "Прозрачный UI-блокировщик интерфейса компьютера. " +
        "Включается только пока предмет переносится."
    )]
    [SerializeField]
    private GameObject workUIInputBlocker;


    [Header("Слои")]

    [SerializeField]
    private string defaultLayerName =
        "Default";

    [SerializeField]
    private string interactableLayerName =
        "Interactable";


    [Header("Размер при переносе")]

    [Tooltip(
        "Во сколько раз предмет становится крупнее, " +
        "когда игрок берёт его курсором."
    )]
    [SerializeField]
    private float heldScaleMultiplier =
        1.25f;

    [Tooltip(
        "Длительность плавного увеличения " +
        "при взятии."
    )]
    [SerializeField]
    private float pickupScaleDuration =
        0.2f;

    [Tooltip(
        "Длительность плавного уменьшения " +
        "до обычного размера при укладке."
    )]
    [SerializeField]
    private float placeScaleDuration =
        0.2f;


    [Header("Поворот")]

    [Tooltip(
        "Поворот предмета, пока он находится " +
        "под курсором."
    )]
    [SerializeField]
    private Vector3 heldEulerAngles;

    [Tooltip(
        "Поворот предмета после укладки на стол."
    )]
    [SerializeField]
    private Vector3 placedEulerAngles;

    [Tooltip(
        "Считать эти углы относительно " +
        "поворота поверхности стола."
    )]
    [SerializeField]
    private bool rotationRelativeToSurface =
        true;


    [Header("Поверхность стола")]

    [Tooltip(
        "Небольшой отступ от поверхности, " +
        "чтобы модель не проваливалась в стол."
    )]
    [SerializeField]
    private float surfaceOffset =
        0.005f;

    [Tooltip(
        "Максимальная дистанция луча " +
        "до поверхности стола."
    )]
    [SerializeField]
    private float placementRayDistance =
        20f;


    [Header("Столкновения с предметами")]

    [Tooltip(
        "Не позволять переносимому предмету " +
        "проходить сквозь другие Collider."
    )]
    [SerializeField]
    private bool preventObstacleOverlap =
        true;

    [Tooltip(
        "Какие слои считаются препятствиями. " +
        "Лучше указать слои предметов на столе."
    )]
    [SerializeField]
    private LayerMask obstacleLayers =
        ~0;

    [Tooltip(
        "Радиус защитной области вокруг предмета. " +
        "Подбирается под размер конкретного предмета."
    )]
    [SerializeField]
    private float obstacleRadius =
        0.04f;

    [Tooltip(
        "Дополнительный зазор между предметами."
    )]
    [SerializeField]
    private float obstaclePadding =
        0.005f;


    [Header("Очередь клиентов")]

    [Tooltip(
        "Не запускать следующего клиента, " +
        "пока подаренный предмет впервые " +
        "не будет положен на стол."
    )]
    [SerializeField]
    private bool blockNextVisitorUntilPlaced =
        true;


    private static DeskCarryItemController
        carryInteractionOwner;


    private Renderer[] itemRenderers;
    private Collider[] itemColliders;

    private Coroutine scaleCoroutine;

    private bool revealed;
    private bool transitionInProgress;
    private bool isHeld;
    private bool placed;

    private bool queueBlocked;


    private Vector3 normalLocalScale;

    private bool hasValidPlacement;

    private Vector3 currentPlacementPosition;

    private Quaternion currentHeldRotation;
    private Quaternion currentPlacedRotation;


    private readonly RaycastHit[]
        obstacleHitBuffer =
            new RaycastHit[16];

    private readonly Collider[]
        obstacleOverlapBuffer =
            new Collider[16];


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        FindReferences();
        CacheItemParts();


        normalLocalScale =
            transform.localScale;


        revealed =
            !hideUntilClientGivesItem;

        isHeld = false;
        placed = false;
        transitionInProgress = false;


        if (hideUntilClientGivesItem)
        {
            SetRenderersEnabled(false);
            SetItemCollidersEnabled(false);
        }
        else
        {
            SetRenderersEnabled(true);
            SetInteractionAvailable(true);
        }


        if (workUIInputBlocker != null)
        {
            workUIInputBlocker
                .SetActive(false);
        }
    }


    private void Update()
    {
        if (!isHeld ||
            transitionInProgress)
        {
            return;
        }


        bool pauseBlocks =
            PauseManager.Instance != null &&
            PauseManager.Instance.IsPaused;


        bool taskPanelBlocks =
            TaskPanelController.Instance != null &&
            TaskPanelController.Instance
                .BlocksWorldInteraction;


        bool loadingBlocks =
            LoadingManager
                .IsLoadingScreenBlockingPause();


        if (pauseBlocks ||
            taskPanelBlocks ||
            loadingBlocks)
        {
            return;
        }


        if (Input.GetMouseButtonDown(0) &&
            hasValidPlacement)
        {
            StartPlace();
        }
    }


    private void LateUpdate()
    {
        if (!isHeld)
            return;

        UpdateHeldPosition();
    }


    // =====================================================
    // ПОЯВЛЕНИЕ В РУКЕ NPC
    // =====================================================

    public void ShowInHand()
    {
        /*
         * Поддерживаем и вариант,
         * когда сам GameObject был выключен.
         */
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }


        FindReferences();
        CacheItemParts();


        revealed = true;
        placed = false;
        isHeld = false;
        transitionInProgress = false;


        SetRenderersEnabled(true);

        /*
         * Пока предмет находится в руке NPC,
         * он видим, но игрок его ещё
         * забрать не может.
         */
        SetItemCollidersEnabled(false);
    }


    // =====================================================
    // ОТКРЕПЛЕНИЕ ОТ АНИМАЦИИ
    // =====================================================

    public void ReleaseFromAnimation(
        Transform releasedItemsRoot,
        Transform presentationPoint)
    {
        if (!revealed)
            return;


        /*
         * Сохраняем мировую позицию
         * при отвязке от кости руки.
         */
        if (releasedItemsRoot != null)
        {
            transform.SetParent(
                releasedItemsRoot,
                true
            );
        }


        /*
         * Если точка задана —
         * переносим предмет туда.
         *
         * Если None —
         * оставляем ровно в том месте,
         * где находилась рука.
         */
        if (presentationPoint != null)
        {
            transform.SetPositionAndRotation(
                presentationPoint.position,
                presentationPoint.rotation
            );
        }


        /*
         * После смены Parent запоминаем
         * нормальный Scale уже
         * в рабочей системе координат.
         */
        normalLocalScale =
            transform.localScale;


        placed = true;


        BlockQueueIfNeeded();

        SetInteractionAvailable(true);
    }


    // =====================================================
    // ВЗЯТИЕ
    // =====================================================

    public void Interact()
    {
        if (!revealed ||
            transitionInProgress ||
            isHeld)
        {
            return;
        }


        if (carryInteractionOwner != null &&
            carryInteractionOwner != this)
        {
            return;
        }


        StartPickup();
    }


    private void StartPickup()
    {
        if (!AcquireCarryLock())
            return;


        if (scaleCoroutine != null)
        {
            StopCoroutine(
                scaleCoroutine
            );

            scaleCoroutine = null;
        }


        transitionInProgress = true;
        placed = false;

        SetInteractionAvailable(false);


        /*
         * Сразу передаём предмет курсору.
         * Во время увеличения он уже
         * следует за мышью.
         */
        isHeld = true;

        UpdateHeldPosition();


        Vector3 heldScale =
            normalLocalScale *
            heldScaleMultiplier;


        scaleCoroutine =
            StartCoroutine(
                PickupScaleRoutine(
                    transform.localScale,
                    heldScale
                )
            );
    }


    private IEnumerator PickupScaleRoutine(
        Vector3 startScale,
        Vector3 heldScale)
    {
        yield return AnimateScale(
            startScale,
            heldScale,
            pickupScaleDuration
        );


        transitionInProgress = false;
        scaleCoroutine = null;
    }


    // =====================================================
    // СЛЕДОВАНИЕ ЗА МЫШЬЮ
    // =====================================================

    private void UpdateHeldPosition()
    {
        if (playerCamera == null ||
            placementSurface == null)
        {
            hasValidPlacement = false;
            return;
        }


        Ray ray =
            playerCamera.ScreenPointToRay(
                Input.mousePosition
            );


        // =====================================================
        // ПЛОСКОСТЬ СТОЛА
        // =====================================================

        Vector3 surfaceNormal =
            placementSurface
                .transform.up;


        Bounds surfaceBounds =
            placementSurface.bounds;


        Vector3 absoluteNormal =
            new Vector3(
                Mathf.Abs(surfaceNormal.x),
                Mathf.Abs(surfaceNormal.y),
                Mathf.Abs(surfaceNormal.z)
            );


        float surfaceExtent =
            Vector3.Dot(
                absoluteNormal,
                surfaceBounds.extents
            );


        Vector3 surfacePoint =
            surfaceBounds.center +
            surfaceNormal *
            surfaceExtent;


        Plane tablePlane =
            new Plane(
                surfaceNormal,
                surfacePoint
            );


        if (!tablePlane.Raycast(
                ray,
                out float enter))
        {
            hasValidPlacement = false;
            return;
        }


        Vector3 desiredPosition =
            ray.GetPoint(enter) +
            surfaceNormal *
            surfaceOffset;


        // =====================================================
        // ПРОВЕРЯЕМ, ЧТО МЫШЬ НАД САМИМ СТОЛОМ
        // =====================================================

        Vector3 closestPoint =
            placementSurface
                .ClosestPoint(
                    desiredPosition
                );


        float distanceFromSurface =
            Vector3.Distance(
                closestPoint,
                desiredPosition
            );


        /*
         * Пока курсор реально находится
         * над Collider стола, расстояние
         * будет очень маленьким.
         */
        bool pointerOverTable =
            distanceFromSurface <=
            Mathf.Max(
                0.03f,
                surfaceOffset + 0.02f
            );


        // =====================================================
        // ROTATION
        // =====================================================

        Quaternion surfaceRotation =
            rotationRelativeToSurface
                ? placementSurface
                    .transform.rotation
                : Quaternion.identity;


        currentHeldRotation =
            surfaceRotation *
            Quaternion.Euler(
                heldEulerAngles
            );


        currentPlacedRotation =
            surfaceRotation *
            Quaternion.Euler(
                placedEulerAngles
            );


        // =====================================================
        // ПРЕПЯТСТВИЯ
        // =====================================================

        Vector3 resolvedPosition =
            desiredPosition;


        if (preventObstacleOverlap &&
            obstacleRadius > 0f)
        {
            resolvedPosition =
                ResolveObstacleMovement(
                    transform.position,
                    desiredPosition
                );
        }


        currentPlacementPosition =
            resolvedPosition;


        transform.SetPositionAndRotation(
            currentPlacementPosition,
            currentHeldRotation
        );


        /*
         * Следовать за мышью он будет всегда,
         * но положить разрешаем только:
         *
         * 1. над столом;
         * 2. не внутри другого объекта.
         */
        hasValidPlacement =
            pointerOverTable &&
            !IsPositionBlocked(
                currentPlacementPosition
            );
    }


    // =====================================================
    // СТОЛКНОВЕНИЯ
    // =====================================================

    private Vector3 ResolveObstacleMovement(
    Vector3 currentPosition,
    Vector3 targetPosition)
    {
        Vector3 delta =
            targetPosition -
            currentPosition;


        float distance =
            delta.magnitude;


        if (distance <= 0.0001f)
        {
            return currentPosition;
        }


        /*
         * Если по какой-то причине предмет уже
         * оказался внутри защитной области
         * другого Collider, не цементируем его
         * навечно.
         *
         * Разрешаем ему выбраться в свободную
         * позицию под курсором.
         */
        if (IsPositionBlocked(
                currentPosition))
        {
            if (!IsPositionBlocked(
                    targetPosition))
            {
                return targetPosition;
            }

            return currentPosition;
        }


        Vector3 direction =
            delta / distance;


        int hitCount =
            Physics.SphereCastNonAlloc(
                currentPosition,
                obstacleRadius,
                direction,
                obstacleHitBuffer,
                distance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore
            );


        float nearestDistance =
            float.MaxValue;

        bool obstacleFound =
            false;


        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider hitCollider =
                obstacleHitBuffer[i]
                    .collider;


            if (ShouldIgnoreObstacle(
                    hitCollider))
            {
                continue;
            }


            float hitDistance =
                obstacleHitBuffer[i]
                    .distance;


            /*
             * Нулевые столкновения возникают,
             * когда SphereCast уже касается
             * Collider в своей начальной точке.
             *
             * Их нельзя использовать как
             * ограничение движения — иначе
             * предмет застывает навсегда.
             */
            if (hitDistance <= 0.001f)
            {
                continue;
            }


            if (hitDistance <
                nearestDistance)
            {
                nearestDistance =
                    hitDistance;

                obstacleFound =
                    true;
            }
        }


        if (obstacleFound)
        {
            float allowedDistance =
                Mathf.Max(
                    0f,
                    nearestDistance -
                    obstaclePadding
                );


            return
                currentPosition +
                direction *
                    allowedDistance;
        }


        /*
         * Путь свободен, но дополнительно
         * убеждаемся, что сама конечная
         * позиция не находится внутри
         * какого-нибудь предмета.
         */
        if (IsPositionBlocked(
                targetPosition))
        {
            return currentPosition;
        }


        return targetPosition;
    }


    private bool IsPositionBlocked(
        Vector3 position)
    {
        if (!preventObstacleOverlap ||
            obstacleRadius <= 0f)
        {
            return false;
        }


        int count =
            Physics.OverlapSphereNonAlloc(
                position,
                obstacleRadius,
                obstacleOverlapBuffer,
                obstacleLayers,
                QueryTriggerInteraction.Ignore
            );


        for (int i = 0;
             i < count;
             i++)
        {
            Collider obstacle =
                obstacleOverlapBuffer[i];


            if (ShouldIgnoreObstacle(
                    obstacle))
            {
                continue;
            }


            return true;
        }


        return false;
    }


    private bool ShouldIgnoreObstacle(
        Collider obstacle)
    {
        if (obstacle == null)
            return true;


        /*
         * Сама поверхность стола
         * препятствием не является.
         */
        if (obstacle ==
            placementSurface)
        {
            return true;
        }


        /*
         * Собственные Collider предмета
         * тоже игнорируем.
         */
        if (obstacle.transform ==
                transform ||
            obstacle.transform
                .IsChildOf(transform))
        {
            return true;
        }


        return false;
    }


    // =====================================================
    // УКЛАДКА
    // =====================================================

    private void StartPlace()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(
                scaleCoroutine
            );

            scaleCoroutine = null;
        }


        transitionInProgress = true;
        isHeld = false;


        transform.SetPositionAndRotation(
            currentPlacementPosition,
            currentPlacedRotation
        );


        scaleCoroutine =
            StartCoroutine(
                PlaceScaleRoutine()
            );
    }


    private IEnumerator PlaceScaleRoutine()
    {
        Vector3 startScale =
            transform.localScale;


        /*
         * Плавно уменьшаем
         * с Held Scale до обычного.
         */
        yield return AnimateScale(
            startScale,
            normalLocalScale,
            placeScaleDuration
        );


        transform.localScale =
            normalLocalScale;


        placed = true;
        hasValidPlacement = false;

        transitionInProgress = false;
        scaleCoroutine = null;


        /*
         * ВАЖНО:
         * после укладки предмет снова
         * становится Interactable,
         * поэтому его можно переложить
         * хоть десять раз.
         */
        SetInteractionAvailable(true);


        ReleaseCarryLock();

        /*
         * Очередь ждёт только
         * ПЕРВУЮ успешную укладку.
         */
        ReleaseQueueBlock();
    }


    // =====================================================
    // БЛОКИРОВКА ДРУГИХ ВЗАИМОДЕЙСТВИЙ
    // =====================================================

    private bool AcquireCarryLock()
    {
        if (carryInteractionOwner != null &&
            carryInteractionOwner != this)
        {
            return false;
        }


        carryInteractionOwner =
            this;


        if (workUIInputBlocker != null)
        {
            workUIInputBlocker
                .SetActive(true);
        }


        return true;
    }


    private void ReleaseCarryLock()
    {
        if (carryInteractionOwner == this)
        {
            carryInteractionOwner =
                null;
        }


        if (workUIInputBlocker != null)
        {
            workUIInputBlocker
                .SetActive(false);
        }
    }


    // =====================================================
    // ОЧЕРЕДЬ
    // =====================================================

    private void BlockQueueIfNeeded()
    {
        if (!blockNextVisitorUntilPlaced ||
            queueBlocked)
        {
            return;
        }


        if (VisitorQueueManager.Instance ==
            null)
        {
            return;
        }


        VisitorQueueManager.Instance
            .BlockNextVisitor(
                this
            );


        queueBlocked = true;
    }


    private void ReleaseQueueBlock()
    {
        if (!queueBlocked)
            return;


        if (VisitorQueueManager.Instance !=
            null)
        {
            VisitorQueueManager.Instance
                .ReleaseNextVisitor(
                    this
                );
        }


        queueBlocked = false;
    }


    // =====================================================
    // SCALE
    // =====================================================

    private IEnumerator AnimateScale(
        Vector3 from,
        Vector3 to,
        float duration)
    {
        if (duration <= 0f)
        {
            transform.localScale =
                to;

            yield break;
        }


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            transform.localScale =
                Vector3.Lerp(
                    from,
                    to,
                    smoothT
                );


            yield return null;
        }


        transform.localScale =
            to;
    }


    // =====================================================
    // VISUAL / COLLIDERS
    // =====================================================

    private void CacheItemParts()
    {
        itemRenderers =
            GetComponentsInChildren<Renderer>(
                true
            );


        itemColliders =
            GetComponentsInChildren<Collider>(
                true
            );
    }


    private void SetRenderersEnabled(
        bool enabledState)
    {
        if (itemRenderers == null)
            return;


        for (int i = 0;
             i < itemRenderers.Length;
             i++)
        {
            if (itemRenderers[i] != null)
            {
                itemRenderers[i].enabled =
                    enabledState;
            }
        }
    }


    private void SetItemCollidersEnabled(
        bool enabledState)
    {
        if (itemColliders == null)
            return;


        for (int i = 0;
             i < itemColliders.Length;
             i++)
        {
            if (itemColliders[i] != null)
            {
                itemColliders[i].enabled =
                    enabledState;
            }
        }
    }


    private void SetInteractionAvailable(
        bool available)
    {
        if (interactionCollider == null)
            return;


        /*
         * Пока предмет не интерактивен,
         * его Collider вообще не нужен.
         */
        SetItemCollidersEnabled(
            available
        );


        string layerName =
            available
                ? interactableLayerName
                : defaultLayerName;


        int layer =
            LayerMask.NameToLayer(
                layerName
            );


        if (layer < 0)
            return;


        interactionCollider
            .gameObject.layer =
            layer;
    }


    // =====================================================
    // REFERENCES
    // =====================================================

    private void FindReferences()
    {
        if (interactionCollider == null)
        {
            interactionCollider =
                GetComponentInChildren
                    <Collider>(
                        true
                    );
        }


        if (playerCamera == null)
        {
            playerCamera =
                Camera.main;
        }
    }


    // =====================================================
    // CLEANUP
    // =====================================================

    private void OnDisable()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(
                scaleCoroutine
            );

            scaleCoroutine = null;
        }


        isHeld = false;
        transitionInProgress = false;


        ReleaseCarryLock();
        ReleaseQueueBlock();
    }


    private void OnValidate()
    {
        heldScaleMultiplier =
            Mathf.Max(
                0.01f,
                heldScaleMultiplier
            );


        pickupScaleDuration =
            Mathf.Max(
                0f,
                pickupScaleDuration
            );


        placeScaleDuration =
            Mathf.Max(
                0f,
                placeScaleDuration
            );


        placementRayDistance =
            Mathf.Max(
                0f,
                placementRayDistance
            );


        obstacleRadius =
            Mathf.Max(
                0f,
                obstacleRadius
            );


        obstaclePadding =
            Mathf.Max(
                0f,
                obstaclePadding
            );
    }
}
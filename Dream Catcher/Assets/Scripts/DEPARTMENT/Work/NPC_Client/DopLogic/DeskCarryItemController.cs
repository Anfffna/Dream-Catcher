using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Подсказка при первом взятии")]

    [Tooltip(
    "Контроллер UI-плашки для подсказки жетона."
    )]
    [SerializeField]
    private ClickInteractionHint pickupHint;


    [Tooltip(
        "Первый текст. Показывается сразу " +
        "после взятия предмета."
    )]
    [TextArea(2, 5)]
    [SerializeField]
    private string pickupHintScrollText =
        "Покрутите колесо мыши, чтобы приблизить или отдалить предмет.";


    [Tooltip(
        "Второй текст. Показывается после " +
        "использования колеса мыши."
    )]
    [TextArea(2, 5)]
    [SerializeField]
    private string pickupHintPlaceText =
        "ЛКМ — положить предмет.";


    [Tooltip(
        "Через сколько секунд после использования " +
        "колеса заменить первый текст на второй."
    )]
    [SerializeField]
    private float pickupHintSecondTextDelay =
        0.5f;


    [Tooltip(
        "Показывать обучение только " +
        "при первом взятии этого предмета."
    )]
    [SerializeField]
    private bool showPickupHintOnlyOnce =
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

    [Header("Расстояние от камеры")]

    [Tooltip(
    "Расстояние от камеры, на котором " +
    "предмет висит под курсором."
    )]
    [SerializeField]
    private float heldDistanceFromCamera =
    0.8f;

    [Tooltip("Минимально допустимое расстояние.")]
    [SerializeField]
    private float minHeldDistance =
        0.35f;

    [Tooltip("Максимально допустимое расстояние.")]
    [SerializeField]
    private float maxHeldDistance =
        1.5f;

    [Tooltip(
        "Скорость приближения и отдаления " +
        "предмета колесом мыши."
    )]
    [SerializeField]
    private float heldDistanceScrollSpeed =
        0.1f;

    [Tooltip(
    "Скорость плавного движения предмета " +
    "к выбранному колесом расстоянию."
    )]
    [SerializeField]
    private float heldDistanceSmoothSpeed =
    8f;

    [Header("Плавный поворот")]

    [Tooltip(
        "За сколько секунд предмет плавно " +
        "поворачивается при взятии и укладке."
    )]
    [SerializeField]
    private float rotationTransitionDuration =
        0.2f;

    [Header("Коллизия во время переноса")]

    [Tooltip(
    "Не позволять предмету проходить сквозь " +
    "стол и препятствия во время переноса."
    )]
    [SerializeField]
    private bool preventHeldClipping =
    true;

    [Tooltip(
        "Радиус предмета при проверке препятствий " +
        "во время переноса."
    )]
    [SerializeField]
    private float heldCollisionRadius =
        0.025f;

    [Tooltip(
        "Небольшой зазор перед препятствием."
    )]
    [SerializeField]
    private float heldCollisionPadding =
        0.005f;

    [Header("Покачивание при движении мыши")]

    [Tooltip(
    "Максимальный наклон предмета в плоскости экрана " +
    "при движении мыши. " +
    "Предмет при этом продолжает смотреть лицом к камере."
    )]
    [FormerlySerializedAs("heldYawSwayAngle")]
    [SerializeField]
    private float heldRollSwayAngle =
    3f;

    [Tooltip(
        "Скорость плавного наклона " +
        "и возвращения в обычное положение."
    )]
    [FormerlySerializedAs("heldYawSwaySpeed")]
    [SerializeField]
    private float heldRollSwaySpeed =
        12f;

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

    [Header("Блокировка укладки за объектами")]

    [Tooltip(
    "Не позволять класть предмет на участок стола, " +
    "если между камерой и этим участком находится другой Collider."
    )]
    [SerializeField]
    private bool preventPlacementBehindObjects =
    true;


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

    private bool pickupHintShown;
    private float currentHeldRollSway;
    private float targetHeldDistance;
    private float currentHeldDistance;

    private bool pickupHintSequenceActive;
    private bool pickupHintScrollDetected;
    private bool pickupHintPlaceStage;

    private float pickupHintScrollTimer;

    private bool queueBlocked;
    private int pickedUpFrame = -1;


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

        float safeMinDistance =
            GetSafeMinHeldDistance();


        heldDistanceFromCamera =
            Mathf.Clamp(
                heldDistanceFromCamera,
                safeMinDistance,
                maxHeldDistance
            );


        targetHeldDistance =
            heldDistanceFromCamera;

        currentHeldDistance =
            heldDistanceFromCamera;

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

    private float GetSafeMinHeldDistance()
    {
        float safeMin =
            minHeldDistance;


        if (playerCamera != null)
        {
            /*
             * Жетон не должен оказаться
             * перед Near Clip Plane камеры.
             *
             * Добавляем радиус самого предмета,
             * чтобы в камеру не вошёл его край.
             */
            float cameraSafeMin =
                playerCamera.nearClipPlane +
                heldCollisionRadius +
                heldCollisionPadding;


            safeMin =
                Mathf.Max(
                    safeMin,
                    cameraSafeMin
                );
        }


        return safeMin;
    }

    private void Update()
    {
        if (!isHeld)
            return;


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


        // =====================================================
        // КОЛЕСО МЫШИ
        // =====================================================

        float scroll =
            Input.mouseScrollDelta.y;


        if (Mathf.Abs(scroll) > 0.01f)
        {
            float safeMinDistance =
                GetSafeMinHeldDistance();


            targetHeldDistance =
                Mathf.Clamp(
                    targetHeldDistance +
                    scroll *
                    heldDistanceScrollSpeed,
                    safeMinDistance,
                    maxHeldDistance
                );


            /*
             * Это поле оставляем синхронным,
             * чтобы в Inspector было понятно,
             * какое расстояние выбрано.
             */
            heldDistanceFromCamera =
                targetHeldDistance;
        }

        // =====================================================
        // ОБУЧЕНИЕ: КОЛЕСО → ЛКМ
        // =====================================================

        if (pickupHintSequenceActive &&
            !pickupHintPlaceStage)
        {
            /*
             * Первый реальный поворот колеса
             * запускает таймер смены текста.
             */
            if (!pickupHintScrollDetected &&
                Mathf.Abs(scroll) > 0.01f)
            {
                pickupHintScrollDetected = true;
                pickupHintScrollTimer = 0f;
            }


            if (pickupHintScrollDetected)
            {
                pickupHintScrollTimer +=
                    Time.deltaTime;


                if (pickupHintScrollTimer >=
                    pickupHintSecondTextDelay)
                {
                    pickupHintPlaceStage = true;


                    if (pickupHint != null)
                    {
                        pickupHint.Show(
                            pickupHintPlaceText,
                            false
                        );
                    }
                }
            }
        }


        // =====================================================
        // УКЛАДКА
        // =====================================================

        /*
         * Не позволяем клику,
         * которым предмет только что взяли,
         * одновременно его положить.
         */
        if (Time.frameCount ==
            pickedUpFrame)
        {
            return;
        }

        /*
         * Пока игрок ещё не выполнил
         * первый шаг обучения с колесом,
         * ЛКМ предмет не кладёт.
         */
        if (pickupHintSequenceActive &&
            !pickupHintPlaceStage)
        {
            return;
        }

        if (!transitionInProgress &&
            Input.GetMouseButtonDown(0))
        {
            TryPlaceAtMousePosition();
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

        Quaternion surfaceRotation =
            rotationRelativeToSurface &&
            placementSurface != null
                ? placementSurface.transform.rotation
                : Quaternion.identity;

        Quaternion releaseRotation =
            surfaceRotation *
            Quaternion.Euler(
                placedEulerAngles
            );

        transform.rotation =
            releaseRotation;


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
        currentHeldRollSway = 0f;

        if (pickupHint != null &&
            (!showPickupHintOnlyOnce ||
             !pickupHintShown))
        {
            pickupHintShown = true;

            pickupHintSequenceActive = true;
            pickupHintScrollDetected = false;
            pickupHintPlaceStage = false;
            pickupHintScrollTimer = 0f;


            pickupHint.Show(
                 pickupHintScrollText,
                 false
             );
        }
        else
        {
            pickupHintSequenceActive = false;
        }

        float safeMinDistance =
            GetSafeMinHeldDistance();


        targetHeldDistance =
            Mathf.Clamp(
                heldDistanceFromCamera,
                safeMinDistance,
                maxHeldDistance
            );

        currentHeldDistance =
            targetHeldDistance;


        pickedUpFrame =
            Time.frameCount;

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
        if (playerCamera == null)
        {
            playerCamera =
                Camera.main;
        }


        if (playerCamera == null)
            return;


        Ray ray =
            playerCamera.ScreenPointToRay(
                Input.mousePosition
            );


        // =====================================================
        // ПОЗИЦИЯ
        // =====================================================

        /*
         * Вот здесь Held Distance реально
         * определяет положение предмета.
         *
         * Поэтому колесо физически двигает
         * предмет вдоль луча:
         * ближе / дальше от камеры.
         */
        /*
 * Сначала узнаём реальную безопасную
 * конечную дистанцию.
 *
 * Если впереди стол или предмет,
 * цель сразу ограничивается его поверхностью.
 */
        float safeTargetDistance =
            ResolveHeldDistance(
                ray,
                targetHeldDistance
            );


        float distanceT =
            1f -
            Mathf.Exp(
                -heldDistanceSmoothSpeed *
                Time.deltaTime
            );


        float nextDistance =
            Mathf.Lerp(
                currentHeldDistance,
                safeTargetDistance,
                distanceT
            );


        /*
         * Если игрок просто крутит колесо —
         * приближение и отдаление плавные.
         *
         * Но если под жетоном ВНЕЗАПНО
         * появился более близкий Collider
         * (например курсор резко перевели
         * на лежащий предмет), нельзя несколько
         * кадров ехать сквозь него.
         */
        
        currentHeldDistance =
            nextDistance;


        Vector3 heldPosition =
            ray.GetPoint(
                currentHeldDistance
            );


        transform.position =
            heldPosition;


        // =====================================================
        // ПОВОРОТ В РУКЕ
        // =====================================================

        /*
         * Основной поворот предмета всегда
         * считается относительно камеры.
         *
         * Поэтому лицевая сторона предмета
         * остаётся направленной примерно
         * одинаково относительно игрока.
         */
        Quaternion baseRotation =
            playerCamera.transform.rotation *
            Quaternion.Euler(
                heldEulerAngles
            );


        // =====================================================
        // ЛЁГКОЕ ПОКАЧИВАНИЕ
        // =====================================================

        /*
         * ВАЖНО:
         *
         * Раньше движение мыши добавляло
         * поворот по локальной Y предмета.
         *
         * Для плоского жетона это означало,
         * что он начинал показывать бок.
         *
         * Теперь мы вращаем его вокруг
         * направления взгляда камеры.
         *
         * Получается небольшой красивый
         * наклон в плоскости экрана,
         * но лицевая сторона остаётся
         * направлена на игрока.
         */
        float mouseX =
            Input.GetAxisRaw(
                "Mouse X"
            );


        float targetRollSway =
            Mathf.Clamp(
                mouseX,
                -1f,
                1f
            ) *
            heldRollSwayAngle;


        float swayT =
            1f -
            Mathf.Exp(
                -heldRollSwaySpeed *
                Time.deltaTime
            );


        currentHeldRollSway =
            Mathf.Lerp(
                currentHeldRollSway,
                targetRollSway,
                swayT
            );


        /*
         * Вращаем именно вокруг Forward камеры.
         *
         * Благодаря этому левое и правое
         * покачивание симметричны и не зависят
         * от локальных осей самой модели.
         */
        Quaternion swayRotation =
            Quaternion.AngleAxis(
                currentHeldRollSway,
                playerCamera.transform.forward
            );


        Quaternion targetRotation =
            swayRotation *
            baseRotation;


        // =====================================================
        // ПЛАВНОСТЬ ПОВОРОТА
        // =====================================================

        if (rotationTransitionDuration <= 0f)
        {
            transform.rotation =
                targetRotation;

            return;
        }


        float rotationT =
            1f -
            Mathf.Exp(
                -Time.deltaTime /
                rotationTransitionDuration
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationT
            );
    }

    private bool IsPlacementPathBlocked(
    Ray ray,
    float tableDistance)
    {
        if (!preventPlacementBehindObjects)
        {
            return false;
        }


        /*
         * Проверка выполняется только в момент
         * попытки положить предмет.
         *
         * Ищем Collider между камерой
         * и найденной точкой стола.
         */
        int hitCount =
            Physics.RaycastNonAlloc(
                ray,
                obstacleHitBuffer,
                tableDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore
            );


        for (int i = 0;
             i < hitCount;
             i++)
        {
            RaycastHit obstacleHit =
                obstacleHitBuffer[i];


            Collider obstacle =
                obstacleHit.collider;


            if (obstacle == null)
                continue;


            /*
             * Используем уже существующую
             * проверку:
             *
             * - собственный Collider жетона игнорируется;
             * - сам placementSurface игнорируется.
             */
            if (ShouldIgnoreObstacle(
                    obstacle))
            {
                continue;
            }


            /*
             * Нулевые попадания около
             * начала луча не учитываем.
             */
            if (obstacleHit.distance <= 0.001f)
            {
                continue;
            }


            /*
             * Нашли реальный объект
             * перед поверхностью стола.
             */
            return true;
        }


        return false;
    }

    private void TryPlaceAtMousePosition()
    {
        if (playerCamera == null ||
            placementSurface == null)
        {
            return;
        }


        Ray ray =
            playerCamera.ScreenPointToRay(
                Input.mousePosition
            );


        if (!placementSurface.Raycast(
        ray,
        out RaycastHit hit,
        placementRayDistance))
        {
            return;
        }


        /*
         * Стол технически есть под курсором,
         * но между камерой и ним может находиться
         * клавиатура, монитор, папка и т.д.
         *
         * В таком случае класть предмет
         * "за объект" запрещаем.
         */
        if (IsPlacementPathBlocked(
                ray,
                hit.distance))
        {
            return;
        }


        Vector3 targetPosition =
            hit.point +
            hit.normal *
            surfaceOffset;


        Quaternion surfaceRotation =
            rotationRelativeToSurface
                ? placementSurface
                    .transform.rotation
                : Quaternion.identity;


        Quaternion targetRotation =
            surfaceRotation *
            Quaternion.Euler(
                placedEulerAngles
            );


        // =====================================================
        // НЕЛЬЗЯ ПОЛОЖИТЬ ВНУТРИ ДРУГОГО ПРЕДМЕТА
        // =====================================================

        if (preventObstacleOverlap &&
            IsPositionBlocked(
                targetPosition))
        {
            return;
        }


        StartPlace(
            targetPosition,
            targetRotation
        );
    }

    private float ResolveHeldDistance(
    Ray ray,
    float desiredDistance)
    {
        float safeMinDistance =
            GetSafeMinHeldDistance();


        desiredDistance =
            Mathf.Clamp(
                desiredDistance,
                safeMinDistance,
                maxHeldDistance
            );


        if (!preventHeldClipping)
        {
            return desiredDistance;
        }


        float safeDistance =
            desiredDistance;


        // =====================================================
        // СТОЛ
        // =====================================================

        /*
         * Обычного Raycast до центра жетона
         * недостаточно.
         *
         * Поэтому смотрим ЧУТЬ ДАЛЬШЕ центра:
         * ещё на радиус жетона.
         *
         * Так мы видим стол ещё до того,
         * как передний край жетона в него войдёт.
         */
        if (placementSurface != null)
        {
            float tableCheckDistance =
                desiredDistance +
                heldCollisionRadius +
                heldCollisionPadding;


            if (placementSurface.Raycast(
                    ray,
                    out RaycastHit surfaceHit,
                    tableCheckDistance))
            {
                float tableSafeDistance =
                    surfaceHit.distance -
                    heldCollisionRadius -
                    heldCollisionPadding;


                safeDistance =
                    Mathf.Min(
                        safeDistance,
                        tableSafeDistance
                    );
            }
        }


        // =====================================================
        // ДРУГИЕ COLLIDER
        // =====================================================

        if (heldCollisionRadius > 0f)
        {
            /*
             * SphereCast уже учитывает размер
             * жетона.
             *
             * Поэтому hit.distance здесь —
             * уже дистанция центра жетона
             * в момент касания.
             *
             * НЕЛЬЗЯ ещё раз вычитать Radius:
             * это как раз давало бы лишний рывок
             * назад.
             */
            int hitCount =
                Physics.SphereCastNonAlloc(
                    ray.origin,
                    heldCollisionRadius,
                    ray.direction,
                    obstacleHitBuffer,
                    desiredDistance,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore
                );


            for (int i = 0;
                 i < hitCount;
                 i++)
            {
                RaycastHit hit =
                    obstacleHitBuffer[i];


                if (ShouldIgnoreObstacle(
                        hit.collider))
                {
                    continue;
                }


                if (hit.distance <= 0.001f)
                {
                    continue;
                }


                float obstacleSafeDistance =
                    hit.distance -
                    heldCollisionPadding;


                safeDistance =
                    Mathf.Min(
                        safeDistance,
                        obstacleSafeDistance
                    );
            }
        }


        /*
         * Если Collider действительно находится
         * очень близко, столкновение важнее
         * пользовательского Min Held Distance.
         *
         * Поэтому здесь НЕ возвращаем safeMinDistance.
         */
        return Mathf.Max(
            0.05f,
            safeDistance
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

    private void StartPlace(
    Vector3 targetPosition,
    Quaternion targetRotation)
    {
        if (pickupHintSequenceActive)
        {
            if (pickupHint != null)
            {
                pickupHint.Hide();
            }


            pickupHintSequenceActive = false;
        }

        if (scaleCoroutine != null)
        {
            StopCoroutine(
                scaleCoroutine
            );

            scaleCoroutine = null;
        }


        transitionInProgress = true;
        isHeld = false;


        scaleCoroutine =
            StartCoroutine(
                PlaceRoutine(
                    targetPosition,
                    targetRotation
                )
            );
    }


    private IEnumerator PlaceRoutine(
    Vector3 targetPosition,
    Quaternion targetRotation)
    {
        Vector3 startScale =
            transform.localScale;


        Quaternion startRotation =
            transform.rotation;


        Vector3 heldPosition =
            transform.position;


        float totalDuration =
            Mathf.Max(
                placeScaleDuration,
                rotationTransitionDuration
            );


        if (totalDuration <= 0f)
        {
            transform.position =
                targetPosition;

            transform.rotation =
                targetRotation;

            transform.localScale =
                normalLocalScale;
        }
        else
        {
            float elapsed = 0f;


            while (elapsed <
                   totalDuration)
            {
                elapsed +=
                    Time.deltaTime;


                // -------------------------
                // POSITION
                // -------------------------

                float positionT =
                    Mathf.Clamp01(
                        elapsed /
                        totalDuration
                    );


                float smoothPositionT =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        positionT
                    );


                transform.position =
                    Vector3.Lerp(
                        heldPosition,
                        targetPosition,
                        smoothPositionT
                    );


                // -------------------------
                // SCALE
                // -------------------------

                float scaleT =
                    placeScaleDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(
                            elapsed /
                            placeScaleDuration
                        );


                scaleT =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        scaleT
                    );


                transform.localScale =
                    Vector3.Lerp(
                        startScale,
                        normalLocalScale,
                        scaleT
                    );


                // -------------------------
                // ROTATION
                // -------------------------

                float rotationT =
                    rotationTransitionDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(
                            elapsed /
                            rotationTransitionDuration
                        );


                rotationT =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        rotationT
                    );


                transform.rotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        rotationT
                    );


                yield return null;
            }
        }


        transform.position =
            targetPosition;

        transform.rotation =
            targetRotation;

        transform.localScale =
            normalLocalScale;


        placed = true;
        hasValidPlacement = false;

        transitionInProgress = false;
        scaleCoroutine = null;


        /*
         * После укладки жетон снова
         * можно взять сколько угодно раз.
         */
        SetInteractionAvailable(true);


        ReleaseCarryLock();


        /*
         * Очередь разблокируем только
         * после первой настоящей укладки.
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

        heldDistanceSmoothSpeed =
            Mathf.Max(
                0.01f,
                heldDistanceSmoothSpeed
            );


        heldCollisionRadius =
            Mathf.Max(
                0f,
                heldCollisionRadius
            );


        heldCollisionPadding =
            Mathf.Max(
                0f,
                heldCollisionPadding
            );
    }
}
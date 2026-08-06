using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ZoomComputerWork :
    MonoBehaviour
{
    [Header("Цель")]
    [Tooltip("Точка в центре экрана компьютера.")]
    [SerializeField] private Transform zoomTarget;

    [Header("Автопоиск игрока")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera playerCamera;

    [SerializeField]
    private WorkMouseInteractionController
        mouseInteractionController;

    [Tooltip("Обычный ручной зум игрока.")]
    [SerializeField] private MonoBehaviour manualZoomComponent;

    [SerializeField]
    private string playerObjectName =
        "Player";

    [Tooltip("Точное имя класса обычного ручного зума.")]
    [SerializeField]
    private string manualZoomTypeName =
        "CameraZoom";

    [Header("Рабочие системы")]
    [Tooltip("Контроллер компьютера.")]
    [SerializeField]
    private WorkComputerController computerController;

    [Tooltip("Главный контроллер рабочей смены.")]
    [SerializeField]
    private WorkSessionManager sessionManager;

    [Header("Настройки зума")]
    [Tooltip("Продолжительность приближения и обратного перехода.")]
    [SerializeField]
    private float zoomDuration =
        0.7f;

    [Tooltip("Чем меньше значение, тем сильнее приближение.")]
    [SerializeField]
    private float targetFieldOfView =
        28f;

    [Tooltip("Заблокировать движение камеры после начала зума.")]
    [SerializeField]
    private bool lockPlayerLook =
        true;

    [Tooltip("Отключить обычный ручной зум игрока.")]
    [SerializeField]
    private bool disableManualZoom =
        true;

    [Tooltip("Отключить клики по объектам игрового мира.")]
    [SerializeField]
    private bool disableWorldInteraction =
        true;

    [Header("События приближения")]
    [Tooltip("Вызывается перед началом приближения.")]
    [SerializeField] private UnityEvent onZoomStarted;

    [Tooltip("Вызывается после достижения конечного зума.")]
    [SerializeField] private UnityEvent onZoomReached;

    [Header("События возврата")]
    [Tooltip("Вызывается перед началом возврата к рабочему виду.")]
    [SerializeField] private UnityEvent onReturnStarted;

    [Tooltip("Вызывается после полного возврата к рабочему виду.")]
    [SerializeField] private UnityEvent onReturnReached;

    private Coroutine transitionCoroutine;

    private bool zoomInProgress;
    private bool returnInProgress;
    private bool zoomLocked;
    private bool initialStateCaptured;

    private float initialFieldOfView;
    private Quaternion initialCameraRotation;
    private Quaternion fixedCameraRotation;

    private bool previousCanControl;
    private bool previousManualZoomEnabled;
    private bool previousMouseInteractionEnabled;

    public bool ZoomActive =>
        zoomInProgress ||
        returnInProgress ||
        zoomLocked;

    public bool IsZoomedIn =>
        zoomLocked &&
        !zoomInProgress &&
        !returnInProgress;

    public bool IsTransitioning =>
        zoomInProgress ||
        returnInProgress;

    public float TransitionDuration =>
        Mathf.Max(
            0f,
            zoomDuration
        );

    private void Awake()
    {
        FindReferences();
    }

    private void LateUpdate()
    {
        if (!zoomLocked ||
            playerCamera == null)
        {
            return;
        }

        // Удерживаем камеру на экране после приближения.
        playerCamera.fieldOfView =
            targetFieldOfView;

        playerCamera.transform.rotation =
            fixedCameraRotation;
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(
                transitionCoroutine
            );

            transitionCoroutine = null;
        }

        RestoreInitialState();
    }

    public void StartZoom()
    {
        if (zoomInProgress ||
            returnInProgress ||
            zoomLocked ||
            transitionCoroutine != null)
        {
            return;
        }

        transitionCoroutine =
            StartCoroutine(
                ZoomRoutine()
            );
    }

    public bool ReturnToWorkView()
    {
        if (!zoomLocked ||
            zoomInProgress ||
            returnInProgress ||
            transitionCoroutine != null ||
            !initialStateCaptured)
        {
            return false;
        }

        transitionCoroutine =
            StartCoroutine(
                ReturnRoutine()
            );

        return true;
    }

    private IEnumerator ZoomRoutine()
    {
        FindReferences();

        if (playerCamera == null ||
            zoomTarget == null)
        {
            transitionCoroutine = null;
            yield break;
        }

        CaptureInitialState();
        DisableConflictingControl();

        zoomInProgress = true;
        returnInProgress = false;

        onZoomStarted?.Invoke();

        float startFieldOfView =
            playerCamera.fieldOfView;

        Quaternion startRotation =
            playerCamera.transform.rotation;

        fixedCameraRotation =
            CalculateTargetRotation();

        yield return AnimateCamera(
            startFieldOfView,
            targetFieldOfView,
            startRotation,
            fixedCameraRotation
        );

        playerCamera.fieldOfView =
            targetFieldOfView;

        playerCamera.transform.rotation =
            fixedCameraRotation;

        zoomInProgress = false;
        zoomLocked = true;
        transitionCoroutine = null;

        onZoomReached?.Invoke();
    }

    private IEnumerator ReturnRoutine()
    {
        FindReferences();

        if (playerCamera == null)
        {
            RestoreInitialState();
            transitionCoroutine = null;
            yield break;
        }

        returnInProgress = true;
        zoomInProgress = false;

        // Перестаём принудительно удерживать камеру на мониторе.
        zoomLocked = false;

        if (computerController != null)
        {
            computerController
                .BeginReturnToWorkView();
        }

        ShowWorkHudAndCursor();

        onReturnStarted?.Invoke();

        float startFieldOfView =
            playerCamera.fieldOfView;

        Quaternion startRotation =
            playerCamera.transform.rotation;

        yield return AnimateCamera(
            startFieldOfView,
            initialFieldOfView,
            startRotation,
            initialCameraRotation
        );

        playerCamera.fieldOfView =
            initialFieldOfView;

        playerCamera.transform.rotation =
            initialCameraRotation;

        RestoreCapturedControlState();

        returnInProgress = false;
        zoomInProgress = false;
        zoomLocked = false;
        initialStateCaptured = false;
        transitionCoroutine = null;

        if (computerController != null)
        {
            computerController
                .CompleteReturnToWorkView();
        }

        onReturnReached?.Invoke();
    }

    private IEnumerator AnimateCamera(
        float startFieldOfView,
        float endFieldOfView,
        Quaternion startRotation,
        Quaternion endRotation)
    {
        float duration =
            Mathf.Max(
                0f,
                zoomDuration
            );

        if (duration <= 0f)
        {
            playerCamera.fieldOfView =
                endFieldOfView;

            playerCamera.transform.rotation =
                endRotation;

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            playerCamera.fieldOfView =
                Mathf.Lerp(
                    startFieldOfView,
                    endFieldOfView,
                    smoothT
                );

            playerCamera.transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    endRotation,
                    smoothT
                );

            yield return null;
        }

        playerCamera.fieldOfView =
            endFieldOfView;

        playerCamera.transform.rotation =
            endRotation;
    }

    private void CaptureInitialState()
    {
        if (initialStateCaptured ||
            playerCamera == null)
        {
            return;
        }

        // Сохраняется именно текущий сидячий вид перед каждым зумом.
        initialFieldOfView =
            playerCamera.fieldOfView;

        initialCameraRotation =
            playerCamera.transform.rotation;

        if (playerController != null)
        {
            previousCanControl =
                playerController.canControl;
        }

        FindManualZoom();

        if (manualZoomComponent != null)
        {
            previousManualZoomEnabled =
                manualZoomComponent.enabled;
        }

        if (mouseInteractionController != null)
        {
            previousMouseInteractionEnabled =
                mouseInteractionController.enabled;
        }

        initialStateCaptured = true;
    }

    private void DisableConflictingControl()
    {
        if (lockPlayerLook &&
            playerController != null)
        {
            playerController.canControl =
                false;
        }

        FindManualZoom();

        if (disableManualZoom &&
            manualZoomComponent != null)
        {
            manualZoomComponent.enabled =
                false;
        }

        if (disableWorldInteraction &&
            mouseInteractionController != null)
        {
            mouseInteractionController.enabled =
                false;
        }

        if (sessionManager != null &&
            sessionManager.cursorController != null)
        {
            sessionManager
                .cursorController
                .SetDefaultCursor();
        }
    }

    private void RestoreCapturedControlState()
    {
        if (playerController != null &&
            lockPlayerLook)
        {
            playerController.canControl =
                previousCanControl;
        }

        if (manualZoomComponent != null &&
            disableManualZoom)
        {
            manualZoomComponent.enabled =
                previousManualZoomEnabled;
        }

        if (mouseInteractionController != null &&
            disableWorldInteraction)
        {
            mouseInteractionController.enabled =
                previousMouseInteractionEnabled;
        }
    }

    private void ShowWorkHudAndCursor()
    {
        if (sessionManager == null)
        {
            sessionManager =
                WorkSessionManager.Instance;
        }

        if (sessionManager == null ||
            !sessionManager.IsSeated)
        {
            return;
        }

        // HUD сам использует собственное плавное появление.
        if (sessionManager.hudManager != null)
        {
            sessionManager
                .hudManager
                .Show();
        }

        if (sessionManager.cursorController != null)
        {
            sessionManager
                .cursorController
                .ShowWorkCursor();

            sessionManager
                .cursorController
                .SetDefaultCursor();
        }
    }

    private Quaternion CalculateTargetRotation()
    {
        Vector3 direction =
            zoomTarget.position -
            playerCamera.transform.position;

        if (direction.sqrMagnitude <
            0.0001f)
        {
            return playerCamera
                .transform.rotation;
        }

        return Quaternion.LookRotation(
            direction.normalized,
            Vector3.up
        );
    }

    private void RestoreInitialState()
    {
        if (!initialStateCaptured)
            return;

        if (playerCamera != null)
        {
            playerCamera.fieldOfView =
                initialFieldOfView;

            playerCamera.transform.rotation =
                initialCameraRotation;
        }

        RestoreCapturedControlState();

        zoomInProgress = false;
        returnInProgress = false;
        zoomLocked = false;
        initialStateCaptured = false;
    }

    private void FindReferences()
    {
        if (playerController == null)
        {
            GameObject playerObject =
                GameObject.Find(
                    playerObjectName
                );

            if (playerObject != null)
            {
                playerController =
                    playerObject
                        .GetComponent<PlayerController>();
            }
        }

        if (playerController == null)
        {
            playerController =
                FindFirstObjectByType<PlayerController>(
                    FindObjectsInactive.Include
                );
        }

        if (playerCamera == null &&
            playerController != null)
        {
            playerCamera =
                playerController
                    .GetComponentInChildren<Camera>(
                        true
                    );
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (mouseInteractionController == null)
        {
            mouseInteractionController =
                FindFirstObjectByType
                    <WorkMouseInteractionController>(
                        FindObjectsInactive.Include
                    );
        }

        if (computerController == null)
        {
            computerController =
                FindFirstObjectByType
                    <WorkComputerController>(
                        FindObjectsInactive.Include
                    );
        }

        if (sessionManager == null)
        {
            sessionManager =
                WorkSessionManager.Instance;
        }

        if (sessionManager == null)
        {
            sessionManager =
                FindFirstObjectByType
                    <WorkSessionManager>(
                        FindObjectsInactive.Include
                    );
        }

        FindManualZoom();
    }

    private void FindManualZoom()
    {
        if (manualZoomComponent != null ||
            playerController == null)
        {
            return;
        }

        MonoBehaviour[] components =
            playerController
                .GetComponentsInChildren<MonoBehaviour>(
                    true
                );

        // Сначала ищем обычный зум по точному имени класса.
        for (int i = 0;
             i < components.Length;
             i++)
        {
            MonoBehaviour component =
                components[i];

            if (component == null ||
                component is ZoomComputerWork)
            {
                continue;
            }

            string typeName =
                component.GetType().Name;

            if (string.Equals(
                    typeName,
                    manualZoomTypeName,
                    StringComparison.OrdinalIgnoreCase))
            {
                manualZoomComponent =
                    component;

                return;
            }
        }

        // Запасной поиск компонента со словом Zoom.
        for (int i = 0;
             i < components.Length;
             i++)
        {
            MonoBehaviour component =
                components[i];

            if (component == null ||
                component is ZoomComputerWork)
            {
                continue;
            }

            string typeName =
                component.GetType().Name;

            if (typeName.IndexOf(
                    "Zoom",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                manualZoomComponent =
                    component;

                return;
            }
        }
    }
}
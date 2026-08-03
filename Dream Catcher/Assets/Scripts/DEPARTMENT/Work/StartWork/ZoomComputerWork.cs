using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ZoomComputerWork : MonoBehaviour
{
    [Header("Цель")]
    [Tooltip("Точка в центре экрана компьютера.")]
    [SerializeField] private Transform zoomTarget;

    [Header("Автопоиск игрока")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private WorkMouseInteractionController mouseInteractionController;

    [Tooltip("Обычный ручной зум игрока.")]
    [SerializeField] private MonoBehaviour manualZoomComponent;

    [SerializeField] private string playerObjectName = "Player";

    [Tooltip("Точное имя класса обычного ручного зума.")]
    [SerializeField] private string manualZoomTypeName = "CameraZoom";

    [Header("Настройки зума")]
    [Tooltip("Продолжительность плавного приближения.")]
    [SerializeField] private float zoomDuration = 0.7f;

    [Tooltip("Чем меньше значение, тем сильнее приближение.")]
    [SerializeField] private float targetFieldOfView = 28f;

    [Tooltip("Заблокировать движение камеры после начала зума.")]
    [SerializeField] private bool lockPlayerLook = true;

    [Tooltip("Отключить обычный ручной зум игрока.")]
    [SerializeField] private bool disableManualZoom = true;

    [Tooltip("Отключить клики по объектам игрового мира.")]
    [SerializeField] private bool disableWorldInteraction = true;

    [Header("События")]
    [Tooltip("Вызывается перед началом приближения.")]
    [SerializeField] private UnityEvent onZoomStarted;

    [Tooltip("Вызывается после достижения конечного зума.")]
    [SerializeField] private UnityEvent onZoomReached;

    private Coroutine zoomCoroutine;

    private bool zoomInProgress;
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
        zoomLocked;

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

        // Удерживаем камеру на экране после окончания приближения.
        playerCamera.fieldOfView =
            targetFieldOfView;

        playerCamera.transform.rotation =
            fixedCameraRotation;
    }

    private void OnDisable()
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
            zoomCoroutine = null;
        }

        RestoreInitialState();
    }

    public void StartZoom()
    {
        if (zoomInProgress ||
            zoomLocked ||
            zoomCoroutine != null)
        {
            return;
        }

        zoomCoroutine =
            StartCoroutine(ZoomRoutine());
    }

    private IEnumerator ZoomRoutine()
    {
        FindReferences();

        if (playerCamera == null ||
            zoomTarget == null)
        {
            zoomCoroutine = null;
            yield break;
        }

        CaptureInitialState();
        DisableConflictingControl();

        zoomInProgress = true;

        onZoomStarted?.Invoke();

        float startFieldOfView =
            playerCamera.fieldOfView;

        Quaternion startRotation =
            playerCamera.transform.rotation;

        fixedCameraRotation =
            CalculateTargetRotation();

        if (zoomDuration <= 0f)
        {
            playerCamera.fieldOfView =
                targetFieldOfView;

            playerCamera.transform.rotation =
                fixedCameraRotation;
        }
        else
        {
            float elapsed = 0f;

            while (elapsed < zoomDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed / zoomDuration
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
                        targetFieldOfView,
                        smoothT
                    );

                playerCamera.transform.rotation =
                    Quaternion.Slerp(
                        startRotation,
                        fixedCameraRotation,
                        smoothT
                    );

                yield return null;
            }

            playerCamera.fieldOfView =
                targetFieldOfView;

            playerCamera.transform.rotation =
                fixedCameraRotation;
        }

        zoomInProgress = false;
        zoomLocked = true;
        zoomCoroutine = null;

        onZoomReached?.Invoke();
    }

    private void CaptureInitialState()
    {
        if (initialStateCaptured ||
            playerCamera == null)
        {
            return;
        }

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
            playerController.canControl = false;
        }

        FindManualZoom();

        if (disableManualZoom &&
            manualZoomComponent != null)
        {
            manualZoomComponent.enabled = false;
        }

        if (disableWorldInteraction &&
            mouseInteractionController != null)
        {
            mouseInteractionController.enabled = false;
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

        zoomInProgress = false;
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

        // Сначала ищем ручной зум по точному имени класса.
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
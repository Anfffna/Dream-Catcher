using UnityEngine;
using System.Collections;

public class PlayerWorkSeatController : MonoBehaviour
{
    [Header("Player")]
    public PlayerController playerController;
    public CharacterController characterController;
    public Transform cameraTransform;

    [Header("Seat")]
    public Transform seatPoint;

    [Header("Sitting Animation")]
    public float sitDuration = 0.55f;

    [Tooltip("Насколько опустить камеру при посадке.")]
    public Vector3 sittingCameraLocalOffset =
        new Vector3(0f, -0.35f, 0f);

    [Header("Initial Sitting View")]
    [Tooltip("Начальный вертикальный угол взгляда после посадки. Отрицательное значение — выше.")]
    public float initialWorkPitch = -12f;

    [Header("Work Look")]
    [Range(10f, 180f)]
    public float horizontalHalfAngle = 90f;

    public float minimumPitch = -55f;
    public float maximumPitch = 55f;

    [Tooltip("Размер зоны у краёв экрана, поворачивающей камеру.")]
    public float screenEdgeSize = 90f;

    public float horizontalLookSpeed = 65f;
    public float verticalLookSpeed = 55f;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string playerObjectName = "Player";

    public bool IsSeated { get; private set; }
    public bool IsTransitioning { get; private set; }

    private Vector3 standingPosition;
    private Quaternion standingRotation;
    private Vector3 standingCameraLocalPosition;

    private bool storedStandingState;
    private bool controllerWasEnabled;

    private void Update()
    {
        if (!IsSeated)
            return;

        if (playerController == null || seatPoint == null)
            return;

        playerController.UpdateWorkLookSettings(
            seatPoint.eulerAngles.y,
            horizontalHalfAngle,
            minimumPitch,
            maximumPitch,
            screenEdgeSize,
            horizontalLookSpeed,
            verticalLookSpeed
        );
    }

    public IEnumerator EnterSeat()
    {
        FindReferences();

        IsSeated = false;
        IsTransitioning = true;

        if (playerController == null ||
            characterController == null ||
            cameraTransform == null ||
            seatPoint == null)
        {
            Debug.LogError(
                "PlayerWorkSeatController: не назначены Player, Camera или SeatPoint."
            );

            IsTransitioning = false;
            yield break;
        }

        playerController.CaptureNormalCameraPoseIfNeeded();

        standingPosition = playerController.transform.position;
        standingRotation = playerController.transform.rotation;
        standingCameraLocalPosition = cameraTransform.localPosition;
        storedStandingState = true;

        playerController.SetMovementEnabled(false);
        playerController.canControl = false;

        controllerWasEnabled = characterController.enabled;

        if (characterController.enabled)
            characterController.enabled = false;

        Vector3 startPosition =
            playerController.transform.position;

        Quaternion startRotation =
            playerController.transform.rotation;

        Vector3 startCameraPosition =
            cameraTransform.localPosition;

        float startCameraPitch = Mathf.DeltaAngle(
            0f,
            cameraTransform.localEulerAngles.x
        );

        float targetCameraPitch = Mathf.Clamp(
            initialWorkPitch,
            minimumPitch,
            maximumPitch
        );

        Vector3 targetCameraPosition =
            standingCameraLocalPosition +
            sittingCameraLocalOffset;

        Quaternion targetRotation = Quaternion.Euler(
            0f,
            seatPoint.eulerAngles.y,
            0f
        );

        float timer = 0f;

        while (timer < sitDuration)
        {
            timer += Time.deltaTime;

            float t = sitDuration <= 0f
                ? 1f
                : Mathf.Clamp01(timer / sitDuration);

            float smoothT = t * t * (3f - 2f * t);

            playerController.transform.position =
                Vector3.Lerp(
                    startPosition,
                    seatPoint.position,
                    smoothT
                );

            playerController.transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    smoothT
                );

            cameraTransform.localPosition =
                Vector3.Lerp(
                    startCameraPosition,
                    targetCameraPosition,
                    smoothT
                );

            float currentCameraPitch = Mathf.Lerp(
                startCameraPitch,
                targetCameraPitch,
                smoothT
            );

            playerController.SetCameraPitchImmediate(
                currentCameraPitch
            );

            yield return null;
        }

        playerController.transform.position =
            seatPoint.position;

        playerController.transform.rotation =
            targetRotation;

        cameraTransform.localPosition =
            targetCameraPosition;

        playerController.SetCameraPitchImmediate(
            targetCameraPitch
        );

        if (controllerWasEnabled)
            characterController.enabled = true;

        playerController.BeginWorkLook(
            seatPoint.eulerAngles.y,
            horizontalHalfAngle,
            minimumPitch,
            maximumPitch,
            screenEdgeSize,
            horizontalLookSpeed,
            verticalLookSpeed
        );

        playerController.SetWorkLookPitch(targetCameraPitch);

        playerController.SetMovementEnabled(false);
        playerController.canControl = true;

        IsSeated = true;
        IsTransitioning = false;
    }

    public void RestoreWorkControlAfterPause()
    {
        if (!IsSeated)
            return;

        FindReferences();

        if (playerController == null)
            return;

        playerController.SetMovementEnabled(false);
        playerController.canControl = true;
    }

    public void ExitWorkInstant(bool restoreStandingTransform)
    {
        FindReferences();

        IsSeated = false;
        IsTransitioning = false;

        if (playerController == null)
            return;

        playerController.EndWorkLook();

        if (restoreStandingTransform && storedStandingState)
        {
            bool wasEnabled =
                characterController != null &&
                characterController.enabled;

            if (characterController != null && wasEnabled)
                characterController.enabled = false;

            playerController.transform.position =
                standingPosition;

            playerController.transform.rotation =
                standingRotation;

            if (cameraTransform != null)
            {
                cameraTransform.localPosition =
                    standingCameraLocalPosition;
            }

            if (characterController != null && wasEnabled)
                characterController.enabled = true;
        }

        playerController.SetMovementEnabled(true);
        playerController.canControl = true;
        playerController.ResetMovementAfterTeleport();
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (playerController == null)
        {
            GameObject playerObject =
                GameObject.Find(playerObjectName);

            if (playerObject != null)
            {
                playerController =
                    playerObject.GetComponent<PlayerController>();
            }
        }

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
        {
            if (characterController == null)
            {
                characterController =
                    playerController.GetComponent<CharacterController>();
            }

            if (cameraTransform == null)
                cameraTransform = playerController.cameraTransform;
        }
    }
}
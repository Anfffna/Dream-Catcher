using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public Transform cameraTransform;

    [Header("Control")]
    public bool canControl = false;
    public bool canMove = true;

    [Header("Footsteps")]
    public AudioSource footstepSource;
    public AudioClip walkClip;
    public float walkVolume = 0.5f;

    public AudioClip runClip;
    public float runVolume = 0.8f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool firstControlFrame = false;

    // Рабочий обзор
    private bool workLookEnabled = false;
    private float workCenterYaw;
    private float workYawOffset;
    private float workYawHalfAngle = 90f;

    private float workMinimumPitch = -55f;
    private float workMaximumPitch = 55f;

    private float workScreenEdgeSize = 90f;
    private float workHorizontalSpeed = 65f;
    private float workVerticalSpeed = 55f;

    private Vector3 normalCameraLocalPosition;
    private Quaternion normalCameraLocalRotation;
    private bool normalCameraPoseCaptured = false;

    public bool IsWorkLookEnabled => workLookEnabled;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        CaptureNormalCameraPoseIfNeeded();
        SyncPitchFromCamera();
    }

    private void Update()
    {
        if (!canControl)
        {
            StopFootsteps();

            Input.GetAxis("Mouse X");
            Input.GetAxis("Mouse Y");
            return;
        }

        if (firstControlFrame)
        {
            firstControlFrame = false;
            SyncPitchFromCamera();

            Input.GetAxis("Mouse X");
            Input.GetAxis("Mouse Y");
            return;
        }

        if (workLookEnabled)
            WorkLook();
        else
            Look();

        if (controller != null && controller.enabled)
            Move();

        HandleFootsteps();
    }

    public void EnableControlSmooth()
    {
        canControl = true;
        canMove = true;
        firstControlFrame = true;
    }

    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;

        if (!enabled)
        {
            velocity = Vector3.zero;
            StopFootsteps();
        }
    }

    public void BeginWorkLook(
        float centerYaw,
        float horizontalHalfAngle,
        float minimumPitch,
        float maximumPitch,
        float screenEdgeSize,
        float horizontalSpeed,
        float verticalSpeed)
    {
        workLookEnabled = true;

        workCenterYaw = NormalizeAngle(centerYaw);
        workYawHalfAngle =
            Mathf.Clamp(horizontalHalfAngle, 0f, 180f);

        workMinimumPitch = minimumPitch;
        workMaximumPitch = maximumPitch;

        workScreenEdgeSize =
            Mathf.Max(1f, screenEdgeSize);

        workHorizontalSpeed =
            Mathf.Max(0f, horizontalSpeed);

        workVerticalSpeed =
            Mathf.Max(0f, verticalSpeed);

        workYawOffset = Mathf.Clamp(
            Mathf.DeltaAngle(
                workCenterYaw,
                transform.eulerAngles.y
            ),
            -workYawHalfAngle,
            workYawHalfAngle
        );

        SyncPitchFromCamera();
        SetMovementEnabled(false);
    }

    public void EndWorkLook()
    {
        workLookEnabled = false;
        firstControlFrame = true;
    }

    public void ResetMovementAfterTeleport()
    {
        velocity = Vector3.zero;
        firstControlFrame = true;

        if (controller == null)
            controller = GetComponent<CharacterController>();

        StopFootsteps();
    }

    public void CaptureNormalCameraPoseIfNeeded()
    {
        if (normalCameraPoseCaptured)
            return;

        if (cameraTransform == null)
            return;

        normalCameraLocalPosition = cameraTransform.localPosition;
        normalCameraLocalRotation = cameraTransform.localRotation;
        normalCameraPoseCaptured = true;
    }

    public void SetWorkLookPitch(float pitch)
    {
        xRotation = Mathf.Clamp(
            pitch,
            workMinimumPitch,
            workMaximumPitch
        );

        ApplyCameraPitch();
    }

    public void SetCameraPitchImmediate(float pitch)
    {
        xRotation = Mathf.Clamp(pitch, -90f, 90f);
        ApplyCameraPitch();
    }

    public void UpdateWorkLookSettings(
        float centerYaw,
        float horizontalHalfAngle,
        float minimumPitch,
        float maximumPitch,
        float screenEdgeSize,
        float horizontalSpeed,
        float verticalSpeed)
    {
        if (!workLookEnabled)
            return;

        float currentWorldYaw = transform.eulerAngles.y;

        workCenterYaw = NormalizeAngle(centerYaw);

        workYawHalfAngle = Mathf.Clamp(
            horizontalHalfAngle,
            0f,
            180f
        );

        // Защита на случай, если значения случайно переставлены местами.
        workMinimumPitch = Mathf.Min(
            minimumPitch,
            maximumPitch
        );

        workMaximumPitch = Mathf.Max(
            minimumPitch,
            maximumPitch
        );

        workScreenEdgeSize = Mathf.Max(
            1f,
            screenEdgeSize
        );

        workHorizontalSpeed = Mathf.Max(
            0f,
            horizontalSpeed
        );

        workVerticalSpeed = Mathf.Max(
            0f,
            verticalSpeed
        );

        workYawOffset = Mathf.Clamp(
            Mathf.DeltaAngle(
                workCenterYaw,
                currentWorldYaw
            ),
            -workYawHalfAngle,
            workYawHalfAngle
        );

        xRotation = Mathf.Clamp(
            xRotation,
            workMinimumPitch,
            workMaximumPitch
        );

        transform.rotation = Quaternion.Euler(
            0f,
            workCenterYaw + workYawOffset,
            0f
        );

        ApplyCameraPitch();
    }

    public void ForceResetToNormalGameplayAfterLoad()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        workLookEnabled = false;
        workYawOffset = 0f;

        canControl = true;
        canMove = true;
        firstControlFrame = true;

        velocity = Vector3.zero;

        if (controller != null && !controller.enabled)
            controller.enabled = true;

        if (cameraTransform != null && normalCameraPoseCaptured)
        {
            cameraTransform.localPosition =
                normalCameraLocalPosition;

            cameraTransform.localRotation =
                normalCameraLocalRotation;
        }

        SyncPitchFromCamera();
        StopFootsteps();

        Input.ResetInputAxes();
    }

    private void Look()
    {
        float mouseX =
            Input.GetAxis("Mouse X") *
            GameSettings.MouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            GameSettings.MouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        ApplyCameraPitch();
    }

    private void WorkLook()
    {
        Vector3 mousePosition = Input.mousePosition;

        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);

        float horizontalInput = 0f;
        float verticalInput = 0f;

        float horizontalEdge =
            Mathf.Min(workScreenEdgeSize, width * 0.45f);

        float verticalEdge =
            Mathf.Min(workScreenEdgeSize, height * 0.45f);

        if (mousePosition.x < horizontalEdge)
        {
            horizontalInput = -Mathf.InverseLerp(
                horizontalEdge,
                0f,
                mousePosition.x
            );
        }
        else if (mousePosition.x > width - horizontalEdge)
        {
            horizontalInput = Mathf.InverseLerp(
                width - horizontalEdge,
                width,
                mousePosition.x
            );
        }

        if (mousePosition.y < verticalEdge)
        {
            verticalInput = -Mathf.InverseLerp(
                verticalEdge,
                0f,
                mousePosition.y
            );
        }
        else if (mousePosition.y > height - verticalEdge)
        {
            verticalInput = Mathf.InverseLerp(
                height - verticalEdge,
                height,
                mousePosition.y
            );
        }

        workYawOffset +=
            horizontalInput *
            workHorizontalSpeed *
            Time.deltaTime;

        workYawOffset = Mathf.Clamp(
            workYawOffset,
            -workYawHalfAngle,
            workYawHalfAngle
        );

        float finalYaw =
            workCenterYaw + workYawOffset;

        transform.rotation =
            Quaternion.Euler(0f, finalYaw, 0f);

        // Верх экрана — смотрим вверх.
        // Низ экрана — смотрим вниз.
        xRotation -=
            verticalInput *
            workVerticalSpeed *
            Time.deltaTime;

        xRotation = Mathf.Clamp(
            xRotation,
            workMinimumPitch,
            workMaximumPitch
        );

        ApplyCameraPitch();
    }

    private void Move()
    {
        if (!canMove)
            return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = right * x + forward * z;

        float speed =
            Input.GetKey(KeyCode.LeftShift)
                ? runSpeed
                : walkSpeed;

        controller.Move(
            move * speed * Time.deltaTime
        );

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        controller.Move(
            velocity * Time.deltaTime
        );
    }

    private void HandleFootsteps()
    {
        if (footstepSource == null)
            return;

        if (!canControl ||
            !canMove ||
            controller == null ||
            !controller.enabled)
        {
            StopFootsteps();
            return;
        }

        bool isMoving =
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
            Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;

        bool isRunning =
            isMoving &&
            Input.GetKey(KeyCode.LeftShift);

        if (!isMoving)
        {
            StopFootsteps();
            return;
        }

        AudioClip targetClip =
            isRunning ? runClip : walkClip;

        float targetVolume =
            isRunning ? runVolume : walkVolume;

        footstepSource.volume = targetVolume;

        if (footstepSource.clip != targetClip)
        {
            footstepSource.Stop();
            footstepSource.clip = targetClip;
            footstepSource.loop = true;
            footstepSource.Play();
        }
        else if (!footstepSource.isPlaying)
        {
            footstepSource.loop = true;
            footstepSource.Play();
        }
    }

    private void ApplyCameraPitch()
    {
        if (cameraTransform == null)
            return;

        float currentZ =
            cameraTransform.localRotation.eulerAngles.z;

        cameraTransform.localRotation =
            Quaternion.Euler(
                xRotation,
                0f,
                currentZ
            );
    }

    private void SyncPitchFromCamera()
    {
        if (cameraTransform == null)
            return;

        float cameraX =
            cameraTransform.localRotation.eulerAngles.x;

        xRotation =
            cameraX > 180f
                ? cameraX - 360f
                : cameraX;
    }

    private void StopFootsteps()
    {
        if (footstepSource != null &&
            footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }
}
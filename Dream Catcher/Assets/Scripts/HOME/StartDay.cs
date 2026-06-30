using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class StartDay : MonoBehaviour
{
    [Header("Player & Camera")]
    public Transform playerTransform;
    public Transform cameraTransform;
    public Camera playerCamera;
    public PlayerController playerController;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string playerObjectName = "Player";
    public string cameraObjectName = "Camera";

    [Header("Loading")]
    public bool skipWhenLoadingSave = true;

    [Header("Pivots")]
    public Transform bedPivot;
    public Transform sitPivot;
    public Transform standPivot;

    [Header("Lying Camera Tilt")]
    [Range(0f, 90f)]
    public float lyingTiltZ = 45f;

    [Header("Timing")]
    public float lyingDuration = 1f;
    public float sittingUpSpeed = 1.5f;

    [Header("ScreenSaver Audio")]
    public AudioSource screenSaverAudioSource;

    [Header("Stand Up")]
    public float standMoveDuration = 2f;

    [Tooltip("Во время вставания камера будет возвращаться к обычному FOV")]
    public float restoreFOVDuration = 1f;

    [Header("TV")]
    public TVController tvController;

    [Header("TV Zoom")]
    public bool zoomToTVAfterSitting = true;
    public Transform tvLookTarget;

    [Tooltip("Чем меньше FOV, тем сильнее приближение. Обычно 25-40.")]
    public float tvZoomFOV = 35f;

    [Tooltip("Сколько секунд длится приближение к телевизору.")]
    public float tvZoomDuration = 1.5f;

    [Tooltip("Насколько сильно камера примагничивается к телевизору, пока идут новости.")]
    public float tvLookMagnetStrength = 6f;

    private bool sequenceStarted = false;
    private bool standUpStarted = false;
    private bool controlEnabled = false;
    private bool stopTVMagnet = false;

    private CharacterController charController;
    private Coroutine tvZoomRoutine;
    private float defaultFOV;

    void Start()
    {
        FindReferences();

        if (playerCamera == null && cameraTransform != null)
            playerCamera = cameraTransform.GetComponent<Camera>();

        if (playerCamera != null)
            defaultFOV = playerCamera.fieldOfView;

        charController = playerController != null
            ? playerController.GetComponent<CharacterController>()
            : null;

        if (skipWhenLoadingSave &&
            SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSave)
        {
            ApplyLoadedSaveState();
            return;
        }

        if (charController != null)
            charController.enabled = false;

        if (bedPivot != null && playerTransform != null)
        {
            playerTransform.position = bedPivot.position;
            playerTransform.rotation = bedPivot.rotation;
        }

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(0f, 0f, lyingTiltZ);

        if (playerController != null)
            playerController.canControl = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnScreenSaverFinished()
    {
        FindReferences();

        if (!sequenceStarted)
        {
            sequenceStarted = true;

            if (screenSaverAudioSource != null)
            {
                screenSaverAudioSource.Play();
            }

            StartCoroutine(WakeUpSequence());
        }
    }

    IEnumerator WakeUpSequence()
    {
        yield return new WaitForSeconds(lyingDuration);

        if (tvController != null)
            tvController.PlayNewsVideo();

        Vector3 startPos = bedPivot.position;
        Quaternion startRot = bedPivot.rotation;

        Vector3 targetPos = sitPivot.position;
        Quaternion targetRot = sitPivot.rotation;

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * sittingUpSpeed;
            progress = Mathf.Clamp01(progress);

            float t = 1f - (1f - progress) * (1f - progress);

            playerTransform.position = Vector3.Lerp(startPos, targetPos, t);
            playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            float currentZ = Mathf.Lerp(lyingTiltZ, 0f, t);
            cameraTransform.localRotation = Quaternion.Euler(0f, 0f, currentZ);

            yield return null;
        }

        playerTransform.position = targetPos;
        playerTransform.rotation = targetRot;
        cameraTransform.localRotation = Quaternion.identity;

        if (zoomToTVAfterSitting && playerCamera != null && tvLookTarget != null)
            tvZoomRoutine = StartCoroutine(ZoomAndMagnetToTVUntilNewsEnd());
    }

    public void BeginStandUp()
    {
        FindReferences();

        Debug.LogWarning(
            "StartDay.BeginStandUp ВЫЗВАН! " +
            "time=" + Time.time +
            " | videoTime=" +
            (tvController != null && tvController.videoPlayer != null
                ? tvController.videoPlayer.time.ToString("F2")
                : "NO_VIDEO") +
            "\nSTACK:\n" + System.Environment.StackTrace
        );

        if (standUpStarted) return;

        standUpStarted = true;

        stopTVMagnet = true;

        if (tvZoomRoutine != null)
        {
            StopCoroutine(tvZoomRoutine);
            tvZoomRoutine = null;
        }

        StartCoroutine(MoveToStandPosition());
    }

    IEnumerator MoveToStandPosition()
    {
        if (standPivot == null)
        {
            EnableCharacterControllerAndControl();
            yield break;
        }

        Vector3 startPosition = playerTransform.position;
        Vector3 targetPosition = standPivot.position;

        float startY = playerTransform.eulerAngles.y;
        float targetY = standPivot.eulerAngles.y;

        float startFOV = playerCamera != null ? playerCamera.fieldOfView : defaultFOV;

        cameraTransform.localRotation = Quaternion.identity;

        float timer = 0f;

        while (timer < standMoveDuration)
        {
            timer += Time.deltaTime;

            float t = standMoveDuration <= 0f
                ? 1f
                : Mathf.Clamp01(timer / standMoveDuration);

            float smoothT = t * t * (3f - 2f * t);

            playerTransform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);

            float y = Mathf.LerpAngle(startY, targetY, smoothT);
            playerTransform.rotation = Quaternion.Euler(0f, y, 0f);

            cameraTransform.localRotation = Quaternion.identity;

            if (playerCamera != null)
            {
                float fovT = restoreFOVDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(timer / restoreFOVDuration);

                float smoothFovT = fovT * fovT * (3f - 2f * fovT);
                playerCamera.fieldOfView = Mathf.Lerp(startFOV, defaultFOV, smoothFovT);
            }

            yield return null;
        }

        playerTransform.position = targetPosition;

        float finalY = standPivot.eulerAngles.y;
        playerTransform.rotation = Quaternion.Euler(0f, finalY, 0f);

        cameraTransform.localRotation = Quaternion.identity;

        if (playerCamera != null)
            playerCamera.fieldOfView = defaultFOV;

        EnableCharacterControllerAndControl();
    }

    void EnableCharacterControllerAndControl()
    {
        if (controlEnabled) return;

        controlEnabled = true;

        if (charController != null && !charController.enabled)
        {
            charController.enabled = true;
            charController.Move(Vector3.zero);
        }

        if (playerController != null)
            playerController.EnableControlSmooth();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Проснулся! Можно идти.");
    }

    IEnumerator ZoomAndMagnetToTVUntilNewsEnd()
    {
        stopTVMagnet = false;

        float startFOV = playerCamera.fieldOfView;
        Quaternion startCameraWorldRotation = cameraTransform.rotation;
        Quaternion targetCameraWorldRotation = GetLookRotationToTV();

        float t = 0f;

        while (t < tvZoomDuration)
        {
            if (stopTVMagnet) yield break;

            t += Time.deltaTime;

            float k = tvZoomDuration <= 0f
                ? 1f
                : Mathf.Clamp01(t / tvZoomDuration);

            float smoothK = 1f - (1f - k) * (1f - k);

            playerCamera.fieldOfView = Mathf.Lerp(startFOV, tvZoomFOV, smoothK);
            cameraTransform.rotation = Quaternion.Slerp(startCameraWorldRotation, targetCameraWorldRotation, smoothK);

            yield return null;
        }

        playerCamera.fieldOfView = tvZoomFOV;

        while (IsNewsStillActive())
        {
            if (stopTVMagnet) yield break;

            Quaternion desiredRotation = GetLookRotationToTV();

            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation,
                desiredRotation,
                Time.deltaTime * tvLookMagnetStrength
            );

            yield return null;
        }
    }

    bool IsNewsStillActive()
    {
        if (tvController == null) return false;
        if (tvController.videoPlayer == null) return false;

        VideoPlayer videoPlayer = tvController.videoPlayer;

        if (videoPlayer.isPlaying)
            return true;

        if (videoPlayer.isPaused)
            return true;

        return false;
    }

    Quaternion GetLookRotationToTV()
    {
        Vector3 direction = tvLookTarget.position - cameraTransform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return cameraTransform.rotation;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    void ApplyLoadedSaveState()
    {
        sequenceStarted = true;
        standUpStarted = true;
        controlEnabled = true;
        stopTVMagnet = true;

        if (tvZoomRoutine != null)
        {
            StopCoroutine(tvZoomRoutine);
            tvZoomRoutine = null;
        }

        if (screenSaverAudioSource != null)
            screenSaverAudioSource.Stop();

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.identity;

        if (playerCamera != null)
            playerCamera.fieldOfView = defaultFOV;

        if (charController != null && !charController.enabled)
        {
            charController.enabled = true;
            charController.Move(Vector3.zero);
        }

        if (playerController != null)
        {
            playerController.canControl = true;
            playerController.canMove = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("StartDay: пропущен, потому что загружается сейв.");
    }

    void OnDrawGizmos()
    {
        if (bedPivot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(bedPivot.position, 0.15f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(bedPivot.position, bedPivot.forward * 0.5f);
        }

        if (sitPivot != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(sitPivot.position, 0.15f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(sitPivot.position, sitPivot.forward * 0.5f);
        }

        if (standPivot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(standPivot.position, 0.15f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(standPivot.position, standPivot.forward * 0.5f);
        }

        if (bedPivot != null && sitPivot != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(bedPivot.position, sitPivot.position);
        }

        if (sitPivot != null && standPivot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(sitPivot.position, standPivot.position);
        }

        if (tvLookTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tvLookTarget.position, 0.1f);
        }
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        // PlayerController + playerTransform
        if (playerController == null || playerTransform == null)
        {
            GameObject playerObj = GameObject.Find(playerObjectName);

            if (playerObj != null)
            {
                if (playerTransform == null)
                    playerTransform = playerObj.transform;

                if (playerController == null)
                    playerController = playerObj.GetComponent<PlayerController>();
            }
        }

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerTransform == null && playerController != null)
            playerTransform = playerController.transform;

        // Camera Transform
        if (cameraTransform == null && playerTransform != null)
        {
            Transform foundCamera = playerTransform.Find(cameraObjectName);

            if (foundCamera != null)
                cameraTransform = foundCamera;
        }

        if (cameraTransform == null)
        {
            GameObject cameraObj = GameObject.Find(cameraObjectName);

            if (cameraObj != null)
                cameraTransform = cameraObj.transform;
        }

        // Camera Component
        if (playerCamera == null && cameraTransform != null)
            playerCamera = cameraTransform.GetComponent<Camera>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (cameraTransform == null && playerCamera != null)
            cameraTransform = playerCamera.transform;
    }
}
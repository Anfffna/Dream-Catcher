using UnityEngine;

public class Zoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    public KeyCode zoomKey = KeyCode.Z;
    public float zoomFactor = 1.5f;
    public float zoomSpeed = 5f;

    [Header("Player")]
    public PlayerController playerController;

    private Camera cam;
    private float originalFOV;
    private float targetFOV;
    private bool isZooming;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("Zoom скрипт должен быть прикреплён к камере!");
            enabled = false;
            return;
        }

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        originalFOV = cam.fieldOfView;
        targetFOV = originalFOV;
    }

    void Update()
    {
        // Пока игроком нельзя управлять, Zoom НЕ трогает FOV.
        // Это важно для StartDay, TV-з zoom, заставки и катсцен.
        if (playerController != null && !playerController.canControl)
            return;

        if (Input.GetKeyDown(zoomKey))
        {
            isZooming = true;
            targetFOV = originalFOV / zoomFactor;
        }
        else if (Input.GetKeyUp(zoomKey))
        {
            isZooming = false;
            targetFOV = originalFOV;
        }

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            Time.deltaTime * zoomSpeed
        );
    }

    public void RefreshOriginalFOV()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null)
            return;

        originalFOV = cam.fieldOfView;
        targetFOV = originalFOV;
    }
}
using UnityEngine;

public class Zoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    public KeyCode zoomKey = KeyCode.Z;      // клавиша для приближения
    public float zoomFactor = 1.5f;          // во сколько раз приблизить (1.5 = 1.5x)
    public float zoomSpeed = 5f;             // скорость плавного изменения

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
        originalFOV = cam.fieldOfView;
        targetFOV = originalFOV;
    }

    void Update()
    {
        if (Input.GetKeyDown(zoomKey))
        {
            isZooming = true;
            targetFOV = originalFOV / zoomFactor; // уменьшаем угол обзора ? приближение
        }
        else if (Input.GetKeyUp(zoomKey))
        {
            isZooming = false;
            targetFOV = originalFOV;
        }

        // Плавный переход к целевому FOV
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }
}
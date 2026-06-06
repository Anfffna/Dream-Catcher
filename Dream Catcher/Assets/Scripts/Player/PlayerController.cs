using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 150f;
    public Transform cameraTransform;

    [Header("Control")]
    public bool canControl = false;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool firstControlFrame = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (!canControl)
        {
            Input.GetAxis("Mouse X");
            Input.GetAxis("Mouse Y");
            return;
        }

        if (firstControlFrame)
        {
            firstControlFrame = false;

            // Читаем текущий поворот камеры
            if (cameraTransform != null)
            {
                Vector3 camLocal = cameraTransform.localRotation.eulerAngles;
                xRotation = camLocal.x > 180f ? camLocal.x - 360f : camLocal.x;
            }

            // НЕ меняем тело! Оставляем его поворот как есть (от bedPivot/sitPivot)
            // Потребляем мышь
            Input.GetAxis("Mouse X");
            Input.GetAxis("Mouse Y");
            return;
        }

        Look();

        if (controller != null && controller.enabled)
        {
            Move();
        }
    }

    public void EnableControlSmooth()
    {
        canControl = true;
        firstControlFrame = true;
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Горизонтальный поворот — поворачиваем ТЕЛО игрока
        transform.Rotate(Vector3.up * mouseX);

        // Вертикальный поворот — поворачиваем КАМЕРУ локально
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraTransform != null)
        {
            // Берём ТЕКУЩИЙ Z камеры (не сбрасываем!)
            float currentZ = cameraTransform.localRotation.eulerAngles.z;
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, currentZ);
        }
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = right * x + forward * z;
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        controller.Move(move * speed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
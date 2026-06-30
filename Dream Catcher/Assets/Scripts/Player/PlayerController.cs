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

    //void Awake()
    //{
    //    // Проверяем, есть ли уже другой PlayerController в сцене
    //    PlayerController[] existing = FindObjectsOfType<PlayerController>();
    //    if (existing.Length > 1)
    //    {
    //        Destroy(gameObject);
    //        return;
    //    }
    //    //DontDestroyOnLoad(gameObject);
    //}

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (!canControl)
        {
            if (footstepSource != null && footstepSource.isPlaying)
                footstepSource.Stop();

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

        HandleFootsteps();
    }

    public void EnableControlSmooth()
    {
        canControl = true;
        canMove = true;
        firstControlFrame = true;
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * GameSettings.MouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * GameSettings.MouseSensitivity;

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
        if (!canMove) return;

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

    void HandleFootsteps()
    {
        if (footstepSource == null)
            return;

        bool isMoving =
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
            Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;

        bool isRunning =
            isMoving &&
            Input.GetKey(KeyCode.LeftShift);

        if (!isMoving)
        {
            if (footstepSource.isPlaying)
                footstepSource.Stop();

            return;
        }

        AudioClip targetClip = isRunning ? runClip : walkClip;
        footstepSource.volume =
            isRunning ? runVolume : walkVolume;

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

        footstepSource.volume =
            isRunning ? runVolume : walkVolume;
    }
}
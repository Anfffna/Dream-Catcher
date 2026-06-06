using UnityEngine;
using System.Collections;

public class StartDay : MonoBehaviour
{
    [Header("Player & Camera")]
    public Transform playerTransform;
    public Transform cameraTransform;
    public PlayerController playerController;

    [Header("Pivots")]
    public Transform bedPivot;          // лежачее положение
    public Transform sitPivot;          // сидячее положение

    [Header("Lying Camera Tilt")]
    [Range(0f, 90f)]
    public float lyingTiltZ = 45f;      // наклон камеры на бок когда лежит

    [Header("Timing")]
    public float lyingDuration = 1f;
    public float sittingUpSpeed = 1.5f;

    [Header("TV")]
    public TVController tvController;

    private bool sequenceStarted = false;
    private CharacterController charController;

    void Start()
    {
        charController = playerController != null
            ? playerController.GetComponent<CharacterController>()
            : null;

        // Отключаем CharacterController
        if (charController != null)
            charController.enabled = false;

        // Перемещаем игрока в bedPivot
        if (bedPivot != null)
        {
            playerTransform.position = bedPivot.position;
            playerTransform.rotation = bedPivot.rotation;
        }

        // Камера смотрит туда же куда тело + наклонена на бок
        cameraTransform.localRotation = Quaternion.Euler(0f, 0f, lyingTiltZ);

        // Блокируем управление
        if (playerController != null)
            playerController.canControl = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnScreenSaverFinished()
    {
        if (!sequenceStarted)
        {
            sequenceStarted = true;
            StartCoroutine(WakeUpSequence());
        }
    }

    IEnumerator WakeUpSequence()
    {
        yield return new WaitForSeconds(lyingDuration);

        // В этот момент ГГ НАЧИНАЕТ вставать
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

            // Двигаем игрока
            playerTransform.position = Vector3.Lerp(startPos, targetPos, t);
            playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            // Выравниваем камеру: Z от lyingTiltZ до 0
            float currentZ = Mathf.Lerp(lyingTiltZ, 0f, t);
            cameraTransform.localRotation = Quaternion.Euler(0f, 0f, currentZ);

            yield return null;
        }

        // Финал — игрок в sitPivot
        playerTransform.position = targetPos;
        playerTransform.rotation = targetRot;
        cameraTransform.localRotation = Quaternion.identity;

        yield return null;

        //// Включаем CharacterController прямо здесь
        //if (charController != null)
        //    charController.enabled = true;

        // Включаем управление
        if (playerController != null)
            playerController.EnableControlSmooth();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Проснулся! Можно идти.");
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

        if (bedPivot != null && sitPivot != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(bedPivot.position, sitPivot.position);
        }
    }
}
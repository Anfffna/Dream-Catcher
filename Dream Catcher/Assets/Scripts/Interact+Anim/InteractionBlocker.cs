using UnityEngine;
using UnityEngine.UI;

public class InteractionBlocker : MonoBehaviour, IInteractable
{
    [Header("Layer")]
    public bool setLayerToInteractableOnStart = true;
    public string interactableLayerName = "Interactable";

    [Header("Hide Cursor Dot Only When Looking At This Blocker")]
    public bool hideInteractionDot = true;
    public string playerCameraObjectName = "Camera";
    public string interactionDotObjectName = "InteractionDot";
    public float checkDistance = 1f;

    private Camera playerCamera;
    private Image interactionDot;
    private Collider blockerCollider;

    private void Start()
    {
        blockerCollider = GetComponent<Collider>();

        if (setLayerToInteractableOnStart)
        {
            int interactableLayer = LayerMask.NameToLayer(interactableLayerName);

            if (interactableLayer != -1)
                gameObject.layer = interactableLayer;
        }

        FindReferences();
    }

    private void LateUpdate()
    {
        if (!hideInteractionDot)
            return;

        if (blockerCollider == null || !blockerCollider.enabled || !gameObject.activeInHierarchy)
            return;

        if (playerCamera == null || interactionDot == null)
            FindReferences();

        if (playerCamera == null || interactionDot == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, checkDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == blockerCollider)
            {
                interactionDot.enabled = false;
                interactionDot.gameObject.SetActive(false);
            }
        }
    }

    public void Interact()
    {
        // Пусто.
        // Блокер ловит интеракт-луч, но ничего не делает.
        // Поэтому объект за ним не срабатывает.
    }

    private void FindReferences()
    {
        if (playerCamera == null)
        {
            GameObject camObj = GameObject.Find(playerCameraObjectName);

            if (camObj != null)
                playerCamera = camObj.GetComponent<Camera>();
        }

        if (interactionDot == null)
        {
            GameObject dotObj = GameObject.Find(interactionDotObjectName);

            if (dotObj != null)
                interactionDot = dotObj.GetComponent<Image>();
        }
    }
}
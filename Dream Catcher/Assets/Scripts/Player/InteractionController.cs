using UnityEngine;
using UnityEngine.UI;

public class InteractionController : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Interaction")]
    public float interactionDistance = 1f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("Interactable Layers")]
    public LayerMask interactableLayers;

    [Header("UI")]
    public Image interactionDot;

    private IInteractable currentInteractable;

    void Start()
    {
        if (interactionDot != null)
            interactionDot.gameObject.SetActive(false);
    }

    void Update()
    {
        CheckInteraction();

        if (currentInteractable != null &&
            Input.GetKeyDown(interactionKey))
        {
            currentInteractable.Interact();
        }
    }

    void CheckInteraction()
    {
        currentInteractable = null;

        if (interactionDot != null)
            interactionDot.gameObject.SetActive(false);

        if (playerCamera == null)
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactableLayers))
        {
            IInteractable interactable =
                hit.collider.GetComponent<IInteractable>();

            if (interactable == null)
            {
                interactable =
                    hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                currentInteractable = interactable;

                if (interactionDot != null)
                    interactionDot.gameObject.SetActive(true);
            }
        }
    }
}
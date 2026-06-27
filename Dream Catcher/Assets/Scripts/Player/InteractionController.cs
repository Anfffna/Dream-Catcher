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
    public Color defaultDotColor = Color.white;
    public Color darkDotColor = new Color(0.298f, 0.298f, 0.298f); // #4C4C4C

    private IInteractable currentInteractable;

    void Start()
    {
        if (interactionDot != null)
        {
            interactionDot.gameObject.SetActive(false);
            interactionDot.color = defaultDotColor;
        }
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
        {
            interactionDot.gameObject.SetActive(false);
            interactionDot.color = defaultDotColor; // теперь в безопасности
        }

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
                {
                    interactionDot.gameObject.SetActive(true);
                    // Определяем цвет точки в зависимости от яркости объекта
                    Color dotColor = GetDotColorForObject(hit.collider.gameObject);
                    interactionDot.color = dotColor;
                }
            }
        }
    }

    private Color GetDotColorForObject(GameObject obj)
    {
        // Пытаемся получить Renderer (MeshRenderer или SkinnedMeshRenderer)
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
            renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer == null)
            renderer = obj.GetComponentInParent<Renderer>();

        if (renderer != null && renderer.sharedMaterial != null)
        {
            // Проверяем, есть ли свойство _Color (стандартный шейдер)
            if (renderer.sharedMaterial.HasProperty("_Color"))
            {
                Color matColor = renderer.sharedMaterial.color;
                float luminance = 0.299f * matColor.r + 0.587f * matColor.g + 0.114f * matColor.b;
                // Если яркость > 0.7, считаем объект светлым
                if (luminance > 1f)
                    return darkDotColor;
            }
        }

        return defaultDotColor;
    }
}
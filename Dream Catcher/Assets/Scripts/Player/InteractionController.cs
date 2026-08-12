using UnityEngine;
using UnityEngine.UI;

public class InteractionController : MonoBehaviour
{
    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string playerCameraObjectName = "Camera";
    public string interactionDotObjectName = "InteractionDot";

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

    private IInteractable currentInteractable;

    void Start()
    {
        FindReferences();
        ClearCurrentInteraction();
    }

    void Update()
    {
        if (playerCamera == null || interactionDot == null)
            FindReferences();

        // ВАЖНО:
        // Если открыт любой игровой UI, вообще не проверяем 3D-взаимодействие.
        if (IsGameplayInteractionBlocked())
        {
            ClearCurrentInteraction();
            return;
        }

        CheckInteraction();

        if (currentInteractable != null &&
            Input.GetKeyDown(interactionKey))
        {
            currentInteractable.Interact();
        }
    }

    private bool IsGameplayInteractionBlocked()
    {
        // Пауза открыта
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return true;

        // Панель заданий открыта
        if (TaskPanelController.Instance != null && TaskPanelController.Instance.IsPanelOpen)
            return true;

        if (WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsWorkModeActive)
        {
            return true;
        }

        // Обычные диалоги DialogueManager
        if (DialogueManager.AnyDialogueActive)
            return true;

        // Loading screen
        if (LoadingManager.IsLoadingScreenBlockingPause())
            return true;

        // Интро / старт дня
        if (StartDay.IntroBlocksPause)
            return true;

        // Новости / телевизор
        if (NewsDialogue.NewsBlocksPause)
            return true;

        return false;
    }

    private void ClearCurrentInteraction()
    {
        currentInteractable = null;

        if (interactionDot != null)
        {
            // Сам объект всегда остаётся активным,
            // скрываем только компонент Image.
            if (!interactionDot.gameObject.activeSelf)
                interactionDot.gameObject.SetActive(true);

            interactionDot.enabled = false;
            interactionDot.color = defaultDotColor;
        }
    }

    void CheckInteraction()
    {
        ClearCurrentInteraction();

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
                    interactionDot.enabled = true;

                    interactionDot.color =
                        defaultDotColor;
                }
            }
        }
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (playerCamera == null)
        {
            GameObject camObj = GameObject.Find(playerCameraObjectName);

            if (camObj != null)
                playerCamera = camObj.GetComponent<Camera>();
        }

        if (interactionDot == null)
        {
            GameObject dotObj =
                GameObject.Find(interactionDotObjectName);

            if (dotObj != null)
            {
                interactionDot = dotObj.GetComponent<Image>();
            }
            else
            {
                Image[] allImages = FindObjectsByType<Image>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

                for (int i = 0; i < allImages.Length; i++)
                {
                    if (allImages[i] == null)
                        continue;

                    if (allImages[i].gameObject.name != interactionDotObjectName)
                        continue;

                    interactionDot = allImages[i];
                    interactionDot.gameObject.SetActive(true);
                    interactionDot.enabled = false;
                    break;
                }
            }
        }
    }
}
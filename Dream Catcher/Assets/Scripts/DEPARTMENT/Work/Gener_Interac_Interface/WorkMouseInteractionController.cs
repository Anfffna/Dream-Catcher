using UnityEngine;
using UnityEngine.EventSystems;

public class WorkMouseInteractionController :
    MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Interaction")]
    public string interactableLayerName =
        "Interactable";

    [Tooltip("ћаксимальное рассто€ние курсорного взаимодействи€.")]
    public float interactionDistance =
        10f;

    [Header("Cursor")]
    public WorkCursorController
        cursorController;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private IInteractable
        hoveredInteractable;

    private int interactableLayerMask;

    private void Awake()
    {
        BuildLayerMask();
        FindReferences();
    }

    private void Start()
    {
        BuildLayerMask();
        FindReferences();
    }

    private void Update()
    {
        if (PauseManager.Instance != null &&
            PauseManager.Instance.IsPaused)
        {
            return;
        }

        if (DialogueManager
            .AnyDialogueActive)
        {
            SetHoveredInteractable(null);
            return;
        }

        // ѕока открыт вариативный диалог,
        // клики не проход€т в 3D-мир.
        if (ClientQuestionDialogueController
                .AnyQuestionDialogueOpen ||
            ClientQuestionDialogueController
                .LastClosedFrame ==
            Time.frameCount)
        {
            SetHoveredInteractable(null);
            return;
        }

        bool isSeated =
            WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsSeated;

        if (!isSeated)
        {
            SetHoveredInteractable(null);
            return;
        }

        // UI получает клик первым и не пропускает его в 3D-мир.
        if (EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject())
        {
            SetHoveredInteractable(null);
            return;
        }

        FindReferences();

        if (playerCamera == null)
            return;

        if (interactableLayerMask == 0)
        {
            BuildLayerMask();

            if (interactableLayerMask == 0)
                return;
        }

        Ray ray =
            playerCamera.ScreenPointToRay(
                Input.mousePosition
            );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactableLayerMask,
            QueryTriggerInteraction.Collide))
        {
            IInteractable interactable =
                FindInteractable(
                    hit.collider
                );

            if (interactable != null)
            {
                SetHoveredInteractable(
                    interactable
                );

                if (showDebugLogs)
                {
                    Debug.Log(
                        "–абочий курсор наведЄн на: " +
                        hit.collider
                            .gameObject.name
                    );
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (showDebugLogs)
                    {
                        Debug.Log(
                            "–абочий клик по: " +
                            hit.collider
                                .gameObject.name
                        );
                    }

                    interactable.Interact();
                }

                return;
            }
        }

        SetHoveredInteractable(null);
    }

    private IInteractable FindInteractable(
        Collider hitCollider)
    {
        if (hitCollider == null)
            return null;

        MonoBehaviour[] behaviours =
            hitCollider
                .GetComponentsInParent
                    <MonoBehaviour>(true);

        for (int i = 0;
             i < behaviours.Length;
             i++)
        {
            if (behaviours[i] is
                IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private void SetHoveredInteractable(
        IInteractable newInteractable)
    {
        if (ReferenceEquals(
            hoveredInteractable,
            newInteractable))
        {
            return;
        }

        hoveredInteractable =
            newInteractable;

        if (cursorController == null)
            return;

        if (hoveredInteractable != null)
        {
            cursorController
                .SetInteractCursor();
        }
        else
        {
            cursorController
                .SetDefaultCursor();
        }
    }

    private void BuildLayerMask()
    {
        int layer =
            LayerMask.NameToLayer(
                interactableLayerName
            );

        if (layer < 0)
        {
            interactableLayerMask = 0;

            Debug.LogError(
                "WorkMouseInteractionController: " +
                "слой не найден: " +
                interactableLayerName
            );

            return;
        }

        interactableLayerMask =
            1 << layer;
    }

    private void FindReferences()
    {
        if (playerCamera == null)
        {
            playerCamera =
                Camera.main;
        }

        if (cursorController == null)
        {
            cursorController =
                FindFirstObjectByType
                    <WorkCursorController>(
                        FindObjectsInactive
                            .Include
                    );
        }
    }
}
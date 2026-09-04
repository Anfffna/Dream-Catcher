using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class WorkMouseInteractionController :
    MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Interaction")]
    public string interactableLayerName =
        "Interactable";

    [Tooltip("Максимальное расстояние курсорного взаимодействия.")]
    public float interactionDistance =
        10f;

    [Header("Cursor")]
    public WorkCursorController
        cursorController;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private IInteractable
        hoveredInteractable;

    private readonly List<MonoBehaviour>
    behaviourBuffer =
        new List<MonoBehaviour>(8);

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
        bool pauseBlocksWorld =
            PauseManager.Instance != null &&
            PauseManager.Instance.IsPaused;

        bool taskPanelBlocksWorld =
            TaskPanelController.Instance != null &&
            TaskPanelController.Instance.BlocksWorldInteraction;

        bool loadingBlocksWorld =
            LoadingManager
                .IsLoadingScreenBlockingPause();

        if (pauseBlocksWorld ||
            taskPanelBlocksWorld ||
            loadingBlocksWorld)
        {
            // Сбрасываем старый hover от объекта,
            // но НЕ меняем сам курсор:
            // курсором сейчас управляет открытое меню.
            hoveredInteractable = null;
            return;
        }

        if (DialogueManager
            .AnyDialogueActive)
        {
            SetHoveredInteractable(null);
            return;
        }

        // Пока открыт вариативный диалог,
        // клики не проходят в 3D-мир.
        if (ClientQuestionDialogueController
                .AnyQuestionDialogueOpen ||
            ClientQuestionDialogueController
                .LastClosedFrame ==
            Time.frameCount)
        {
            SetHoveredInteractable(null);
            return;
        }

        // Пока игрок переносит специальный предмет,
        // никакие другие объекты рабочего места
        // не принимают взаимодействие.
        if (DeskCarryItemController
                .AnyCarryInteractionActive)
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
                        "Рабочий курсор наведён на: " +
                        hit.collider
                            .gameObject.name
                    );
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (showDebugLogs)
                    {
                        Debug.Log(
                            "Рабочий клик по: " +
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

        Transform current =
            hitCollider.transform;

        while (current != null)
        {
            behaviourBuffer.Clear();

            current.GetComponents(
                behaviourBuffer
            );

            for (int i = 0;
                 i < behaviourBuffer.Count;
                 i++)
            {
                if (behaviourBuffer[i] is
                    IInteractable interactable)
                {
                    return interactable;
                }
            }

            current = current.parent;
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
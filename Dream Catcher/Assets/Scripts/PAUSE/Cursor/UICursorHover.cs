using UnityEngine;
using UnityEngine.EventSystems;

public class UICursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TaskPanelController taskPanelController;

    void Awake()
    {
        taskPanelController = FindFirstObjectByType<TaskPanelController>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (taskPanelController != null)
            taskPanelController.SetInteractCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (taskPanelController != null)
            taskPanelController.SetDefaultCursor();
    }
}
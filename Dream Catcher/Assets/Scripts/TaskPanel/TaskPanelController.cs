using UnityEngine;

public class TaskPanelController : MonoBehaviour
{
    [Header("Panel")]
    public GameObject taskPanel;
    public CanvasGroup taskPanelCanvasGroup;

    [Header("Player")]
    public PlayerController playerController;

    [Header("Custom Cursors")]
    public Texture2D defaultCursor;
    public Texture2D interactCursor;

    [Header("Cursor Hotspot")]
    public Vector2 defaultCursorHotspot = Vector2.zero;
    public Vector2 interactCursorHotspot = Vector2.zero;

    private bool isPanelOpen = false;
    private bool cursorIsDefault = false;
    private bool cursorIsInteract = false;

    void Start()
    {
        if (taskPanel != null)
            taskPanel.SetActive(true);

        if (taskPanelCanvasGroup == null && taskPanel != null)
            taskPanelCanvasGroup = taskPanel.GetComponent<CanvasGroup>();

        if (taskPanelCanvasGroup == null && taskPanel != null)
            taskPanelCanvasGroup = taskPanel.AddComponent<CanvasGroup>();

        ClosePanel();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isPanelOpen)
                ClosePanel();
            else
                OpenPanel();
        }
    }

    public void OpenPanel()
    {
        isPanelOpen = true;

        if (taskPanelCanvasGroup != null)
        {
            taskPanelCanvasGroup.alpha = 1f;
            taskPanelCanvasGroup.interactable = true;
            taskPanelCanvasGroup.blocksRaycasts = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetDefaultCursor();

        if (playerController != null)
            playerController.canControl = false;
    }

    public void ClosePanel()
    {
        isPanelOpen = false;

        if (taskPanelCanvasGroup != null)
        {
            taskPanelCanvasGroup.alpha = 0f;
            taskPanelCanvasGroup.interactable = false;
            taskPanelCanvasGroup.blocksRaycasts = false;
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        cursorIsDefault = false;
        cursorIsInteract = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerController != null)
            playerController.canControl = true;
    }

    public void SetDefaultCursor()
    {
        if (!isPanelOpen) return;
        if (cursorIsDefault) return;

        Cursor.SetCursor(defaultCursor, defaultCursorHotspot, CursorMode.Auto);

        cursorIsDefault = true;
        cursorIsInteract = false;
    }

    public void SetInteractCursor()
    {
        if (!isPanelOpen) return;
        if (cursorIsInteract) return;

        Cursor.SetCursor(interactCursor, interactCursorHotspot, CursorMode.Auto);

        cursorIsInteract = true;
        cursorIsDefault = false;
    }
}
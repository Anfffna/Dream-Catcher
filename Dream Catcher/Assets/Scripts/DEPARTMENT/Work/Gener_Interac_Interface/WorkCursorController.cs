using UnityEngine;

public class WorkCursorController : MonoBehaviour
{
    [Header("Cursor")]
    public Texture2D defaultCursor;
    public Texture2D interactCursor;

    public Vector2 defaultCursorHotspot = Vector2.zero;
    public Vector2 interactCursorHotspot = Vector2.zero;

    [Header("Auto Copy")]
    public bool copyFromTaskPanelController = true;

    private void FindCursorTextures()
    {
        if (!copyFromTaskPanelController)
            return;

        if (TaskPanelController.Instance == null)
            return;

        if (defaultCursor == null)
        {
            defaultCursor =
                TaskPanelController.Instance.defaultCursor;

            defaultCursorHotspot =
                TaskPanelController.Instance.defaultCursorHotspot;
        }

        if (interactCursor == null)
        {
            interactCursor =
                TaskPanelController.Instance.interactCursor;

            interactCursorHotspot =
                TaskPanelController.Instance.interactCursorHotspot;
        }
    }

    public void ShowWorkCursor()
    {
        FindCursorTextures();

        Cursor.SetCursor(
            defaultCursor,
            defaultCursorHotspot,
            CursorMode.ForceSoftware
        );

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetDefaultCursor()
    {
        FindCursorTextures();

        Cursor.SetCursor(
            defaultCursor,
            defaultCursorHotspot,
            CursorMode.ForceSoftware
        );
    }

    public void SetInteractCursor()
    {
        FindCursorTextures();

        Cursor.SetCursor(
            interactCursor,
            interactCursorHotspot,
            CursorMode.ForceSoftware
        );
    }

    public void HideAndLockGameplayCursor()
    {
        FindCursorTextures();

        Cursor.SetCursor(
            defaultCursor,
            defaultCursorHotspot,
            CursorMode.ForceSoftware
        );

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
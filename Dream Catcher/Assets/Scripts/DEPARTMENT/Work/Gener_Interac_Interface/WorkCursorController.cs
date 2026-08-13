using UnityEngine;

public class WorkCursorController : MonoBehaviour
{
    private bool workCursorShown;
    private bool interactCursorActive;


    public void ShowWorkCursor()
    {
        workCursorShown = true;
        interactCursorActive = false;

        ApplyCurrentCursor();
    }


    public void SetDefaultCursor()
    {
        interactCursorActive = false;

        if (workCursorShown)
            ApplyCurrentCursor();
    }


    public void SetInteractCursor()
    {
        interactCursorActive = true;

        if (workCursorShown)
            ApplyCurrentCursor();
    }


    public void HideAndLockGameplayCursor()
    {
        workCursorShown = false;
        interactCursorActive = false;

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance
                .HideGameplayCursor();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }
    }


    private void ApplyCurrentCursor()
    {
        if (!workCursorShown)
            return;

        Cursor.lockState =
            CursorLockMode.Confined;

        if (PauseManager.Instance != null)
        {
            if (interactCursorActive)
            {
                PauseManager.Instance
                    .SetInteractCursor();
            }
            else
            {
                PauseManager.Instance
                    .SetDefaultCursor();
            }
        }

        Cursor.visible = true;
    }


    private void OnApplicationFocus(
        bool hasFocus)
    {
        if (!hasFocus ||
            !workCursorShown)
        {
            return;
        }

        ApplyCurrentCursor();
    }
}
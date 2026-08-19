using UnityEngine;

public class WorkCursorController :
    MonoBehaviour
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
            ForceCursorHidden();
        }
    }


    private void ApplyCurrentCursor()
    {
        if (!workCursorShown)
            return;

        // ¬о врем€ загрузки Work Cursor
        // вообще не имеет права показыватьс€.
        if (LoadingManager
            .IsLoadingScreenBlockingPause())
        {
            ForceCursorHidden();
            return;
        }

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


    private void ForceCursorHidden()
    {
        Cursor.visible = false;

        Cursor.lockState =
            CursorLockMode.Locked;
    }


    private void OnApplicationFocus(
        bool hasFocus)
    {
        if (!hasFocus ||
            !workCursorShown)
        {
            return;
        }

        // ѕри возвращении фокуса
        // загрузка всЄ равно имеет
        // абсолютный приоритет.
        if (LoadingManager
            .IsLoadingScreenBlockingPause())
        {
            ForceCursorHidden();
            return;
        }

        ApplyCurrentCursor();
    }
}
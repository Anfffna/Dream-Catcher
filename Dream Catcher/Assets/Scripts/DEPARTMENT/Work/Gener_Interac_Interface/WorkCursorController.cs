using System.Collections;
using UnityEngine;

public class WorkCursorController : MonoBehaviour
{
    [Header("Cursor")]

    public Texture2D defaultCursor;
    public Texture2D interactCursor;

    public Vector2 defaultCursorHotspot =
        Vector2.zero;

    public Vector2 interactCursorHotspot =
        Vector2.zero;


    [Header("Auto Copy")]

    [Tooltip(
        "Автоматически брать текстуры курсора " +
        "из TaskPanelController."
    )]
    public bool copyFromTaskPanelController =
        true;


    [Header("Защита курсора")]

    [Tooltip(
        "Повторно восстанавливать рабочий курсор, " +
        "когда окно игры снова получает фокус."
    )]
    [SerializeField]
    private bool restoreAfterFocus = true;

    [Tooltip(
        "Повторно восстанавливать курсор, " +
        "если мышь вышла за окно игры и вернулась."
    )]
    [SerializeField]
    private bool restoreAfterMouseReturn = true;


    private bool workCursorShown = false;
    private bool interactCursorActive = false;

    private bool applicationHasFocus = true;
    private bool pointerWasInsideGameWindow = true;

    private Coroutine focusRestoreRoutine;


    private void Awake()
    {
        pointerWasInsideGameWindow =
            IsPointerInsideGameWindow();
    }


    private void Update()
    {
        if (!workCursorShown)
            return;

        if (!restoreAfterMouseReturn)
            return;

        if (!applicationHasFocus)
            return;

        bool pointerInside =
            IsPointerInsideGameWindow();

        // Мышь была за пределами окна,
        // а теперь снова вошла.
        if (pointerInside &&
            !pointerWasInsideGameWindow)
        {
            ReapplyWorkCursor();
        }

        pointerWasInsideGameWindow =
            pointerInside;
    }


    private void OnApplicationFocus(
        bool hasFocus)
    {
        applicationHasFocus =
            hasFocus;

        if (!hasFocus)
            return;

        if (!restoreAfterFocus)
            return;

        if (!workCursorShown)
            return;

        StartFocusRestore();
    }


    private void OnApplicationPause(
        bool pauseStatus)
    {
        // Возврат приложения после системной паузы.
        if (pauseStatus)
            return;

        if (!restoreAfterFocus)
            return;

        if (!workCursorShown)
            return;

        StartFocusRestore();
    }


    private void StartFocusRestore()
    {
        if (focusRestoreRoutine != null)
        {
            StopCoroutine(
                focusRestoreRoutine
            );
        }

        focusRestoreRoutine =
            StartCoroutine(
                RestoreCursorAfterFocus()
            );
    }


    private IEnumerator RestoreCursorAfterFocus()
    {
        // Сразу пробуем вернуть состояние.
        ReapplyWorkCursor();

        // Ещё раз через кадр.
        // Это страхует случай, когда Unity или ОС
        // меняют состояние курсора после Focus-события.
        yield return null;

        if (workCursorShown)
        {
            ReapplyWorkCursor();
        }

        // И ещё один кадр для Editor / смены окна.
        yield return null;

        if (workCursorShown)
        {
            ReapplyWorkCursor();
        }

        pointerWasInsideGameWindow =
            IsPointerInsideGameWindow();

        focusRestoreRoutine = null;
    }


    private void FindCursorTextures()
    {
        if (!copyFromTaskPanelController)
            return;

        if (TaskPanelController.Instance == null)
            return;

        if (defaultCursor == null)
        {
            defaultCursor =
                TaskPanelController.Instance
                    .defaultCursor;

            defaultCursorHotspot =
                TaskPanelController.Instance
                    .defaultCursorHotspot;
        }

        if (interactCursor == null)
        {
            interactCursor =
                TaskPanelController.Instance
                    .interactCursor;

            interactCursorHotspot =
                TaskPanelController.Instance
                    .interactCursorHotspot;
        }
    }


    public void ShowWorkCursor()
    {
        FindCursorTextures();

        workCursorShown = true;
        interactCursorActive = false;

        ReapplyWorkCursor();

        pointerWasInsideGameWindow =
            IsPointerInsideGameWindow();
    }


    public void SetDefaultCursor()
    {
        FindCursorTextures();

        interactCursorActive = false;

        ApplyCursorTexture(
            defaultCursor,
            defaultCursorHotspot
        );
    }


    public void SetInteractCursor()
    {
        FindCursorTextures();

        interactCursorActive = true;

        ApplyCursorTexture(
            interactCursor,
            interactCursorHotspot
        );
    }


    public void HideAndLockGameplayCursor()
    {
        FindCursorTextures();

        workCursorShown = false;
        interactCursorActive = false;

        if (focusRestoreRoutine != null)
        {
            StopCoroutine(
                focusRestoreRoutine
            );

            focusRestoreRoutine = null;
        }

        Cursor.SetCursor(
            defaultCursor,
            defaultCursorHotspot,
            CursorMode.Auto
        );

        Cursor.visible = false;

        Cursor.lockState =
            CursorLockMode.Locked;
    }


    private void ReapplyWorkCursor()
    {
        if (!workCursorShown)
            return;

        FindCursorTextures();

        // Сначала освобождаем системный курсор.
        Cursor.lockState =
            CursorLockMode.None;

        if (interactCursorActive)
        {
            ApplyCursorTexture(
                interactCursor,
                interactCursorHotspot
            );
        }
        else
        {
            ApplyCursorTexture(
                defaultCursor,
                defaultCursorHotspot
            );
        }

        Cursor.visible = true;
    }


    private void ApplyCursorTexture(
        Texture2D texture,
        Vector2 hotspot)
    {
        Cursor.SetCursor(
            texture,
            hotspot,
            CursorMode.Auto
        );
    }


    private bool IsPointerInsideGameWindow()
    {
        Vector3 mousePosition =
            Input.mousePosition;

        return
            mousePosition.x >= 0f &&
            mousePosition.y >= 0f &&
            mousePosition.x < Screen.width &&
            mousePosition.y < Screen.height;
    }
}
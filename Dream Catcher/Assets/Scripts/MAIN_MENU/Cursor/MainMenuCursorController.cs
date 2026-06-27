using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuCursorController : MonoBehaviour
{
    [Header("Custom Cursors")]
    public Texture2D defaultCursor;
    public Texture2D interactCursor;

    [Header("Cursor Hotspot")]
    public Vector2 defaultCursorHotspot = Vector2.zero;
    public Vector2 interactCursorHotspot = Vector2.zero;

    void Start()
    {
        // Показываем курсор и разблокируем
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetDefaultCursor();

        // Находим все кнопки в сцене и добавляем EventTrigger для смены курсора
        AddCursorEventsToButtons();
    }

    void SetDefaultCursor()
    {
        Cursor.SetCursor(defaultCursor, defaultCursorHotspot, CursorMode.ForceSoftware);
    }

    void SetInteractCursor()
    {
        Cursor.SetCursor(interactCursor, interactCursorHotspot, CursorMode.ForceSoftware);
    }

    void AddCursorEventsToButtons()
    {
        // Находим все объекты с компонентом Button (включая дочерние)
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button btn in buttons)
        {
            // Добавляем EventTrigger, если его нет
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = btn.gameObject.AddComponent<EventTrigger>();

            // Создаём вход для PointerEnter
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { SetInteractCursor(); });
            trigger.triggers.Add(entryEnter);

            // Создаём вход для PointerExit
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { SetDefaultCursor(); });
            trigger.triggers.Add(entryExit);
        }
    }
}
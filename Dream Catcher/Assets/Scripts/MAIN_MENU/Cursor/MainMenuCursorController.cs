using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuCursorController :
    MonoBehaviour
{
    [Header("Курсоры")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D interactCursor;

    [Header("Активная точка курсора")]
    [SerializeField]
    private Vector2 defaultCursorHotspot =
        Vector2.zero;

    [SerializeField]
    private Vector2 interactCursorHotspot =
        Vector2.zero;

    private IEnumerator Start()
    {
        Cursor.SetCursor(
            defaultCursor,
            defaultCursorHotspot,
            CursorMode.ForceSoftware
        );

        // На всём протяжении загрузки каждый кадр
        // принудительно удерживаем курсор скрытым.
        while (LoadingManager.Instance != null &&
               LoadingManager.Instance.IsLoading)
        {
            Cursor.visible = false;
            Cursor.lockState =
                CursorLockMode.Locked;

            yield return null;
        }

        // Загрузочный Canvas уже закончил исчезновение.
        yield return null;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        SetDefaultCursor();
        AddCursorEventsToButtons();
    }

    private void SetDefaultCursor()
    {
        Cursor.SetCursor(
            defaultCursor,
            defaultCursorHotspot,
            CursorMode.ForceSoftware
        );
    }

    private void SetInteractCursor()
    {
        Cursor.SetCursor(
            interactCursor,
            interactCursorHotspot,
            CursorMode.ForceSoftware
        );
    }

    private void AddCursorEventsToButtons()
    {
        Button[] buttons =
            FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0;
             i < buttons.Length;
             i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            EventTrigger trigger =
                button.GetComponent<EventTrigger>();

            if (trigger == null)
            {
                trigger =
                    button.gameObject
                        .AddComponent<EventTrigger>();
            }

            if (trigger.triggers == null)
            {
                trigger.triggers =
                    new System.Collections.Generic
                        .List<EventTrigger.Entry>();
            }

            EventTrigger.Entry enterEntry =
                new EventTrigger.Entry();

            enterEntry.eventID =
                EventTriggerType.PointerEnter;

            enterEntry.callback.AddListener(
                _ => SetInteractCursor()
            );

            trigger.triggers.Add(
                enterEntry
            );

            EventTrigger.Entry exitEntry =
                new EventTrigger.Entry();

            exitEntry.eventID =
                EventTriggerType.PointerExit;

            exitEntry.callback.AddListener(
                _ => SetDefaultCursor()
            );

            trigger.triggers.Add(
                exitEntry
            );
        }
    }
}
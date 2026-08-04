using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIRaycastProbe : MonoBehaviour
{
    private readonly List<RaycastResult> results =
        new List<RaycastResult>();

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.f8Key.wasPressedThisFrame)
        {
            return;
        }

        if (EventSystem.current == null)
        {
            Debug.LogError(
                "UIRaycastProbe: EventSystem не найден."
            );

            return;
        }

        if (Mouse.current == null)
            return;

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current);

        pointerData.position =
            Mouse.current.position.ReadValue();

        results.Clear();

        EventSystem.current.RaycastAll(
            pointerData,
            results
        );

        if (results.Count == 0)
        {
            Debug.Log(
                "UIRaycastProbe: курсор не попал ни в один UI-объект."
            );

            return;
        }

        StringBuilder message =
            new StringBuilder();

        message.AppendLine(
            "UIRaycastProbe: объекты под курсором:"
        );

        for (int i = 0;
             i < results.Count;
             i++)
        {
            RaycastResult result =
                results[i];

            message.AppendLine(
                i +
                ": " +
                result.gameObject.name +
                " | Canvas: " +
                GetCanvasName(result.gameObject) +
                " | Module: " +
                result.module.GetType().Name
            );
        }

        Debug.Log(message.ToString());
    }

    private string GetCanvasName(
        GameObject target)
    {
        Canvas canvas =
            target.GetComponentInParent<Canvas>();

        return canvas != null
            ? canvas.name
            : "нет Canvas";
    }
}
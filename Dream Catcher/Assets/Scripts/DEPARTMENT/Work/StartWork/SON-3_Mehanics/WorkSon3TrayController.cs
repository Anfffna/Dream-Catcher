using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WorkSon3TrayController : MonoBehaviour
{
    [Header("Обводка")]
    [Tooltip("Показывать обучающую обводку в текущий день.")]
    [SerializeField] private bool showGuidanceOutlineThisDay = true;

    [SerializeField] private InteractionOutline interactionOutline;

    [Header("Размещение")]
    [SerializeField] private Transform snapPoint;

    [Header("Автопоиск")]
    [SerializeField]
    private string snapPointObjectName =
        "rack_SON-3_Position";

    [Header("События")]
    [Tooltip("Вызывается после полного появления SON-3 в лотке.")]
    [SerializeField] private UnityEvent onSon3FullyShown;

    private bool placementEnabled;
    private Coroutine outlineCoroutine;

    public Transform SnapPoint => snapPoint;
    public bool PlacementEnabled => placementEnabled;

    public event Action Son3FullyShown;

    private void Awake()
    {
        FindReferences();
        HideOutline();
    }

    private IEnumerator Start()
    {
        yield return null;

        if (!placementEnabled)
            HideOutline();
    }

    private void OnDisable()
    {
        if (outlineCoroutine != null)
        {
            StopCoroutine(outlineCoroutine);
            outlineCoroutine = null;
        }

        HideOutline();
    }

    public void EnablePlacement()
    {
        FindReferences();

        placementEnabled = true;

        if (!showGuidanceOutlineThisDay)
        {
            HideOutline();
            return;
        }

        if (outlineCoroutine != null)
            StopCoroutine(outlineCoroutine);

        outlineCoroutine =
            StartCoroutine(
                ShowOutlineNextFrame()
            );
    }

    public bool TryAcceptSon3(
        Son3DragController son3,
        Vector3 desiredWorldScale)
    {
        FindReferences();

        if (!placementEnabled ||
            son3 == null ||
            snapPoint == null)
        {
            return false;
        }

        placementEnabled = false;

        HideOutline();

        son3.AttachToTray(
            snapPoint,
            desiredWorldScale
        );

        return true;
    }

    public void NotifySon3FullyShown()
    {
        Son3FullyShown?.Invoke();
        onSon3FullyShown?.Invoke();
    }

    private IEnumerator ShowOutlineNextFrame()
    {
        yield return null;

        FindReferences();

        if (!placementEnabled ||
            !showGuidanceOutlineThisDay ||
            interactionOutline == null)
        {
            outlineCoroutine = null;
            yield break;
        }

        interactionOutline.enabled = true;

        if (!interactionOutline.gameObject.activeSelf)
        {
            interactionOutline
                .gameObject
                .SetActive(true);
        }

        interactionOutline
            .ForceRedrawOutline();

        outlineCoroutine = null;
    }

    private void HideOutline()
    {
        if (interactionOutline != null)
            interactionOutline.HideOutline();
    }

    private void FindReferences()
    {
        if (interactionOutline == null)
        {
            interactionOutline =
                GetComponentInChildren<InteractionOutline>(
                    true
                );
        }

        if (snapPoint != null)
            return;

        Transform[] transforms =
            GetComponentsInChildren<Transform>(
                true
            );

        for (int i = 0;
             i < transforms.Length;
             i++)
        {
            if (transforms[i].name ==
                snapPointObjectName)
            {
                snapPoint = transforms[i];
                return;
            }
        }
    }

    public void ConfigureForDay(
        int dayNumber)
    {
        showGuidanceOutlineThisDay =
            dayNumber == 1;

        if (!showGuidanceOutlineThisDay)
        {
            HideOutline();
            return;
        }

        if (placementEnabled)
        {
            if (outlineCoroutine != null)
                StopCoroutine(outlineCoroutine);

            outlineCoroutine =
                StartCoroutine(
                    ShowOutlineNextFrame()
                );
        }
    }
}
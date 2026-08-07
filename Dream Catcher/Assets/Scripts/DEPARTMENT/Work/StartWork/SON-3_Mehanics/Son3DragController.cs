using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Son3DragController : MonoBehaviour, IInteractable
{
    [Header("Ссылки")]
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private WorkSon3TrayController tray;

    [Header("Автопоиск")]
    [SerializeField]
    private string colliderObjectName =
        "SON3_InteractionCollider";

    [Header("Слои")]
    [SerializeField] private string defaultLayerName = "Default";
    [SerializeField] private string interactableLayerName = "Interactable";

    [Header("Исчезновение и появление")]
    [Tooltip("Время исчезновения SON-3 после клика.")]
    [SerializeField] private float disappearDuration = 0.5f;

    [Tooltip("Время появления SON-3 на подставке.")]
    [SerializeField] private float appearDuration = 0.5f;

    [Tooltip("Размер SON-3 в момент исчезновения.")]
    [Range(0f, 1f)]
    [SerializeField] private float hiddenScaleMultiplier = 0.65f;

    private bool interactionAvailable;
    private bool transitionInProgress;
    private bool placedInTray;

    private bool scriptControlsTransform;
    private Transform currentSnapPoint;

    private Transform originalParent;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;

    private bool originalTransformCaptured;
    private bool returnToOriginalEnabled;

    private static Son3DragController
        returnInteractionOwner;

    public static bool AnyReturnInteractionActive =>
        returnInteractionOwner != null;

    public bool ReturnToOriginalEnabled =>
        returnToOriginalEnabled;

    public event Action ReturnedToOriginalPlace;

    private Vector3 controlledWorldPosition;
    private Quaternion controlledWorldRotation;
    private Vector3 controlledLocalScale;

    private Renderer[] allRenderers;

    private readonly List<Material> fadeMaterials =
        new List<Material>();

    private readonly List<Color> originalColors =
        new List<Color>();

    private readonly List<int> colorPropertyIds =
        new List<int>();

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    public bool IsPlacedInTray => placedInTray;

    private void Awake()
    {
        FindReferences();
        CacheVisuals();
        CaptureOriginalTransform();

        SetRenderersEnabled(true);
        SetAlpha(1f);
        SetInteractionAvailable(false);
    }

    private void Reset()
    {
        FindReferences();
    }

    private void OnDisable()
    {
        returnToOriginalEnabled = false;

        if (returnInteractionOwner == this)
        {
            returnInteractionOwner = null;
        }
    }

    private void LateUpdate()
    {
        if (!scriptControlsTransform)
            return;

        if (placedInTray &&
            currentSnapPoint != null)
        {
            // Удерживаем SON-3 точно в позиции лотка.
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = controlledLocalScale;

            return;
        }

        // После отсоединения не даём Animator вернуть SON-3 к NPC.
        transform.SetPositionAndRotation(
            controlledWorldPosition,
            controlledWorldRotation
        );

        transform.localScale = controlledLocalScale;
    }

    public void PrepareForPlayer(
        Transform releasedItemsParent,
        WorkSon3TrayController targetTray)
    {
        FindReferences();
        CacheVisuals();

        tray = targetTray;

        // Отсоединяем SON-3 от NPC с сохранением мировой позиции.
        transform.SetParent(
            releasedItemsParent,
            true
        );

        controlledWorldPosition =
            transform.position;

        controlledWorldRotation =
            transform.rotation;

        controlledLocalScale =
            transform.localScale;

        currentSnapPoint = null;
        scriptControlsTransform = true;
        transitionInProgress = false;
        placedInTray = false;

        SetRenderersEnabled(true);
        SetAlpha(1f);
        SetInteractionAvailable(true);
    }

    public void Interact()
    {
        if (!interactionAvailable ||
            transitionInProgress)
        {
            return;
        }

        // Во время финального диалога
        // СОН-3 можно забрать обратно из лотка.
        if (returnToOriginalEnabled &&
            placedInTray)
        {
            StartCoroutine(
                ReturnToOriginalPlaceRoutine()
            );

            return;
        }

        // Обычный первый перенос:
        // от клиента в лоток.
        if (placedInTray)
            return;

        StartCoroutine(
            MoveToTrayRoutine()
        );
    }

    private void CaptureOriginalTransform()
    {
        if (originalTransformCaptured)
            return;

        originalParent =
            transform.parent;

        originalLocalPosition =
            transform.localPosition;

        originalLocalRotation =
            transform.localRotation;

        originalLocalScale =
            transform.localScale;

        originalTransformCaptured =
            true;
    }

    private IEnumerator MoveToTrayRoutine()
    {
        transitionInProgress = true;
        SetInteractionAvailable(false);

        Vector3 startScale =
            controlledLocalScale;

        Vector3 hiddenScale =
            startScale *
            hiddenScaleMultiplier;

        Vector3 normalWorldScale =
            transform.lossyScale;

        // Уменьшаем и скрываем SON-3 на старом месте.
        yield return AnimateVisual(
            startScale,
            hiddenScale,
            1f,
            0f,
            disappearDuration
        );

        SetRenderersEnabled(false);

        bool placedSuccessfully =
            tray != null &&
            tray.TryAcceptSon3(
                this,
                normalWorldScale
            );

        if (!placedSuccessfully)
        {
            // Возвращаем SON-3, если лоток не найден.
            controlledLocalScale =
                hiddenScale;

            transform.localScale =
                controlledLocalScale;

            SetAlpha(0f);
            SetRenderersEnabled(true);

            yield return AnimateVisual(
                hiddenScale,
                startScale,
                0f,
                1f,
                appearDuration
            );

            transitionInProgress = false;
            SetInteractionAvailable(true);

            yield break;
        }

        Vector3 trayScale =
            controlledLocalScale;

        Vector3 trayHiddenScale =
            trayScale *
            hiddenScaleMultiplier;

        controlledLocalScale =
            trayHiddenScale;

        transform.localScale =
            controlledLocalScale;

        SetAlpha(0f);
        SetRenderersEnabled(true);

        // Увеличиваем и показываем SON-3 в лотке.
        yield return AnimateVisual(
            trayHiddenScale,
            trayScale,
            0f,
            1f,
            appearDuration
        );

        controlledLocalScale =
            trayScale;

        transform.localScale =
            controlledLocalScale;

        SetAlpha(1f);

        transitionInProgress = false;
        placedInTray = true;

        // Сообщаем, что SON-3 полностью проявился в лотке.
        tray?.NotifySon3FullyShown();
    }

    public bool EnableReturnToOriginalPlace()
    {
        if (!originalTransformCaptured ||
            !placedInTray ||
            transitionInProgress)
        {
            return false;
        }

        returnToOriginalEnabled = true;

        returnInteractionOwner =
            this;

        SetInteractionAvailable(true);

        return true;
    }

    private IEnumerator
    ReturnToOriginalPlaceRoutine()
    {
        transitionInProgress = true;
        returnToOriginalEnabled = false;

        if (returnInteractionOwner == this)
        {
            returnInteractionOwner = null;
        }

        SetInteractionAvailable(false);

        Vector3 startScale =
            controlledLocalScale;

        Vector3 hiddenScale =
            startScale *
            hiddenScaleMultiplier;

        // Сначала устройство исчезает
        // прямо на лотке.
        yield return AnimateVisual(
            startScale,
            hiddenScale,
            1f,
            0f,
            disappearDuration
        );

        SetRenderersEnabled(false);

        // Теперь возвращаем его
        // именно к исходному родителю.
        transform.SetParent(
            originalParent,
            false
        );

        transform.localPosition =
            originalLocalPosition;

        transform.localRotation =
            originalLocalRotation;

        currentSnapPoint = null;
        placedInTray = false;

        // Больше не удерживаем устройство
        // в позиции рабочего лотка.
        scriptControlsTransform = false;

        Vector3 originalHiddenScale =
            originalLocalScale *
            hiddenScaleMultiplier;

        controlledLocalScale =
            originalHiddenScale;

        transform.localScale =
            originalHiddenScale;

        SetAlpha(0f);
        SetRenderersEnabled(true);

        // Устройство появляется уже
        // в своей исходной точке у NPC.
        yield return AnimateVisual(
            originalHiddenScale,
            originalLocalScale,
            0f,
            1f,
            appearDuration
        );

        controlledLocalScale =
            originalLocalScale;

        transform.localScale =
            originalLocalScale;

        SetAlpha(1f);

        transitionInProgress = false;

        ReturnedToOriginalPlace?.Invoke();
    }

    public void AttachToTray(
        Transform snapPoint,
        Vector3 desiredWorldScale)
    {
        if (snapPoint == null)
            return;

        currentSnapPoint = snapPoint;
        placedInTray = true;

        transform.SetParent(
            snapPoint,
            false
        );

        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;

        Vector3 parentScale =
            snapPoint.lossyScale;

        controlledLocalScale =
            new Vector3(
                DivideScale(
                    desiredWorldScale.x,
                    parentScale.x
                ),
                DivideScale(
                    desiredWorldScale.y,
                    parentScale.y
                ),
                DivideScale(
                    desiredWorldScale.z,
                    parentScale.z
                )
            );

        transform.localScale =
            controlledLocalScale;
    }

    private IEnumerator AnimateVisual(
        Vector3 fromScale,
        Vector3 toScale,
        float fromAlpha,
        float toAlpha,
        float duration)
    {
        if (duration <= 0f)
        {
            controlledLocalScale =
                toScale;

            transform.localScale =
                controlledLocalScale;

            SetAlpha(toAlpha);

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            controlledLocalScale =
                Vector3.Lerp(
                    fromScale,
                    toScale,
                    smoothT
                );

            transform.localScale =
                controlledLocalScale;

            SetAlpha(
                Mathf.Lerp(
                    fromAlpha,
                    toAlpha,
                    smoothT
                )
            );

            yield return null;
        }

        controlledLocalScale =
            toScale;

        transform.localScale =
            controlledLocalScale;

        SetAlpha(toAlpha);
    }

    public void SetInteractionAvailable(
        bool available)
    {
        FindReferences();

        interactionAvailable = available;

        if (interactionCollider == null)
            return;

        string layerName =
            available
                ? interactableLayerName
                : defaultLayerName;

        int targetLayer =
            LayerMask.NameToLayer(
                layerName
            );

        if (targetLayer < 0)
            return;

        interactionCollider.gameObject.layer =
            targetLayer;
    }

    private void CacheVisuals()
    {
        if (allRenderers != null &&
            allRenderers.Length > 0 &&
            fadeMaterials.Count > 0)
        {
            return;
        }

        allRenderers =
            GetComponentsInChildren<Renderer>(
                true
            );

        fadeMaterials.Clear();
        originalColors.Clear();
        colorPropertyIds.Clear();

        for (int i = 0;
             i < allRenderers.Length;
             i++)
        {
            Renderer currentRenderer =
                allRenderers[i];

            if (currentRenderer == null)
                continue;

            Material[] materials =
                currentRenderer.materials;

            for (int m = 0;
                 m < materials.Length;
                 m++)
            {
                Material material =
                    materials[m];

                if (material == null)
                    continue;

                int propertyId = -1;

                if (material.HasProperty(
                    BaseColorId))
                {
                    propertyId =
                        BaseColorId;
                }
                else if (material.HasProperty(
                    ColorId))
                {
                    propertyId =
                        ColorId;
                }

                if (propertyId < 0)
                    continue;

                fadeMaterials.Add(
                    material
                );

                originalColors.Add(
                    material.GetColor(
                        propertyId
                    )
                );

                colorPropertyIds.Add(
                    propertyId
                );
            }
        }
    }

    private void SetAlpha(
        float alpha)
    {
        for (int i = 0;
             i < fadeMaterials.Count;
             i++)
        {
            Material material =
                fadeMaterials[i];

            if (material == null)
                continue;

            Color color =
                originalColors[i];

            color.a *= alpha;

            material.SetColor(
                colorPropertyIds[i],
                color
            );
        }
    }

    private void SetRenderersEnabled(
        bool enabledState)
    {
        if (allRenderers == null)
            return;

        for (int i = 0;
             i < allRenderers.Length;
             i++)
        {
            if (allRenderers[i] != null)
            {
                allRenderers[i].enabled =
                    enabledState;
            }
        }
    }

    private void FindReferences()
    {
        if (interactionCollider != null)
            return;

        Collider[] colliders =
            GetComponentsInChildren<Collider>(
                true
            );

        // Ищем коллайдер SON-3 по точному имени.
        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            if (colliders[i].gameObject.name ==
                colliderObjectName)
            {
                interactionCollider =
                    colliders[i];

                return;
            }
        }

        // Запасной поиск, если имя объекта изменилось.
        if (colliders.Length > 0)
        {
            interactionCollider =
                colliders[0];
        }
    }

    private float DivideScale(
        float worldScale,
        float parentScale)
    {
        if (Mathf.Abs(parentScale) <
            0.0001f)
        {
            return worldScale;
        }

        return worldScale /
               parentScale;
    }
}
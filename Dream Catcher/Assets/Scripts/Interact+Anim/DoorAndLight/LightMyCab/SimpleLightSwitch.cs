using UnityEngine;
using System.Collections;

public class SimpleLightSwitch : MonoBehaviour, IInteractable
{
    [Header("Lights")]

    [Tooltip("Первый источник света.")]
    public Light roomLight1;

    [Tooltip("Второй источник света.")]
    public Light roomLight2;


    [Header("Первая лампа — Emission")]

    [Tooltip(
        "Материал первой лампы, у которого " +
        "включается и выключается Emission."
    )]
    public Material lampMaterial;

    [Tooltip(
        "Цвет Emission первой лампы " +
        "во включённом состоянии."
    )]
    [SerializeField]
    private Color emissionOnColor =
        new Color32(
            103,
            86,
            62,
            255
        );


    [Header("Вторая лампа — замена материала")]

    [Tooltip(
        "Renderer объекта второй лампы, " +
        "у которого нужно менять материал."
    )]
    [SerializeField]
    private Renderer secondLampRenderer;

    [Tooltip(
        "Материал второй лампы, когда свет включён."
    )]
    [SerializeField]
    private Material secondLampOnMaterial;

    [Tooltip(
        "Исходный материал второй лампы, " +
        "который возвращается при выключении света."
    )]
    [SerializeField]
    private Material secondLampOffMaterial;


    [Header("Interaction Layer")]

    public string interactableLayerName =
        "Interactable";

    public bool applyLayerToChildren =
        true;


    [Header("Outline")]

    public InteractionOutline interactionOutline;

    public bool showOutlineOnStart =
        true;

    public bool hideOutlineAfterFirstInteraction =
        true;


    [Header("Audio")]

    public AudioSource audioSource;


    private bool isOn = false;

    private bool outlineHiddenAfterInteraction =
        false;


    private void Awake()
    {
        SetInteractableLayer();
        FindOutline();
    }


    private void Start()
    {
        SetInteractableLayer();
        FindOutline();

        // Определяем исходное состояние
        // по первой лампе.
        if (roomLight1 != null)
        {
            isOn = roomLight1.enabled;
        }
        else if (roomLight2 != null)
        {
            isOn = roomLight2.enabled;
        }

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        // Сразу синхронизируем:
        // 1. оба источника света;
        // 2. Emission первой лампы;
        // 3. материал второй лампы.
        ApplyLightState();

        if (showOutlineOnStart)
        {
            StartCoroutine(
                ShowOutlineNextFrames()
            );
        }
    }


    private void OnEnable()
    {
        SetInteractableLayer();
    }


    public void Interact()
    {
        SetInteractableLayer();

        isOn = !isOn;

        ApplyLightState();

        if (audioSource != null)
        {
            audioSource.Play();
        }

        HideOutlineAfterFirstInteraction();
    }


    // =====================================================
    // ОБЩЕЕ СОСТОЯНИЕ СВЕТА
    // =====================================================

    private void ApplyLightState()
    {
        if (roomLight1 != null)
        {
            roomLight1.enabled =
                isOn;
        }

        if (roomLight2 != null)
        {
            roomLight2.enabled =
                isOn;
        }

        // Первая лампа.
        SetEmission(isOn);

        // Вторая лампа.
        SetSecondLampMaterial(isOn);
    }


    // =====================================================
    // ПЕРВАЯ ЛАМПА — EMISSION
    // =====================================================

    private void SetEmission(bool enabled)
    {
        if (lampMaterial == null)
            return;

        if (!lampMaterial.HasProperty(
                "_EmissionColor"))
        {
            return;
        }

        if (enabled)
        {
            lampMaterial.SetColor(
                "_EmissionColor",
                emissionOnColor
            );

            lampMaterial.EnableKeyword(
                "_EMISSION"
            );

            lampMaterial.globalIlluminationFlags &=
                ~MaterialGlobalIlluminationFlags
                    .EmissiveIsBlack;
        }
        else
        {
            lampMaterial.SetColor(
                "_EmissionColor",
                Color.black
            );

            lampMaterial.DisableKeyword(
                "_EMISSION"
            );

            lampMaterial.globalIlluminationFlags |=
                MaterialGlobalIlluminationFlags
                    .EmissiveIsBlack;
        }
    }


    // =====================================================
    // ВТОРАЯ ЛАМПА — ЗАМЕНА МАТЕРИАЛА
    // =====================================================

    private void SetSecondLampMaterial(
        bool enabled)
    {
        if (secondLampRenderer == null)
            return;

        Material targetMaterial =
            enabled
                ? secondLampOnMaterial
                : secondLampOffMaterial;

        if (targetMaterial == null)
            return;

        secondLampRenderer.sharedMaterial =
            targetMaterial;
    }


    // =====================================================
    // OUTLINE
    // =====================================================

    private IEnumerator ShowOutlineNextFrames()
    {
        yield return null;
        yield return null;

        FindOutline();

        if (interactionOutline != null &&
            !outlineHiddenAfterInteraction)
        {
            interactionOutline
                .ForceRedrawOutline();
        }
    }


    private void HideOutlineAfterFirstInteraction()
    {
        if (!hideOutlineAfterFirstInteraction)
            return;

        if (outlineHiddenAfterInteraction)
            return;

        FindOutline();

        if (interactionOutline != null)
        {
            interactionOutline.HideOutline();
        }

        outlineHiddenAfterInteraction =
            true;
    }


    private void FindOutline()
    {
        if (interactionOutline != null)
            return;

        interactionOutline =
            GetComponent<InteractionOutline>();

        if (interactionOutline == null)
        {
            interactionOutline =
                GetComponentInChildren
                    <InteractionOutline>(
                        true
                    );
        }

        if (interactionOutline == null)
        {
            interactionOutline =
                GetComponentInParent
                    <InteractionOutline>();
        }
    }


    // =====================================================
    // INTERACTION LAYER
    // =====================================================

    private void SetInteractableLayer()
    {
        int layer =
            LayerMask.NameToLayer(
                interactableLayerName
            );

        if (layer < 0)
        {
            Debug.LogWarning(
                "SimpleLightSwitch: слой не найден: " +
                interactableLayerName
            );

            return;
        }

        if (applyLayerToChildren)
        {
            SetLayerRecursively(
                gameObject,
                layer
            );
        }
        else
        {
            gameObject.layer =
                layer;
        }
    }


    private void SetLayerRecursively(
        GameObject obj,
        int layer)
    {
        obj.layer = layer;

        for (int i = 0;
             i < obj.transform.childCount;
             i++)
        {
            Transform child =
                obj.transform.GetChild(i);

            if (child != null)
            {
                SetLayerRecursively(
                    child.gameObject,
                    layer
                );
            }
        }
    }
}
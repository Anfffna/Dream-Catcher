using UnityEngine;
using System.Collections;

public class SimpleLightSwitch :
    MonoBehaviour,
    IInteractable
{
    [Header("Lights")]

    [Tooltip("ѕервый источник света.")]
    public Light roomLight1;

    [Tooltip("¬торой источник света.")]
    public Light roomLight2;


    [Header("Ћампы Ч Emission")]

    [Tooltip(
        "Renderer первой лампы. " +
        "÷вет Emission берЄтс€ из еЄ материала автоматически."
    )]
    [SerializeField]
    private Renderer lampRenderer1;

    [Tooltip(
        "Renderer второй лампы. " +
        "÷вет Emission берЄтс€ из еЄ материала автоматически."
    )]
    [SerializeField]
    private Renderer lampRenderer2;


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


    private MaterialPropertyBlock
        lampPropertyBlock1;

    private MaterialPropertyBlock
        lampPropertyBlock2;


    private Color originalEmissionColor1 =
        Color.white;

    private Color originalEmissionColor2 =
        Color.white;


    private static readonly int
        EmissionColorId =
            Shader.PropertyToID(
                "_EmissionColor"
            );


    private void Awake()
    {
        SetInteractableLayer();
        FindOutline();

        lampPropertyBlock1 =
            new MaterialPropertyBlock();

        lampPropertyBlock2 =
            new MaterialPropertyBlock();

        RememberEmissionColors();
    }


    private void Start()
    {
        SetInteractableLayer();
        FindOutline();

        // ќпредел€ем исходное состо€ние
        // по первому доступному источнику света.
        if (roomLight1 != null)
        {
            isOn =
                roomLight1.enabled;
        }
        else if (roomLight2 != null)
        {
            isOn =
                roomLight2.enabled;
        }

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        // —инхронизируем свет
        // и визуальный Emission ламп.
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
    // ќЅў≈≈ —ќ—“ќяЌ»≈ —¬≈“ј
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

        SetLampEmission(
            lampRenderer1,
            lampPropertyBlock1,
            originalEmissionColor1,
            isOn
        );

        SetLampEmission(
            lampRenderer2,
            lampPropertyBlock2,
            originalEmissionColor2,
            isOn
        );
    }


    // =====================================================
    // EMISSION
    // =====================================================

    private void RememberEmissionColors()
    {
        originalEmissionColor1 =
            GetOriginalEmissionColor(
                lampRenderer1
            );

        originalEmissionColor2 =
            GetOriginalEmissionColor(
                lampRenderer2
            );
    }


    private Color GetOriginalEmissionColor(
        Renderer targetRenderer)
    {
        if (targetRenderer == null)
            return Color.white;

        Material material =
            targetRenderer.sharedMaterial;

        if (material == null)
            return Color.white;

        if (!material.HasProperty(
                EmissionColorId))
        {
            return Color.white;
        }

        return material.GetColor(
            EmissionColorId
        );
    }


    private void SetLampEmission(
        Renderer targetRenderer,
        MaterialPropertyBlock propertyBlock,
        Color emissionOnColor,
        bool enabled)
    {
        if (targetRenderer == null)
            return;

        if (propertyBlock == null)
            return;

        targetRenderer.GetPropertyBlock(
            propertyBlock
        );

        propertyBlock.SetColor(
            EmissionColorId,
            enabled
                ? emissionOnColor
                : Color.black
        );

        targetRenderer.SetPropertyBlock(
            propertyBlock
        );
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
        obj.layer =
            layer;

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
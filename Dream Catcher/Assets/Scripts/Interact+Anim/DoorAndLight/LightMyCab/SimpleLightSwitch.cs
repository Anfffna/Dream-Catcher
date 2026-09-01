using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

public class SimpleLightSwitch :
    MonoBehaviour,
    IInteractable
{
    [Header("Light")]

    [Tooltip("Источник света.")]
    [FormerlySerializedAs("roomLight1")]
    [SerializeField]
    private Light roomLight;


    [Header("Лампа — Emission")]

    [Tooltip(
        "Renderer лампы. " +
        "Цвет Emission берётся из её материала автоматически."
    )]
    [FormerlySerializedAs("lampRenderer1")]
    [SerializeField]
    private Renderer lampRenderer;


    [Header("Анимация кнопки")]

    [Tooltip(
        "Animator кнопки выключателя. " +
        "Можно оставить пустым, если у этого выключателя нет анимации."
    )]
    [SerializeField]
    private Animator switchAnimator;


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
        lampPropertyBlock;

    private Color originalEmissionColor =
        Color.white;


    private static readonly int
        EmissionColorId =
            Shader.PropertyToID(
                "_EmissionColor"
            );


    /*
     * Названия Trigger фиксированные.
     * В Inspector их задавать не нужно.
     */
    private static readonly int
        VklTrigger =
            Animator.StringToHash(
                "Vkl"
            );

    private static readonly int
        ViklTrigger =
            Animator.StringToHash(
                "Vikl"
            );


    private void Awake()
    {
        SetInteractableLayer();
        FindOutline();

        lampPropertyBlock =
            new MaterialPropertyBlock();

        RememberEmissionColor();
    }


    private void Start()
    {
        SetInteractableLayer();
        FindOutline();

        /*
         * Исходное состояние выключателя
         * берём из самого Light.
         */
        if (roomLight != null)
        {
            isOn =
                roomLight.enabled;
        }

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        /*
         * Только синхронизируем свет
         * и Emission.
         *
         * Анимацию при старте НЕ запускаем.
         */
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

        PlaySwitchAnimation();

        if (audioSource != null)
        {
            audioSource.Play();
        }

        HideOutlineAfterFirstInteraction();
    }


    // =====================================================
    // СОСТОЯНИЕ СВЕТА
    // =====================================================

    private void ApplyLightState()
    {
        if (roomLight != null)
        {
            roomLight.enabled =
                isOn;
        }

        SetLampEmission(
            lampRenderer,
            lampPropertyBlock,
            originalEmissionColor,
            isOn
        );
    }


    // =====================================================
    // АНИМАЦИЯ КНОПКИ
    // =====================================================

    private void PlaySwitchAnimation()
    {
        if (switchAnimator == null)
            return;


        /*
         * На всякий случай очищаем
         * противоположный Trigger.
         */
        if (isOn)
        {
            switchAnimator.ResetTrigger(
                ViklTrigger
            );

            switchAnimator.SetTrigger(
                VklTrigger
            );
        }
        else
        {
            switchAnimator.ResetTrigger(
                VklTrigger
            );

            switchAnimator.SetTrigger(
                ViklTrigger
            );
        }
    }


    // =====================================================
    // EMISSION
    // =====================================================

    private void RememberEmissionColor()
    {
        originalEmissionColor =
            GetOriginalEmissionColor(
                lampRenderer
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
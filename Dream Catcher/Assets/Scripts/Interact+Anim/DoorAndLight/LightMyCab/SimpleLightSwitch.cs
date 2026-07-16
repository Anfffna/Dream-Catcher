using UnityEngine;
using System.Collections;

public class SimpleLightSwitch : MonoBehaviour, IInteractable
{
    [Header("Lights")]
    public Light roomLight1;
    public Light roomLight2;

    [Header("Interaction Layer")]
    public string interactableLayerName = "Interactable";
    public bool applyLayerToChildren = true;

    [Header("Outline")]
    public InteractionOutline interactionOutline;
    public bool showOutlineOnStart = true;
    public bool hideOutlineAfterFirstInteraction = true;

    [Header("Audio")]
    public AudioSource audioSource;

    private bool isOn = false;
    private bool outlineHiddenAfterInteraction = false;

    private void Awake()
    {
        SetInteractableLayer();
        FindOutline();
    }

    private void Start()
    {
        SetInteractableLayer();
        FindOutline();

        if (roomLight1 != null)
            isOn = roomLight1.enabled;
        else if (roomLight2 != null)
            isOn = roomLight2.enabled;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (showOutlineOnStart)
            StartCoroutine(ShowOutlineNextFrames());
    }

    private void OnEnable()
    {
        SetInteractableLayer();
    }

    public void Interact()
    {
        SetInteractableLayer();

        isOn = !isOn;

        if (roomLight1 != null)
            roomLight1.enabled = isOn;

        if (roomLight2 != null)
            roomLight2.enabled = isOn;

        if (audioSource != null)
            audioSource.Play();

        HideOutlineAfterFirstInteraction();
    }

    private IEnumerator ShowOutlineNextFrames()
    {
        // ∆дЄм, чтобы камера, GlobalCanvas и InteractionOutlineCanvas точно успели включитьс€.
        yield return null;
        yield return null;

        FindOutline();

        if (interactionOutline != null && !outlineHiddenAfterInteraction)
            interactionOutline.ForceRedrawOutline();
    }

    private void HideOutlineAfterFirstInteraction()
    {
        if (!hideOutlineAfterFirstInteraction)
            return;

        if (outlineHiddenAfterInteraction)
            return;

        FindOutline();

        if (interactionOutline != null)
            interactionOutline.HideOutline();

        outlineHiddenAfterInteraction = true;
    }

    private void FindOutline()
    {
        if (interactionOutline != null)
            return;

        interactionOutline = GetComponent<InteractionOutline>();

        if (interactionOutline == null)
            interactionOutline = GetComponentInChildren<InteractionOutline>(true);

        if (interactionOutline == null)
            interactionOutline = GetComponentInParent<InteractionOutline>();
    }

    private void SetInteractableLayer()
    {
        int layer = LayerMask.NameToLayer(interactableLayerName);

        if (layer < 0)
        {
            Debug.LogWarning("SimpleLightSwitch: слой не найден: " + interactableLayerName);
            return;
        }

        if (applyLayerToChildren)
            SetLayerRecursively(gameObject, layer);
        else
            gameObject.layer = layer;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            Transform child = obj.transform.GetChild(i);

            if (child != null)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Animators")]
    public List<Animator> doorAnimators = new List<Animator>();

    [Header("Animation Triggers")]
    public string openTrigger = "Open";
    public string closeTrigger = "Close";

    [Header("Animation Durations")]
    public float openAnimDuration = 1f;
    public float closeAnimDuration = 1f;

    [Header("Interaction")]
    public bool availableOnStart = true;
    public bool allowManualClose = false;
    public bool setLayerRecursively = true;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;
    public bool IsAvailable => isAvailable;

    private bool isOpen = false;
    private bool isAnimating = false;
    private bool isAvailable = false;

    private int defaultLayer;
    private int interactableLayer;

    void Start()
    {
        defaultLayer = LayerMask.NameToLayer("Default");
        interactableLayer = LayerMask.NameToLayer("Interactable");

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        SetDoorAvailable(availableOnStart);
    }

    public void SetDoorAvailable(bool state)
    {
        isAvailable = state;

        int targetLayer = state ? interactableLayer : defaultLayer;

        if (setLayerRecursively)
            SetLayerRecursive(transform, targetLayer);
        else
            gameObject.layer = targetLayer;
    }

    public void Interact()
    {
        if (isAnimating)
            return;

        if (isOpen)
        {
            if (allowManualClose)
                CloseDoor();

            return;
        }

        if (!isAvailable)
            return;

        OpenDoor();
    }

    public void OpenDoor()
    {
        if (isOpen || isAnimating)
            return;

        StartCoroutine(OpenRoutine());
    }

    public void CloseDoor()
    {
        if (!isOpen || isAnimating)
            return;

        StartCoroutine(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isAnimating = true;
        SetDoorAvailable(false);

        if (audioSource != null && openSound != null)
            audioSource.PlayOneShot(openSound);

        PlayTriggerOnAllAnimators(openTrigger, closeTrigger);

        yield return new WaitForSeconds(openAnimDuration);

        isOpen = true;
        isAnimating = false;

        // Если разрешено ручное закрытие,
        // оставляем открытую дверь доступной для взаимодействия.
        SetDoorAvailable(allowManualClose);
    }

    private IEnumerator CloseRoutine()
    {
        isAnimating = true;
        SetDoorAvailable(false);

        if (audioSource != null && closeSound != null)
            audioSource.PlayOneShot(closeSound);

        PlayTriggerOnAllAnimators(closeTrigger, openTrigger);

        yield return new WaitForSeconds(closeAnimDuration);

        isOpen = false;
        isAnimating = false;

        // После закрытия снова можно открыть.
        SetDoorAvailable(true);
    }

    private void PlayTriggerOnAllAnimators(string triggerToSet, string triggerToReset)
    {
        if (doorAnimators == null)
            return;

        foreach (Animator animator in doorAnimators)
        {
            if (animator == null)
                continue;

            animator.ResetTrigger(triggerToReset);
            animator.SetTrigger(triggerToSet);
        }
    }

    private void SetLayerRecursive(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursive(child, layer);
        }
    }
}
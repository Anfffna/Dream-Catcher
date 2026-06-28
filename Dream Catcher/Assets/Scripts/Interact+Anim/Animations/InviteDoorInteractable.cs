using UnityEngine;
using System.Collections;

public class InviteDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Animator")]
    public Animator doorAnimator;

    [Header("Animation Triggers")]
    public string openTrigger = "Open";
    public string closeTrigger = "Close";

    [Header("Animation Durations (если нет Animation Events)")]
    public float openAnimDuration = 1f;
    public float closeAnimDuration = 1f;

    [Header("Invite Door (сюжетный контроллер)")]
    public InviteDoor inviteDoor;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("Auto Close Triggers")]
    public Collider[] autoCloseTriggers;

    [Header("Auto Close")]
    public string playerTag = "Player";

    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;

    private bool isOpen = false;
    private bool isAnimating = false;
    private bool isAvailable = false; // доступна ли дл€ взаимодействи€

    void Start()
    {
        SetDoorAvailable(false);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void SetDoorAvailable(bool state)
    {
        isAvailable = state;
        gameObject.layer = LayerMask.NameToLayer(state ? "Interactable" : "Default");
    }

    public void Interact()
    {
        if (!isAvailable || isOpen || isAnimating) return;

        // ѕровер€ем, можно ли открыть дверь (добавленна€ логика)
        if (inviteDoor != null && !inviteDoor.CanOpenDoor())
        {
            inviteDoor.ShowDoorBlockDialogue();
            return;
        }

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isAnimating = true;
        SetDoorAvailable(false); // блокируем повторное нажатие

        // «вук открыти€
        if (audioSource != null && openSound != null)
            audioSource.PlayOneShot(openSound);

        if (doorAnimator != null)
            doorAnimator.SetTrigger(openTrigger);

        yield return new WaitForSeconds(openAnimDuration);

        isOpen = true;
        isAnimating = false;

        // ”ведомл€ем InviteDoor, что дверь открыта
        if (inviteDoor != null)
            inviteDoor.OnDoorOpened();
    }

    public void CloseDoor()
    {
        Debug.Log("CloseDoor() вызван, isOpen=" + isOpen + ", isAnimating=" + isAnimating);
        if (!isOpen || isAnimating) return;
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        isAnimating = true;
        SetDoorAvailable(false);

        // «вук закрыти€
        if (audioSource != null && closeSound != null)
            audioSource.PlayOneShot(closeSound);

        if (doorAnimator != null)
            doorAnimator.SetTrigger(closeTrigger);

        yield return new WaitForSeconds(closeAnimDuration);

        isOpen = false;
        isAnimating = false;

        // ѕосле закрыти€ делаем дверь снова доступной (если нужно дл€ повторного открыти€)
        SetDoorAvailable(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        // ѕровер€ем, что коллайдер, в который вошЄл игрок, есть в массиве autoCloseTriggers
        bool isInArray = false;
        if (autoCloseTriggers != null)
        {
            foreach (var trigger in autoCloseTriggers)
            {
                if (trigger == other)
                {
                    isInArray = true;
                    break;
                }
            }
        }

        if (isInArray && other.CompareTag(playerTag) && isOpen && !isAnimating)
        {
            CloseDoor();
        }
    }
}
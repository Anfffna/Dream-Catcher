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

    private bool isOpen = false;
    private bool isAnimating = false;
    private bool isAvailable = false; // доступна ли для взаимодействия

    void Start()
    {
        SetDoorAvailable(false);
    }

    public void SetDoorAvailable(bool state)
    {
        isAvailable = state;
        gameObject.layer = LayerMask.NameToLayer(state ? "Interactable" : "Default");
    }

    public void Interact()
    {
        if (!isAvailable || isOpen || isAnimating) return;
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isAnimating = true;
        SetDoorAvailable(false); // блокируем повторное нажатие

        if (doorAnimator != null)
            doorAnimator.SetTrigger(openTrigger);

        yield return new WaitForSeconds(openAnimDuration);

        isOpen = true;
        isAnimating = false;

        // Уведомляем InviteDoor, что дверь открыта
        if (inviteDoor != null)
            inviteDoor.OnDoorOpened();
    }

    public void CloseDoor()
    {
        if (!isOpen || isAnimating) return;
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        isAnimating = true;
        SetDoorAvailable(false);

        if (doorAnimator != null)
            doorAnimator.SetTrigger(closeTrigger);

        yield return new WaitForSeconds(closeAnimDuration);

        isOpen = false;
        isAnimating = false;

        // После закрытия делаем дверь снова доступной (если нужно для повторного открытия)
        // Если не нужно — закомментируй следующую строку
        SetDoorAvailable(true);
    }
}
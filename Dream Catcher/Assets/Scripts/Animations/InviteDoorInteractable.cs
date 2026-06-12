using UnityEngine;

public class InviteDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Animator")]
    public Animator doorAnimator;

    [Header("Interaction Collider")]
    public Collider interactionCollider;

    [Header("Invite Door")]
    public InviteDoor inviteDoor;

    private bool opened = false;

    void Start()
    {
        SetDoorAvailable(false);
    }

    public void SetDoorAvailable(bool state)
    {
        gameObject.layer = LayerMask.NameToLayer(
            state ? "Interactable" : "Default"
        );
    }

    public void Interact()
    {
        if (opened) return;

        opened = true;

        SetDoorAvailable(false);

        if (inviteDoor != null)
            inviteDoor.StopKnock();

        if (doorAnimator != null)
            doorAnimator.SetTrigger("Open");
    }
}
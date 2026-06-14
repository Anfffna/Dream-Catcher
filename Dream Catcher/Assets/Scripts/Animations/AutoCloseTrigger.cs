using UnityEngine;

public class AutoCloseTrigger : MonoBehaviour
{
    public InviteDoorInteractable door;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && door != null && door.IsOpen && !door.IsAnimating)
        {
            door.CloseDoor();
        }
    }
}
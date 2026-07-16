using UnityEngine;

public class MultiDoorAutoCloseTrigger : MonoBehaviour
{
    [Header("Door")]
    public MultiDoorInteractable door;

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Trigger Setup")]
    public bool makeColliderTriggerOnStart = true;

    private Collider triggerCollider;

    void Start()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null && makeColliderTriggerOnStart)
            triggerCollider.isTrigger = true;

        if (door == null)
            door = GetComponentInParent<MultiDoorInteractable>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCloseDoor(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCloseDoor(other);
    }

    private void TryCloseDoor(Collider other)
    {
        if (door == null)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (door.IsOpen && !door.IsAnimating)
        {
            door.CloseDoor();
        }
    }
}
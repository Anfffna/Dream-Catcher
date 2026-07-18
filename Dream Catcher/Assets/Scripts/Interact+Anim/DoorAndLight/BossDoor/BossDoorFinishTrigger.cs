using UnityEngine;

public class BossDoorFinishTrigger : MonoBehaviour
{
    [Header("Boss Door")]
    public BossDoor door;

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Trigger Setup")]
    public bool makeColliderTriggerOnStart = true;

    private Collider triggerCollider;

    private void Start()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogError(
                "BossDoorFinishTrigger: на объекте нет Collider.",
                this
            );

            enabled = false;
            return;
        }

        if (makeColliderTriggerOnStart)
            triggerCollider.isTrigger = true;

        if (door == null)
            door = GetComponentInParent<BossDoor>();

        if (door == null)
        {
            Debug.LogError(
                "BossDoorFinishTrigger: не назначена дверь BossDoor.",
                this
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryActivate(other);
    }

    private void TryActivate(Collider other)
    {
        if (door == null)
            return;

        if (!IsPlayer(other))
            return;

        // Этот метод сработает только после первого открытия.
        door.TryCloseFromFinishTrigger();
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        return other.transform.root.CompareTag(playerTag);
    }
}
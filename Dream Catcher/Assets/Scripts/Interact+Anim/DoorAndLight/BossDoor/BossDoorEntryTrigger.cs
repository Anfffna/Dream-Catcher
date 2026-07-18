using UnityEngine;

public class BossDoorEntryTrigger : MonoBehaviour
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
                "BossDoorEntryTrigger: на объекте нет Collider.",
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
                "BossDoorEntryTrigger: не назначена дверь BossDoor.",
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

        // До второго обычного открытия этот метод ничего не делает.
        door.TryCloseFromEntryTrigger();
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        return other.transform.root.CompareTag(playerTag);
    }
}
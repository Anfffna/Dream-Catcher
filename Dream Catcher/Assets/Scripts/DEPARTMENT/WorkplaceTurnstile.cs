using UnityEngine;

public enum WorkplaceTurnstileZoneType
{
    ForwardOpen,
    ForwardClose,
    BackwardOpen,
    BackwardClose
}

public enum WorkplaceTurnstileState
{
    Locked,
    WaitingForStart,
    ForwardOpened,
    BackwardOpened
}

public class WorkplaceTurnstile : MonoBehaviour
{
    private const string OPEN_TRIGGER = "Open";
    private const string CLOSE_TRIGGER = "Close";
    private const string OPEN2_TRIGGER = "Open2";
    private const string CLOSE2_TRIGGER = "Close2";

    [Header("Forward Direction Zones")]
    public BoxCollider forwardOpenBox;
    public BoxCollider forwardCloseBox;

    [Header("Backward Direction Zones")]
    public BoxCollider backwardOpenBox;
    public BoxCollider backwardCloseBox;

    [Header("Animator")]
    public Animator turnstileAnimator;

    [Header("Player Detection")]
    public bool requirePlayerTag = true;
    public string playerTag = "Player";

    [Header("Physics Helper")]
    public bool autoAddKinematicRigidbodyToZones = true;

    [Header("Debug")]
    public bool debugLogs = true;

    private WorkplaceTurnstileState state = WorkplaceTurnstileState.Locked;

    void Start()
    {
        if (turnstileAnimator == null)
            turnstileAnimator = GetComponent<Animator>();

        if (turnstileAnimator == null)
            turnstileAnimator = GetComponentInChildren<Animator>(true);

        SetupZone(forwardOpenBox, WorkplaceTurnstileZoneType.ForwardOpen);
        SetupZone(forwardCloseBox, WorkplaceTurnstileZoneType.ForwardClose);
        SetupZone(backwardOpenBox, WorkplaceTurnstileZoneType.BackwardOpen);
        SetupZone(backwardCloseBox, WorkplaceTurnstileZoneType.BackwardClose);

        LockTurnstile();
    }

    public void UnlockTurnstile()
    {
        if (state != WorkplaceTurnstileState.Locked)
            return;

        state = WorkplaceTurnstileState.WaitingForStart;

        // После ключей игрок может начать проход с любой стороны.
        SetZone(forwardOpenBox, true, true);
        SetZone(backwardOpenBox, true, true);

        // Close-зоны пока выключены, чтобы не было ложного закрытия.
        SetZone(forwardCloseBox, false, true);
        SetZone(backwardCloseBox, false, true);

        if (debugLogs)
            Debug.Log("Турникет разблокирован. Активны только входные зоны: ForwardOpen и BackwardOpen.");
    }

    public void LockTurnstile()
    {
        state = WorkplaceTurnstileState.Locked;

        // До ключей входные зоны физически блокируют проход.
        SetZone(forwardOpenBox, true, false);
        SetZone(backwardOpenBox, true, false);

        // Зоны закрытия до разблокировки не нужны.
        SetZone(forwardCloseBox, false, true);
        SetZone(backwardCloseBox, false, true);

        if (debugLogs)
            Debug.Log("Турникет заблокирован. ForwardOpen и BackwardOpen не Trigger и блокируют проход.");
    }

    public void HandleZoneEnter(WorkplaceTurnstileZoneType zoneType, Collider other)
    {
        if (!IsValidPlayer(other))
            return;

        switch (state)
        {
            case WorkplaceTurnstileState.Locked:
                return;

            case WorkplaceTurnstileState.WaitingForStart:
                HandleWaitingForStart(zoneType);
                break;

            case WorkplaceTurnstileState.ForwardOpened:
                HandleForwardOpened(zoneType);
                break;

            case WorkplaceTurnstileState.BackwardOpened:
                HandleBackwardOpened(zoneType);
                break;
        }
    }

    private void HandleWaitingForStart(WorkplaceTurnstileZoneType zoneType)
    {
        if (zoneType == WorkplaceTurnstileZoneType.ForwardOpen)
        {
            PlayOpenForward();

            state = WorkplaceTurnstileState.ForwardOpened;

            // Пока игрок идёт вперёд, активна только правильная зона закрытия.
            SetZone(forwardOpenBox, false, true);
            SetZone(backwardOpenBox, false, true);
            SetZone(backwardCloseBox, false, true);
            SetZone(forwardCloseBox, true, true);

            if (debugLogs)
                Debug.Log("Последовательность: ForwardOpen ? ждём ForwardClose.");

            return;
        }

        if (zoneType == WorkplaceTurnstileZoneType.BackwardOpen)
        {
            PlayOpenBackward();

            state = WorkplaceTurnstileState.BackwardOpened;

            // Пока игрок идёт назад, активна только правильная зона закрытия.
            SetZone(forwardOpenBox, false, true);
            SetZone(backwardOpenBox, false, true);
            SetZone(forwardCloseBox, false, true);
            SetZone(backwardCloseBox, true, true);

            if (debugLogs)
                Debug.Log("Последовательность: BackwardOpen ? ждём BackwardClose.");

            return;
        }
    }

    private void HandleForwardOpened(WorkplaceTurnstileZoneType zoneType)
    {
        if (zoneType != WorkplaceTurnstileZoneType.ForwardClose)
            return;

        PlayCloseForward();

        state = WorkplaceTurnstileState.WaitingForStart;

        // После закрытия снова можно начать с любой стороны.
        SetZone(forwardOpenBox, true, true);
        SetZone(backwardOpenBox, true, true);
        SetZone(forwardCloseBox, false, true);
        SetZone(backwardCloseBox, false, true);

        if (debugLogs)
            Debug.Log("Forward-проход завершён. Турникет снова ждёт вход с любой стороны.");
    }

    private void HandleBackwardOpened(WorkplaceTurnstileZoneType zoneType)
    {
        if (zoneType != WorkplaceTurnstileZoneType.BackwardClose)
            return;

        PlayCloseBackward();

        state = WorkplaceTurnstileState.WaitingForStart;

        // После закрытия снова можно начать с любой стороны.
        SetZone(forwardOpenBox, true, true);
        SetZone(backwardOpenBox, true, true);
        SetZone(forwardCloseBox, false, true);
        SetZone(backwardCloseBox, false, true);

        if (debugLogs)
            Debug.Log("Backward-проход завершён. Турникет снова ждёт вход с любой стороны.");
    }

    private void PlayOpenForward()
    {
        if (turnstileAnimator == null)
        {
            Debug.LogWarning("Animator турникета не назначен.");
            return;
        }

        turnstileAnimator.ResetTrigger(CLOSE_TRIGGER);
        turnstileAnimator.ResetTrigger(OPEN2_TRIGGER);
        turnstileAnimator.ResetTrigger(CLOSE2_TRIGGER);

        turnstileAnimator.SetTrigger(OPEN_TRIGGER);

        if (debugLogs)
            Debug.Log("Animator Trigger: Open");
    }

    private void PlayCloseForward()
    {
        if (turnstileAnimator == null)
        {
            Debug.LogWarning("Animator турникета не назначен.");
            return;
        }

        turnstileAnimator.ResetTrigger(OPEN_TRIGGER);
        turnstileAnimator.ResetTrigger(OPEN2_TRIGGER);
        turnstileAnimator.ResetTrigger(CLOSE2_TRIGGER);

        turnstileAnimator.SetTrigger(CLOSE_TRIGGER);

        if (debugLogs)
            Debug.Log("Animator Trigger: Close");
    }

    private void PlayOpenBackward()
    {
        if (turnstileAnimator == null)
        {
            Debug.LogWarning("Animator турникета не назначен.");
            return;
        }

        turnstileAnimator.ResetTrigger(OPEN_TRIGGER);
        turnstileAnimator.ResetTrigger(CLOSE_TRIGGER);
        turnstileAnimator.ResetTrigger(CLOSE2_TRIGGER);

        turnstileAnimator.SetTrigger(OPEN2_TRIGGER);

        if (debugLogs)
            Debug.Log("Animator Trigger: Open2");
    }

    private void PlayCloseBackward()
    {
        if (turnstileAnimator == null)
        {
            Debug.LogWarning("Animator турникета не назначен.");
            return;
        }

        turnstileAnimator.ResetTrigger(OPEN_TRIGGER);
        turnstileAnimator.ResetTrigger(CLOSE_TRIGGER);
        turnstileAnimator.ResetTrigger(OPEN2_TRIGGER);

        turnstileAnimator.SetTrigger(CLOSE2_TRIGGER);

        if (debugLogs)
            Debug.Log("Animator Trigger: Close2");
    }

    private bool IsValidPlayer(Collider other)
    {
        if (other == null)
            return false;

        if (!requirePlayerTag)
            return true;

        if (other.CompareTag(playerTag))
            return true;

        if (other.transform.root != null && other.transform.root.CompareTag(playerTag))
            return true;

        return false;
    }

    private void SetupZone(BoxCollider boxCollider, WorkplaceTurnstileZoneType zoneType)
    {
        if (boxCollider == null)
            return;

        WorkplaceTurnstileZoneProxy proxy =
            boxCollider.GetComponent<WorkplaceTurnstileZoneProxy>();

        if (proxy == null)
            proxy = boxCollider.gameObject.AddComponent<WorkplaceTurnstileZoneProxy>();

        proxy.owner = this;
        proxy.zoneType = zoneType;

        if (autoAddKinematicRigidbodyToZones)
        {
            Rigidbody rb = boxCollider.GetComponent<Rigidbody>();

            if (rb == null)
                rb = boxCollider.gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void SetZone(BoxCollider boxCollider, bool enabled, bool isTrigger)
    {
        if (boxCollider == null)
            return;

        boxCollider.enabled = enabled;
        boxCollider.isTrigger = isTrigger;
    }
}

public class WorkplaceTurnstileZoneProxy : MonoBehaviour
{
    public WorkplaceTurnstile owner;
    public WorkplaceTurnstileZoneType zoneType;

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null)
            owner.HandleZoneEnter(zoneType, other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (owner != null)
            owner.HandleZoneEnter(zoneType, other);
    }
}
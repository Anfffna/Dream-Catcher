using UnityEngine;
using UnityEngine.SceneManagement;

// Должен выполняться ПОСЛЕ скрипта поворота камеры в LateUpdate.
[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class PhoneCameraFollow : MonoBehaviour
{
    [Header("Калибровка")]
    [SerializeField] private PhoneCameraReference reference;

    [Tooltip("Можно оставить пустым: камера с тегом MainCamera найдётся при начале использования.")]
    [SerializeField] private Camera playerCamera;

    [Tooltip("Сохранять Scale самого Phone таким, каким он был при запуске, даже если клип содержит ключи Scale.")]
    [SerializeField] private bool preservePhoneScale = true;

    [Header("Состояния Animator — как на твоём скриншоте")]
    [SerializeField] private string idleState = "StayPhone";
    [SerializeField] private string takeFaceState = "TakePhone";
    [SerializeField] private string holdFaceState = "PhoneAtFace";
    [SerializeField] private string putFaceState = "PutPhone";
    [SerializeField] private string takeEarState = "CallBoss";
    [SerializeField] private string holdEarState = "PhoneAtEar";
    [SerializeField] private string putEarState = "NoCallBoss";

    private Animator animator;
    private Transform originalParent;
    private Transform carrier;
    private int originalSibling;
    private int idleHash, takeFaceHash, holdFaceHash, putFaceHash;
    private int takeEarHash, holdEarHash, putEarHash;
    private bool initialized;
    private bool hierarchySupported;
    private bool reportedError;
    private Vector3 initialPhoneScale;

    public PhoneCameraReference Reference => reference;
    public string IdleState => idleState;
    public string HoldFaceState => holdFaceState;
    public string HoldEarState => holdEarState;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        animator = GetComponent<Animator>();
        originalParent = transform.parent;
        originalSibling = transform.GetSiblingIndex();
        initialPhoneScale = transform.localScale;

        idleHash = Animator.StringToHash(idleState);
        takeFaceHash = Animator.StringToHash(takeFaceState);
        holdFaceHash = Animator.StringToHash(holdFaceState);
        putFaceHash = Animator.StringToHash(putFaceState);
        takeEarHash = Animator.StringToHash(takeEarState);
        holdEarHash = Animator.StringToHash(holdEarState);
        putEarHash = Animator.StringToHash(putEarState);

        hierarchySupported = HasUniformParents(originalParent);

        // Промежуточный родитель имеет локальные position=0, rotation=0, scale=1.
        // Поэтому SetParent(false) ЗДЕСЬ сохраняет прежнее пространство координат.
        // Родитель камеры здесь никогда не используется.
        GameObject rig = new GameObject(name + "_CameraFollow_Runtime");
        SceneManager.MoveGameObjectToScene(rig, gameObject.scene);
        carrier = rig.transform;
        carrier.SetParent(originalParent, false);
        carrier.SetSiblingIndex(originalSibling);
        transform.SetParent(carrier, false);

        ResolveCamera();
    }

    private static bool HasUniformParents(Transform parent)
    {
        for (Transform p = parent; p != null; p = p.parent)
        {
            Vector3 s = p.localScale;
            if (s.x <= 0f || s.y <= 0f || s.z <= 0f ||
                !Mathf.Approximately(s.x, s.y) || !Mathf.Approximately(s.x, s.z))
                return false;
        }
        return true;
    }

    private void ResolveCamera()
    {
        if (playerCamera == null) playerCamera = Camera.main;
    }

    public bool TryPrepare()
    {
        Initialize();
        ResolveCamera();
        string error = null;
        if (!isActiveAndEnabled)
            error = "Включи компонент PhoneCameraFollow.";
        else if (reference == null || !reference.Calibrated)
            error = "Создай Camera Reference и сохрани эталонный взгляд в Play Mode.";
        else if (playerCamera == null)
            error = "Не найдена камера игрока. Назначь Player Camera или тег MainCamera.";
        else if (!hierarchySupported)
            error = "У родителя Phone неодинаковый/отрицательный Scale. Нужен одинаковый положительный Scale по XYZ у всех родителей. Scale самого Phone менять не нужно.";
        else if (animator == null || animator.runtimeAnimatorController == null)
            error = "Не назначен Animator Controller на Phone.";
        else if (animator.applyRootMotion)
            error = "Выключи Apply Root Motion у Animator телефона.";
        else if (animator.updateMode == AnimatorUpdateMode.Fixed)
            error = "Поставь Animator Update Mode = Normal.";
        else if (!HasState(idleState) || !HasState(takeFaceState) ||
                 !HasState(holdFaceState) || !HasState(putFaceState) ||
                 !HasState(takeEarState) || !HasState(holdEarState) || !HasState(putEarState))
            error = "Имена состояний в PhoneCameraFollow не совпадают с состояниями первого слоя Animator.";

        if (error != null)
        {
            if (!reportedError) Debug.LogError("[Phone] " + error, this);
            reportedError = true;
            return false;
        }

        reportedError = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return true;
    }

    public bool HasState(string state)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        return animator.HasState(0, Animator.StringToHash(animator.GetLayerName(0) + "." + state));
    }

    public bool IsStableState(string state)
    {
        return animator != null && animator.enabled &&
               !animator.IsInTransition(0) &&
               animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(state);
    }

    private float StateWeight(AnimatorStateInfo state)
    {
        int hash = state.shortNameHash;
        if (hash == holdFaceHash || hash == holdEarHash) return 1f;
        float t = Mathf.Clamp01(state.normalizedTime);
        // Нулевая скорость коррекции на обоих концах клипа.
        float smooth = t * t * (3f - 2f * t);
        if (hash == takeFaceHash || hash == takeEarHash) return smooth;
        if (hash == putFaceHash || hash == putEarHash) return 1f - smooth;
        return 0f;
    }

    private void LateUpdate()
    {
        if (carrier == null) return;
        if (preservePhoneScale) transform.localScale = initialPhoneScale;
        if (!hierarchySupported || reference == null || !reference.Calibrated ||
            playerCamera == null || animator == null || !animator.enabled ||
            animator.runtimeAnimatorController == null)
        {
            ResetCarrier();
            return;
        }

        float weight = StateWeight(animator.GetCurrentAnimatorStateInfo(0));
        if (animator.IsInTransition(0))
        {
            float next = StateWeight(animator.GetNextAnimatorStateInfo(0));
            weight = Mathf.Lerp(weight, next,
                Mathf.Clamp01(animator.GetAnimatorTransitionInfo(0).normalizedTime));
        }

        if (weight <= 0f)
        {
            ResetCarrier();
            return;
        }

        Vector3 origin = originalParent != null ? originalParent.position : Vector3.zero;
        Quaternion basis = originalParent != null ? originalParent.rotation : Quaternion.identity;
        Vector3 referencePosition = originalParent != null
            ? originalParent.TransformPoint(reference.CameraPosition)
            : reference.CameraPosition;
        Quaternion referenceRotation = basis * reference.CameraRotation;

        Transform cameraTransform = playerCamera.transform;
        Quaternion delta = cameraTransform.rotation * Quaternion.Inverse(referenceRotation);
        Quaternion partialDelta = Quaternion.Slerp(Quaternion.identity, delta, weight);
        Vector3 virtualCamera = Vector3.Lerp(referencePosition, cameraTransform.position, weight);

        // Вращаем относительно эталонной КАМЕРЫ, а не мирового нуля.
        // При weight=1 поза телефона относительно камеры равна исходной позе
        // из клипа относительно эталонной камеры. Размер не копируется.
        carrier.SetPositionAndRotation(
            virtualCamera + partialDelta * (origin - referencePosition),
            partialDelta * basis);
    }

    private void ResetCarrier()
    {
        if (carrier == null) return;
        carrier.localPosition = Vector3.zero;
        carrier.localRotation = Quaternion.identity;
        // Scale carrier всегда равен 1.
    }

    public void ResetToDesk()
    {
        if (!initialized) return;
        ResetCarrier();
        if (animator == null || animator.runtimeAnimatorController == null || !HasState(idleState)) return;
        animator.enabled = true;
        animator.ResetTrigger("TakePhone");
        animator.ResetTrigger("PutPhone");
        animator.ResetTrigger("CallBoss");
        animator.ResetTrigger("NoCallBoss");
        animator.Play(Animator.StringToHash(animator.GetLayerName(0) + "." + idleState), 0, 0f);
        if (gameObject.activeInHierarchy) animator.Update(0f);
        if (preservePhoneScale) transform.localScale = initialPhoneScale;
    }

    // Вызывается редакторской кнопкой. Сам runtime asset на диск не пишет.
    public bool CaptureReference(out string error)
    {
        error = null;
        if (!Application.isPlaying)
        {
            error = "Сначала запусти Play Mode и сядь за рабочий стол.";
            return false;
        }
        Initialize();
        ResolveCamera();
        if (reference == null || playerCamera == null)
        {
            error = "Нужны Camera Reference и камера игрока.";
            return false;
        }
        if (animator == null || animator.runtimeAnimatorController == null ||
            animator.IsInTransition(0) || animator.GetCurrentAnimatorStateInfo(0).shortNameHash != idleHash)
        {
            error = "Калибруй, когда телефон лежит на столе в StayPhone.";
            return false;
        }
        Transform cameraTransform = playerCamera.transform;
        reference.SetReference(
            originalParent != null ? originalParent.InverseTransformPoint(cameraTransform.position) : cameraTransform.position,
            originalParent != null ? Quaternion.Inverse(originalParent.rotation) * cameraTransform.rotation : cameraTransform.rotation);
        reportedError = false;
        return true;
    }

    private void OnDisable()
    {
        ResetCarrier();
    }
}

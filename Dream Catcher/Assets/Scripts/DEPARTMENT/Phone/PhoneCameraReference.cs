using UnityEngine;

// Один asset на один телефон/рабочее место. Не хранит ссылки на Player.
[CreateAssetMenu(menuName = "Phone/Camera Reference", fileName = "PhoneCameraReference")]
public sealed class PhoneCameraReference : ScriptableObject
{
    [SerializeField, HideInInspector] private bool calibrated;
    [SerializeField, HideInInspector] private Vector3 cameraPosition;
    [SerializeField, HideInInspector] private Quaternion cameraRotation = Quaternion.identity;

    public bool Calibrated => calibrated;
    public Vector3 CameraPosition => cameraPosition;
    public Quaternion CameraRotation => cameraRotation;

    public void SetReference(Vector3 position, Quaternion rotation)
    {
        cameraPosition = position;
        cameraRotation = rotation;
        calibrated = true;
    }
}

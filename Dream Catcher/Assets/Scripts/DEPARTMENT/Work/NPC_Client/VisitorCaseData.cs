using UnityEngine;

[CreateAssetMenu(
    fileName = "Visitor_",
    menuName = "Департамент сна/Дело клиента"
)]
public class VisitorCaseData : ScriptableObject
{
    [Header("Основные данные")]

    [Tooltip("Уникальный технический идентификатор клиента.")]
    [SerializeField]
    private string visitorId =
        "visitor_001";

    [Tooltip("Имя клиента, отображаемое в его деле.")]
    [SerializeField] private string clientName;

    [Tooltip("Регистрационный номер записи сна.")]
    [SerializeField] private string registrationNumber;

    [Tooltip("Дата создания или регистрации записи.")]
    [SerializeField] private string recordDate;

    [Tooltip("Должность или место работы клиента.")]
    [SerializeField] private string occupation;

    public string VisitorId =>
        visitorId;

    public string ClientName =>
        clientName;

    public string RegistrationNumber =>
        registrationNumber;

    public string RecordDate =>
        recordDate;

    public string Occupation =>
        occupation;
}
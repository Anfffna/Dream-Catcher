using TMPro;
using UnityEngine;

public class ClientInfoPanelController :
    MonoBehaviour
{
    [Header("Текстовые поля")]

    [Tooltip("Текст с именем клиента.")]
    [SerializeField] private TMP_Text clientNameText;

    [Tooltip("Текст с регистрационным номером.")]
    [SerializeField] private TMP_Text registrationNumberText;

    [Tooltip("Текст с датой записи.")]
    [SerializeField] private TMP_Text recordDateText;

    [Tooltip("Текст с должностью или местом работы.")]
    [SerializeField] private TMP_Text occupationText;

    [Header("Текущий клиент")]

    [Tooltip("Данные клиента, которые временно используются для проверки.")]
    [SerializeField] private VisitorCaseData currentClient;

    [Tooltip("Автоматически показывать назначенного клиента при включении панели.")]
    [SerializeField]
    private bool showAssignedClientOnEnable =
        true;

    [Header("Подписи перед значениями")]

    [Tooltip("Текст перед именем клиента. Можно оставить пустым.")]
    [SerializeField]
    private string clientNamePrefix =
        "";

    [Tooltip("Текст перед регистрационным номером. Можно оставить пустым.")]
    [SerializeField]
    private string registrationNumberPrefix =
        "";

    [Tooltip("Текст перед датой записи. Можно оставить пустым.")]
    [SerializeField]
    private string recordDatePrefix =
        "";

    [Tooltip("Текст перед должностью. Можно оставить пустым.")]
    [SerializeField]
    private string occupationPrefix =
        "";

    public VisitorCaseData CurrentClient =>
        currentClient;

    private void Awake()
    {
        FindMissingReferences();
    }

    private void OnEnable()
    {
        if (showAssignedClientOnEnable)
            RefreshClientInformation();
    }

    public void ShowClient(
        VisitorCaseData clientData)
    {
        currentClient = clientData;

        RefreshClientInformation();
    }

    public void ClearClient()
    {
        currentClient = null;

        ClearTexts();
    }

    private void RefreshClientInformation()
    {
        FindMissingReferences();

        if (currentClient == null)
        {
            ClearTexts();
            return;
        }

        SetText(
            clientNameText,
            clientNamePrefix,
            currentClient.ClientName
        );

        SetText(
            registrationNumberText,
            registrationNumberPrefix,
            currentClient.RegistrationNumber
        );

        SetText(
            recordDateText,
            recordDatePrefix,
            currentClient.RecordDate
        );

        SetText(
            occupationText,
            occupationPrefix,
            currentClient.Occupation
        );
    }

    private void ClearTexts()
    {
        SetTextDirectly(
            clientNameText,
            ""
        );

        SetTextDirectly(
            registrationNumberText,
            ""
        );

        SetTextDirectly(
            recordDateText,
            ""
        );

        SetTextDirectly(
            occupationText,
            ""
        );
    }

    private void SetText(
        TMP_Text targetText,
        string prefix,
        string value)
    {
        if (targetText == null)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            targetText.text = "";
            return;
        }

        targetText.text =
            prefix + value;
    }

    private void SetTextDirectly(
        TMP_Text targetText,
        string value)
    {
        if (targetText != null)
            targetText.text = value;
    }

    private void FindMissingReferences()
    {
        if (clientNameText == null)
        {
            clientNameText =
                FindChildText(
                    "Name_Klient"
                );
        }

        if (registrationNumberText == null)
        {
            registrationNumberText =
                FindChildText(
                    "Reg_Number"
                );
        }

        if (recordDateText == null)
        {
            recordDateText =
                FindChildText(
                    "Date_Zapisi"
                );
        }

        if (occupationText == null)
        {
            occupationText =
                FindChildText(
                    "Dolzhnost_Reviz"
                );
        }
    }

    private TMP_Text FindChildText(
        string childName)
    {
        Transform child =
            transform.Find(
                childName
            );

        if (child == null)
            return null;

        return child.GetComponent<TMP_Text>();
    }
}
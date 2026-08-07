using TMPro;
using UnityEngine;

public class ClientInfoPanelController :
    MonoBehaviour
{
    [Header("Тексты направления")]

    [Tooltip("Текст с именем клиента.")]
    [SerializeField]
    private TMP_Text clientNameText;

    [Tooltip("Текст с регистрационным номером.")]
    [SerializeField]
    private TMP_Text registrationNumberText;

    [Tooltip("Текст с датой записи.")]
    [SerializeField]
    private TMP_Text recordDateText;

    [Tooltip("Текст с должностью клиента.")]
    [SerializeField]
    private TMP_Text occupationText;

    private VisitorCaseData currentClient;

    private VisitorCaseData.VisitorCaseVariant
        currentVariant;

    public VisitorCaseData CurrentClient =>
        currentClient;

    public VisitorCaseData.VisitorCaseVariant
        CurrentVariant =>
            currentVariant;

    private void Awake()
    {
        FindMissingReferences();
    }

    private void OnEnable()
    {
        FindMissingReferences();

        if (currentClient != null)
        {
            ShowClient(
                currentClient,
                currentVariant
            );
        }
    }

    public void ShowClient(
        VisitorCaseData clientData,
        VisitorCaseData.VisitorCaseVariant
            variantData)
    {
        currentClient = clientData;
        currentVariant = variantData;

        if (currentClient == null)
        {
            ClearClient();
            return;
        }

        CurrentClientContext.SetCurrentCase(
            currentClient,
            currentVariant
        );

        FindMissingReferences();

        SetText(
            clientNameText,
            currentClient.ClientName
        );

        SetText(
            registrationNumberText,
            currentClient
                .ResolveRegistrationNumber(
                    currentVariant
                )
        );

        SetText(
            recordDateText,
            currentClient
                .ResolveRecordDate(
                    currentVariant
                )
        );

        SetText(
            occupationText,
            currentClient.Occupation
        );
    }

    public void ClearClient()
    {
        currentClient = null;
        currentVariant = null;

        CurrentClientContext.Clear();

        SetText(clientNameText, "");
        SetText(registrationNumberText, "");
        SetText(recordDateText, "");
        SetText(occupationText, "");
    }

    private void SetText(
        TMP_Text targetText,
        string value)
    {
        if (targetText == null)
            return;

        targetText.text =
            string.IsNullOrWhiteSpace(value)
                ? ""
                : value;
    }

    private void FindMissingReferences()
    {
        if (clientNameText == null)
        {
            clientNameText =
                FindChildText(
                    "Name_klient"
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

        return child
            .GetComponent<TMP_Text>();
    }
}
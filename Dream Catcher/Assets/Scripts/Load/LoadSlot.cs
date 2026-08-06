using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class LoadSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Texts")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI timeText;

    [Header("Hover")]
    public Color hoverTextColor = new Color32(190, 212, 169, 255); // #BED4A9

    private Color defaultNameColor;
    private Color defaultTimeColor;
    private bool colorsSaved = false;

    private int saveIndex;
    private LoadPanelController panelController;
    private Button button;

    public void Setup(SaveData data, int index, LoadPanelController controller)
    {
        saveIndex = index;
        panelController = controller;

        if (nameText != null)
            nameText.text = data.saveName;

        if (timeText != null)
            timeText.text = data.dateTime;

        SaveDefaultColors();

        button = GetComponent<Button>();

        if (button == null)
            button = GetComponentInChildren<Button>(true);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogError(gameObject.name + ": на LoadSlotPrefab нет Button.");
        }
    }

    private void SaveDefaultColors()
    {
        if (colorsSaved)
            return;

        if (nameText != null)
            defaultNameColor = nameText.color;

        if (timeText != null)
            defaultTimeColor = timeText.color;

        colorsSaved = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHoverTextColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetDefaultTextColor();
    }

    private void SetHoverTextColor()
    {
        if (nameText != null)
            nameText.color = hoverTextColor;

        if (timeText != null)
            timeText.color = hoverTextColor;
    }

    private void SetDefaultTextColor()
    {
        if (nameText != null)
            nameText.color = defaultNameColor;

        if (timeText != null)
            timeText.color = defaultTimeColor;
    }

    private void OnDisable()
    {
        if (colorsSaved)
            SetDefaultTextColor();
    }

    public void OnClick()
    {
        if (panelController != null)
            panelController.OnLoadSlotClicked(saveIndex);
    }
}
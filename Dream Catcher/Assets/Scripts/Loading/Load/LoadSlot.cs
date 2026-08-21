using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class LoadSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Тексты")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI timeText;

    [Header("Hover")]
    public Color hoverTextColor =
        new Color32(190, 212, 169, 255); // #BED4A9

    [Header("Выбранное сохранение")]
    [Tooltip("Цвет плашки после клика. Она останется такой до выбора другой или нажатия Нет.")]
    public Color selectedTextColor =
        new Color32(190, 212, 169, 255); // #BED4A9

    private Color defaultNameColor;
    private Color defaultTimeColor;

    private bool colorsSaved = false;
    private bool isSelected = false;
    private bool isPointerInside = false;

    private int saveIndex;
    private LoadPanelController panelController;
    private Button button;


    public void Setup(
        SaveData data,
        int index,
        LoadPanelController controller)
    {
        saveIndex = index;
        panelController = controller;

        if (nameText != null)
            nameText.text = data.saveName;

        if (timeText != null)
            timeText.text = data.dateTime;

        SaveDefaultColors();

        isSelected = false;
        isPointerInside = false;

        ApplyVisualState();

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
            Debug.LogError(
                gameObject.name +
                ": на LoadSlotPrefab нет Button."
            );
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


    public void OnPointerEnter(
        PointerEventData eventData)
    {
        isPointerInside = true;

        ApplyVisualState();
    }


    public void OnPointerExit(
        PointerEventData eventData)
    {
        isPointerInside = false;

        ApplyVisualState();
    }


    public void SetSelected(bool selected)
    {
        isSelected = selected;

        ApplyVisualState();
    }


    private void ApplyVisualState()
    {
        if (!colorsSaved)
            return;

        /*
         * SELECTED имеет приоритет над Hover.
         *
         * Поэтому если мышка ушла с уже выбранной
         * плашки, её цвет не сбрасывается.
         */
        if (isSelected)
        {
            SetTextColor(selectedTextColor);
            return;
        }

        if (isPointerInside)
        {
            SetTextColor(hoverTextColor);
            return;
        }

        SetDefaultTextColor();
    }


    private void SetTextColor(Color color)
    {
        if (nameText != null)
            nameText.color = color;

        if (timeText != null)
            timeText.color = color;
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
        isSelected = false;
        isPointerInside = false;

        if (colorsSaved)
            SetDefaultTextColor();
    }


    public void OnClick()
    {
        if (panelController != null)
        {
            panelController.OnLoadSlotClicked(
                saveIndex,
                this
            );
        }
    }
}
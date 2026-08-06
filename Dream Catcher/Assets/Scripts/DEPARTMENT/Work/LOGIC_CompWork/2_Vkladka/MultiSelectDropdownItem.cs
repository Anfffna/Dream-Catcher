using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class MultiSelectDropdownItem :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Компоненты")]
    [Tooltip("Кнопка этого пункта.")]
    [SerializeField] private Button button;

    [Tooltip("Фоновое изображение всей плашки.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Текст с названием симптома.")]
    [SerializeField] private TMP_Text label;

    [Tooltip("Кружок слева, который появляется после выбора.")]
    [SerializeField] private GameObject selectionCircle;

    [Tooltip("Image кружка для изменения его цвета. Можно оставить пустым.")]
    [SerializeField] private Image selectionCircleImage;

    [Header("Обычное состояние")]
    [Tooltip("Светлый фон обычной плашки.")]
    [SerializeField]
    private Color normalBackgroundColor =
        new Color32(184, 178, 163, 255);

    [Tooltip("Тёмный цвет обычного текста.")]
    [SerializeField]
    private Color normalTextColor =
        new Color32(62, 63, 64, 255);

    [Header("Наведение без выбора")]
    [Tooltip("Тёмный фон при наведении на невыбранный пункт.")]
    [SerializeField]
    private Color hoverBackgroundColor =
        new Color32(119, 115, 106, 255);

    [Tooltip("Светлый текст при наведении на невыбранный пункт.")]
    [SerializeField]
    private Color hoverTextColor =
        new Color32(211, 204, 187, 255);

    [Header("Выбрано без наведения")]
    [Tooltip("Светлый фон выбранного пункта после ухода курсора.")]
    [SerializeField]
    private Color selectedIdleBackgroundColor =
        new Color32(184, 178, 163, 255);

    [Tooltip("Тёмный текст выбранного пункта после ухода курсора.")]
    [SerializeField]
    private Color selectedIdleTextColor =
        new Color32(62, 63, 64, 255);

    [Tooltip("Тёмный цвет точки выбранного пункта после ухода курсора.")]
    [SerializeField]
    private Color selectedIdleCircleColor =
        new Color32(62, 63, 64, 255);

    [Header("Выбрано при наведении")]
    [Tooltip("Серый фон выбранного пункта при наведении.")]
    [SerializeField]
    private Color selectedBackgroundColor =
        new Color32(119, 115, 106, 255);

    [Tooltip("Светлый текст выбранного пункта при наведении.")]
    [SerializeField]
    private Color selectedTextColor =
        new Color32(211, 204, 187, 255);

    [Tooltip("Светлый цвет точки выбранного пункта при наведении.")]
    [SerializeField]
    private Color selectedHoverCircleColor =
        new Color32(211, 204, 187, 255);

    private SelectDropdownController owner;

    private int itemIndex;
    private bool isSelected;
    private bool pointerInside;

    private void Awake()
    {
        FindReferences();
        RefreshVisual();
    }

    private void OnEnable()
    {
        pointerInside = false;
        RefreshVisual();
    }

    private void OnDisable()
    {
        pointerInside = false;
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleClick
            );
        }
    }

    public void Initialize(
        SelectDropdownController dropdownOwner,
        int index,
        string title)
    {
        FindReferences();

        owner = dropdownOwner;
        itemIndex = index;

        if (label != null)
            label.text = title;

        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleClick
            );

            button.onClick.AddListener(
                HandleClick
            );
        }

        isSelected = false;
        pointerInside = false;

        RefreshVisual();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshVisual();
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        pointerInside = true;
        RefreshVisual();
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        pointerInside = false;
        RefreshVisual();
    }

    private void HandleClick()
    {
        if (owner == null)
            return;

        owner.HandleItemPressed(
            itemIndex
        );
    }

    private void RefreshVisual()
    {
        if (selectionCircle != null)
        {
            selectionCircle.SetActive(
                isSelected
            );
        }

        // Выбранный пункт при наведении.
        if (isSelected &&
            pointerInside)
        {
            SetColors(
                selectedBackgroundColor,
                selectedTextColor
            );

            SetCircleColor(
                selectedHoverCircleColor
            );

            return;
        }

        // Выбранный пункт без наведения.
        if (isSelected)
        {
            SetColors(
                selectedIdleBackgroundColor,
                selectedIdleTextColor
            );

            SetCircleColor(
                selectedIdleCircleColor
            );

            return;
        }

        // Обычное наведение без выбора.
        if (pointerInside)
        {
            SetColors(
                hoverBackgroundColor,
                hoverTextColor
            );

            return;
        }

        // Обычный невыбранный пункт.
        SetColors(
            normalBackgroundColor,
            normalTextColor
        );
    }

    private void SetColors(
        Color backgroundColor,
        Color textColor)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color =
                backgroundColor;
        }

        if (label != null)
            label.color = textColor;
    }

    private void SetCircleColor(
        Color circleColor)
    {
        if (selectionCircleImage != null)
        {
            selectionCircleImage.color =
                circleColor;
        }
    }

    private void FindReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (backgroundImage == null)
        {
            backgroundImage =
                GetComponent<Image>();
        }

        if (label == null)
        {
            label =
                GetComponentInChildren<TMP_Text>(
                    true
                );
        }

        if (selectionCircleImage == null &&
            selectionCircle != null)
        {
            selectionCircleImage =
                selectionCircle
                    .GetComponent<Image>();

            if (selectionCircleImage == null)
            {
                selectionCircleImage =
                    selectionCircle
                        .GetComponentInChildren<Image>(
                            true
                        );
            }
        }
    }
}
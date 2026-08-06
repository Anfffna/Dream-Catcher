using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OsnovanieDropdownController :
    MonoBehaviour
{
    [Header("Кнопка раскрытия")]
    [Tooltip("Кнопка с надписью «Выберите основание».")]
    [SerializeField] private Button dropdownButton;

    [Tooltip("Текст внутри кнопки раскрытия.")]
    [SerializeField] private TMP_Text dropdownButtonText;

    [Tooltip("Постоянная надпись внутри закрытой плашки.")]
    [SerializeField]
    private string placeholderText =
        "Выберите основание";

    [Header("Стрелки")]
    [Tooltip("Стрелка закрытого списка.")]
    [SerializeField] private GameObject closedArrow;

    [Tooltip("Стрелка раскрытого списка.")]
    [SerializeField] private GameObject openedArrow;

    [Header("Раскрытый список")]
    [Tooltip("Общий объект раскрытого списка оснований.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("Объект Content внутри Scroll View и Viewport.")]
    [SerializeField] private RectTransform optionsContent;

    [Tooltip("Компонент Scroll Rect раскрытого списка.")]
    [SerializeField] private ScrollRect scrollRect;

    [Tooltip("Префаб одной строки. Можно использовать тот же префаб, что и у физических симптомов.")]
    [SerializeField] private MultiSelectDropdownItem optionPrefab;

    [Header("Выбранные основания")]
    [Tooltip("Текст, в котором через запятую показываются выбранные основания.")]
    [SerializeField] private TMP_Text selectedGroundsText;

    [Tooltip("Разделитель между выбранными основаниями.")]
    [SerializeField]
    private string selectedSeparator =
        ", ";

    [Header("Настройки выбора")]
    [Tooltip("Максимальное количество выбранных оснований. Ноль означает без ограничения.")]
    [SerializeField] private int maximumSelection;

    [Tooltip("Оставлять раскрытый список открытым после выбора.")]
    [SerializeField]
    private bool keepOpenAfterSelection =
        true;

    [Tooltip("Возвращать список наверх при каждом открытии.")]
    [SerializeField]
    private bool resetScrollOnOpen =
        true;

    [Header("Список оснований")]
    [Tooltip("Все доступные варианты оснований.")]
    [SerializeField]
    private List<string> options =
        new List<string>();

    private readonly List<MultiSelectDropdownItem>
        createdItems =
            new List<MultiSelectDropdownItem>();

    private readonly HashSet<int> selectedIndices =
        new HashSet<int>();

    private bool isOpen;

    private void Awake()
    {
        AddButtonListener();

        selectedIndices.Clear();

        BuildOptions();
        RefreshAllVisuals();
        CloseDropdown();
    }

    private void OnEnable()
    {
        RefreshAllVisuals();
    }

    private void OnDisable()
    {
        CloseDropdown();
    }

    private void OnDestroy()
    {
        RemoveButtonListener();
    }

    public void ToggleDropdown()
    {
        if (isOpen)
        {
            CloseDropdown();
        }
        else
        {
            OpenDropdown();
        }
    }

    public void OpenDropdown()
    {
        if (popupRoot == null ||
            optionsContent == null)
        {
            return;
        }

        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();

        isOpen = true;

        if (scrollRect != null)
        {
            scrollRect.content =
                optionsContent;
        }

        RefreshArrowState();

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder
            .ForceRebuildLayoutImmediate(
                optionsContent
            );

        if (resetScrollOnOpen &&
            scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition =
                1f;
        }
    }

    public void CloseDropdown()
    {
        isOpen = false;

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        RefreshArrowState();
    }

    public void HandleItemPressed(
        int itemIndex)
    {
        if (itemIndex < 0 ||
            itemIndex >= options.Count)
        {
            return;
        }

        if (selectedIndices.Contains(itemIndex))
        {
            selectedIndices.Remove(
                itemIndex
            );
        }
        else
        {
            bool selectionLimitReached =
                maximumSelection > 0 &&
                selectedIndices.Count >=
                maximumSelection;

            if (selectionLimitReached)
                return;

            selectedIndices.Add(
                itemIndex
            );
        }

        RefreshAllVisuals();

        if (!keepOpenAfterSelection)
        {
            CloseDropdown();
        }
    }

    public void ClearSelection()
    {
        selectedIndices.Clear();

        RefreshAllVisuals();
    }

    public List<string> GetSelectedValues()
    {
        List<string> selectedValues =
            new List<string>();

        for (int i = 0;
             i < options.Count;
             i++)
        {
            if (!selectedIndices.Contains(i))
                continue;

            selectedValues.Add(
                options[i]
            );
        }

        return selectedValues;
    }

    private void BuildOptions()
    {
        if (optionsContent == null ||
            optionPrefab == null)
        {
            return;
        }

        ClearOptionsContent();

        for (int i = 0;
             i < options.Count;
             i++)
        {
            MultiSelectDropdownItem item =
                Instantiate(
                    optionPrefab,
                    optionsContent,
                    false
                );

            item.gameObject.name =
                "Osnovanie_" + options[i];

            item.gameObject.SetActive(true);

            RectTransform itemRect =
                item.transform as RectTransform;

            if (itemRect != null)
            {
                itemRect.localScale =
                    Vector3.one;

                itemRect.localRotation =
                    Quaternion.identity;
            }

            TMP_Text itemLabel =
                item.GetComponentInChildren
                    <TMP_Text>(true);

            if (itemLabel != null)
            {
                itemLabel.text =
                    options[i];
            }

            Button itemButton =
                item.GetComponent<Button>();

            int capturedIndex = i;

            if (itemButton != null)
            {
                itemButton.onClick.AddListener(
                    () =>
                        HandleItemPressed(
                            capturedIndex
                        )
                );
            }

            item.SetSelected(false);

            createdItems.Add(item);
        }

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder
            .ForceRebuildLayoutImmediate(
                optionsContent
            );
    }

    private void ClearOptionsContent()
    {
        createdItems.Clear();

        if (optionsContent == null)
            return;

        for (int i =
                 optionsContent.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                optionsContent.GetChild(i);

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void RefreshAllVisuals()
    {
        for (int i = 0;
             i < createdItems.Count;
             i++)
        {
            MultiSelectDropdownItem item =
                createdItems[i];

            if (item == null)
                continue;

            item.SetSelected(
                selectedIndices.Contains(i)
            );
        }

        RefreshSelectedGroundsText();
        RefreshDropdownButtonText();
        RefreshArrowState();
    }

    private void RefreshSelectedGroundsText()
    {
        if (selectedGroundsText == null)
            return;

        selectedGroundsText.text =
            BuildSelectedText();
    }

    private string BuildSelectedText()
    {
        StringBuilder builder =
            new StringBuilder();

        for (int i = 0;
             i < options.Count;
             i++)
        {
            if (!selectedIndices.Contains(i))
                continue;

            if (builder.Length > 0)
            {
                builder.Append(
                    selectedSeparator
                );
            }

            builder.Append(
                options[i]
            );
        }

        return builder.ToString();
    }

    private void RefreshDropdownButtonText()
    {
        if (dropdownButtonText == null)
            return;

        dropdownButtonText.text =
            placeholderText;
    }

    private void RefreshArrowState()
    {
        if (closedArrow != null)
        {
            closedArrow.SetActive(
                !isOpen
            );
        }

        if (openedArrow != null)
        {
            openedArrow.SetActive(
                isOpen
            );
        }
    }

    private void AddButtonListener()
    {
        if (dropdownButton == null)
            return;

        dropdownButton.onClick.RemoveListener(
            ToggleDropdown
        );

        dropdownButton.onClick.AddListener(
            ToggleDropdown
        );
    }

    private void RemoveButtonListener()
    {
        if (dropdownButton == null)
            return;

        dropdownButton.onClick.RemoveListener(
            ToggleDropdown
        );
    }

    private void OnValidate()
    {
        maximumSelection =
            Mathf.Max(
                0,
                maximumSelection
            );
    }
}
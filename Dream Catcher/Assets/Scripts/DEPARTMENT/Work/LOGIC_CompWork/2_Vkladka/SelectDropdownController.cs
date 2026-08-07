using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectDropdownController :
    MonoBehaviour
{
    [Header("Включение физических симптомов")]
    [Tooltip("Главная галочка рядом с надписью «ФИЗ. СИМПТОМЫ».")]
    [SerializeField] private Toggle symptomsToggle;

    [Tooltip("Объект с плашкой «Выберите симптомы» и текстом выбранных симптомов.")]
    [SerializeField] private GameObject dropdownSectionRoot;

    [Header("Доступ после разговора")]
    [Tooltip("Контроллер вариативного диалога текущего посетителя.")]
    [SerializeField]
    private ClientQuestionDialogueController
    questionDialogueController;

    [Tooltip("Сообщение о необходимости поговорить с посетителем.")]
    [SerializeField]
    private SymptomRequirementWarningController
        requirementWarning;

    [Tooltip("Начинать игру с выключенной галочкой физических симптомов.")]
    [SerializeField] private bool startDisabled = true;

    [Tooltip("Очищать выбранные симптомы при снятии главной галочки.")]
    [SerializeField] private bool clearSelectionWhenDisabled = true;

    [Header("Кнопка раскрытия")]
    [Tooltip("Кнопка с надписью «Выберите симптомы».")]
    [SerializeField] private Button dropdownButton;

    [Tooltip("Текст внутри кнопки раскрытия.")]
    [SerializeField] private TMP_Text dropdownButtonText;

    [SerializeField]
    private string placeholderText =
        "Выберите симптомы";

    [Header("Стрелки")]
    [SerializeField] private GameObject closedArrow;
    [SerializeField] private GameObject openedArrow;

    [Header("Раскрытый список")]
    [Tooltip("Общий объект раскрытого списка.")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("Именно Content внутри Scroll View/Viewport.")]
    [SerializeField] private RectTransform optionsContent;

    [Tooltip("Компонент Scroll Rect на объекте Scroll View.")]
    [SerializeField] private ScrollRect scrollRect;

    [Tooltip("Префаб одной строки симптома из папки Project.")]
    [SerializeField] private MultiSelectDropdownItem optionPrefab;

    [Header("Выбранные симптомы")]
    [Tooltip("Текст под закрытой плашкой.")]
    [SerializeField] private TMP_Text selectedSymptomsText;

    [Tooltip("Разделитель между выбранными симптомами.")]
    [SerializeField]
    private string selectedSeparator =
        ", ";

    [Header("Настройки выбора")]
    [Tooltip("Ноль означает отсутствие ограничения.")]
    [SerializeField] private int maximumSelection;

    [Tooltip("Оставлять список открытым после выбора.")]
    [SerializeField]
    private bool keepOpenAfterSelection =
        true;

    [Tooltip("Возвращать список наверх при каждом открытии.")]
    [SerializeField]
    private bool resetScrollOnOpen =
        true;

    [Header("Названия симптомов")]
    [SerializeField]
    private List<string> options =
        new List<string>();

    private readonly List<MultiSelectDropdownItem>
        createdItems =
            new List<MultiSelectDropdownItem>();

    private readonly HashSet<int> selectedIndices =
        new HashSet<int>();

    private bool isOpen;
    private bool itemsCreated;

    private void Awake()
    {
        FindReferences();
        AddListeners();

        if (startDisabled &&
            symptomsToggle != null)
        {
            symptomsToggle.SetIsOnWithoutNotify(
                false
            );
        }

        selectedIndices.Clear();

        BuildOptions();
        RefreshAllVisuals();

        bool symptomsEnabled =
            symptomsToggle == null ||
            symptomsToggle.isOn;

        ApplySymptomsEnabled(
            symptomsEnabled
        );
    }

    private void LateUpdate()
    {
        if (!isOpen)
            return;

        // Ждём отпускания мыши.
        // К этому моменту UI-кнопка уже успела выбрать пункт.
        if (!Input.GetMouseButtonUp(0))
            return;

        // Клик по раскрытому списку или кнопке
        // ничего дополнительно не закрывает.
        if (IsPointerInsideDropdown())
            return;

        // Любой другой клик закрывает список.
        CloseDropdown();
    }

    private void OnEnable()
    {
        FindReferences();
        AddListeners();
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }

    public void ToggleDropdown()
    {
        if (symptomsToggle != null &&
            !symptomsToggle.isOn)
        {
            return;
        }

        if (isOpen)
            CloseDropdown();
        else
            OpenDropdown();
    }

    public void OpenDropdown()
    {
        if (popupRoot == null ||
            optionsContent == null)
        {
            return;
        }

        if (!itemsCreated)
            BuildOptions();

        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();

        isOpen = true;
        RefreshArrowState();

        if (scrollRect != null)
        {
            scrollRect.content =
                optionsContent;
        }

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder
            .ForceRebuildLayoutImmediate(
                optionsContent
            );

        if (resetScrollOnOpen &&
            scrollRect != null)
        {
            scrollRect
                .verticalNormalizedPosition =
                1f;
        }
    }

    public void CloseDropdown()
    {
        isOpen = false;

        if (popupRoot != null)
            popupRoot.SetActive(false);

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

        if (selectedIndices.Contains(
                itemIndex))
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
            CloseDropdown();
    }

    public void ClearSelection()
    {
        selectedIndices.Clear();
        RefreshAllVisuals();
    }

    public void ResetDropdown()
    {
        CloseDropdown();

        selectedIndices.Clear();

        if (symptomsToggle != null)
        {
            symptomsToggle
                .SetIsOnWithoutNotify(false);
        }

        if (dropdownSectionRoot != null)
        {
            dropdownSectionRoot.SetActive(false);
        }

        if (requirementWarning != null)
        {
            requirementWarning
                .HideImmediately();
        }

        RefreshAllVisuals();
    }

    public bool SymptomsEnabled =>
        symptomsToggle != null &&
        symptomsToggle.isOn;

    public List<string> GetSelectedValues()
    {
        List<string> values =
            new List<string>();

        for (int i = 0;
             i < options.Count;
             i++)
        {
            if (selectedIndices.Contains(i))
                values.Add(options[i]);
        }

        return values;
    }

    private void HandleSymptomsToggleChanged(
    bool enabled)
    {
        if (!enabled)
        {
            ApplySymptomsEnabled(false);
            return;
        }

        FindReferences();

        bool symptomsAvailable =
            questionDialogueController != null &&
            questionDialogueController
                .SymptomsDiscussed;

        if (!symptomsAvailable)
        {
            if (symptomsToggle != null)
            {
                symptomsToggle
                    .SetIsOnWithoutNotify(
                        false
                    );
            }

            ApplySymptomsEnabled(false);

            if (requirementWarning != null)
            {
                requirementWarning
                    .ShowWarning();
            }

            return;
        }

        ApplySymptomsEnabled(true);
    }

    private void ApplySymptomsEnabled(
        bool enabled)
    {
        if (!enabled)
        {
            CloseDropdown();

            if (clearSelectionWhenDisabled)
                ClearSelection();
        }

        if (dropdownSectionRoot != null)
        {
            dropdownSectionRoot.SetActive(
                enabled
            );
        }

        if (enabled)
            RefreshAllVisuals();
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
                "Symptom_" + options[i];

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

            item.Initialize(
                this,
                i,
                options[i]
            );

            createdItems.Add(item);
        }

        itemsCreated = true;
    }

    private void ClearOptionsContent()
    {
        createdItems.Clear();

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

        RefreshSelectedSymptomsText();
        RefreshDropdownButtonText();
        RefreshArrowState();
    }

    private void RefreshSelectedSymptomsText()
    {
        if (selectedSymptomsText == null)
            return;

        selectedSymptomsText.text =
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

            builder.Append(options[i]);
        }

        return builder.ToString();
    }

    private void RefreshDropdownButtonText()
    {
        if (dropdownButtonText != null)
        {
            dropdownButtonText.text =
                placeholderText;
        }
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

    private void AddListeners()
    {
        if (dropdownButton != null)
        {
            dropdownButton.onClick
                .RemoveListener(
                    ToggleDropdown
                );

            dropdownButton.onClick
                .AddListener(
                    ToggleDropdown
                );
        }

        if (symptomsToggle != null)
        {
            symptomsToggle.onValueChanged
                .RemoveListener(
                    HandleSymptomsToggleChanged
                );

            symptomsToggle.onValueChanged
                .AddListener(
                    HandleSymptomsToggleChanged
                );
        }
    }

    private void RemoveListeners()
    {
        if (dropdownButton != null)
        {
            dropdownButton.onClick
                .RemoveListener(
                    ToggleDropdown
                );
        }

        if (symptomsToggle != null)
        {
            symptomsToggle.onValueChanged
                .RemoveListener(
                    HandleSymptomsToggleChanged
                );
        }
    }

    private bool IsPointerInsideDropdown()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData =
            new PointerEventData(
                EventSystem.current
            )
            {
                position = Input.mousePosition
            };

        List<RaycastResult> results =
            new List<RaycastResult>();

        EventSystem.current.RaycastAll(
            pointerData,
            results
        );

        Transform popupTransform =
            popupRoot != null
                ? popupRoot.transform
                : null;

        Transform buttonTransform =
            dropdownButton != null
                ? dropdownButton.transform
                : null;

        for (int i = 0;
             i < results.Count;
             i++)
        {
            GameObject hitObject =
                results[i].gameObject;

            if (hitObject == null)
                continue;

            Transform hitTransform =
                hitObject.transform;

            if (IsSameOrChild(
                hitTransform,
                popupTransform))
            {
                return true;
            }

            if (IsSameOrChild(
                hitTransform,
                buttonTransform))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSameOrChild(
        Transform target,
        Transform parent)
    {
        if (target == null ||
            parent == null)
        {
            return false;
        }

        return
            target == parent ||
            target.IsChildOf(parent);
    }

    private void FindReferences()
    {
        if (questionDialogueController == null)
        {
            questionDialogueController =
                FindFirstObjectByType
                    <ClientQuestionDialogueController>(
                        FindObjectsInactive.Include
                    );
        }
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
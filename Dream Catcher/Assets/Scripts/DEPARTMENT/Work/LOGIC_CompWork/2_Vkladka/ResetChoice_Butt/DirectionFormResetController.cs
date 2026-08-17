using UnityEngine;
using UnityEngine.UI;

public class DirectionFormResetController :
    MonoBehaviour
{
    [Header("Кнопка сброса")]

    [Tooltip("Красная кнопка «Сбросить выбор».")]
    [SerializeField]
    private Button resetButton;

    [Header("Списки")]

    [Tooltip("Контроллер физических симптомов.")]
    [SerializeField]
    private SelectDropdownController
        symptomsDropdown;

    [Tooltip("Контроллер списка оснований.")]
    [SerializeField]
    private OsnovanieDropdownController
        groundsDropdown;

    [Header("Решение")]

    [Tooltip("Toggle Group трёх решений: отпустить, лечение и тюрьма.")]
    [SerializeField]
    private ToggleGroup decisionToggleGroup;

    private void Awake()
    {
        if (resetButton == null)
        {
            resetButton =
                GetComponent<Button>();
        }

        AddButtonListener();
    }

    private void Start()
    {
        // При первом открытии формы
        // ни одно решение не выбрано.
        ClearDecisionSelection();
    }

    private void OnDestroy()
    {
        RemoveButtonListener();
    }

    public void ResetForm()
    {
        if (symptomsDropdown != null)
        {
            symptomsDropdown.ResetDropdown();
        }

        if (groundsDropdown != null)
        {
            groundsDropdown.CloseDropdown();
            groundsDropdown.ClearSelection();
        }

        ClearDecisionSelection();
    }

    private void ClearDecisionSelection()
    {
        if (decisionToggleGroup == null)
            return;

        decisionToggleGroup.allowSwitchOff =
            true;

        decisionToggleGroup
            .SetAllTogglesOff(true);
    }

    private void AddButtonListener()
    {
        if (resetButton == null)
            return;

        resetButton.onClick.RemoveListener(
            ResetForm
        );

        resetButton.onClick.AddListener(
            ResetForm
        );
    }

    private void RemoveButtonListener()
    {
        if (resetButton == null)
            return;

        resetButton.onClick.RemoveListener(
            ResetForm
        );
    }
}
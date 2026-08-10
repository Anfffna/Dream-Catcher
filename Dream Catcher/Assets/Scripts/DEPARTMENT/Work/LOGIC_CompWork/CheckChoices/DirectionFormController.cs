using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DirectionFormController :
    MonoBehaviour
{
    [Header("Итоговое решение")]

    [Tooltip("Toggle кнопки «ОТПУСТИТЬ».")]
    [SerializeField]
    private Toggle releaseToggle;

    [Tooltip("Toggle кнопки «НАПРАВИТЬ НА ЛЕЧЕНИЕ».")]
    [SerializeField]
    private Toggle treatmentToggle;

    [Tooltip("Toggle кнопки «ПЕРЕДАТЬ ОРГАНАМ НАДЗОРА».")]
    [SerializeField]
    private Toggle prisonToggle;

    [Header("Физические симптомы")]

    [Tooltip("Контроллер выбора физических симптомов.")]
    [SerializeField]
    private SelectDropdownController
        symptomsDropdown;

    [Header("Основания")]

    [Tooltip("Контроллер выбора оснований.")]
    [SerializeField]
    private OsnovanieDropdownController
        groundsDropdown;

    [Header("Правила заполнения")]

    [Tooltip("Требовать хотя бы одно основание перед отправкой направления.")]
    [SerializeField]
    private bool requireAtLeastOneGround =
        true;

    private void Awake()
    {
        FindReferences();
    }

    public DirectionDecision GetSelectedDecision()
    {
        if (releaseToggle != null &&
            releaseToggle.isOn)
        {
            return DirectionDecision.Release;
        }

        if (treatmentToggle != null &&
            treatmentToggle.isOn)
        {
            return DirectionDecision.Treatment;
        }

        if (prisonToggle != null &&
            prisonToggle.isOn)
        {
            return DirectionDecision.Prison;
        }

        return DirectionDecision.None;
    }

    public bool GetPhysicalSymptomsEnabled()
    {
        FindReferences();

        return symptomsDropdown != null &&
               symptomsDropdown.SymptomsEnabled;
    }

    public List<string> GetSelectedSymptoms()
    {
        FindReferences();

        if (symptomsDropdown == null)
        {
            return new List<string>();
        }

        return symptomsDropdown
            .GetSelectedValues();
    }

    public List<string> GetSelectedGrounds()
    {
        FindReferences();

        if (groundsDropdown == null)
        {
            return new List<string>();
        }

        return groundsDropdown
            .GetSelectedValues();
    }

    public bool IsFormComplete()
    {
        FindReferences();

        // Одно из трёх итоговых решений
        // обязательно должно быть выбрано.
        if (GetSelectedDecision() ==
            DirectionDecision.None)
        {
            return false;
        }

        // Если игрок сам включил
        // физические симптомы,
        // он должен выбрать хотя бы один.
        if (GetPhysicalSymptomsEnabled())
        {
            List<string> symptoms =
                GetSelectedSymptoms();

            if (symptoms.Count == 0)
            {
                return false;
            }
        }

        // Основание является обязательным
        // полем направления.
        if (requireAtLeastOneGround)
        {
            List<string> grounds =
                GetSelectedGrounds();

            if (grounds.Count == 0)
            {
                return false;
            }
        }

        return true;
    }

    public void CloseOpenDropdowns()
    {
        FindReferences();

        if (symptomsDropdown != null)
        {
            symptomsDropdown
                .CloseDropdown();
        }

        if (groundsDropdown != null)
        {
            groundsDropdown
                .CloseDropdown();
        }
    }

    private void FindReferences()
    {
        if (symptomsDropdown == null)
        {
            symptomsDropdown =
                FindFirstObjectByType
                    <SelectDropdownController>(
                        FindObjectsInactive.Include
                    );
        }

        if (groundsDropdown == null)
        {
            groundsDropdown =
                FindFirstObjectByType
                    <OsnovanieDropdownController>(
                        FindObjectsInactive.Include
                    );
        }
    }
}
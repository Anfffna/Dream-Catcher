using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SavePanelController : MonoBehaviour
{
    private enum InputMode
    {
        None,
        NewSave,
        Overwrite
    }

    [Header("UI Elements")]
    public Transform saveContainer;
    public GameObject saveSlotPrefab;
    public GameObject newSavePrefab;
    public GameObject emptyPrefab;
    public GameObject inputSavePrefab;

    [Header("Overwrite Panel")]
    public GameObject overwritePanel;
    public Button yesButton;
    public Button noButton;

    private int selectedSaveIndex = -1;
    private InputMode inputMode = InputMode.None;

    private TMP_InputField activeInputField;
    private Button activeOkButton;

    private void Awake()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnYesClicked);
            yesButton.onClick.AddListener(OnYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(OnNoClicked);
            noButton.onClick.AddListener(OnNoClicked);
        }

        if (overwritePanel != null)
            overwritePanel.SetActive(false);
    }

    private void Update()
    {
        // Подтверждение сохранения клавишей Enter,
        // но только когда сейчас реально открыто поле ввода.
        if (activeInputField == null)
            return;

        if (inputMode == InputMode.None)
            return;

        if (!activeInputField.gameObject.activeInHierarchy)
            return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnOkClicked();
        }
    }

    public void PrepareSavePanel()
    {
        selectedSaveIndex = -1;
        inputMode = InputMode.None;

        activeInputField = null;
        activeOkButton = null;

        if (overwritePanel != null)
            overwritePanel.SetActive(false);

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (saveContainer == null)
        {
            Debug.LogError("SavePanelController: saveContainer не назначен.");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SavePanelController: SaveManager.Instance == null.");
            return;
        }

        ClearContainer();

        List<SaveData> saves = SaveManager.Instance.GetSaves();

        if (saves == null)
            saves = new List<SaveData>();

        // Верхняя строка:
        // либо поле ввода нового сохранения,
        // либо кнопка "Новое сохранение".
        if (inputMode == InputMode.NewSave)
        {
            CreateInputSlot();
        }
        else
        {
            if (saves.Count < SaveManager.Instance.maxSaves)
            {
                GameObject newSaveObj = Instantiate(newSavePrefab, saveContainer);
                AddButtonListener(newSaveObj, OnNewSaveClicked);
            }
        }

        // Если сохранений нет — показываем "Пусто" ниже первой строки.
        if (saves.Count == 0)
        {
            Instantiate(emptyPrefab, saveContainer);
            return;
        }

        // Существующие сохранения.
        // ВАЖНО:
        // SaveManager сейчас добавляет новые сохранения в конец списка.
        // Поэтому здесь мы отображаем список в обратном порядке,
        // чтобы новое сохранение визуально появлялось сверху.
        for (int i = saves.Count - 1; i >= 0; i--)
        {
            // Если это выбранное сохранение и игрок подтвердил перезапись,
            // вместо этой конкретной плашки показываем поле ввода.
            if (inputMode == InputMode.Overwrite && i == selectedSaveIndex)
            {
                CreateInputSlot();
                continue;
            }

            GameObject slotObj = Instantiate(saveSlotPrefab, saveContainer);

            SaveSlot slot = slotObj.GetComponent<SaveSlot>();

            if (slot == null)
                slot = slotObj.GetComponentInChildren<SaveSlot>(true);

            if (slot != null)
            {
                // Передаём настоящий индекс сохранения из SaveManager.
                // Даже если визуально список перевёрнут, перезапись будет работать правильно.
                slot.Setup(saves[i], i, this);
            }
            else
            {
                Debug.LogError("SaveSlotPrefab: на префабе нет компонента SaveSlot.");
            }
        }
    }

    private void ClearContainer()
    {
        activeInputField = null;
        activeOkButton = null;

        for (int i = saveContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(saveContainer.GetChild(i).gameObject);
        }
    }

    private void CreateInputSlot()
    {
        if (inputSavePrefab == null)
        {
            Debug.LogError("SavePanelController: inputSavePrefab не назначен.");
            return;
        }

        GameObject inputObj = Instantiate(inputSavePrefab, saveContainer);

        activeInputField = inputObj.GetComponentInChildren<TMP_InputField>(true);
        activeOkButton = inputObj.GetComponentInChildren<Button>(true);

        if (activeInputField != null)
        {
            activeInputField.text = "";

            // Поле для названия сохранения лучше держать однострочным,
            // чтобы Enter был подтверждением, а не переносом строки.
            activeInputField.lineType = TMP_InputField.LineType.SingleLine;

            // На случай, если TMP_InputField сам вызовет Submit по Enter.
            activeInputField.onSubmit.RemoveAllListeners();
            activeInputField.onSubmit.AddListener(OnInputSubmit);

            activeInputField.Select();
            activeInputField.ActivateInputField();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(activeInputField.gameObject);
        }
        else
        {
            Debug.LogError("InputSavePrefab: не найден TMP_InputField.");
        }

        if (activeOkButton != null)
        {
            activeOkButton.onClick.RemoveAllListeners();
            activeOkButton.onClick.AddListener(OnOkClicked);
        }
        else
        {
            Debug.LogError("InputSavePrefab: не найдена кнопка OK.");
        }
    }

    private void OnInputSubmit(string text)
    {
        if (activeInputField == null)
            return;

        if (inputMode == InputMode.None)
            return;

        OnOkClicked();
    }

    private void AddButtonListener(GameObject obj, UnityAction action)
    {
        Button button = obj.GetComponent<Button>();

        if (button == null)
            button = obj.GetComponentInChildren<Button>(true);

        if (button == null)
        {
            Debug.LogError(obj.name + ": на объекте нет Button.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void OnNewSaveClicked()
    {
        selectedSaveIndex = -1;
        inputMode = InputMode.NewSave;

        if (overwritePanel != null)
            overwritePanel.SetActive(false);

        RefreshUI();
    }

    public void OnSaveSlotClicked(int index)
    {
        selectedSaveIndex = index;
        inputMode = InputMode.None;

        if (overwritePanel != null)
            overwritePanel.SetActive(true);
    }

    public void OnYesClicked()
    {
        if (selectedSaveIndex < 0)
            return;

        inputMode = InputMode.Overwrite;

        if (overwritePanel != null)
            overwritePanel.SetActive(false);

        RefreshUI();
    }

    public void OnNoClicked()
    {
        selectedSaveIndex = -1;
        inputMode = InputMode.None;

        if (overwritePanel != null)
            overwritePanel.SetActive(false);

        RefreshUI();
    }

    public void OnOkClicked()
    {
        if (activeInputField == null)
            return;

        string saveName = activeInputField.text.Trim();

        if (string.IsNullOrEmpty(saveName))
        {
            Debug.LogWarning("Введите название сохранения.");
            return;
        }

        if (inputMode == InputMode.Overwrite && selectedSaveIndex >= 0)
        {
            SaveManager.Instance.OverwriteSave(selectedSaveIndex, saveName);
        }
        else if (inputMode == InputMode.NewSave)
        {
            SaveManager.Instance.CreateNewSave(saveName);
        }

        selectedSaveIndex = -1;
        inputMode = InputMode.None;

        activeInputField = null;
        activeOkButton = null;

        if (overwritePanel != null)
            overwritePanel.SetActive(false);

        RefreshUI();
    }
}
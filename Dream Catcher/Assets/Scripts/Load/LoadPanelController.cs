using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LoadPanelController : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform loadContainer;
    public GameObject loadSlotPrefab;
    public GameObject emptyPrefab;

    [Header("Confirm Load")]
    public GameObject loadConfirmPanel;
    public TextMeshProUGUI loadingText;
    public Button yesButton;
    public Button noButton;

    private int selectedLoadIndex = -1;

    private void Awake()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(OnYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(OnNoClicked);
        }

        if (loadConfirmPanel != null)
            loadConfirmPanel.SetActive(false);
    }

    public void PrepareLoadPanel()
    {
        selectedLoadIndex = -1;

        if (loadConfirmPanel != null)
            loadConfirmPanel.SetActive(false);

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (loadContainer == null)
        {
            Debug.LogError("LoadPanelController: loadContainer не назначен.");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("LoadPanelController: SaveManager.Instance == null.");
            return;
        }

        ClearContainer();

        List<SaveData> saves = SaveManager.Instance.GetSaves();

        if (saves == null || saves.Count == 0)
        {
            Instantiate(emptyPrefab, loadContainer);
            return;
        }

        // ВАЖНО:
        // В SavePanelController сохранения отображаются в обратном порядке,
        // чтобы новые были сверху.
        // Здесь делаем так же, чтобы загрузки были синхронизированы с сохранениями.
        for (int i = saves.Count - 1; i >= 0; i--)
        {
            GameObject slotObj = Instantiate(loadSlotPrefab, loadContainer);

            LoadSlot slot = slotObj.GetComponent<LoadSlot>();

            if (slot == null)
                slot = slotObj.GetComponentInChildren<LoadSlot>(true);

            if (slot != null)
            {
                // Передаём настоящий индекс из SaveManager.
                // Даже если визуально список перевёрнут, загрузится правильное сохранение.
                slot.Setup(saves[i], i, this);
            }
            else
            {
                Debug.LogError("LoadSlotPrefab: на префабе нет компонента LoadSlot.");
            }
        }
    }

    private void ClearContainer()
    {
        for (int i = loadContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(loadContainer.GetChild(i).gameObject);
        }
    }

    public void OnLoadSlotClicked(int index)
    {
        selectedLoadIndex = index;

        if (loadingText != null)
            loadingText.text = "Загрузить игру?";

        if (loadConfirmPanel != null)
            loadConfirmPanel.SetActive(true);
    }

    public void OnYesClicked()
    {
        if (selectedLoadIndex < 0)
        {
            Debug.LogWarning("LoadPanelController: сохранение для загрузки не выбрано.");
            return;
        }

        if (loadConfirmPanel != null)
            loadConfirmPanel.SetActive(false);

        if (PauseManager.Instance != null)
            PauseManager.Instance.HidePauseMenuBeforeLoading();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadSave(selectedLoadIndex);
        }
        else
        {
            Debug.LogError("LoadPanelController: SaveManager.Instance == null.");
        }
    }

    public void OnNoClicked()
    {
        selectedLoadIndex = -1;

        if (loadConfirmPanel != null)
            loadConfirmPanel.SetActive(false);
    }
}
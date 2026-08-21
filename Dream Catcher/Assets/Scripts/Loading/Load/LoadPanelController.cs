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

    /*
     * Конкретная визуальная плашка,
     * которую сейчас выбрал игрок.
     */
    private LoadSlot selectedLoadSlot;


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
        ResetSelectedLoadSlot();

        selectedLoadIndex = -1;

        if (loadConfirmPanel != null)
            loadConfirmPanel.SetActive(false);

        RefreshUI();
    }


    public void RefreshUI()
    {
        if (loadContainer == null)
        {
            Debug.LogError(
                "LoadPanelController: loadContainer не назначен."
            );

            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError(
                "LoadPanelController: SaveManager.Instance == null."
            );

            return;
        }

        ClearContainer();

        List<SaveData> saves =
            SaveManager.Instance.GetSaves();

        if (saves == null || saves.Count == 0)
        {
            Instantiate(
                emptyPrefab,
                loadContainer
            );

            return;
        }

        /*
         * Сохранения отображаются в обратном порядке,
         * чтобы новые находились сверху.
         */
        for (int i = saves.Count - 1; i >= 0; i--)
        {
            GameObject slotObj =
                Instantiate(
                    loadSlotPrefab,
                    loadContainer
                );

            LoadSlot slot =
                slotObj.GetComponent<LoadSlot>();

            if (slot == null)
            {
                slot =
                    slotObj.GetComponentInChildren<LoadSlot>(
                        true
                    );
            }

            if (slot != null)
            {
                /*
                 * Передаём настоящий индекс SaveManager.
                 */
                slot.Setup(
                    saves[i],
                    i,
                    this
                );
            }
            else
            {
                Debug.LogError(
                    "LoadSlotPrefab: на префабе нет компонента LoadSlot."
                );
            }
        }
    }


    private void ClearContainer()
    {
        /*
         * Все старые визуальные слоты сейчас
         * будут уничтожены, поэтому ссылка
         * на выбранный тоже больше не нужна.
         */
        selectedLoadSlot = null;

        for (int i = loadContainer.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                loadContainer
                    .GetChild(i)
                    .gameObject
            );
        }
    }


    /*
     * Оставляем старую версию метода,
     * чтобы случайно не сломать другие
     * существующие ссылки в проекте.
     */
    public void OnLoadSlotClicked(int index)
    {
        OnLoadSlotClicked(
            index,
            null
        );
    }


    public void OnLoadSlotClicked(
        int index,
        LoadSlot clickedSlot)
    {
        /*
         * Если до этого была выбрана другая
         * плашка — снимаем с неё Selected.
         */
        if (selectedLoadSlot != null &&
            selectedLoadSlot != clickedSlot)
        {
            selectedLoadSlot.SetSelected(false);
        }

        selectedLoadSlot = clickedSlot;
        selectedLoadIndex = index;

        /*
         * Новая нажатая плашка остаётся
         * выбранной даже после ухода мыши.
         */
        if (selectedLoadSlot != null)
            selectedLoadSlot.SetSelected(true);

        if (loadingText != null)
            loadingText.text = "Загрузить игру?";

        if (loadConfirmPanel != null)
            loadConfirmPanel.SetActive(true);
    }


    public void OnYesClicked()
    {
        if (selectedLoadIndex < 0)
        {
            Debug.LogWarning(
                "LoadPanelController: сохранение для загрузки не выбрано."
            );

            return;
        }

        if (loadConfirmPanel != null)
            loadConfirmPanel.SetActive(false);

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance
                .HidePauseMenuBeforeLoading();
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance
                .LoadSave(selectedLoadIndex);
        }
        else
        {
            Debug.LogError(
                "LoadPanelController: SaveManager.Instance == null."
            );
        }
    }


    public void OnNoClicked()
    {
        /*
         * Игрок отказался от загрузки:
         * снимаем постоянное выделение.
         */
        ResetSelectedLoadSlot();

        selectedLoadIndex = -1;

        if (loadConfirmPanel != null)
            loadConfirmPanel.SetActive(false);
    }


    private void ResetSelectedLoadSlot()
    {
        if (selectedLoadSlot != null)
        {
            selectedLoadSlot
                .SetSelected(false);
        }

        selectedLoadSlot = null;
    }
}
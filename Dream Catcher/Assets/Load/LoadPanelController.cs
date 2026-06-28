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

    public void PrepareLoadPanel()
    {
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

        for (int i = loadContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(loadContainer.GetChild(i).gameObject);
        }

        List<SaveData> saves = SaveManager.Instance.GetSaves();

        if (saves == null || saves.Count == 0)
        {
            Instantiate(emptyPrefab, loadContainer);
            return;
        }

        for (int i = 0; i < saves.Count; i++)
        {
            GameObject slotObj = Instantiate(loadSlotPrefab, loadContainer);

            LoadSlot slot = slotObj.GetComponent<LoadSlot>();

            if (slot == null)
                slot = slotObj.GetComponentInChildren<LoadSlot>(true);

            if (slot != null)
            {
                slot.Setup(saves[i], i, this);
            }
            else
            {
                Debug.LogError("LoadSlotPrefab: на префабе нет компонента LoadSlot.");
            }
        }
    }

    public void OnLoadSlotClicked(int index)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.LoadSave(index);
    }
}
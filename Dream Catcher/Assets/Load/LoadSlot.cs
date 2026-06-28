using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadSlot : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI timeText;

    private int saveIndex;
    private LoadPanelController panelController;

    public void Setup(SaveData data, int index, LoadPanelController controller)
    {
        saveIndex = index;
        panelController = controller;

        if (nameText != null)
            nameText.text = data.saveName;

        if (timeText != null)
            timeText.text = data.dateTime;

        Button button = GetComponent<Button>();

        if (button == null)
            button = GetComponentInChildren<Button>(true);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogError(gameObject.name + ": на LoadSlotPrefab нет Button.");
        }
    }

    public void OnClick()
    {
        if (panelController != null)
            panelController.OnLoadSlotClicked(saveIndex);
    }
}
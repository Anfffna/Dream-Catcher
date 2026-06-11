using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Lights")]
    public Light roomLight1;
    public Light roomLight2;

    [Header("Quest")]
    public QuestUIManager questUIManager;
    public string questIdToComplete = "turn_on_light";

    private bool isOn = false;
    private bool questCompleted = false;

    void Start()
    {
        if (roomLight1 != null)
            isOn = roomLight1.enabled;
        else if (roomLight2 != null)
            isOn = roomLight2.enabled;
    }

    public void Interact()
    {
        isOn = !isOn;

        if (roomLight1 != null)
            roomLight1.enabled = isOn;

        if (roomLight2 != null)
            roomLight2.enabled = isOn;

        if (isOn && !questCompleted)
        {
            questCompleted = true;

            if (questUIManager != null)
                questUIManager.CompleteQuest(questIdToComplete);
        }
    }
}
using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Lights")]
    public Light roomLight1;
    public Light roomLight2;

    [Header("Quest")]
    public QuestUIManager questUIManager;
    public string questIdToComplete = "turn_on_light";

    [Header("Invite Door")]
    public InviteDoor inviteDoor;

    [Header("Audio")]
    public AudioSource audioSource;

    private bool isOn = false;
    private bool sequenceStarted = false;

    void Start()
    {
        if (roomLight1 != null)
            isOn = roomLight1.enabled;
        else if (roomLight2 != null)
            isOn = roomLight2.enabled;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (questUIManager == null)
            questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();
    }

    public void Interact()
    {
        isOn = !isOn;

        if (roomLight1 != null)
            roomLight1.enabled = isOn;

        if (roomLight2 != null)
            roomLight2.enabled = isOn;

        if (audioSource != null)
            audioSource.Play();

        if (!isOn)
            return;

        if (sequenceStarted)
            return;

        if (questUIManager == null)
            questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        // «апускаем дверную цепочку только если задание реально активно.
        if (questUIManager != null && !questUIManager.IsQuestActive(questIdToComplete))
            return;

        sequenceStarted = true;

        if (inviteDoor != null)
            inviteDoor.StartInviteDoorSequence();
        else
            Debug.LogWarning("InviteDoor не назначен в LightSwitch");
    }
}
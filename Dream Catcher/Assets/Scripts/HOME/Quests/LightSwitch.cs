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
    public AudioSource audioSource;   // ссылка на AudioSource (клип уже в нём)

    private bool isOn = false;
    private bool questCompleted = false;

    void Start()
    {
        if (roomLight1 != null)
            isOn = roomLight1.enabled;
        else if (roomLight2 != null)
            isOn = roomLight2.enabled;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void Interact()
    {
        // Переключаем свет
        isOn = !isOn;

        if (roomLight1 != null)
            roomLight1.enabled = isOn;
        if (roomLight2 != null)
            roomLight2.enabled = isOn;

        // Воспроизводим звук переключения (при любом нажатии)
        if (audioSource != null)
            audioSource.Play();

        // Если свет включён и задание ещё не завершено
        if (isOn && !questCompleted)
        {
            questCompleted = true;

            if (questUIManager != null)
                questUIManager.CompleteQuest(questIdToComplete);

            if (inviteDoor != null)
                inviteDoor.StartInviteDoorSequence();
            else
                Debug.LogWarning("InviteDoor не назначен в LightSwitch");
        }
    }
}
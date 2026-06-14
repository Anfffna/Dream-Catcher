using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InviteDoor : MonoBehaviour
{
    [Header("Audio (стук)")]
    public AudioSource audioSource;
    public AudioClip knockClip;
    public bool loopKnock = true;

    [Header("Timing")]
    public float delayAfterLight = 3f;
    public float delayAfterKnock = 1.5f;

    [Header("Quest")]
    public QuestUIManager questUIManager;
    public string questIdToAdd = "find_the_key";   // ID задани€, которое по€витс€

    [Header("Dialogues")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> ggLines = new List<DialogueManager.DialogueLine>();   // после стука, до открыти€
    public List<DialogueManager.DialogueLine> workerLines = new List<DialogueManager.DialogueLine>(); // после открыти€ двери

    [Header("Door Block Dialogue")]
    public List<DialogueManager.DialogueLine> doorLines = new List<DialogueManager.DialogueLine>();

    [Header("Door")]
    public InviteDoorInteractable doorInteractable;

    [Header("NPC")]
    public GameObject npcToHide;

    [Header("Items Activator")]
    public HomeItemsActivator homeItemsActivator;

    private bool started = false;
    private bool waitingForOpen = false; // чтобы не повторно обработать открытие

    public void StartInviteDoorSequence()
    {
        if (started) return;
        started = true;
        StartCoroutine(InviteRoutine());
    }

    private IEnumerator InviteRoutine()
    {
        if (doorInteractable != null)
            doorInteractable.SetDoorAvailable(false);

        yield return new WaitForSeconds(delayAfterLight);

        StartKnock();

        yield return new WaitForSeconds(delayAfterKnock);

        // ѕервый диалог (ggLines)
        if (dialogueManager != null && ggLines != null && ggLines.Count > 0)
        {
            dialogueManager.StartDialogue(ggLines);
            yield return new WaitUntil(() => dialogueManager.DialogueActive == false);
        }

        // ѕосле первого диалога дверь становитс€ интерактивной (игрок может открыть)
        if (doorInteractable != null)
            doorInteractable.SetDoorAvailable(true);
    }

    // Ётот метод вызываетс€ из InviteDoorInteractable после завершени€ анимации открыти€
    public void OnDoorOpened()
    {
        if (waitingForOpen) return;
        waitingForOpen = true;

        StopKnock();

        // «апускаем второй диалог (worker)
        if (dialogueManager != null && workerLines != null && workerLines.Count > 0)
        {
            StartCoroutine(DialogueThenClose());
        }
        else
        {
            // ≈сли диалога нет, просто закрываем дверь
            if (doorInteractable != null)
                doorInteractable.CloseDoor();
        }
    }

    private IEnumerator DialogueThenClose()
    {
        dialogueManager.StartDialogue(workerLines, true);   // движение заблокировано
        yield return new WaitUntil(() => dialogueManager.DialogueActive == false);

        // «акрываем дверь
        if (doorInteractable != null)
            doorInteractable.CloseDoor();

        // ∆дЄм, пока дверь закроетс€ (используем длительность из Interactable)
        if (npcToHide != null && doorInteractable != null)
        {
            yield return new WaitForSeconds(doorInteractable.closeAnimDuration);
            npcToHide.SetActive(false);
        }

        // ƒобавл€ем новое задание
        if (questUIManager != null && !string.IsNullOrEmpty(questIdToAdd))
        {
            questUIManager.AddQuest(questIdToAdd);
        }

        // ? јктивируем домашние предметы
        if (homeItemsActivator != null)
        {
            homeItemsActivator.ActivateItems();
        }
    }

    public bool CanOpenDoor()
    {
        // ≈сли задание "find_the_key" ещЄ не выполнено (активно), то открывать нельз€
        if (questUIManager != null && questUIManager.IsQuestActive(questIdToAdd))
            return false;
        return true;
    }

    public void ShowDoorBlockDialogue()
    {
        if (dialogueManager != null && doorLines != null && doorLines.Count > 0)
        {
            dialogueManager.StartDialogue(doorLines); // не блокируем движение (можно false)
        }
    }

    private void StartKnock()
    {
        if (audioSource == null || knockClip == null) return;
        audioSource.clip = knockClip;
        audioSource.loop = loopKnock;
        audioSource.Play();
    }

    public void StopKnock()
    {
        if (audioSource == null) return;
        audioSource.Stop();
        audioSource.loop = false;
    }
}
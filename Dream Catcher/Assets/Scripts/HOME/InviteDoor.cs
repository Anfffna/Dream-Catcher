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

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string playerObjectName = "Player";
    public string dialogueManagerObjectName = "DialogueManager";
    public string questUIManagerObjectName = "QuestUIManager";

    [Header("Player")]
    public PlayerController playerController;

    [Header("Quest Complete")]
    public string questIdToComplete = "turn_on_light";

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

    void Awake()
    {
        FindReferences();
    }

    void Start()
    {
        FindReferences();
    }

    public void StartInviteDoorSequence()
    {
        FindReferences();

        if (started) return;
        started = true;
        StartCoroutine(InviteRoutine());
    }

    private IEnumerator InviteRoutine()
    {
        FindReferences();

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
        FindReferences();

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
        FindReferences();
        // ќтключаем звуки шагов перед блокировкой движени€
        if (playerController != null && playerController.footstepSource != null)
        {
            playerController.footstepSource.Stop();          // остановить, если играет
            playerController.footstepSource.enabled = false; // отключить источник
        }

        dialogueManager.StartDialogue(workerLines, true);   // движение заблокировано
        yield return new WaitUntil(() => dialogueManager.DialogueActive == false);

        // ¬ключаем звуки шагов обратно
        if (playerController != null && playerController.footstepSource != null)
        {
            playerController.footstepSource.enabled = true;
        }

        // «акрываем дверь
        if (doorInteractable != null)
            doorInteractable.CloseDoor();

        // ∆дЄм, пока дверь закроетс€
        if (npcToHide != null && doorInteractable != null)
        {
            yield return new WaitForSeconds(doorInteractable.closeAnimDuration);
            npcToHide.SetActive(false);
        }

        if (questUIManager == null)
            questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        // «авершаем текущее задание только после всей дверной цепочки.
        if (questUIManager != null && !string.IsNullOrEmpty(questIdToComplete))
        {
            if (questUIManager.IsQuestActive(questIdToComplete))
                questUIManager.CompleteQuest(questIdToComplete);
        }

        // ƒобавл€ем следующее задание.
        if (questUIManager != null && !string.IsNullOrEmpty(questIdToAdd))
        {
            questUIManager.AddQuest(questIdToAdd);
        }

        // јктивируем домашние предметы.
        if (homeItemsActivator != null)
        {
            homeItemsActivator.ActivateItems();
        }
    }

    public bool CanOpenDoor()
    {
        FindReferences();
        // ≈сли задание "find_the_key" ещЄ не выполнено (активно), то открывать нельз€
        if (questUIManager != null && questUIManager.IsQuestActive(questIdToAdd))
            return false;
        return true;
    }

    public void ShowDoorBlockDialogue()
    {
        FindReferences();

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

    public void ResetSequenceForQuestStart()
    {
        FindReferences();

        StopAllCoroutines();
        StopKnock();

        started = false;
        waitingForOpen = false;

        if (doorInteractable != null)
        {
            doorInteractable.SetDoorAvailable(false);
            doorInteractable.CloseDoor();
        }

        if (npcToHide != null)
            npcToHide.SetActive(true);
    }

    public void ApplyCompletedState()
    {
        FindReferences();

        StopKnock();

        started = true;
        waitingForOpen = true;

        if (doorInteractable != null)
        {
            doorInteractable.CloseDoor();
            doorInteractable.SetDoorAvailable(true);
        }

        if (npcToHide != null)
            npcToHide.SetActive(false);

        if (homeItemsActivator != null)
            homeItemsActivator.ActivateItems();
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        // QuestUIManager
        if (questUIManager == null)
            questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
        {
            GameObject obj = GameObject.Find(questUIManagerObjectName);

            if (obj != null)
                questUIManager = obj.GetComponent<QuestUIManager>();
        }

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        // DialogueManager Ч строго ищем объект с именем DialogueManager
        if (dialogueManager == null || dialogueManager.gameObject.name != dialogueManagerObjectName)
        {
            GameObject obj = GameObject.Find(dialogueManagerObjectName);

            if (obj != null)
                dialogueManager = obj.GetComponent<DialogueManager>();
        }

        if (dialogueManager == null)
        {
            DialogueManager[] managers = FindObjectsOfType<DialogueManager>();

            foreach (DialogueManager manager in managers)
            {
                if (manager.gameObject.name == dialogueManagerObjectName)
                {
                    dialogueManager = manager;
                    break;
                }
            }
        }

        // PlayerController
        if (playerController == null)
        {
            GameObject obj = GameObject.Find(playerObjectName);

            if (obj != null)
                playerController = obj.GetComponent<PlayerController>();
        }

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
    }
}
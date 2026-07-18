using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WalkBoss : MonoBehaviour
{
    [Header("Quest")]
    public QuestUIManager questUIManager;
    public string questId = "walk_to_boss";

    [Header("Boss Dialogue Trigger")]
    public Collider bossDialogueTrigger;
    public bool makeBossDialogueColliderTriggerOnStart = true;

    [Header("Exit Complete Trigger")]
    public Collider exitCompleteTrigger;
    public bool makeExitCompleteColliderTriggerOnStart = true;

    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> dialogueLines = new List<DialogueManager.DialogueLine>();
    public bool blockMovementDuringDialogue = true;

    [Header("Boss Door")]
    public BossDoor bossDoor;

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string questUIManagerObjectName = "QuestUIManager";
    public string dialogueManagerObjectName = "DialogueManager";

    private enum AppliedState
    {
        None,
        NotStarted,
        ActiveBeforeBossDialogue,
        ActiveAfterBossDialogueThisSession,
        Completed
    }

    private AppliedState appliedState = AppliedState.None;

    private bool dialogueRoutineStarted = false;
    private bool completionStarted = false;

    // Не сохраняется. Если загрузить сейв, где walk_to_boss active,
    // диалог с боссом снова будет доступен.
    private bool bossDialogueDoneThisSession = false;

    private void Start()
    {
        FindReferences();

        SetupBossDialogueTrigger();
        SetupExitCompleteTrigger();

        SetTriggerEnabled(bossDialogueTrigger, false);
        SetTriggerEnabled(exitCompleteTrigger, false);
    }

    private void Update()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoadingSave)
            return;

        RefreshFromCurrentQuestState();
    }

    private void RefreshFromCurrentQuestState()
    {
        FindReferences();

        if (questUIManager == null)
        {
            ApplyNotStartedState();
            return;
        }

        if (questUIManager.IsQuestCompleted(questId))
        {
            ApplyCompletedState();
            return;
        }

        if (questUIManager.IsQuestActive(questId))
        {
            if (dialogueRoutineStarted)
                return;

            if (bossDialogueDoneThisSession)
                ApplyActiveAfterBossDialogueState();
            else
                ApplyActiveBeforeBossDialogueState();

            return;
        }

        ApplyNotStartedState();
    }

    private void ApplyNotStartedState()
    {
        if (appliedState == AppliedState.NotStarted)
            return;

        appliedState = AppliedState.NotStarted;

        dialogueRoutineStarted = false;
        completionStarted = false;
        bossDialogueDoneThisSession = false;

        SetTriggerEnabled(bossDialogueTrigger, false);
        SetTriggerEnabled(exitCompleteTrigger, false);

        Debug.Log("WalkBoss: состояние NOT STARTED.");
    }

    private void ApplyActiveBeforeBossDialogueState()
    {
        if (appliedState == AppliedState.ActiveBeforeBossDialogue)
            return;

        appliedState = AppliedState.ActiveBeforeBossDialogue;

        dialogueRoutineStarted = false;
        completionStarted = false;
        bossDialogueDoneThisSession = false;

        SetTriggerEnabled(bossDialogueTrigger, true);
        SetTriggerEnabled(exitCompleteTrigger, false);

        if (bossDoor != null)
            bossDoor.ResetDoorForWalkBossActiveQuest();

        Debug.Log("WalkBoss: состояние ACTIVE BEFORE BOSS DIALOGUE. Диалог доступен, дверь сброшена.");
    }

    private void ApplyActiveAfterBossDialogueState()
    {
        if (appliedState == AppliedState.ActiveAfterBossDialogueThisSession)
            return;

        appliedState = AppliedState.ActiveAfterBossDialogueThisSession;

        SetTriggerEnabled(bossDialogueTrigger, false);
        SetTriggerEnabled(exitCompleteTrigger, true);

        Debug.Log("WalkBoss: состояние ACTIVE AFTER BOSS DIALOGUE. Ждём выхода из кабинета.");
    }

    private void ApplyCompletedState()
    {
        if (appliedState == AppliedState.Completed)
            return;

        appliedState = AppliedState.Completed;

        dialogueRoutineStarted = false;
        completionStarted = true;
        bossDialogueDoneThisSession = false;

        SetTriggerEnabled(bossDialogueTrigger, false);
        SetTriggerEnabled(exitCompleteTrigger, false);

        if (bossDoor != null)
            bossDoor.ForceFinishedClosedAfterBossQuest();

        Debug.Log("WalkBoss: состояние COMPLETED. Дверь финально закрыта.");
    }

    public void HandleBossDialogueTriggerEnter(Collider other)
    {
        if (dialogueRoutineStarted)
            return;

        if (bossDialogueDoneThisSession)
            return;

        if (!IsPlayer(other))
            return;

        FindReferences();

        if (questUIManager == null)
            return;

        if (!questUIManager.IsQuestActive(questId))
            return;

        if (questUIManager.IsQuestCompleted(questId))
            return;

        StartCoroutine(BossDialogueRoutine());
    }

    public void HandleExitCompleteTriggerEnter(Collider other)
    {
        if (completionStarted)
            return;

        if (!bossDialogueDoneThisSession)
            return;

        if (!IsPlayer(other))
            return;

        FindReferences();

        if (questUIManager == null)
            return;

        if (!questUIManager.IsQuestActive(questId))
            return;

        if (questUIManager.IsQuestCompleted(questId))
            return;

        CompleteWalkBossQuest();
    }

    private IEnumerator BossDialogueRoutine()
    {
        dialogueRoutineStarted = true;

        SetTriggerEnabled(bossDialogueTrigger, false);
        SetTriggerEnabled(exitCompleteTrigger, false);

        if (dialogueManager != null &&
            dialogueLines != null &&
            dialogueLines.Count > 0)
        {
            dialogueManager.StartDialogue(dialogueLines, blockMovementDuringDialogue);

            while (dialogueManager != null && dialogueManager.DialogueActive)
                yield return null;
        }

        bossDialogueDoneThisSession = true;
        dialogueRoutineStarted = false;

        appliedState = AppliedState.None;
        RefreshFromCurrentQuestState();

        Debug.Log("WalkBoss: диалог с боссом завершён. Квест пока НЕ завершён — игрок должен выйти из кабинета.");
    }

    private void CompleteWalkBossQuest()
    {
        completionStarted = true;

        if (questUIManager != null && questUIManager.IsQuestActive(questId))
            questUIManager.CompleteQuest(questId);

        appliedState = AppliedState.None;
        RefreshFromCurrentQuestState();

        Debug.Log("WalkBoss: игрок вышел из кабинета, задание walk_to_boss завершено.");
    }

    private void SetupBossDialogueTrigger()
    {
        if (bossDialogueTrigger == null)
            return;

        if (makeBossDialogueColliderTriggerOnStart)
            bossDialogueTrigger.isTrigger = true;

        WalkBossDialogueTriggerProxy proxy =
            bossDialogueTrigger.GetComponent<WalkBossDialogueTriggerProxy>();

        if (proxy == null)
            proxy = bossDialogueTrigger.gameObject.AddComponent<WalkBossDialogueTriggerProxy>();

        proxy.owner = this;

        Rigidbody rb = bossDialogueTrigger.GetComponent<Rigidbody>();

        if (rb == null)
            rb = bossDialogueTrigger.gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void SetupExitCompleteTrigger()
    {
        if (exitCompleteTrigger == null)
            return;

        if (makeExitCompleteColliderTriggerOnStart)
            exitCompleteTrigger.isTrigger = true;

        WalkBossExitCompleteTriggerProxy proxy =
            exitCompleteTrigger.GetComponent<WalkBossExitCompleteTriggerProxy>();

        if (proxy == null)
            proxy = exitCompleteTrigger.gameObject.AddComponent<WalkBossExitCompleteTriggerProxy>();

        proxy.owner = this;

        Rigidbody rb = exitCompleteTrigger.GetComponent<Rigidbody>();

        if (rb == null)
            rb = exitCompleteTrigger.gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void SetTriggerEnabled(Collider triggerCollider, bool state)
    {
        if (triggerCollider != null)
            triggerCollider.enabled = state;
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag(playerTag))
            return true;

        if (other.transform.root != null && other.transform.root.CompareTag(playerTag))
            return true;

        return false;
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

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

        if (dialogueManager == null)
        {
            GameObject obj = GameObject.Find(dialogueManagerObjectName);

            if (obj != null)
                dialogueManager = obj.GetComponent<DialogueManager>();
        }

        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>();

        if (bossDoor == null)
            bossDoor = FindObjectOfType<BossDoor>();
    }
}

public class WalkBossDialogueTriggerProxy : MonoBehaviour
{
    public WalkBoss owner;

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null)
            owner.HandleBossDialogueTriggerEnter(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (owner != null)
            owner.HandleBossDialogueTriggerEnter(other);
    }
}

public class WalkBossExitCompleteTriggerProxy : MonoBehaviour
{
    public WalkBoss owner;

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null)
            owner.HandleExitCompleteTriggerEnter(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (owner != null)
            owner.HandleExitCompleteTriggerEnter(other);
    }
}
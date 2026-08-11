using UnityEngine;
using System.Collections;

public class WorkSessionManager : MonoBehaviour
{
    public static WorkSessionManager Instance { get; private set; }

    public enum WorkSessionState
    {
        Inactive,
        ReadyToSit,
        EnteringSeat,
        Seated
    }

    [Header("Quest")]
    public string questId = "get_to_work";
    public QuestUIManager questUIManager;

    [Header("Work Modules")]
    public WorkChairStarter chairStarter;
    public PlayerWorkSeatController seatController;
    public WorkHUDManager hudManager;
    public WorkCursorController cursorController;

    [Header("Auto Find")]
    public bool autoFindReferences = true;

    [SerializeField]
    private WorkSessionState currentState = WorkSessionState.Inactive;

    private Coroutine startWorkCoroutine;
    private bool initialized;

    public WorkSessionState CurrentState => currentState;

    public bool IsWorkModeActive =>
        currentState == WorkSessionState.EnteringSeat ||
        currentState == WorkSessionState.Seated;

    public bool IsSeated =>
        currentState == WorkSessionState.Seated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(InitializeRoutine());
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSave)
        {
            return;
        }

        RefreshFromQuestState();
    }

    private IEnumerator InitializeRoutine()
    {
        FindReferences();

        while (SaveManager.Instance != null &&
               SaveManager.Instance.IsLoadingSave)
        {
            yield return null;
        }

        // Даём SaveManager и QuestWorldState закончить восстановление.
        yield return null;

        if (hudManager != null)
            hudManager.HideInstant();

        if (cursorController != null)
            cursorController.HideAndLockGameplayCursor();

        currentState = WorkSessionState.Inactive;
        initialized = true;

        RefreshFromQuestState();
    }

    private void RefreshFromQuestState()
    {
        FindReferences();

        bool questActive =
            questUIManager != null &&
            questUIManager.IsQuestActive(questId);

        bool questCompleted =
            questUIManager != null &&
            questUIManager.IsQuestCompleted(questId);

        if (!questActive || questCompleted)
        {
            if (IsWorkModeActive)
                ForceStopWork();

            currentState = WorkSessionState.Inactive;

            if (chairStarter != null)
                chairStarter.SetAvailable(false);

            return;
        }

        // Квест активен, но работа ещё не начата.
        if (currentState == WorkSessionState.Inactive)
            currentState = WorkSessionState.ReadyToSit;

        if (chairStarter != null)
        {
            chairStarter.SetAvailable(
                currentState == WorkSessionState.ReadyToSit
            );
        }
    }

    public void StartWork()
    {
        FindReferences();
        RefreshFromQuestState();

        if (currentState != WorkSessionState.ReadyToSit)
            return;

        if (seatController == null)
        {
            Debug.LogError(
                "WorkSessionManager: PlayerWorkSeatController не назначен."
            );
            return;
        }

        if (startWorkCoroutine != null)
            StopCoroutine(startWorkCoroutine);

        startWorkCoroutine = StartCoroutine(StartWorkRoutine());
    }

    private IEnumerator StartWorkRoutine()
    {
        currentState = WorkSessionState.EnteringSeat;

        if (chairStarter != null)
            chairStarter.SetAvailable(false);

        yield return seatController.EnterSeat();

        if (!seatController.IsSeated)
        {
            Debug.LogError(
                "WorkSessionManager: не удалось посадить игрока."
            );

            currentState = WorkSessionState.ReadyToSit;

            if (chairStarter != null)
                chairStarter.SetAvailable(true);

            startWorkCoroutine = null;
            yield break;
        }

        if (SessionStatsManager.Instance != null)
        {
            SessionStatsManager.Instance
                .BeginWorkCheckpoint();
        }

        if (hudManager != null)
            hudManager.Show();

        if (cursorController != null)
            cursorController.ShowWorkCursor();

        currentState = WorkSessionState.Seated;
        startWorkCoroutine = null;

        Debug.Log("WorkSessionManager: рабочий режим запущен.");
    }

    public void RestoreAfterPause()
    {
        if (!IsWorkModeActive)
            return;

        if (seatController != null)
            seatController.RestoreWorkControlAfterPause();

        if (cursorController != null)
            cursorController.ShowWorkCursor();
    }

    public void ForceStopWork()
    {
        if (startWorkCoroutine != null)
        {
            StopCoroutine(startWorkCoroutine);
            startWorkCoroutine = null;
        }

        if (seatController != null)
            seatController.ExitWorkInstant(false);

        if (hudManager != null)
            hudManager.HideInstant();

        if (cursorController != null)
            cursorController.HideAndLockGameplayCursor();

        currentState = WorkSessionState.Inactive;
    }

    public void ResetAfterLoad()
    {
        if (startWorkCoroutine != null)
        {
            StopCoroutine(startWorkCoroutine);
            startWorkCoroutine = null;
        }

        currentState = WorkSessionState.Inactive;

        if (chairStarter != null)
            chairStarter.SetAvailable(false);

        if (hudManager != null)
            hudManager.HideInstant();

        if (cursorController != null)
            cursorController.HideAndLockGameplayCursor();

        Debug.Log(
            "WorkSessionManager: временное состояние работы сброшено после загрузки."
        );
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (questUIManager == null)
            questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        if (chairStarter == null)
            chairStarter = FindObjectOfType<WorkChairStarter>();

        if (seatController == null)
            seatController = FindObjectOfType<PlayerWorkSeatController>();

        if (hudManager == null)
            hudManager = FindObjectOfType<WorkHUDManager>();

        if (cursorController == null)
            cursorController = FindObjectOfType<WorkCursorController>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
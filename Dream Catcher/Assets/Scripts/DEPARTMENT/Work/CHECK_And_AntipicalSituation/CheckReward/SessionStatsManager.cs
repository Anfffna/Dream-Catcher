using System;
using UnityEngine;

public class SessionStatsManager : MonoBehaviour
{
    // Версия 2:
    // 1 = рассудок + стаж
    // 2 = рассудок + стаж + деньги
    public const int SaveVersion = 2;

    public static SessionStatsManager Instance
    {
        get;
        private set;
    }

    // =====================================================
    // РАССУДОК
    // =====================================================

    [Header("Рассудок")]

    [Tooltip("Максимальный уровень рассудка.")]
    [SerializeField]
    private int maxSanity = 100;

    [Tooltip("Уровень рассудка при начале новой игры.")]
    [SerializeField]
    private int startingSanity = 100;

    [Tooltip("Сколько рассудка теряется после завершения одного клиента.")]
    [SerializeField]
    private int sanityLossPerClient = 5;

    // =====================================================
    // РАБОЧЕЕ ВРЕМЯ
    // =====================================================

    [Header("Рабочее время")]

    [Tooltip("Сколько минут длится одна рабочая смена. 480 минут = 8 часов.")]
    [SerializeField]
    private int shiftDurationMinutes = 480;

    [Tooltip("Сколько минут рабочего времени уходит после одного клиента.")]
    [SerializeField]
    private int workMinutesLossPerClient = 80;

    // =====================================================
    // СТАЖ
    // =====================================================

    [Header("Стаж")]

    [Tooltip("Стаж игрока при начале новой игры.")]
    [SerializeField]
    private int startingExperience = 0;

    [Tooltip("Текущий стаж игрока.")]
    [SerializeField]
    private int currentExperience = 0;

    // =====================================================
    // ДЕНЬГИ
    // =====================================================

    [Header("Деньги")]

    [Tooltip("Количество денег при начале новой игры.")]
    [SerializeField]
    private int startingMoney = 100;

    [Tooltip("Текущее количество денег игрока.")]
    [SerializeField]
    private int currentMoney = 100;

    // =====================================================
    // ТЕКУЩЕЕ СОСТОЯНИЕ
    // =====================================================

    [Header("Текущее состояние")]

    [SerializeField]
    private int currentSanity = 100;

    [SerializeField]
    private int currentWorkMinutes = 480;

    // =====================================================
    // СНИМОК ПЕРЕД СМЕНОЙ
    // =====================================================

    [Header("Снимок перед рабочей сменой")]

    [Tooltip(
        "Активен, пока текущая рабочая смена не завершена полностью."
    )]
    [SerializeField]
    private bool workCheckpointActive;

    [Tooltip(
        "Рассудок, который был у игрока перед началом текущей смены."
    )]
    [SerializeField]
    private int workStartSanity = 100;

    [Tooltip(
        "Стаж, который был у игрока перед началом текущей смены."
    )]
    [SerializeField]
    private int workStartExperience = 0;

    [Tooltip(
        "Деньги, которые были у игрока перед началом текущей смены."
    )]
    [SerializeField]
    private int workStartMoney = 100;

    private ClientNPCController trackedClient;

    // =====================================================
    // СВОЙСТВА
    // =====================================================

    public int CurrentSanity =>
        currentSanity;

    public int MaxSanity =>
        maxSanity;

    public int CurrentWorkMinutes =>
        currentWorkMinutes;

    public int ShiftDurationMinutes =>
        shiftDurationMinutes;

    public bool HasActiveWorkCheckpoint =>
        workCheckpointActive;

    public float NormalizedSanity =>
        maxSanity <= 0
            ? 0f
            : (float)currentSanity /
              maxSanity;

    public int CurrentExperience =>
        currentExperience;

    public int CurrentMoney =>
        currentMoney;

    // =====================================================
    // СОБЫТИЯ
    // =====================================================

    /// <summary>
    /// Обычное игровое изменение рассудка.
    /// Старое значение, новое значение.
    /// </summary>
    public event Action<int, int>
        SanityChanged;

    /// <summary>
    /// Восстановление рассудка из сейва.
    /// </summary>
    public event Action<int>
        SanityRestored;

    /// <summary>
    /// Обычное изменение рабочего времени.
    /// Старое значение, новое значение.
    /// </summary>
    public event Action<int, int>
        WorkTimeChanged;

    /// <summary>
    /// Рабочее время было сброшено к началу смены.
    /// </summary>
    public event Action<int>
        WorkTimeReset;

    // Стаж.
    public event Action<int, int>
        ExperienceChanged;

    public event Action<int>
        ExperienceRestored;

    // Деньги.
    public event Action<int, int>
        MoneyChanged;

    public event Action<int>
        MoneyRestored;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        // Рассудок.
        maxSanity =
            Mathf.Max(
                1,
                maxSanity
            );

        startingSanity =
            Mathf.Clamp(
                startingSanity,
                0,
                maxSanity
            );

        currentSanity =
            Mathf.Clamp(
                currentSanity,
                0,
                maxSanity
            );

        // Время.
        shiftDurationMinutes =
            Mathf.Max(
                1,
                shiftDurationMinutes
            );

        workMinutesLossPerClient =
            Mathf.Max(
                0,
                workMinutesLossPerClient
            );

        currentWorkMinutes =
            Mathf.Clamp(
                currentWorkMinutes,
                0,
                shiftDurationMinutes
            );

        // Стаж.
        startingExperience =
            Mathf.Max(
                0,
                startingExperience
            );

        currentExperience =
            Mathf.Max(
                0,
                currentExperience
            );

        // Деньги.
        startingMoney =
            Mathf.Max(
                0,
                startingMoney
            );

        currentMoney =
            Mathf.Max(
                0,
                currentMoney
            );

        workStartExperience =
            currentExperience;

        workStartSanity =
            currentSanity;

        workStartMoney =
            currentMoney;
    }

    // =====================================================
    // РАБОЧАЯ СМЕНА
    // =====================================================

    public void BeginWorkCheckpoint()
    {
        // Если смена уже идёт,
        // второй раз её не начинаем.
        if (workCheckpointActive)
            return;

        workStartSanity =
            currentSanity;

        workStartExperience =
            currentExperience;

        workStartMoney =
            currentMoney;

        workCheckpointActive =
            true;

        ResetWorkTimeForShift();
    }

    /// <summary>
    /// Вызывать только после полного
    /// завершения всей рабочей смены.
    /// </summary>
    public void CommitWorkCheckpoint()
    {
        if (!workCheckpointActive)
            return;

        workCheckpointActive =
            false;

        workStartSanity =
            currentSanity;

        workStartExperience =
            currentExperience;

        workStartMoney =
            currentMoney;
    }

    public void RollbackWorkCheckpoint()
    {
        if (!workCheckpointActive)
            return;

        // Рассудок.
        currentSanity =
            Mathf.Clamp(
                workStartSanity,
                0,
                maxSanity
            );

        workStartSanity =
            currentSanity;

        // Стаж.
        currentExperience =
            Mathf.Max(
                0,
                workStartExperience
            );

        workStartExperience =
            currentExperience;

        // Деньги.
        currentMoney =
            Mathf.Max(
                0,
                workStartMoney
            );

        workStartMoney =
            currentMoney;

        workCheckpointActive =
            false;

        ExperienceRestored?.Invoke(
            currentExperience
        );

        MoneyRestored?.Invoke(
            currentMoney
        );

        StopTrackingClient();

        ResetWorkTimeForShift();

        SanityRestored?.Invoke(
            currentSanity
        );
    }

    public int GetSanityForSave()
    {
        if (workCheckpointActive)
        {
            return workStartSanity;
        }

        return currentSanity;
    }

    public int GetExperienceForSave()
    {
        if (workCheckpointActive)
        {
            return workStartExperience;
        }

        return currentExperience;
    }

    public int GetMoneyForSave()
    {
        if (workCheckpointActive)
        {
            return workStartMoney;
        }

        return currentMoney;
    }

    // =====================================================
    // КЛИЕНТ
    // =====================================================

    public void TrackClient(
        ClientNPCController client)
    {
        if (client == null)
            return;

        if (trackedClient == client)
            return;

        StopTrackingClient();

        trackedClient =
            client;

        trackedClient.ClientFinished +=
            HandleClientFinished;
    }

    private void HandleClientFinished(
        ClientNPCController client)
    {
        if (client != trackedClient)
            return;

        // Рассудок.
        ChangeSanity(
            -sanityLossPerClient
        );

        // Рабочее время.
        ChangeWorkTime(
            -workMinutesLossPerClient
        );

        StopTrackingClient();
    }

    private void StopTrackingClient()
    {
        if (trackedClient == null)
            return;

        trackedClient.ClientFinished -=
            HandleClientFinished;

        trackedClient = null;
    }

    // =====================================================
    // РАССУДОК
    // =====================================================

    public void ChangeSanity(
        int amount)
    {
        SetSanity(
            currentSanity +
            amount
        );
    }

    public void SetSanity(
        int value)
    {
        int newValue =
            Mathf.Clamp(
                value,
                0,
                maxSanity
            );

        if (newValue ==
            currentSanity)
        {
            return;
        }

        int oldValue =
            currentSanity;

        currentSanity =
            newValue;

        SanityChanged?.Invoke(
            oldValue,
            currentSanity
        );
    }

    // =====================================================
    // РАБОЧЕЕ ВРЕМЯ
    // =====================================================

    public void ChangeWorkTime(
        int amount)
    {
        SetWorkTime(
            currentWorkMinutes +
            amount
        );
    }

    public void SetWorkTime(
        int value)
    {
        int newValue =
            Mathf.Clamp(
                value,
                0,
                shiftDurationMinutes
            );

        if (newValue ==
            currentWorkMinutes)
        {
            return;
        }

        int oldValue =
            currentWorkMinutes;

        currentWorkMinutes =
            newValue;

        WorkTimeChanged?.Invoke(
            oldValue,
            currentWorkMinutes
        );
    }

    private void ResetWorkTimeForShift()
    {
        currentWorkMinutes =
            shiftDurationMinutes;

        WorkTimeReset?.Invoke(
            currentWorkMinutes
        );
    }

    // =====================================================
    // СТАЖ
    // =====================================================

    public void ChangeExperience(
        int amount)
    {
        SetExperience(
            currentExperience +
            amount
        );
    }

    public void SetExperience(
        int value)
    {
        int newValue =
            Mathf.Max(
                0,
                value
            );

        if (newValue ==
            currentExperience)
        {
            return;
        }

        int oldValue =
            currentExperience;

        currentExperience =
            newValue;

        ExperienceChanged?.Invoke(
            oldValue,
            currentExperience
        );
    }

    // =====================================================
    // ДЕНЬГИ
    // =====================================================

    public void ChangeMoney(
        int amount)
    {
        SetMoney(
            currentMoney +
            amount
        );
    }

    public void SetMoney(
        int value)
    {
        int newValue =
            Mathf.Max(
                0,
                value
            );

        if (newValue ==
            currentMoney)
        {
            return;
        }

        int oldValue =
            currentMoney;

        currentMoney =
            newValue;

        MoneyChanged?.Invoke(
            oldValue,
            currentMoney
        );
    }

    // =====================================================
    // SAVE / LOAD
    // =====================================================

    // Старый overload оставляем,
    // чтобы случайно не сломать другой существующий код.
    public void RestoreFromSave(
        int savedSanity,
        int savedExperience)
    {
        RestoreFromSave(
            savedSanity,
            savedExperience,
            startingMoney
        );
    }

    public void RestoreFromSave(
        int savedSanity,
        int savedExperience,
        int savedMoney)
    {
        StopTrackingClient();

        // Рассудок.
        currentSanity =
            Mathf.Clamp(
                savedSanity,
                0,
                maxSanity
            );

        // Стаж.
        currentExperience =
            Mathf.Max(
                0,
                savedExperience
            );

        // Деньги.
        currentMoney =
            Mathf.Max(
                0,
                savedMoney
            );

        // После загрузки незавершённая
        // рабочая смена больше не активна.
        workCheckpointActive =
            false;

        workStartSanity =
            currentSanity;

        workStartExperience =
            currentExperience;

        workStartMoney =
            currentMoney;

        // Рабочее время не хранится в сейве.
        currentWorkMinutes =
            shiftDurationMinutes;

        // Сохранённые значения ставим
        // мгновенно, без анимации награды.
        SanityRestored?.Invoke(
            currentSanity
        );

        ExperienceRestored?.Invoke(
            currentExperience
        );

        MoneyRestored?.Invoke(
            currentMoney
        );

        WorkTimeReset?.Invoke(
            currentWorkMinutes
        );
    }

    public void ResetForNewGame()
    {
        StopTrackingClient();

        currentSanity =
            startingSanity;

        currentExperience =
            startingExperience;

        currentMoney =
            startingMoney;

        workCheckpointActive =
            false;

        workStartSanity =
            startingSanity;

        workStartExperience =
            startingExperience;

        workStartMoney =
            startingMoney;

        currentWorkMinutes =
            shiftDurationMinutes;

        ExperienceRestored?.Invoke(
            currentExperience
        );

        MoneyRestored?.Invoke(
            currentMoney
        );

        SanityRestored?.Invoke(
            currentSanity
        );

        WorkTimeReset?.Invoke(
            currentWorkMinutes
        );
    }

    // =====================================================
    // ВРЕМЕННЫЙ ТЕСТ ЗАВЕРШЕНИЯ СМЕНЫ
    // =====================================================

    [ContextMenu(
        "TEST: считать рабочую смену завершённой"
    )]
    private void TestCommitWorkCheckpoint()
    {
        CommitWorkCheckpoint();
    }

    [ContextMenu("TEST: Рассудок 100")]
    private void TestSanity100()
    {
        SetSanity(100);
    }

    [ContextMenu("TEST: Рассудок 80")]
    private void TestSanity80()
    {
        SetSanity(80);
    }

    [ContextMenu("TEST: Рассудок 60")]
    private void TestSanity60()
    {
        SetSanity(60);
    }

    [ContextMenu("TEST: Рассудок 20")]
    private void TestSanity20()
    {
        SetSanity(20);
    }

    [ContextMenu("TEST: Рассудок 1")]
    private void TestSanity1()
    {
        SetSanity(1);
    }

    private void OnDestroy()
    {
        StopTrackingClient();

        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        // Рассудок.
        maxSanity =
            Mathf.Max(
                1,
                maxSanity
            );

        startingSanity =
            Mathf.Clamp(
                startingSanity,
                0,
                maxSanity
            );

        sanityLossPerClient =
            Mathf.Max(
                0,
                sanityLossPerClient
            );

        currentSanity =
            Mathf.Clamp(
                currentSanity,
                0,
                maxSanity
            );

        workStartSanity =
            Mathf.Clamp(
                workStartSanity,
                0,
                maxSanity
            );

        // Стаж.
        startingExperience =
            Mathf.Max(
                0,
                startingExperience
            );

        currentExperience =
            Mathf.Max(
                0,
                currentExperience
            );

        workStartExperience =
            Mathf.Max(
                0,
                workStartExperience
            );

        // Деньги.
        startingMoney =
            Mathf.Max(
                0,
                startingMoney
            );

        currentMoney =
            Mathf.Max(
                0,
                currentMoney
            );

        workStartMoney =
            Mathf.Max(
                0,
                workStartMoney
            );

        // Время.
        shiftDurationMinutes =
            Mathf.Max(
                1,
                shiftDurationMinutes
            );

        workMinutesLossPerClient =
            Mathf.Max(
                0,
                workMinutesLossPerClient
            );

        currentWorkMinutes =
            Mathf.Clamp(
                currentWorkMinutes,
                0,
                shiftDurationMinutes
            );
    }
}
using System;
using UnityEngine;

public class SessionStatsManager : MonoBehaviour
{
    public const int SaveVersion = 1;

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

    [SerializeField]
    private int workStartExperience = 0;

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

    // =====================================================
    // СОБЫТИЯ
    // =====================================================

    /// <summary>
    /// Обычное игровое изменение рассудка.
    /// HUD должен его анимировать.
    /// Старое значение, новое значение.
    /// </summary>
    public event Action<int, int>
        SanityChanged;

    /// <summary>
    /// Восстановление рассудка из сейва.
    /// HUD должен выставить значение мгновенно.
    /// </summary>
    public event Action<int>
        SanityRestored;

    /// <summary>
    /// Обычное уменьшение рабочего времени.
    /// Старое количество минут, новое количество минут.
    /// </summary>
    public event Action<int, int>
        WorkTimeChanged;

    /// <summary>
    /// Рабочее время было сброшено
    /// к началу новой смены.
    /// HUD должен показать его мгновенно.
    /// </summary>
    public event Action<int>
        WorkTimeReset;

    //стаж
    public event Action<int, int>
    ExperienceChanged;

    public event Action<int>
        ExperienceRestored;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        //рассудок
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
        //время
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
        //стаж
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
            currentExperience;

        workStartSanity =
            currentSanity;
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
    }

    public void RollbackWorkCheckpoint()
    {
        if (!workCheckpointActive)
            return;
        //рассудок
        currentSanity =
            Mathf.Clamp(
                workStartSanity,
                0,
                maxSanity
            );

        workCheckpointActive =
            false;

        workStartSanity =
            currentSanity;

        //стаж
        currentExperience =
            Mathf.Max(
                0,
                workStartExperience
            );

        workStartExperience =
            currentExperience;

        ExperienceRestored?.Invoke(
            currentExperience
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

    public int GetExperienceForSave()
    {
        if (workCheckpointActive)
        {
            return workStartExperience;
        }

        return currentExperience;
    }

    // =====================================================
    // SAVE / LOAD
    // =====================================================

    public void RestoreFromSave(
    int savedSanity,
    int savedExperience)
    {
        StopTrackingClient();

        // Восстанавливаем рассудок.
        currentSanity =
            Mathf.Clamp(
                savedSanity,
                0,
                maxSanity
            );

        // Восстанавливаем стаж.
        currentExperience =
            Mathf.Max(
                0,
                savedExperience
            );

        // После загрузки никакая незавершённая
        // рабочая смена больше не считается активной.
        workCheckpointActive =
            false;

        workStartSanity =
            currentSanity;

        workStartExperience =
            currentExperience;

        // Рабочее время не хранится в сейве.
        // При следующем начале смены оно снова будет
        // начинаться с полного рабочего дня.
        currentWorkMinutes =
            shiftDurationMinutes;

        // HUD получает сохранённые значения мгновенно,
        // без анимации начисления.
        SanityRestored?.Invoke(
            currentSanity
        );

        ExperienceRestored?.Invoke(
            currentExperience
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

        workCheckpointActive =
            false;

        workStartSanity =
            startingSanity;

        currentWorkMinutes =
            shiftDurationMinutes;

        currentExperience =
            startingExperience;

        workStartExperience =
            startingExperience;

        ExperienceRestored?.Invoke(
            currentExperience
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
        maxSanity =
            Mathf.Max(
                1,
                maxSanity
            );
        //рассудок
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
        //стаж
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
using System;
using UnityEngine;

public class SessionStatsManager : MonoBehaviour
{
    public static SessionStatsManager Instance { get; private set; }

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

    [Header("Текущее состояние")]

    [SerializeField]
    private int currentSanity = 100;

    private ClientNPCController trackedClient;

    public int CurrentSanity => currentSanity;
    public int MaxSanity => maxSanity;

    public float NormalizedSanity =>
        maxSanity <= 0
            ? 0f
            : (float)currentSanity / maxSanity;

    /// <summary>
    /// Старое значение, новое значение.
    /// </summary>
    public event Action<int, int> SanityChanged;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        maxSanity =
            Mathf.Max(1, maxSanity);

        startingSanity =
            Mathf.Clamp(
                startingSanity,
                0,
                maxSanity
            );

        currentSanity =
            startingSanity;
    }

    public void TrackClient(
        ClientNPCController client)
    {
        if (client == null)
            return;

        if (trackedClient == client)
            return;

        StopTrackingClient();

        trackedClient = client;

        trackedClient.ClientFinished +=
            HandleClientFinished;
    }

    private void HandleClientFinished(
        ClientNPCController client)
    {
        if (client != trackedClient)
            return;

        ChangeSanity(
            -sanityLossPerClient
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

    public void ChangeSanity(int amount)
    {
        SetSanity(
            currentSanity + amount
        );
    }

    public void SetSanity(int value)
    {
        int newValue =
            Mathf.Clamp(
                value,
                0,
                maxSanity
            );

        if (newValue == currentSanity)
            return;

        int oldValue =
            currentSanity;

        currentSanity =
            newValue;

        SanityChanged?.Invoke(
            oldValue,
            currentSanity
        );
    }

    public void ResetForNewGame()
    {
        SetSanity(startingSanity);
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
    }
}
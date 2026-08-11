using System.Collections;
using UnityEngine;

public class WorkTimeHUDController :
    MonoBehaviour
{
    [Header("Анимация времени")]

    [Tooltip(
        "Универсальный компонент, который " +
        "показывает и анимирует рабочее время."
    )]
    [SerializeField]
    private AnimatedHUDValue animatedValue;

    private SessionStatsManager statsManager;

    private Coroutine findManagerCoroutine;

    private void OnEnable()
    {
        TryConnectToStats();
    }

    private void OnDisable()
    {
        DisconnectFromStats();

        if (findManagerCoroutine != null)
        {
            StopCoroutine(
                findManagerCoroutine
            );

            findManagerCoroutine =
                null;
        }
    }

    private void TryConnectToStats()
    {
        if (SessionStatsManager.Instance != null)
        {
            ConnectToStats(
                SessionStatsManager.Instance
            );

            return;
        }

        if (findManagerCoroutine == null)
        {
            findManagerCoroutine =
                StartCoroutine(
                    WaitForStatsManager()
                );
        }
    }

    private IEnumerator WaitForStatsManager()
    {
        while (SessionStatsManager.Instance == null)
        {
            yield return null;
        }

        findManagerCoroutine =
            null;

        ConnectToStats(
            SessionStatsManager.Instance
        );
    }

    private void ConnectToStats(
        SessionStatsManager manager)
    {
        DisconnectFromStats();

        statsManager =
            manager;

        if (statsManager == null)
            return;

        statsManager.WorkTimeChanged +=
            HandleWorkTimeChanged;

        statsManager.WorkTimeReset +=
            HandleWorkTimeReset;

        if (animatedValue != null)
        {
            animatedValue.SetImmediate(
                statsManager.CurrentWorkMinutes
            );
        }
    }

    private void DisconnectFromStats()
    {
        if (statsManager == null)
            return;

        statsManager.WorkTimeChanged -=
            HandleWorkTimeChanged;

        statsManager.WorkTimeReset -=
            HandleWorkTimeReset;

        statsManager =
            null;
    }

    private void HandleWorkTimeChanged(
        int oldMinutes,
        int newMinutes)
    {
        if (animatedValue == null)
            return;

        animatedValue.AnimateTo(
            newMinutes
        );
    }

    private void HandleWorkTimeReset(
        int minutes)
    {
        if (animatedValue == null)
            return;

        animatedValue.SetImmediate(
            minutes
        );
    }
}
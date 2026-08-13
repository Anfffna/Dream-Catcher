using System.Collections;
using UnityEngine;

public class MoneyHUDController :
    MonoBehaviour
{
    [Header("Анимация значения")]

    [Tooltip(
        "Универсальный компонент, который " +
        "показывает и анимирует количество денег."
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

        statsManager.MoneyChanged +=
            HandleMoneyChanged;

        statsManager.MoneyRestored +=
            HandleMoneyRestored;

        if (animatedValue != null)
        {
            // При первом появлении HUD
            // просто показываем настоящее
            // количество денег.
            // Без анимации и звука.
            animatedValue.SetImmediate(
                statsManager.CurrentMoney
            );
        }
    }

    private void DisconnectFromStats()
    {
        if (statsManager == null)
            return;

        statsManager.MoneyChanged -=
            HandleMoneyChanged;

        statsManager.MoneyRestored -=
            HandleMoneyRestored;

        statsManager =
            null;
    }

    private void HandleMoneyChanged(
        int oldValue,
        int newValue)
    {
        if (animatedValue == null)
            return;

        animatedValue.AnimateTo(
            newValue
        );
    }

    private void HandleMoneyRestored(
        int restoredValue)
    {
        if (animatedValue == null)
            return;

        // Загрузка сейва —
        // сразу настоящее количество денег,
        // без эффекта начисления.
        animatedValue.SetImmediate(
            restoredValue
        );
    }
}
using System.Collections;
using UnityEngine;

public class ExperienceHUDController :
    MonoBehaviour
{
    [Header("Анимация значения")]

    [Tooltip(
        "Универсальный компонент, который " +
        "показывает и анимирует число стажа."
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

        statsManager.ExperienceChanged +=
            HandleExperienceChanged;

        statsManager.ExperienceRestored +=
            HandleExperienceRestored;

        if (animatedValue != null)
        {
            // При первом появлении HUD
            // просто показываем настоящее значение.
            // Никакой анимации и звука.
            animatedValue.SetImmediate(
                statsManager.CurrentExperience
            );
        }
    }

    private void DisconnectFromStats()
    {
        if (statsManager == null)
            return;

        statsManager.ExperienceChanged -=
            HandleExperienceChanged;

        statsManager.ExperienceRestored -=
            HandleExperienceRestored;

        statsManager =
            null;
    }

    private void HandleExperienceChanged(
        int oldValue,
        int newValue)
    {
        if (animatedValue == null)
            return;

        animatedValue.AnimateTo(
            newValue
        );
    }

    private void HandleExperienceRestored(
        int restoredValue)
    {
        if (animatedValue == null)
            return;

        // Загрузка сейва —
        // сразу настоящее число,
        // без эффекта начисления.
        animatedValue.SetImmediate(
            restoredValue
        );
    }
}
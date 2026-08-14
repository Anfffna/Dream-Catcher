using System.Collections;
using UnityEngine;

public class WorkDirectionRewardController :
    MonoBehaviour
{
    [Header("Отправка направления")]

    [Tooltip(
        "Контроллер отправки направления. " +
        "Если пусто — найдётся автоматически."
    )]
    [SerializeField]
    private DirectionSubmitController submitController;


    [Header("Возврат камеры")]

    [Tooltip(
        "Контроллер зума компьютера. " +
        "Если пусто — найдётся автоматически."
    )]
    [SerializeField]
    private ZoomComputerWork zoomComputerWork;


    [Header("Награда за правильное направление")]

    [Tooltip(
        "Сколько стажа начисляется за полностью правильное направление."
    )]
    [SerializeField]
    private int experienceReward = 50;

    [Tooltip(
        "Сколько денег начисляется за полностью правильное направление."
    )]
    [SerializeField]
    private int moneyReward = 70;


    [Header("Плашка правильного результата")]

    [Tooltip(
        "Плашка, которая выезжает после правильного направления."
    )]
    [SerializeField]
    private TaskUpdateToast correctResultToast;


    [Header("Плашка неправильного результата")]

    [Tooltip(
        "Плашка обычной ошибки. " +
        "Не показывается при опасном Release вместо Prison, " +
        "потому что там будет отдельный телефонный штраф."
    )]
    [SerializeField]
    private TaskUpdateToast incorrectResultToast;

    [Header("Плашка штрафа")]

    [Tooltip(
    "Плашка, которая показывается " +
    "после телефонного разговора " +
    "при опасной ошибке."
    )]
    [SerializeField]
    private TaskUpdateToast penaltyResultToast;


    [Header("Штраф за опасную ошибку")]

    [Tooltip(
        "Штраф, если клиента нужно было " +
        "отправить в тюрьму, но игрок его отпустил."
    )]
    [SerializeField]
    private int dangerousReleasePenalty = 100;

    private Coroutine rewardCoroutine;


    private void OnEnable()
    {
        FindReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (rewardCoroutine != null)
        {
            StopCoroutine(
                rewardCoroutine
            );

            rewardCoroutine = null;
        }
    }


    // =====================================================
    // ПОДПИСКА
    // =====================================================

    private void Subscribe()
    {
        if (submitController == null)
            return;

        submitController.DirectionSubmitted -=
            HandleDirectionSubmitted;

        submitController.DirectionSubmitted +=
            HandleDirectionSubmitted;
    }

    private void Unsubscribe()
    {
        if (submitController == null)
            return;

        submitController.DirectionSubmitted -=
            HandleDirectionSubmitted;
    }


    // =====================================================
    // РЕЗУЛЬТАТ
    // =====================================================

    private void HandleDirectionSubmitted(
        DirectionEvaluationController
            .EvaluationResult result)
    {
        if (result == null)
            return;

        if (rewardCoroutine != null)
        {
            StopCoroutine(
                rewardCoroutine
            );
        }

        rewardCoroutine =
            StartCoroutine(
                RewardAfterReturnRoutine(
                    result
                )
            );
    }


    private IEnumerator RewardAfterReturnRoutine(
    DirectionEvaluationController
        .EvaluationResult result)
    {
        FindReferences();

        yield return null;

        if (zoomComputerWork != null)
        {
            while (zoomComputerWork.ZoomActive)
            {
                yield return null;
            }
        }

        bool dangerousRelease =
            IsDangerousRelease(
                result
            );

        if (result.IsCorrect)
        {
            SessionStatsManager stats =
                SessionStatsManager.Instance;

            if (stats != null)
            {
                stats.ChangeExperience(
                    experienceReward
                );

                stats.ChangeMoney(
                    moneyReward
                );
            }

            if (correctResultToast != null)
            {
                correctResultToast
                    .ShowToast();
            }
        }
        else if (!dangerousRelease)
        {
            // Любая обычная ошибка.
            if (incorrectResultToast != null)
            {
                incorrectResultToast
                    .ShowToast();
            }
        }

        // dangerousRelease здесь
        // ничего не показывает.
        // Сначала должна пройти
        // телефонная сцена.

        rewardCoroutine = null;
    }

    public static bool IsDangerousRelease(
        DirectionEvaluationController
            .EvaluationResult result)
    {
        if (result == null)
            return false;

        return
            result.CorrectDecision ==
                DirectionDecision.Prison &&
            result.SelectedDecision ==
                DirectionDecision.Release;
    }


    public void ApplyDangerousReleasePenalty()
    {
        SessionStatsManager stats =
            SessionStatsManager.Instance;

        if (stats != null)
        {
            stats.ChangeMoney(
                -dangerousReleasePenalty
            );
        }

        if (penaltyResultToast != null)
        {
            penaltyResultToast
                .ShowToast();
        }
    }


    // =====================================================
    // REFERENCES
    // =====================================================

    private void FindReferences()
    {
        if (submitController == null)
        {
            submitController =
                FindFirstObjectByType
                    <DirectionSubmitController>(
                        FindObjectsInactive.Include
                    );
        }

        if (zoomComputerWork == null)
        {
            zoomComputerWork =
                FindFirstObjectByType
                    <ZoomComputerWork>(
                        FindObjectsInactive.Include
                    );
        }
    }


    private void OnValidate()
    {
        experienceReward =
            Mathf.Max(
                0,
                experienceReward
            );

        moneyReward =
            Mathf.Max(
                0,
                moneyReward
            );

        dangerousReleasePenalty =
            Mathf.Max(
                0,
                dangerousReleasePenalty
            );
    }
}
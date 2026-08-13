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
        "Плашка, которая выезжает после неправильного направления."
    )]
    [SerializeField]
    private TaskUpdateToast incorrectResultToast;

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
                    result.IsCorrect
                )
            );
    }

    private IEnumerator RewardAfterReturnRoutine(
        bool isCorrect)
    {
        FindReferences();

        // DirectionSubmitController запускает
        // возврат камеры в тот же кадр.
        // Даём ZoomComputerWork один кадр,
        // чтобы войти в состояние возврата.
        yield return null;

        if (zoomComputerWork != null)
        {
            while (zoomComputerWork.ZoomActive)
            {
                yield return null;
            }
        }

        // Камера полностью вернулась
        // к обычному рабочему положению.

        if (isCorrect)
        {
            // За полностью правильное направление
            // одновременно начисляем
            // стаж и деньги.
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
        else
        {
            // Направление отправлено,
            // но заполнено неправильно.
            // Стаж и деньги не начисляются.
            if (incorrectResultToast != null)
            {
                incorrectResultToast
                    .ShowToast();
            }
        }

        rewardCoroutine = null;
    }

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
    }
}
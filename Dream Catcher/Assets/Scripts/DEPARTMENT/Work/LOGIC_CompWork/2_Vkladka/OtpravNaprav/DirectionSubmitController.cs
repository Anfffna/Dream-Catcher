using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DirectionSubmitController :
    MonoBehaviour
{
    [Header("Кнопка отправки")]

    [Tooltip("Зелёная кнопка «ОТПРАВИТЬ НАПРАВЛЕНИЕ».")]
    [SerializeField]
    private Button submitButton;

    [Header("Проверка направления")]

    [Tooltip("Контроллер текущего заполнения формы.")]
    [SerializeField]
    private DirectionFormController
        formController;

    [Tooltip("Контроллер проверки правильности направления.")]
    [SerializeField]
    private DirectionEvaluationController
        evaluationController;

    [Header("Ошибка заполнения")]

    [Tooltip("Плашка ошибки, которая появляется при попытке отправить незаполненное направление.")]
    [SerializeField]
    private SymptomRequirementWarningController
        incompleteWarning;

    [Header("Исчезающий интерфейс")]

    [Tooltip("Общий RectTransform всего интерфейса с двумя вкладками.")]
    [SerializeField]
    private RectTransform interfaceRoot;

    [Tooltip("Canvas Group общего интерфейса с двумя вкладками.")]
    [SerializeField]
    private CanvasGroup interfaceCanvasGroup;

    [Header("Компьютер")]

    [Tooltip("Контроллер рабочего компьютера.")]
    [SerializeField]
    private WorkComputerController
    computerController;

    [Header("Анимация отправки")]

    [Tooltip("Во сколько раз уменьшить интерфейс перед исчезновением. Например 0.85 означает 85% исходного размера.")]
    [SerializeField]
    [Range(0.01f, 1f)]
    private float targetScaleMultiplier =
        0.85f;

    [Tooltip("Длительность уменьшения и исчезновения.")]
    [SerializeField]
    private float hideDuration =
        0.4f;

    [Header("Возврат к рабочему виду")]

    [Tooltip("Контроллер зума компьютера. Если поле пустое, будет найден автоматически.")]
    [SerializeField]
    private ZoomComputerWork
    zoomComputerWork;

    [Header("Временная проверка")]

    [Tooltip("Пока HUD не готов, писать результат отправки в Console.")]
    [SerializeField]
    private bool printResultToConsole =
        true;

    private Vector3 normalScale;
    private float normalAlpha = 1f;

    private bool submissionLocked;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        FindReferences();
        SaveInitialState();
        AddButtonListener();
    }

    private void LateUpdate()
    {
        if (!submissionLocked)
            return;

        if (hideCoroutine != null)
            return;

        KeepSubmittedInterfaceHidden();
    }

    private void OnDestroy()
    {
        RemoveButtonListener();
    }

    private void HandleSubmitPressed()
    {
        if (submissionLocked)
            return;

        FindReferences();

        if (formController == null ||
            evaluationController == null)
        {
            Debug.LogWarning(
                "DirectionSubmitController: " +
                "не найдены контроллер формы " +
                "или контроллер проверки."
            );

            return;
        }

        // Сначала проверяем только полноту формы.
        if (!formController.IsFormComplete())
        {
            if (incompleteWarning != null)
            {
                incompleteWarning
                    .ShowWarning();
            }

            return;
        }

        // Теперь проверяем правильность.
        bool evaluated =
            evaluationController
                .TryEvaluate(
                    out DirectionEvaluationController
                        .EvaluationResult result
                );

        if (!evaluated ||
            result == null)
        {
            Debug.LogWarning(
                "DirectionSubmitController: " +
                "не удалось проверить текущее дело."
            );

            return;
        }

        submissionLocked = true;

        formController
            .CloseOpenDropdowns();

        if (incompleteWarning != null)
        {
            incompleteWarning
                .HideImmediately();
        }

        if (submitButton != null)
        {
            submitButton.interactable =
                false;
        }

        if (interfaceCanvasGroup != null)
        {
            // Сразу запрещаем клики,
            // пока интерфейс исчезает.
            interfaceCanvasGroup.interactable =
                false;

            interfaceCanvasGroup.blocksRaycasts =
                false;
        }

        if (printResultToConsole)
        {
            PrintEvaluationResult(
                result
            );
        }

        if (computerController != null)
        {
            computerController
                .LockUntilNextClient();
        }

        ClientNPCController currentClient =
            ClientNPCController
                .CurrentActiveClient;

        if (currentClient != null)
        {
            currentClient
                .NotifyDirectionSubmitted();
        }

        // Возврат камеры и исчезновение интерфейса начинаются в одном кадре.
        StartReturnToWorkView();
        StartHideAnimation();
    }

    private void StartReturnToWorkView()
    {
        if (zoomComputerWork == null)
        {
            FindReferences();
        }

        if (zoomComputerWork == null)
            return;

        if (!zoomComputerWork.IsZoomedIn)
            return;

        zoomComputerWork
            .ReturnToWorkView();
    }

    private void StartHideAnimation()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(
                hideCoroutine
            );
        }

        hideCoroutine =
            StartCoroutine(
                HideInterfaceRoutine()
            );
    }

    private void KeepSubmittedInterfaceHidden()
    {
        if (interfaceRoot != null)
        {
            interfaceRoot.localScale =
                normalScale;
        }

        if (interfaceCanvasGroup != null)
        {
            interfaceCanvasGroup.alpha =
                0f;

            interfaceCanvasGroup.interactable =
                false;

            interfaceCanvasGroup.blocksRaycasts =
                false;
        }
    }

    private IEnumerator HideInterfaceRoutine()
    {
        if (interfaceRoot == null)
        {
            hideCoroutine = null;
            yield break;
        }

        Vector3 startScale =
            interfaceRoot.localScale;

        Vector3 targetScale =
            normalScale *
            targetScaleMultiplier;

        float startAlpha =
            interfaceCanvasGroup != null
                ? interfaceCanvasGroup.alpha
                : 1f;

        if (hideDuration <= 0f)
        {
            if (interfaceCanvasGroup != null)
            {
                interfaceCanvasGroup.alpha =
                    0f;

                interfaceCanvasGroup.interactable =
                    false;

                interfaceCanvasGroup.blocksRaycasts =
                    false;
            }

            interfaceRoot.localScale =
                normalScale;

            hideCoroutine = null;

            KeepSubmittedInterfaceHidden();

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < hideDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    hideDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            interfaceRoot.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    smoothT
                );

            if (interfaceCanvasGroup != null)
            {
                interfaceCanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        smoothT
                    );
            }

            yield return null;
        }

        // Анимация полностью закончена.
        if (interfaceCanvasGroup != null)
        {
            interfaceCanvasGroup.alpha =
                0f;

            interfaceCanvasGroup.interactable =
                false;

            interfaceCanvasGroup.blocksRaycasts =
                false;
        }

        // Пока интерфейс уже полностью невидим, мгновенно возвращаем его исходный размер.
        interfaceRoot.localScale =
            normalScale;

        hideCoroutine = null;

        // Интерфейс остаётся активным, но полностью невидимым до следующего дела.
        KeepSubmittedInterfaceHidden();
    }

    public void ResetForNextCase()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(
                hideCoroutine
            );

            hideCoroutine = null;
        }

        submissionLocked = false;

        if (interfaceRoot != null)
        {
            interfaceRoot.gameObject
                .SetActive(true);

            interfaceRoot.localScale =
                normalScale;
        }

        if (interfaceCanvasGroup != null)
        {
            interfaceCanvasGroup.alpha =
                normalAlpha;

            interfaceCanvasGroup.interactable =
                true;

            interfaceCanvasGroup.blocksRaycasts =
                true;
        }

        if (submitButton != null)
        {
            submitButton.interactable =
                true;
        }

        if (incompleteWarning != null)
        {
            incompleteWarning
                .HideImmediately();
        }
    }

    private void PrintEvaluationResult(
        DirectionEvaluationController
            .EvaluationResult result)
    {
        string overallResult =
            result.IsCorrect
                ? "ВЕРНО"
                : "НЕВЕРНО";

        Debug.Log(
            "ОТПРАВКА НАПРАВЛЕНИЯ\n" +
            "Клиент: " +
            result.ClientName +
            "\nВариант: " +
            result.VariantId +
            "\nРешение: " +
            (
                result.DecisionCorrect
                    ? "ВЕРНО"
                    : "НЕВЕРНО"
            ) +
            "\nФизические симптомы: " +
            (
                result.SymptomsCorrect
                    ? "ВЕРНО"
                    : "НЕВЕРНО"
            ) +
            "\nОснование: " +
            (
                result.GroundsCorrect
                    ? "ВЕРНО"
                    : "НЕВЕРНО"
            ) +
            "\nИТОГ: " +
            overallResult
        );
    }

    private void SaveInitialState()
    {
        if (interfaceRoot != null)
        {
            normalScale =
                interfaceRoot.localScale;
        }
        else
        {
            normalScale =
                Vector3.one;
        }

        if (interfaceCanvasGroup != null)
        {
            normalAlpha =
                interfaceCanvasGroup.alpha;
        }
    }

    private void AddButtonListener()
    {
        if (submitButton == null)
            return;

        submitButton.onClick
            .RemoveListener(
                HandleSubmitPressed
            );

        submitButton.onClick
            .AddListener(
                HandleSubmitPressed
            );
    }

    private void RemoveButtonListener()
    {
        if (submitButton == null)
            return;

        submitButton.onClick
            .RemoveListener(
                HandleSubmitPressed
            );
    }

    private void FindReferences()
    {
        if (formController == null)
        {
            formController =
                FindFirstObjectByType
                    <DirectionFormController>(
                        FindObjectsInactive.Include
                    );
        }

        if (evaluationController == null)
        {
            evaluationController =
                FindFirstObjectByType
                    <DirectionEvaluationController>(
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

        if (interfaceRoot != null &&
            interfaceCanvasGroup == null)
        {
            interfaceCanvasGroup =
                interfaceRoot
                    .GetComponent<CanvasGroup>();
        }

        if (computerController == null)
        {
            computerController =
                FindFirstObjectByType
                    <WorkComputerController>(
                        FindObjectsInactive.Include
                    );
        }
    }

    private void OnValidate()
    {
        targetScaleMultiplier =
            Mathf.Clamp(
                targetScaleMultiplier,
                0.01f,
                1f
            );

        hideDuration =
            Mathf.Max(
                0f,
                hideDuration
            );
    }
}
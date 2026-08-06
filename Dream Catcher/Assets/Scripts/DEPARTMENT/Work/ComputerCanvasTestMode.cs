using System.Collections;
using UnityEngine;

public class ComputerCanvasTestMode :
    MonoBehaviour
{
    [Header("Тестовый режим")]
    [Tooltip("Автоматически открыть готовый интерфейс компьютера после загрузки сцены.")]
    [SerializeField] private bool enableTestMode;

    [Tooltip("Задержка после загрузки сохранения, чтобы все глобальные системы успели восстановиться.")]
    [SerializeField] private float delayAfterSceneLoad = 1f;

    [Tooltip("Сколько максимум ждать завершения посадки.")]
    [SerializeField] private float seatingTimeout = 5f;

    [Header("Основные системы")]
    [SerializeField] private WorkSessionManager sessionManager;
    [SerializeField] private WorkComputerController computerController;
    [SerializeField] private ZoomComputerWork zoomComputerWork;

    [Header("Скрываемые объекты")]
    [Tooltip("NPC, SON-3 и другие объекты обычной последовательности.")]
    [SerializeField] private GameObject[] objectsToDisable;

    private bool testApplied;

    private IEnumerator Start()
    {
#if UNITY_EDITOR
        if (!enableTestMode ||
            testApplied)
        {
            yield break;
        }

        // Ждём полного исчезновения загрузочного экрана.
        while (LoadingManager.Instance != null &&
               LoadingManager.Instance.IsLoading)
        {
            yield return null;
        }

        yield return null;
        yield return null;

        // Даём PauseManager завершить последний кадр блокировки курсора.
        yield return null;
        yield return null;

        // Ждём завершения загрузки сохранения и сброса старого состояния.
        if (delayAfterSceneLoad > 0f)
        {
            yield return new WaitForSecondsRealtime(
                delayAfterSceneLoad
            );
        }

        // Дополнительно пропускаем два кадра.
        yield return null;
        yield return null;

        FindReferences();

        if (sessionManager == null ||
            computerController == null ||
            zoomComputerWork == null)
        {
            yield break;
        }

        testApplied = true;

        DisableSequenceObjects();

        // Сначала запускаем настоящую рабочую посадку.
        if (!sessionManager.IsSeated)
        {
            sessionManager.StartWork();

            float elapsed = 0f;

            while (!sessionManager.IsSeated &&
                   elapsed < seatingTimeout)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                yield return null;
            }
        }

        // Без завершённой посадки зум не запускаем.
        if (!sessionManager.IsSeated)
        {
            testApplied = false;
            yield break;
        }

        // Даём контроллеру посадки полностью закончить последний кадр.
        yield return null;
        yield return null;

        // Устанавливаем компьютер в готовое состояние.
        computerController
            .PrepareCanvasForTest();

        yield return null;

        // Запускаем настоящий зум из уже правильной сидячей позиции.
        zoomComputerWork.StartZoom();

        // В тестовом режиме курсор должен точно остаться видимым.
        if (sessionManager.cursorController != null)
        {
            sessionManager
                .cursorController
                .ShowWorkCursor();

            sessionManager
                .cursorController
                .SetDefaultCursor();
        }
#endif
    }

    private void DisableSequenceObjects()
    {
        if (objectsToDisable == null)
            return;

        for (int i = 0;
             i < objectsToDisable.Length;
             i++)
        {
            GameObject target =
                objectsToDisable[i];

            if (target != null)
                target.SetActive(false);
        }
    }

    private void FindReferences()
    {
        if (sessionManager == null)
        {
            sessionManager =
                WorkSessionManager.Instance;
        }

        if (sessionManager == null)
        {
            sessionManager =
                FindFirstObjectByType
                    <WorkSessionManager>(
                        FindObjectsInactive.Include
                    );
        }

        if (computerController == null)
        {
            computerController =
                FindFirstObjectByType
                    <WorkComputerController>(
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
}
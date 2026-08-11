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
    #if !UNITY_EDITOR
            yield break;
    #else

        if (!enableTestMode || testApplied)
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

        if (delayAfterSceneLoad > 0f)
        {
            yield return new WaitForSecondsRealtime(
                delayAfterSceneLoad
            );
        }

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

        if (!sessionManager.IsSeated)
        {
            sessionManager.StartWork();

            float elapsed = 0f;

            while (!sessionManager.IsSeated &&
                   elapsed < seatingTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (!sessionManager.IsSeated)
        {
            testApplied = false;
            yield break;
        }

        yield return null;
        yield return null;

        computerController.PrepareCanvasForTest();

        yield return null;

        zoomComputerWork.StartZoom();

        if (sessionManager.cursorController != null)
        {
            sessionManager.cursorController.ShowWorkCursor();
            sessionManager.cursorController.SetDefaultCursor();
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
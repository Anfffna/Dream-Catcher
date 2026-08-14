using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VisitorQueueManager :
    MonoBehaviour
{
    // =====================================================
    // ДАННЫЕ ОДНОГО ЧЕЛОВЕКА В СЦЕНЕ
    // =====================================================

    [Serializable]
    public class VisitorQueueEntry
    {
        [Tooltip(
            "Конкретный NPC этого человека, " +
            "который уже находится в сцене."
        )]
        [SerializeField]
        private ClientNPCController npc;

        [Tooltip(
            "Данные этого человека и его вариантов дела."
        )]
        [SerializeField]
        private VisitorCaseData visitorData;

        public ClientNPCController NPC =>
            npc;

        public VisitorCaseData VisitorData =>
            visitorData;
    }


    // =====================================================
    // RUNTIME-ЗАПИСЬ
    // =====================================================

    private class RuntimeVisitorEntry
    {
        public ClientNPCController NPC;
        public VisitorCaseData VisitorData;
        public int VariantIndex;
    }


    // =====================================================
    // INSTANCE
    // =====================================================

    public static VisitorQueueManager Instance
    {
        get;
        private set;
    }


    // =====================================================
    // ОЧЕРЕДЬ
    // =====================================================

    [Header("Посетители текущей смены")]

    [Tooltip(
        "Шесть посетителей этой смены. " +
        "Каждая запись связывает NPC в сцене " +
        "с его VisitorCaseData."
    )]
    [SerializeField]
    private List<VisitorQueueEntry> visitors =
        new List<VisitorQueueEntry>();


    [Header("Количество посетителей")]

    [Tooltip(
        "Сколько посетителей должно пройти " +
        "за одну полную смену."
    )]
    [SerializeField]
    private int visitorsPerShift = 6;


    [Header("Первый обучающий посетитель")]

    [Tooltip(
        "Если включено, один посетитель " +
        "всегда остаётся первым. " +
        "Для первого дня оставить включённым."
    )]
    [SerializeField]
    private bool keepTutorialVisitorFirst =
        true;

    [Tooltip(
        "Индекс обучающего посетителя " +
        "в списке Visitors. " +
        "Для текущего первого клиента — 0."
    )]
    [SerializeField]
    private int tutorialVisitorIndex = 0;


    // =====================================================
    // ОБЩИЕ СИСТЕМЫ
    // =====================================================

    [Header("Общие системы рабочего места")]

    [Tooltip(
        "Общий блок информации о текущем клиенте."
    )]
    [SerializeField]
    private ClientInfoPanelController
        clientInfoPanel;

    [Tooltip(
        "Контроллер красной кнопки сброса формы. " +
        "Используется между клиентами."
    )]
    [SerializeField]
    private DirectionFormResetController
        directionFormResetController;

    [Tooltip(
        "Общий контроллер отправки направления."
    )]
    [SerializeField]
    private DirectionSubmitController
        directionSubmitController;

    [Tooltip(
        "Общий рабочий компьютер."
    )]
    [SerializeField]
    private WorkComputerController
        computerController;

    [Tooltip(
    "Навигация вкладок общего рабочего компьютера."
    )]
    [SerializeField]
    private ComputerInterfaceNavigation
    computerNavigation;


    // =====================================================
    // ЗАВЕРШЕНИЕ СМЕНЫ
    // =====================================================

    [Header("Завершение очереди")]

    [Tooltip(
        "Вызывается после полного завершения " +
        "последнего, шестого посетителя " +
        "и всех обязательных ситуаций после него."
    )]
    [SerializeField]
    private UnityEvent onQueueFinished;


    public event Action QueueFinished;


    // =====================================================
    // RUNTIME
    // =====================================================

    private readonly List<RuntimeVisitorEntry>
        runtimeQueue =
            new List<RuntimeVisitorEntry>();

    private readonly HashSet<UnityEngine.Object>
        nextVisitorBlockers =
            new HashSet<UnityEngine.Object>();


    private int currentVisitorIndex =
        -1;

    private RuntimeVisitorEntry
        currentVisitor;

    private Coroutine advanceCoroutine;

    private bool queueStarted;

    private bool queueFinished;


    // =====================================================
    // PUBLIC STATE
    // =====================================================

    public bool QueueStarted =>
        queueStarted;

    public bool QueueFinishedState =>
        queueFinished;

    public int CurrentVisitorIndex =>
        currentVisitorIndex;

    public int ProcessedVisitorCount =>
        Mathf.Clamp(
            currentVisitorIndex,
            0,
            runtimeQueue.Count
        );

    public int TotalVisitorCount =>
        runtimeQueue.Count;


    public bool IsNextVisitorBlocked
    {
        get
        {
            CleanupDestroyedBlockers();

            return nextVisitorBlockers.Count > 0;
        }
    }


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        FindReferences();
    }


    private void OnDestroy()
    {
        UnsubscribeFromCurrentVisitor();

        if (Instance == this)
        {
            Instance = null;
        }
    }


    // =====================================================
    // СТАРТ ОЧЕРЕДИ
    // =====================================================

    public void StartQueue()
    {
        if (queueStarted)
            return;

        FindReferences();

        if (!BuildRuntimeQueue())
            return;

        queueStarted = true;
        queueFinished = false;

        currentVisitorIndex = 0;

        StartCurrentVisitor();
    }


    // =====================================================
    // ПОСТРОЕНИЕ РАНДОМНОЙ ОЧЕРЕДИ
    // =====================================================

    private bool BuildRuntimeQueue()
    {
        runtimeQueue.Clear();

        if (visitors == null ||
            visitors.Count !=
                visitorsPerShift)
        {
            Debug.LogError(
                "VisitorQueueManager: " +
                "в списке Visitors должно быть ровно " +
                visitorsPerShift +
                " посетителей."
            );

            return false;
        }


        List<VisitorQueueEntry> pool =
            new List<VisitorQueueEntry>();


        for (int i = 0;
             i < visitors.Count;
             i++)
        {
            VisitorQueueEntry entry =
                visitors[i];

            if (entry == null ||
                entry.NPC == null ||
                entry.VisitorData == null)
            {
                Debug.LogError(
                    "VisitorQueueManager: " +
                    "у одного из посетителей " +
                    "не назначен NPC или VisitorCaseData."
                );

                return false;
            }


            if (entry.VisitorData
                    .VariantCount <= 0)
            {
                Debug.LogError(
                    "VisitorQueueManager: " +
                    "у посетителя " +
                    entry.VisitorData.ClientName +
                    " нет ни одного варианта дела."
                );

                return false;
            }


            pool.Add(entry);
        }


        // -------------------------------------------------
        // ПЕРВЫЙ ОБУЧАЮЩИЙ
        // -------------------------------------------------

        VisitorQueueEntry tutorialEntry =
            null;


        if (keepTutorialVisitorFirst)
        {
            int safeTutorialIndex =
                Mathf.Clamp(
                    tutorialVisitorIndex,
                    0,
                    pool.Count - 1
                );

            tutorialEntry =
                pool[safeTutorialIndex];

            pool.RemoveAt(
                safeTutorialIndex
            );
        }


        // -------------------------------------------------
        // ПЕРЕМЕШИВАЕМ ОСТАЛЬНЫХ
        // -------------------------------------------------

        Shuffle(
            pool
        );


        // -------------------------------------------------
        // ПЕРВЫЙ ОСТАЁТСЯ ПЕРВЫМ
        // -------------------------------------------------

        if (tutorialEntry != null)
        {
            AddRuntimeEntry(
                tutorialEntry
            );
        }


        // -------------------------------------------------
        // ОСТАЛЬНЫЕ В РАНДОМНОМ ПОРЯДКЕ
        // -------------------------------------------------

        for (int i = 0;
             i < pool.Count;
             i++)
        {
            AddRuntimeEntry(
                pool[i]
            );
        }


        return
            runtimeQueue.Count ==
            visitorsPerShift;
    }


    private void AddRuntimeEntry(
        VisitorQueueEntry source)
    {
        if (source == null ||
            source.NPC == null ||
            source.VisitorData == null)
        {
            return;
        }


        // Variant выбирается ОДИН РАЗ
        // при построении очереди.
        int variantIndex =
            source.VisitorData
                .GetRandomVariantIndex();


        RuntimeVisitorEntry runtimeEntry =
            new RuntimeVisitorEntry
            {
                NPC =
                    source.NPC,

                VisitorData =
                    source.VisitorData,

                VariantIndex =
                    variantIndex
            };


        runtimeQueue.Add(
            runtimeEntry
        );
    }


    private void Shuffle(
        List<VisitorQueueEntry> list)
    {
        if (list == null)
            return;


        // Fisher-Yates.
        for (int i = list.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );


            VisitorQueueEntry temp =
                list[i];

            list[i] =
                list[randomIndex];

            list[randomIndex] =
                temp;
        }
    }


    // =====================================================
    // ЗАПУСК ТЕКУЩЕГО ПОСЕТИТЕЛЯ
    // =====================================================

    private void StartCurrentVisitor()
    {
        if (queueFinished)
            return;


        if (currentVisitorIndex < 0 ||
            currentVisitorIndex >=
                runtimeQueue.Count)
        {
            FinishQueue();
            return;
        }


        currentVisitor =
            runtimeQueue[
                currentVisitorIndex
            ];


        if (currentVisitor == null ||
            currentVisitor.NPC == null ||
            currentVisitor.VisitorData == null)
        {
            return;
        }


        VisitorCaseData
            .VisitorCaseVariant variant =
                currentVisitor
                    .VisitorData
                    .GetVariant(
                        currentVisitor
                            .VariantIndex
                    );


        if (variant == null)
        {
            return;
        }


        // Очередь первой фиксирует
        // единственную настоящую пару:
        // человек + выбранный вариант.
        CurrentClientContext
            .SetCurrentCase(
                currentVisitor
                    .VisitorData,
                variant
            );


        // Передаём те же данные
        // конкретному NPC.
        currentVisitor
            .NPC
            .Initialize(
                currentVisitor
                    .VisitorData,
                currentVisitor
                    .VariantIndex,
                clientInfoPanel
            );


        SubscribeToCurrentVisitor();


        // NPC сам:
        // - включает TrackClient;
        // - запускает Podhodit;
        // - ведёт весь свой обычный цикл.
        currentVisitor
            .NPC
            .StartApproach();
    }


    // =====================================================
    // CLIENT FINISHED
    // =====================================================

    private void SubscribeToCurrentVisitor()
    {
        if (currentVisitor == null ||
            currentVisitor.NPC == null)
        {
            return;
        }


        currentVisitor
            .NPC
            .ClientFinished -=
                HandleClientFinished;


        currentVisitor
            .NPC
            .ClientFinished +=
                HandleClientFinished;
    }


    private void UnsubscribeFromCurrentVisitor()
    {
        if (currentVisitor == null ||
            currentVisitor.NPC == null)
        {
            return;
        }


        currentVisitor
            .NPC
            .ClientFinished -=
                HandleClientFinished;
    }


    private void HandleClientFinished(
        ClientNPCController client)
    {
        if (currentVisitor == null ||
            client != currentVisitor.NPC)
        {
            return;
        }


        UnsubscribeFromCurrentVisitor();


        if (advanceCoroutine != null)
            return;


        advanceCoroutine =
            StartCoroutine(
                AdvanceQueueRoutine()
            );
    }


    // =====================================================
    // ПЕРЕХОД К СЛЕДУЮЩЕМУ
    // =====================================================

    private IEnumerator AdvanceQueueRoutine()
    {
        // -------------------------------------------------
        // СНАЧАЛА ЖДЁМ ВСЕ ОСОБЫЕ СИТУАЦИИ.
        //
        // Телефон начальника,
        // будущая взятка,
        // охрана и т.д.
        // -------------------------------------------------
        // Даём всем подписчикам ClientFinished
        // полностью обработать событие.
        //
        // Это позволит будущим системам
        // взятки, охраны и другим ситуациям
        // успеть заблокировать следующего клиента
        // в том же кадре.
        yield return null;

        while (IsNextVisitorBlocked)
        {
            yield return null;
        }


        // -------------------------------------------------
        // ТЕПЕРЬ МОЖНО УБРАТЬ СТАРОЕ ДЕЛО
        // -------------------------------------------------

        PrepareCommonSystemsForNextVisitor();


        currentVisitorIndex++;


        // -------------------------------------------------
        // ШЕСТЬ ЧЕЛОВЕК ЗАКОНЧИЛИСЬ
        // -------------------------------------------------

        if (currentVisitorIndex >=
            runtimeQueue.Count)
        {
            FinishQueue();

            advanceCoroutine =
                null;

            yield break;
        }


        // Один кадр даём UI
        // применить полный reset.
        yield return null;


        StartCurrentVisitor();


        advanceCoroutine =
            null;
    }


    // =====================================================
    // RESET ОБЩИХ СИСТЕМ
    // =====================================================

    private void PrepareCommonSystemsForNextVisitor()
    {
        // =====================================================
        // ВКЛАДКИ
        // =====================================================

        // Каждое новое дело начинается
        // с первой вкладки — записи сна.
        if (computerNavigation != null)
        {
            computerNavigation
                .ResetForNextClient();
        }


        // =====================================================
        // ФОРМА
        // =====================================================

        if (directionFormResetController !=
            null)
        {
            directionFormResetController
                .ResetForm();
        }


        // =====================================================
        // SUBMIT
        // =====================================================

        // Сначала полностью подготавливаем
        // внутренний интерфейс нового дела.
        //
        // ResetForNextCase может включать
        // свой Interface Root обратно,
        // поэтому он ОБЯЗАТЕЛЬНО должен
        // выполняться ДО финального скрытия
        // Canvas компьютера.
        if (directionSubmitController !=
            null)
        {
            directionSubmitController
                .ResetForNextCase();
        }


        // =====================================================
        // СТАРЫЕ ДАННЫЕ
        // =====================================================

        if (clientInfoPanel != null)
        {
            clientInfoPanel
                .ClearClient();
        }
        else
        {
            CurrentClientContext
                .Clear();
        }


        // =====================================================
        // КОМПЬЮТЕР — ВСЕГДА ПОСЛЕДНИМ
        // =====================================================

        // После ВСЕХ reset'ов окончательно
        // скрываем рабочий Canvas.
        //
        // Он останется невидимым до тех пор,
        // пока новый SON-3 полностью
        // не окажется в Tray.
        if (computerController != null)
        {
            computerController
                .PrepareForNextClient();
        }
    }


    // =====================================================
    // ОБЩИЙ БЛОКИРОВЩИК СЛЕДУЮЩЕГО NPC
    // =====================================================

    public void BlockNextVisitor(
        UnityEngine.Object source)
    {
        if (source == null)
            return;


        nextVisitorBlockers.Add(
            source
        );
    }


    public void ReleaseNextVisitor(
        UnityEngine.Object source)
    {
        if (source == null)
            return;


        nextVisitorBlockers.Remove(
            source
        );
    }


    private void CleanupDestroyedBlockers()
    {
        nextVisitorBlockers
            .RemoveWhere(
                blocker =>
                    blocker == null
            );
    }


    // =====================================================
    // КОНЕЦ ОЧЕРЕДИ
    // =====================================================

    private void FinishQueue()
    {
        if (queueFinished)
            return;


        queueFinished = true;


        UnsubscribeFromCurrentVisitor();


        currentVisitor = null;


        if (clientInfoPanel != null)
        {
            clientInfoPanel
                .ClearClient();
        }
        else
        {
            CurrentClientContext
                .Clear();
        }


        QueueFinished?.Invoke();

        onQueueFinished?.Invoke();
    }


    // =====================================================
    // REFERENCES
    // =====================================================

    private void FindReferences()
    {
        if (clientInfoPanel == null)
        {
            clientInfoPanel =
                FindFirstObjectByType
                    <ClientInfoPanelController>(
                        FindObjectsInactive
                            .Include
                    );
        }


        if (directionFormResetController ==
            null)
        {
            directionFormResetController =
                FindFirstObjectByType
                    <DirectionFormResetController>(
                        FindObjectsInactive
                            .Include
                    );
        }


        if (directionSubmitController ==
            null)
        {
            directionSubmitController =
                FindFirstObjectByType
                    <DirectionSubmitController>(
                        FindObjectsInactive
                            .Include
                    );
        }


        if (computerController == null)
        {
            computerController =
                FindFirstObjectByType
                    <WorkComputerController>(
                        FindObjectsInactive
                            .Include
                    );
        }

        if (computerNavigation == null)
        {
            computerNavigation =
                FindFirstObjectByType
                    <ComputerInterfaceNavigation>(
                        FindObjectsInactive
                            .Include
                    );
        }
    }


    // =====================================================
    // INSPECTOR
    // =====================================================

    private void OnValidate()
    {
        visitorsPerShift =
            Mathf.Max(
                1,
                visitorsPerShift
            );


        tutorialVisitorIndex =
            Mathf.Max(
                0,
                tutorialVisitorIndex
            );
    }
}
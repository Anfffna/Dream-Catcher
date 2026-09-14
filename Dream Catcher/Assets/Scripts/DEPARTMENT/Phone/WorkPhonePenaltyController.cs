using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkPhonePenaltyController :
    MonoBehaviour,
    IInteractable
{
    [Header("Отправка направления")]

    [Tooltip(
        "Контроллер отправки направления. " +
        "Если пусто — найдётся автоматически."
    )]
    [SerializeField]
    private DirectionSubmitController submitController;


    [Header("Награда и штраф")]

    [Tooltip(
        "Контроллер наград, в котором " +
        "находится штрафная плашка " +
        "и списание денег."
    )]
    [SerializeField]
    private WorkDirectionRewardController rewardController;


    [Header("Телефон")]

    [Tooltip(
        "Animator телефона. " +
        "Если пусто — найдётся на этом объекте."
    )]
    [SerializeField]
    private Animator phoneAnimator;

    [Tooltip(
        "Collider телефона. " +
        "После звонка автоматически " +
        "получает слой Interactable."
    )]
    [SerializeField]
    private Collider phoneCollider;


    [Header("Звонок")]

    [Tooltip(
        "AudioSource звонка. " +
        "Loop = On, Play On Awake = Off."
    )]
    [SerializeField]
    private AudioSource ringingAudioSource;

    [Header("Ручное использование")]

    [Tooltip(
    "Контроллер ручного использования телефона."
    )]
    [SerializeField]
    private WorkPhoneManualController
    manualPhoneController;

    [Header("Вибрация")]

    [Tooltip(
        "Насколько сильно телефон " +
        "двигается влево-вправо."
    )]
    [SerializeField]
    private float vibrationAmount =
        0.0025f;

    [Tooltip(
        "Скорость вибрации."
    )]
    [SerializeField]
    private float vibrationSpeed =
        55f;


    [Header("Телефонный диалог")]

    [Tooltip(
        "Реплики телефонного разговора."
    )]
    [SerializeField]
    private List<DialogueManager.DialogueLine>
        phoneDialogue =
            new List<DialogueManager.DialogueLine>();


    // =====================================================
    // КОНСТАНТЫ
    // =====================================================

    private const string InteractableLayerName =
        "Interactable";

    private const string CallBossTrigger =
        "CallBoss";
    private const string NoCallBossTrigger =
        "NoCallBoss";
    private const string TakePhoneTrigger =
    "TakePhone";
    private const string PutPhoneTrigger =
        "PutPhone";

    private const string CallEarTrigger = "CallEar";

    private const float AnimationTimeout =
        10f;

    public bool PenaltyCallPendingOrActive =>
    dangerousCallPending ||
    waitingForPhoneClick ||
    phoneSequenceActive;

    // =====================================================
    // RUNTIME
    // =====================================================

    private DialogueManager dialogueManager;

    private ClientNPCController pendingClient;

    private Coroutine phoneCoroutine;
    private Coroutine vibrationCoroutine;


    // =====================================================
    // ВИБРАЦИЯ
    // =====================================================

    private Vector3 vibrationStartPosition;

    private bool vibrationPositionStored;

    private bool animatorWasEnabledBeforeVibration;

    private bool lastPhoneInteractableState;


    // =====================================================
    // СОСТОЯНИЕ ТЕЛЕФОНА
    // =====================================================

    private bool dangerousCallPending;
    private bool waitingForPhoneClick;
    private bool phoneSequenceActive;
    private int originalPhoneLayer;


    // =====================================================
    // ПРИКРЕПЛЕНИЕ К КАМЕРЕ
    // =====================================================

    [Header("Следование за камерой")]
    [SerializeField] private PhoneCameraFollow cameraFollow;

    public bool LastAnimationSucceeded { get; private set; }

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        FindReferences();


        // Запоминаем настоящий исходный Layer
        // объекта с Collider.
        if (phoneCollider != null)
        {
            originalPhoneLayer =
                phoneCollider
                    .gameObject
                    .layer;
        }


        // При запуске игры телефон
        // НЕ двигаем вообще.
        //
        // Только гарантируем,
        // что он пока не Interactable.
        MakePhoneNotInteractable();
    }

    private void Update()
    {
        bool manualInteractionAvailable =
            manualPhoneController != null &&
            manualPhoneController
                .WantsPhoneColliderInteractable &&
            !dangerousCallPending &&
            !phoneSequenceActive;

        bool shouldBeInteractable =
            waitingForPhoneClick ||
            manualInteractionAvailable;

        if (shouldBeInteractable ==
            lastPhoneInteractableState)
        {
            return;
        }

        lastPhoneInteractableState =
            shouldBeInteractable;

        if (shouldBeInteractable)
        {
            MakePhoneInteractable();
        }
        else
        {
            MakePhoneNotInteractable();
        }
    }

    private void OnEnable()
    {
        FindReferences();
        Subscribe();
    }


    private void OnDisable()
    {
        Unsubscribe();

        UnsubscribeFromPendingClient();


        if (phoneCoroutine != null)
        {
            StopCoroutine(
                phoneCoroutine
            );

            phoneCoroutine = null;
        }


        StopRinging();

        StopPhoneVibration();


        // При выключении объекта не оставляем незавершённую позу.
        if (cameraFollow != null)
            cameraFollow.ResetToDesk();

        MakePhoneNotInteractable();


        dangerousCallPending = false;

        waitingForPhoneClick = false;

        phoneSequenceActive = false;

        if (VisitorQueueManager.Instance != null)
        {
            VisitorQueueManager.Instance
                .ReleaseNextVisitor(this);
        }
    }


    // =====================================================
    // ПОДПИСКА НА ОТПРАВКУ НАПРАВЛЕНИЯ
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
    // ПРОВЕРКА ОПАСНОЙ ОШИБКИ
    // =====================================================

    private void HandleDirectionSubmitted(
        DirectionEvaluationController
            .EvaluationResult result)
    {
        // Телефонная ситуация нужна
        // ТОЛЬКО при конкретной ошибке:
        //
        // правильно было Prison,
        // игрок выбрал Release.
        if (!WorkDirectionRewardController
            .IsDangerousRelease(result))
        {
            return;
        }


        if (dangerousCallPending ||
            phoneSequenceActive)
        {
            return;
        }


        // Запоминаем опасную ошибку.
        //
        // НО текущему NPC вообще
        // не мешаем закончить свой цикл.
        dangerousCallPending = true;

        if (VisitorQueueManager.Instance != null)
        {
            VisitorQueueManager.Instance
                .BlockNextVisitor(this);
        }

        pendingClient =
            ClientNPCController
                .CurrentActiveClient;


        // В нормальной рабочей ситуации
        // здесь всегда должен быть
        // текущий активный клиент.
        //
        // Но не оставляем систему
        // заблокированной навечно,
        // если ссылки почему-то нет.
        if (pendingClient == null)
        {
            dangerousCallPending = false;

            if (VisitorQueueManager.Instance != null)
            {
                VisitorQueueManager.Instance
                    .ReleaseNextVisitor(this);
            }

            return;
        }


        pendingClient.ClientFinished -=
            HandlePendingClientFinished;

        pendingClient.ClientFinished +=
            HandlePendingClientFinished;
    }


    // =====================================================
    // КЛИЕНТ ПОЛНОСТЬЮ ЗАКОНЧИЛСЯ
    // =====================================================

    private void HandlePendingClientFinished(
        ClientNPCController client)
    {
        if (client != pendingClient)
            return;


        UnsubscribeFromPendingClient();


        dangerousCallPending = false;

        phoneSequenceActive = true;


        phoneCoroutine =
            StartCoroutine(
                BeginPhoneCallRoutine()
            );
    }


    private void UnsubscribeFromPendingClient()
    {
        if (pendingClient == null)
            return;


        pendingClient.ClientFinished -=
            HandlePendingClientFinished;

        pendingClient = null;
    }


    // =====================================================
    // НАЧАЛО ЗВОНКА
    // =====================================================

    private IEnumerator BeginPhoneCallRoutine()
    {
        FindReferences();


        // Звонок начинается.
        StartRinging();


        // Одновременно телефон начинает
        // постоянно вибрировать.
        StartPhoneVibration();


        // Маленькая задержка нужна только
        // для ощущения начала звонка.
        //
        // Вибрация после неё НЕ прекращается.
        yield return
            new WaitForSecondsRealtime(
                0.25f
            );


        // Телефон автоматически
        // получает слой Interactable.
        MakePhoneInteractable();


        waitingForPhoneClick = true;

        phoneCoroutine = null;
    }


    // =====================================================
    // IINTERACTABLE
    // =====================================================

    public void Interact()
    {
        // =====================================================
        // СТАРЫЙ ВХОДЯЩИЙ ЗВОНОК БОССА
        // ИМЕЕТ МАКСИМАЛЬНЫЙ ПРИОРИТЕТ.
        // =====================================================

        if (waitingForPhoneClick)
        {
            if (phoneCoroutine != null)
                return;

            if (!CanUseCameraMotion())
                return;

            waitingForPhoneClick = false;

            MakePhoneNotInteractable();

            StopRinging();
            StopPhoneVibration();

            phoneCoroutine =
                StartCoroutine(
                    AnswerPhoneRoutine()
                );

            return;
        }


        // =====================================================
        // ЕСЛИ ШТРАФНОЙ ЗВОНОК УЖЕ ГОТОВИТСЯ
        // ИЛИ ИДЁТ — РУЧНОЙ ТЕЛЕФОН НЕ ТРОГАЕМ.
        // =====================================================

        if (dangerousCallPending ||
            phoneSequenceActive ||
            phoneCoroutine != null)
        {
            return;
        }


        // =====================================================
        // ОБЫЧНОЕ РУЧНОЕ ИСПОЛЬЗОВАНИЕ
        // =====================================================

        if (manualPhoneController != null)
        {
            manualPhoneController
                .TryOpenPhone();
        }
    }


    // =====================================================
    // TAKE
    // ->
    // ПРИКРЕПЛЕНИЕ К КАМЕРЕ
    // ->
    // ДИАЛОГ
    // ->
    // ОТКРЕПЛЕНИЕ
    // ->
    // PUT
    // ->
    // ШТРАФ
    // =====================================================

    private IEnumerator AnswerPhoneRoutine()
    {
        FindReferences();


        // -------------------------------------------------
        // TAKE PHONE
        // -------------------------------------------------

        // Локальная анимация + плавная коррекция родителя относительно камеры.
        yield return StartCoroutine(
            PlayTriggeredAnimation(
                CallBossTrigger
            )
        );


        // -------------------------------------------------
        // ПРИКРЕПЛЯЕМ К КАМЕРЕ
        // -------------------------------------------------

        if (!LastAnimationSucceeded)
        {
            AbortPhoneMotion();
            // Ошибка настройки не запускает диалог/штраф. Можно исправить и ответить снова.
            StartRinging();
            StartPhoneVibration();
            waitingForPhoneClick = true;
            MakePhoneInteractable();
            phoneCoroutine = null;
            yield break;
        }

        AttachPhoneToEar();


        // -------------------------------------------------
        // ДИАЛОГ
        // -------------------------------------------------

        FindDialogueManager();


        // На всякий случай ждём,
        // если предыдущий диалог
        // закрывается именно сейчас.
        while (DialogueManager
            .AnyDialogueActive)
        {
            yield return null;
        }


        if (dialogueManager != null &&
            phoneDialogue != null &&
            phoneDialogue.Count > 0)
        {
            dialogueManager.StartDialogue(
                phoneDialogue,
                false
            );


            while (dialogueManager != null &&
                   dialogueManager.DialogueActive)
            {
                yield return null;
            }
        }


        // -------------------------------------------------
        // ОТКРЕПЛЯЕМ ОТ КАМЕРЫ
        // -------------------------------------------------

        // Совместимый вызов. Коррекция не выключается резко:
        // она плавно уменьшается внутри NoCallBoss.
        DetachPhoneFromCamera();


        // -------------------------------------------------
        // PUT PHONE
        // -------------------------------------------------

        // Animator всё время работает в неизменном локальном пространстве.
        yield return StartCoroutine(
            PlayTriggeredAnimation(
                NoCallBossTrigger
            )
        );
        if (!LastAnimationSucceeded)
            AbortPhoneMotion();


        // -------------------------------------------------
        // ШТРАФ
        // -------------------------------------------------

        if (rewardController != null)
        {
            rewardController
                .ApplyDangerousReleasePenalty();
        }

        if (VisitorQueueManager.Instance != null)
        {
            VisitorQueueManager.Instance
                .ReleaseNextVisitor(this);
        }


        phoneSequenceActive = false;

        phoneCoroutine = null;
    }


    // =====================================================
    // ПРИКРЕПЛЕНИЕ К КАМЕРЕ
    // =====================================================

    // Эти методы сохранены для совместимости со старой телефонной логикой.
    // Реальная привязка теперь определяется состоянием Animator в PhoneCameraFollow.
    // Переподчинять Phone камере и выключать Animator больше нельзя.
    private void AttachPhoneToEar() { }
    private void AttachPhoneToFace() { }
    public void PreparePhoneForCameraHold() { }
    private void DetachPhoneFromCamera() { }

    public bool CanUseCameraMotion()
    {
        if (cameraFollow == null)
            cameraFollow = GetComponent<PhoneCameraFollow>();
        if (cameraFollow == null)
        {
            Debug.LogError("[Phone] Добавь PhoneCameraFollow на тот же объект Phone.", this);
            return false;
        }
        return cameraFollow.TryPrepare();
    }

    public void AbortPhoneMotion()
    {
        if (cameraFollow != null)
            cameraFollow.ResetToDesk();
    }

    // =====================================================
    // ANIMATOR
    //
    // Используем только Trigger.
    //
    // Имена конечных состояний берём из PhoneCameraFollow.
    // =====================================================

    public IEnumerator PlayManualTakeAnimation()
    {
        yield return
            PlayTriggeredAnimation(
                TakePhoneTrigger
            );
    }


    public IEnumerator PlayManualCallEarAnimation()
    {
        yield return PlayTriggeredAnimation(CallEarTrigger);
    }

    public IEnumerator PlayManualPutAnimation()
    {
        FindReferences();
        // A call finishes at the ear; browsing contacts finishes at the face.
        bool atEar = cameraFollow != null &&
            cameraFollow.IsStableState(cameraFollow.HoldEarState);
        yield return PlayTriggeredAnimation(
            atEar ? NoCallBossTrigger : PutPhoneTrigger);
    }


    public void AttachPhoneForManualUse()
    {
        AttachPhoneToFace();
    }


    public void DetachPhoneForManualUse()
    {
        DetachPhoneFromCamera();
    }

    private IEnumerator PlayTriggeredAnimation(string triggerName)
    {
        LastAnimationSucceeded = false;
        FindReferences();
        if (!CanUseCameraMotion() || phoneAnimator == null)
            yield break;

        string targetState;
        if (triggerName == TakePhoneTrigger)
            targetState = cameraFollow.HoldFaceState;
        else if (triggerName == CallBossTrigger || triggerName == CallEarTrigger)
            targetState = cameraFollow.HoldEarState;
        else
            targetState = cameraFollow.IdleState;

        bool hasTrigger = false;
        foreach (AnimatorControllerParameter parameter in phoneAnimator.parameters)
        {
            if (parameter.name == triggerName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                hasTrigger = true;
                break;
            }
        }
        if (!hasTrigger)
        {
            Debug.LogError("[Phone] Нет Trigger: " + triggerName, this);
            yield break;
        }

        phoneAnimator.enabled = true;
        phoneAnimator.ResetTrigger(triggerName);
        phoneAnimator.SetTrigger(triggerName);

        // Ждём конкретную конечную позу, включая окончание перехода.
        // Не считаем любое постороннее состояние успешным завершением.
        float elapsed = 0f;
        do
        {
            yield return null;
            if (cameraFollow.IsStableState(targetState))
            {
                LastAnimationSucceeded = true;
                yield break;
            }
            elapsed += phoneAnimator.updateMode == AnimatorUpdateMode.UnscaledTime
                ? Time.unscaledDeltaTime : Time.deltaTime;
        }
        while (elapsed < AnimationTimeout);

        Debug.LogError("[Phone] После " + triggerName + " не достигнуто " + targetState +
            ". Проверь переходы, Conditions, Has Exit Time и имена состояний.", this);
        AbortPhoneMotion();
    }


    // =====================================================
    // ПОСТОЯННАЯ ВИБРАЦИЯ
    // =====================================================

    private void StartPhoneVibration()
    {
        // Если старая вибрация
        // почему-то ещё существует,
        // останавливаем только её корутину.
        if (vibrationCoroutine != null)
        {
            StopCoroutine(
                vibrationCoroutine
            );


            vibrationCoroutine = null;
        }


        // Сначала запоминаем
        // НАСТОЯЩУЮ текущую позицию.
        //
        // До этого момента Transform
        // вообще не меняем.
        vibrationStartPosition =
            transform.localPosition;


        vibrationPositionStored =
            true;


        animatorWasEnabledBeforeVibration =
            phoneAnimator != null &&
            phoneAnimator.enabled;


        // Пока телефон вибрирует на столе,
        // Animator не должен каждый кадр
        // перезаписывать его Transform.
        if (animatorWasEnabledBeforeVibration)
        {
            phoneAnimator.enabled =
                false;
        }


        vibrationCoroutine =
            StartCoroutine(
                VibratePhoneRoutine()
            );
    }


    private IEnumerator VibratePhoneRoutine()
    {
        float elapsed =
            0f;


        while (true)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            // Только движение
            // влево-вправо.
            //
            // Никаких кругов
            // вокруг исходной точки.
            float offset =
                Mathf.Sin(
                    elapsed *
                    vibrationSpeed
                ) *
                vibrationAmount;


            transform.localPosition =
                vibrationStartPosition +
                new Vector3(
                    offset,
                    0f,
                    0f
                );


            yield return null;
        }
    }


    private void StopPhoneVibration()
    {
        if (vibrationCoroutine != null)
        {
            StopCoroutine(
                vibrationCoroutine
            );


            vibrationCoroutine = null;
        }


        // Возвращаем позицию
        // только если реально
        // сохраняли её перед вибрацией.
        //
        // Благодаря этому телефон
        // никогда не улетает в (0,0,0)
        // при запуске игры.
        if (vibrationPositionStored)
        {
            transform.localPosition =
                vibrationStartPosition;
        }


        if (phoneAnimator != null &&
            animatorWasEnabledBeforeVibration)
        {
            phoneAnimator.enabled =
                true;
        }


        vibrationPositionStored =
            false;


        animatorWasEnabledBeforeVibration =
            false;
    }


    // =====================================================
    // INTERACTABLE
    // =====================================================

    private void MakePhoneInteractable()
    {
        if (phoneCollider == null)
            return;


        int interactableLayer =
            LayerMask.NameToLayer(
                InteractableLayerName
            );


        if (interactableLayer < 0)
            return;


        phoneCollider
            .gameObject
            .layer =
                interactableLayer;
    }


    private void MakePhoneNotInteractable()
    {
        if (phoneCollider == null)
            return;


        phoneCollider
            .gameObject
            .layer =
                originalPhoneLayer;
    }


    // =====================================================
    // AUDIO
    // =====================================================

    private void StartRinging()
    {
        if (ringingAudioSource == null)
            return;


        if (!ringingAudioSource.isPlaying)
        {
            ringingAudioSource.Play();
        }
    }


    private void StopRinging()
    {
        if (ringingAudioSource == null)
            return;


        ringingAudioSource.Stop();
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


        if (rewardController == null)
        {
            rewardController =
                FindFirstObjectByType
                    <WorkDirectionRewardController>(
                        FindObjectsInactive.Include
                    );
        }


        if (phoneAnimator == null)
        {
            phoneAnimator =
                GetComponent<Animator>();


            if (phoneAnimator == null)
            {
                phoneAnimator =
                    GetComponentInChildren
                        <Animator>(true);
            }
        }


        if (phoneCollider == null)
        {
            phoneCollider =
                GetComponent<Collider>();


            if (phoneCollider == null)
            {
                phoneCollider =
                    GetComponentInChildren
                        <Collider>(true);
            }
        }

        if (manualPhoneController == null)
        {
            manualPhoneController =
                GetComponent<
                    WorkPhoneManualController>();
        }

        FindDialogueManager();

        if (cameraFollow == null)
            cameraFollow = GetComponent<PhoneCameraFollow>();
    }


    private void FindDialogueManager()
    {
        if (dialogueManager != null)
            return;


        GameObject dialogueObject =
            GameObject.Find(
                "DialogueManager"
            );


        if (dialogueObject != null)
        {
            dialogueManager =
                dialogueObject
                    .GetComponent
                        <DialogueManager>();
        }


        if (dialogueManager == null)
        {
            dialogueManager =
                FindFirstObjectByType
                    <DialogueManager>(
                        FindObjectsInactive.Include
                    );
        }
    }


    // =====================================================
    // INSPECTOR
    // =====================================================

    private void OnValidate()
    {
        vibrationAmount =
            Mathf.Max(
                0f,
                vibrationAmount
            );


        vibrationSpeed =
            Mathf.Max(
                0f,
                vibrationSpeed
            );
    }
}

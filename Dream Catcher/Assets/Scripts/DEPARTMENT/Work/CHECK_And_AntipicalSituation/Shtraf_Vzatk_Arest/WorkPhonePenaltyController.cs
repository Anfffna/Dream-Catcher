using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private const string TakePhoneTrigger =
        "TakePhone";

    private const string PutPhoneTrigger =
        "PutPhone";

    private const string PhoneHoldAnchorName =
        "PhoneHoldAnchor";

    private const float AnimationTimeout =
        10f;


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
    private bool animatorDisabledForCameraHold;


    // =====================================================
    // СОСТОЯНИЕ ТЕЛЕФОНА
    // =====================================================

    private bool dangerousCallPending;

    private bool waitingForPhoneClick;

    private bool phoneSequenceActive;


    // =====================================================
    // LAYER
    // =====================================================

    private int originalPhoneLayer;


    // =====================================================
    // ПРИКРЕПЛЕНИЕ К КАМЕРЕ
    // =====================================================

    private Transform phoneHoldAnchor;

    private Transform originalPhoneParent;

    private Scene originalPhoneScene;

    private int originalSiblingIndex;

    private bool attachedToCamera;


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        FindReferences();


        // Запоминаем настоящую исходную
        // иерархию телефона.
        originalPhoneParent =
            transform.parent;

        originalPhoneScene =
            gameObject.scene;

        originalSiblingIndex =
            transform.GetSiblingIndex();


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


        // Если объект каким-то образом
        // выключился прямо во время разговора,
        // стараемся не оставлять телефон
        // дочерним объектом камеры.
        if (attachedToCamera)
        {
            DetachPhoneFromCamera();
        }


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
        if (!waitingForPhoneClick)
            return;


        if (phoneCoroutine != null)
            return;


        waitingForPhoneClick = false;


        // Сразу запрещаем
        // повторный клик.
        MakePhoneNotInteractable();


        // Игрок взял трубку:
        // звонок и вибрация
        // останавливаются одновременно.
        StopRinging();

        StopPhoneVibration();


        phoneCoroutine =
            StartCoroutine(
                AnswerPhoneRoutine()
            );
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

        // Пока TakePhone проигрывается,
        // телефон остаётся в своей
        // обычной сценовой иерархии.
        //
        // Поэтому существующая анимация
        // не меняет пространство координат.
        yield return StartCoroutine(
            PlayTriggeredAnimation(
                TakePhoneTrigger
            )
        );


        // -------------------------------------------------
        // ПРИКРЕПЛЯЕМ К КАМЕРЕ
        // -------------------------------------------------

        // TakePhone уже физически
        // довёл телефон до уха.
        //
        // Теперь телефон начинает
        // следовать за камерой игрока.
        AttachPhoneToCamera();


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

        // Телефон остаётся в той же
        // МИРОВОЙ позиции около головы,
        // но перестаёт следовать за камерой.
        DetachPhoneFromCamera();


        // -------------------------------------------------
        // PUT PHONE
        // -------------------------------------------------

        // Теперь Animator снова работает
        // в исходной сценовой иерархии
        // и может положить телефон обратно.
        yield return StartCoroutine(
            PlayTriggeredAnimation(
                PutPhoneTrigger
            )
        );


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

    private void AttachPhoneToCamera()
    {
        if (attachedToCamera)
            return;

        FindPhoneHoldAnchor();

        if (phoneHoldAnchor == null)
            return;


        // Запоминаем ТОЧНУЮ мировую позу,
        // в которой закончилась TakePhone.
        Vector3 worldPosition =
            transform.position;

        Quaternion worldRotation =
            transform.rotation;


        // Пока телефон находится у уха,
        // Animator вообще не должен
        // переписывать его Transform.
        if (phoneAnimator != null &&
            phoneAnimator.enabled)
        {
            phoneAnimator.enabled =
                false;

            animatorDisabledForCameraHold =
                true;
        }


        // Делаем ребёнком камеры.
        transform.SetParent(
            phoneHoldAnchor,
            true
        );


        // Дополнительно принудительно
        // возвращаем ТОЧНУЮ мировую позу,
        // которую дал конец TakePhone.
        transform.position =
            worldPosition;

        transform.rotation =
            worldRotation;


        attachedToCamera =
            true;
    }


    private void DetachPhoneFromCamera()
    {
        if (!attachedToCamera)
            return;


        // Запоминаем положение телефона
        // около головы В МОМЕНТ окончания диалога.
        Vector3 worldPosition =
            transform.position;

        Quaternion worldRotation =
            transform.rotation;


        // Сначала отсоединяем от камеры,
        // сохраняя мировую позицию.
        transform.SetParent(
            null,
            true
        );


        // Возвращаем телефон
        // в исходную рабочую сцену.
        if (originalPhoneScene.IsValid() &&
            originalPhoneScene.isLoaded &&
            gameObject.scene !=
                originalPhoneScene)
        {
            SceneManager.MoveGameObjectToScene(
                gameObject,
                originalPhoneScene
            );
        }


        // Возвращаем исходного родителя,
        // если он был.
        if (originalPhoneParent != null)
        {
            transform.SetParent(
                originalPhoneParent,
                true
            );

            int maxSiblingIndex =
                Mathf.Max(
                    0,
                    originalPhoneParent
                        .childCount - 1
                );

            transform.SetSiblingIndex(
                Mathf.Clamp(
                    originalSiblingIndex,
                    0,
                    maxSiblingIndex
                )
            );
        }


        // После всех переподчинений
        // снова выставляем ту самую
        // мировую позу у головы.
        transform.position =
            worldPosition;

        transform.rotation =
            worldRotation;


        // Animator включаем только ПОСЛЕ
        // возвращения телефона
        // в его нормальную иерархию.
        if (phoneAnimator != null &&
            animatorDisabledForCameraHold)
        {
            phoneAnimator.enabled =
                true;
        }

        animatorDisabledForCameraHold =
            false;

        attachedToCamera =
            false;
    }


    // =====================================================
    // ПОИСК PHONE HOLD ANCHOR
    // =====================================================

    private void FindPhoneHoldAnchor()
    {
        if (phoneHoldAnchor != null)
            return;


        Camera playerCamera =
            Camera.main;


        if (playerCamera == null)
            return;


        Transform[] cameraHierarchy =
            playerCamera
                .GetComponentsInChildren
                    <Transform>(true);


        for (int i = 0;
             i < cameraHierarchy.Length;
             i++)
        {
            Transform current =
                cameraHierarchy[i];


            if (current == null)
                continue;


            if (current.name ==
                PhoneHoldAnchorName)
            {
                phoneHoldAnchor =
                    current;

                return;
            }
        }
    }


    // =====================================================
    // ANIMATOR
    //
    // Используем только Trigger.
    //
    // Никаких названий Animator State
    // в Inspector не требуется.
    // =====================================================

    private IEnumerator PlayTriggeredAnimation(
        string triggerName)
    {
        if (phoneAnimator == null)
            yield break;


        const int layer = 0;


        AnimatorStateInfo startState =
            phoneAnimator
                .GetCurrentAnimatorStateInfo(
                    layer
                );


        int startStateHash =
            startState.fullPathHash;


        // Чистим этот Trigger
        // перед новым запуском.
        phoneAnimator.ResetTrigger(
            triggerName
        );


        phoneAnimator.SetTrigger(
            triggerName
        );


        bool enteredNewState =
            false;


        int animationStateHash =
            0;


        float elapsed =
            0f;


        // -------------------------------------------------
        // ЖДЁМ ВХОД В НОВОЕ СОСТОЯНИЕ
        // -------------------------------------------------

        while (elapsed <
               AnimationTimeout)
        {
            AnimatorStateInfo current =
                phoneAnimator
                    .GetCurrentAnimatorStateInfo(
                        layer
                    );


            if (phoneAnimator
                .IsInTransition(layer))
            {
                AnimatorStateInfo next =
                    phoneAnimator
                        .GetNextAnimatorStateInfo(
                            layer
                        );


                if (next.fullPathHash != 0 &&
                    next.fullPathHash !=
                        startStateHash)
                {
                    animationStateHash =
                        next.fullPathHash;


                    enteredNewState =
                        true;


                    break;
                }
            }


            if (current.fullPathHash !=
                startStateHash)
            {
                animationStateHash =
                    current.fullPathHash;


                enteredNewState =
                    true;


                break;
            }


            elapsed +=
                Time.unscaledDeltaTime;


            yield return null;
        }


        // Не зависаем навечно,
        // если Animator настроен неправильно.
        if (!enteredNewState)
            yield break;


        elapsed = 0f;


        // -------------------------------------------------
        // ЖДЁМ ОКОНЧАНИЕ ЗАПУЩЕННОЙ АНИМАЦИИ
        // -------------------------------------------------

        while (elapsed <
               AnimationTimeout)
        {
            AnimatorStateInfo current =
                phoneAnimator
                    .GetCurrentAnimatorStateInfo(
                        layer
                    );


            bool transition =
                phoneAnimator
                    .IsInTransition(
                        layer
                    );


            if (current.fullPathHash ==
                animationStateHash)
            {
                if (current.normalizedTime >=
                        1f &&
                    !transition)
                {
                    yield break;
                }
            }
            else if (!transition)
            {
                // Animator уже вышел
                // из этой анимации
                // через Has Exit Time.
                yield break;
            }


            elapsed +=
                Time.unscaledDeltaTime;


            yield return null;
        }
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


        FindDialogueManager();

        FindPhoneHoldAnchor();
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
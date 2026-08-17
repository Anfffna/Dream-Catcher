using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientNPCController :
    MonoBehaviour,
    IInteractable
{
    public static ClientNPCController
    CurrentActiveClient
    {
        get;
        private set;
    }

    private enum ClientDialogueStage
    {
        WaitingForApproach,
        FirstDialogueReady,
        FirstDialogueRunning,
        WaitingForDirectionTab,
        QuestionDialogueReady,

        GiveSon3DialogueReady,
        GiveSon3DialogueRunning,

        WaitingForSon3Return,

        FinalDialogueRunning,
        TakeSon3AnimationRunning,
        Completed
    }

    [Header("Данные клиента")]

    [Tooltip("Карточка этого конкретного человека.")]
    [SerializeField]
    private VisitorCaseData visitorData;

    [Tooltip("Индекс выбранного варианта дела. Для первого варианта используется 0.")]
    [SerializeField]
    private int activeVariantIndex;

    [Tooltip("Панель информации на экране направления.")]
    [SerializeField]
    private ClientInfoPanelController
        clientInfoPanel;

    [Header("Вариативный диалог")]

    [Tooltip("Контроллер двух вопросов клиента.")]
    [SerializeField]
    private ClientQuestionDialogueController
        questionDialogueController;

    [Tooltip("Навигация двух вкладок монитора.")]
    [SerializeField]
    private ComputerInterfaceNavigation
        computerNavigation;

    [Header("Голос клиента")]

    [Tooltip("AudioSource на NPC для воспроизведения его голоса.")]
    [SerializeField]
    private AudioSource voiceAudioSource;

    [Header("Анимация")]

    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string approachTriggerName =
        "Podhodit";

    [SerializeField]
    private string giveSon3TriggerName =
        "Give_SON3";

    [Tooltip("Триггер финальной анимации, в которой клиент забирает СОН-3.")]
    [SerializeField]
    private string takeSon3TriggerName =
    "Take_SON3";

    [Tooltip("Максимальное время ожидания запуска финальной анимации.")]
    [SerializeField]
    private float takeSon3StartTimeout =
        2f;

    [Tooltip(
    "Максимальное время ожидания " +
    "реального запуска Give_SON3."
)]
    [SerializeField]
    private float giveSon3StartTimeout =
    2f;

    [Tooltip(
        "Момент Give_SON3, после которого " +
        "SON-3 уже гарантированно находится " +
        "в руке клиента."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float giveSon3ReadyNormalizedTime =
        0.9f;

    [SerializeField]
    private int animatorLayerIndex;

    [Tooltip("Максимальное время ожидания входа в анимацию подхода.")]
    [SerializeField]
    private float approachStartTimeout =
        5f;

    [Header("SON-3")]

    [SerializeField]
    private Son3DragController son3;

    [SerializeField]
    private WorkSon3TrayController
        son3Tray;

    [SerializeField]
    private Transform workItemsRoot;

    [Header("Взаимодействие")]

    [SerializeField]
    private Collider interactionCollider;

    [SerializeField]
    private string defaultLayerName =
        "Default";

    [SerializeField]
    private string interactableLayerName =
        "Interactable";

    [SerializeField]
    private string clientColliderObjectName =
        "ClientInteractionCollider";

    [Header("Диалог")]

    [SerializeField]
    private DialogueManager dialogueManager;

    [SerializeField]
    private string dialogueManagerObjectName =
        "DialogueManager";

    [Header("Текущее состояние")]

    [SerializeField]
    private ClientDialogueStage dialogueStage =
        ClientDialogueStage
            .WaitingForApproach;

    [SerializeField]
    private bool approachStarted;

    [SerializeField]
    private bool interactionAvailable;

    [SerializeField]
    private bool dialogueInteractionLocked;

    [SerializeField]
    private bool directionTabOpened;

    private bool directionSubmitted;
    private bool waitingForSon3Return;

    private VisitorCaseData.VisitorCaseVariant
        activeVariant;

    private Coroutine approachCoroutine;
    private Coroutine dialogueCoroutine;
    private Coroutine takeSon3Coroutine;
    private Coroutine giveSon3Coroutine;
    private bool giveSon3AnimationReady;

    public bool IsFinished =>
        dialogueStage ==
            ClientDialogueStage.Completed;

    public event Action<ClientNPCController>
        ClientFinished;

    private void Awake()
    {
        FindReferences();
        ApplyClientData();
        SetInteractionAvailable(false);
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeToComputerNavigation();
    }

    private void Reset()
    {
        FindReferences();
    }

    private void OnDisable()
    {
        UnsubscribeFromComputerNavigation();

        if (approachCoroutine != null)
        {
            StopCoroutine(
                approachCoroutine
            );

            approachCoroutine = null;
        }

        if (dialogueCoroutine != null)
        {
            StopCoroutine(
                dialogueCoroutine
            );

            dialogueCoroutine = null;
        }

        if (giveSon3Coroutine != null)
        {
            StopCoroutine(
                giveSon3Coroutine
            );

            giveSon3Coroutine =
                null;
        }

        if (takeSon3Coroutine != null)
        {
            StopCoroutine(
                takeSon3Coroutine
            );

            takeSon3Coroutine = null;
        }

        if (questionDialogueController != null &&
            questionDialogueController.IsOpen)
        {
            questionDialogueController
                .CloseDialogue();
        }

        dialogueInteractionLocked = false;

        if (son3 != null)
        {
            son3.ReturnedToOriginalPlace -=
                HandleSon3Returned;
        }

        if (CurrentActiveClient == this)
        {
            CurrentActiveClient = null;
        }
    }

    public void Initialize(
        VisitorCaseData newVisitorData,
        int newVariantIndex,
        ClientInfoPanelController
            newClientInfoPanel)
    {
        visitorData =
            newVisitorData;

        activeVariantIndex =
            newVariantIndex;

        if (newClientInfoPanel != null)
        {
            clientInfoPanel =
                newClientInfoPanel;
        }

        ResetRuntimeStateForNewClient();
        ApplyClientData();
    }

    public void StartApproach()
    {
        if (approachStarted)
            return;

        FindReferences();
        ApplyClientData();

        if (animator == null ||
            interactionCollider == null ||
            activeVariant == null)
        {
            return;
        }

        CurrentActiveClient = this;
        if (SessionStatsManager.Instance != null)
        {
            SessionStatsManager.Instance
                .TrackClient(this);
        }
        approachStarted = true;

        dialogueStage =
            ClientDialogueStage
                .WaitingForApproach;

        SetInteractionAvailable(false);

        animator.ResetTrigger(
            approachTriggerName
        );

        animator.SetTrigger(
            approachTriggerName
        );

        if (approachCoroutine != null)
        {
            StopCoroutine(
                approachCoroutine
            );
        }

        approachCoroutine =
            StartCoroutine(
                WaitForApproachToFinish()
            );
    }

    public void Interact()
    {
        if (!interactionAvailable ||
            dialogueInteractionLocked)
        {
            return;
        }

        if (DialogueManager
            .AnyDialogueActive)
        {
            return;
        }

        if (dialogueStage ==
            ClientDialogueStage
                .FirstDialogueReady)
        {
            StartFirstDialogue();
            return;
        }

        if (dialogueStage ==
            ClientDialogueStage
                .QuestionDialogueReady)
        {
            ToggleQuestionDialogue();
            return;
        }

        if (dialogueStage ==
            ClientDialogueStage
                .GiveSon3DialogueReady)
        {
            StartGiveSon3Dialogue();
            return;
        }
    }

    public void ApplyClientInformation()
    {
        if (visitorData == null ||
            activeVariant == null ||
            clientInfoPanel == null)
        {
            return;
        }

        clientInfoPanel.ShowClient(
            visitorData,
            activeVariant
        );
    }

    public void UnlockQuestionDialogue()
    {
        directionTabOpened = true;

        TryUnlockQuestionDialogue();
    }

    public void SetInteractionAvailable(
        bool available)
    {
        FindInteractionReferences();

        interactionAvailable =
            available;

        if (interactionCollider == null)
            return;

        string layerName =
            available
                ? interactableLayerName
                : defaultLayerName;

        int targetLayer =
            LayerMask.NameToLayer(
                layerName
            );

        if (targetLayer < 0)
            return;

        interactionCollider
            .gameObject.layer =
            targetLayer;
    }

    public void MakeInteractable()
    {
        SetInteractionAvailable(true);
    }

    public void MakeNotInteractable()
    {
        SetInteractionAvailable(false);
    }

    private IEnumerator WaitForApproachToFinish()
    {
        int approachStateHash =
            Animator.StringToHash(
                approachTriggerName
            );

        float elapsed = 0f;
        bool enteredApproachState =
            false;

        while (elapsed <
               approachStartTimeout)
        {
            AnimatorStateInfo currentState =
                animator
                    .GetCurrentAnimatorStateInfo(
                        animatorLayerIndex
                    );

            AnimatorStateInfo nextState =
                animator
                    .GetNextAnimatorStateInfo(
                        animatorLayerIndex
                    );

            bool currentIsApproach =
                currentState.shortNameHash ==
                    approachStateHash ||
                currentState.IsName(
                    approachTriggerName
                );

            bool nextIsApproach =
                nextState.shortNameHash ==
                    approachStateHash ||
                nextState.IsName(
                    approachTriggerName
                );

            if (currentIsApproach ||
                nextIsApproach)
            {
                enteredApproachState =
                    true;

                break;
            }

            elapsed += Time.deltaTime;

            yield return null;
        }

        if (!enteredApproachState)
        {
            approachCoroutine = null;
            yield break;
        }

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator
                    .GetCurrentAnimatorStateInfo(
                        animatorLayerIndex
                    );

            bool isApproachState =
                stateInfo.shortNameHash ==
                    approachStateHash ||
                stateInfo.IsName(
                    approachTriggerName
                );

            if (isApproachState)
                break;

            yield return null;
        }

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator
                    .GetCurrentAnimatorStateInfo(
                        animatorLayerIndex
                    );

            bool isApproachState =
                stateInfo.shortNameHash ==
                    approachStateHash ||
                stateInfo.IsName(
                    approachTriggerName
                );

            bool isTransitioning =
                animator.IsInTransition(
                    animatorLayerIndex
                );

            if (isApproachState &&
                stateInfo.normalizedTime >=
                    1f &&
                !isTransitioning)
            {
                break;
            }

            if (!isApproachState &&
                !isTransitioning)
            {
                break;
            }

            yield return null;
        }

        dialogueStage =
            ClientDialogueStage
                .FirstDialogueReady;

        SetInteractionAvailable(true);

        approachCoroutine = null;
    }

    private void StartFirstDialogue()
    {
        if (activeVariant == null ||
            activeVariant.FirstDialogue ==
                null ||
            activeVariant.FirstDialogue
                .Count == 0)
        {
            return;
        }

        FindDialogueManagerByExactName();

        if (dialogueManager == null)
            return;

        dialogueInteractionLocked =
            true;

        dialogueStage =
            ClientDialogueStage
                .FirstDialogueRunning;

        SetInteractionAvailable(false);

        ApplyVoiceSettings();

        dialogueManager.StartDialogue(
            activeVariant.FirstDialogue,
            false
        );

        if (!dialogueManager
            .DialogueActive)
        {
            dialogueInteractionLocked =
                false;

            dialogueStage =
                ClientDialogueStage
                    .FirstDialogueReady;

            SetInteractionAvailable(true);

            return;
        }

        if (dialogueCoroutine != null)
        {
            StopCoroutine(
                dialogueCoroutine
            );
        }

        dialogueCoroutine =
            StartCoroutine(
                WaitForFirstDialogueToFinish()
            );
    }

    private IEnumerator
    WaitForFirstDialogueToFinish()
    {
        bool giveSon3Triggered =
            false;


        while (dialogueManager != null &&
               dialogueManager.DialogueActive)
        {
            bool shouldGiveSon3 =
                activeVariant != null &&
                activeVariant
                    .GiveSon3DuringFirstDialogue;

            int giveSon3Index =
                activeVariant != null
                    ? activeVariant
                        .GiveSon3DialogueIndex
                    : -1;


            // =====================================================
            // ЗАПУСК GIVE_SON3
            // =====================================================

            // Используем >=, а не ==.
            //
            // Даже если из-за быстрого переключения
            // нужный индекс был пройден между кадрами,
            // анимация всё равно будет запущена.
            if (shouldGiveSon3 &&
                !giveSon3Triggered &&
                giveSon3Index >= 0 &&
                dialogueManager
                    .CurrentLineIndex >=
                        giveSon3Index)
            {
                giveSon3Triggered = true;

                StartGiveSon3Animation();
            }


            yield return null;
        }


        // =====================================================
        // ЗАЩИТА ОТ ПОЛНОГО СКИПА
        // =====================================================

        bool needsSon3 =
            activeVariant != null &&
            activeVariant
                .GiveSon3DuringFirstDialogue;


        // Если каким-то образом весь диалог закончился
        // раньше, чем наша корутина увидела нужную
        // реплику, всё равно запускаем передачу.
        if (needsSon3 &&
            !giveSon3Triggered)
        {
            giveSon3Triggered = true;

            StartGiveSon3Animation();
        }


        // =====================================================
        // ЖДЁМ НЕ ВРЕМЯ, А РЕАЛЬНУЮ АНИМАЦИЮ
        // =====================================================

        if (needsSon3 &&
            giveSon3Triggered)
        {
            while (!giveSon3AnimationReady &&
                   giveSon3Coroutine != null)
            {
                yield return null;
            }
        }


        // =====================================================
        // ТОЛЬКО ТЕПЕРЬ ОТДАЁМ SON-3 ИГРОКУ
        // =====================================================

        if (needsSon3 &&
            giveSon3AnimationReady &&
            son3 != null &&
            son3Tray != null)
        {
            son3.PrepareForPlayer(
                workItemsRoot,
                son3Tray
            );

            son3Tray.EnablePlacement();
        }


        dialogueStage =
            ClientDialogueStage
                .WaitingForDirectionTab;

        SetInteractionAvailable(false);


        yield return null;

        RestoreWorkStateAfterDialogue();


        yield return null;

        RestoreWorkStateAfterDialogue();


        dialogueInteractionLocked =
            false;

        dialogueCoroutine =
            null;

        TryUnlockQuestionDialogue();
    }

    private void StartGiveSon3Animation()
    {
        if (giveSon3AnimationReady)
            return;

        if (giveSon3Coroutine != null)
            return;

        if (animator == null)
            return;


        animator.ResetTrigger(
            giveSon3TriggerName
        );

        animator.SetTrigger(
            giveSon3TriggerName
        );


        giveSon3Coroutine =
            StartCoroutine(
                WaitForGiveSon3Ready()
            );
    }


    private IEnumerator WaitForGiveSon3Ready()
    {
        int stateHash =
            Animator.StringToHash(
                giveSon3TriggerName
            );


        // =====================================================
        // ЖДЁМ РЕАЛЬНОГО ВХОДА В GIVE_SON3
        // =====================================================

        float elapsed = 0f;
        bool enteredState = false;


        while (elapsed <
               giveSon3StartTimeout)
        {
            AnimatorStateInfo stateInfo =
                animator
                    .GetCurrentAnimatorStateInfo(
                        animatorLayerIndex
                    );


            bool isGiveSon3State =
                stateInfo.shortNameHash ==
                    stateHash ||
                stateInfo.IsName(
                    giveSon3TriggerName
                );


            if (isGiveSon3State)
            {
                enteredState = true;
                break;
            }


            elapsed +=
                Time.deltaTime;

            yield return null;
        }


        if (!enteredState)
        {
            Debug.LogError(
                "ClientNPCController: NPC \"" +
                gameObject.name +
                "\" не вошёл в состояние " +
                giveSon3TriggerName +
                "."
            );

            giveSon3Coroutine =
                null;

            yield break;
        }


        // =====================================================
        // ЖДЁМ МОМЕНТА, КОГДА SON-3 УЖЕ В РУКЕ
        // =====================================================

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator
                    .GetCurrentAnimatorStateInfo(
                        animatorLayerIndex
                    );


            bool isGiveSon3State =
                stateInfo.shortNameHash ==
                    stateHash ||
                stateInfo.IsName(
                    giveSon3TriggerName
                );


            if (isGiveSon3State &&
                stateInfo.normalizedTime >=
                    giveSon3ReadyNormalizedTime)
            {
                break;
            }


            // Если состояние уже полностью закончилось,
            // значит нужный момент тем более был пройден.
            if (!isGiveSon3State &&
                !animator.IsInTransition(
                    animatorLayerIndex
                ))
            {
                break;
            }


            yield return null;
        }


        giveSon3AnimationReady =
            true;

        giveSon3Coroutine =
            null;
    }

    private void ToggleQuestionDialogue()
    {
        if (questionDialogueController ==
            null)
        {
            return;
        }

        if (questionDialogueController
            .IsOpen)
        {
            questionDialogueController
                .CloseDialogue();

            return;
        }

        ApplyVoiceSettings();

        questionDialogueController
            .OpenDialogue();
    }

    private void HandleElectronicDirectionOpened()
    {
        directionTabOpened = true;

        TryUnlockQuestionDialogue();
    }

    private void TryUnlockQuestionDialogue()
    {
        // Если вкладка уже реально открыта,
        // но событие по какой-либо причине
        // было пропущено, синхронизируемся
        // с фактическим состоянием компьютера.
        if (!directionTabOpened &&
            computerNavigation != null &&
            computerNavigation
                .IsElectronicDirectionSelected)
        {
            directionTabOpened = true;
        }

        if (!directionTabOpened ||
            dialogueStage !=
            ClientDialogueStage
                .WaitingForDirectionTab)
        {
            return;
        }

        dialogueStage =
            ClientDialogueStage
                .QuestionDialogueReady;

        SetInteractionAvailable(true);
    }

    public void NotifyDirectionSubmitted()
    {
        if (directionSubmitted)
            return;

        directionSubmitted = true;
        waitingForSon3Return = false;

        if (questionDialogueController != null &&
            questionDialogueController.IsOpen)
        {
            questionDialogueController
                .CloseDialogue();
        }

        dialogueStage =
            ClientDialogueStage
                .GiveSon3DialogueReady;

        SetInteractionAvailable(true);
    }

    private void StartGiveSon3Dialogue()
    {
        if (!directionSubmitted ||
            activeVariant == null)
        {
            return;
        }

        FindDialogueManagerByExactName();

        if (dialogueManager == null)
            return;

        if (activeVariant.GiveSon3Dialogue == null ||
            activeVariant.GiveSon3Dialogue.Count == 0)
        {
            BeginWaitingForSon3Return();
            return;
        }

        dialogueInteractionLocked =
            true;

        dialogueStage =
            ClientDialogueStage
                .GiveSon3DialogueRunning;

        SetInteractionAvailable(false);

        ApplyVoiceSettings();

        dialogueManager.StartDialogue(
            activeVariant.GiveSon3Dialogue,
            false
        );

        if (!dialogueManager.DialogueActive)
        {
            dialogueInteractionLocked =
                false;

            BeginWaitingForSon3Return();

            return;
        }

        if (dialogueCoroutine != null)
        {
            StopCoroutine(
                dialogueCoroutine
            );
        }

        dialogueCoroutine =
            StartCoroutine(
                WaitForGiveSon3DialogueToFinish()
            );
    }

    private IEnumerator
    WaitForGiveSon3DialogueToFinish()
    {
        while (dialogueManager != null &&
               dialogueManager.DialogueActive)
        {
            yield return null;
        }

        dialogueInteractionLocked =
            false;

        dialogueCoroutine =
            null;

        yield return null;

        RestoreWorkStateAfterDialogue();

        BeginWaitingForSon3Return();
    }

    private void BeginWaitingForSon3Return()
    {
        waitingForSon3Return = true;

        dialogueStage =
            ClientDialogueStage
                .WaitingForSon3Return;

        SetInteractionAvailable(false);

        if (son3 == null)
            return;

        son3.ReturnedToOriginalPlace -=
            HandleSon3Returned;

        son3.ReturnedToOriginalPlace +=
            HandleSon3Returned;

        son3.EnableReturnToOriginalPlace();
    }

    private void HandleSon3Returned()
    {
        if (!waitingForSon3Return)
            return;

        waitingForSon3Return =
            false;

        if (son3 != null)
        {
            son3.ReturnedToOriginalPlace -=
                HandleSon3Returned;
        }

        StartFinalDialogue();
    }

    private void StartFinalDialogue()
    {
        FindDialogueManagerByExactName();

        if (dialogueManager == null ||
            activeVariant == null)
        {
            CompleteFinalDialogue();
            return;
        }

        if (activeVariant.FinalDialogue == null ||
            activeVariant.FinalDialogue.Count == 0)
        {
            CompleteFinalDialogue();
            return;
        }

        dialogueInteractionLocked =
            true;

        dialogueStage =
            ClientDialogueStage
                .FinalDialogueRunning;

        SetInteractionAvailable(false);

        ApplyVoiceSettings();

        dialogueManager.StartDialogue(
            activeVariant.FinalDialogue,
            false
        );

        if (!dialogueManager.DialogueActive)
        {
            dialogueInteractionLocked =
                false;

            CompleteFinalDialogue();

            return;
        }

        if (dialogueCoroutine != null)
        {
            StopCoroutine(
                dialogueCoroutine
            );
        }

        dialogueCoroutine =
            StartCoroutine(
                WaitForFinalDialogueToFinish()
            );
    }

    private IEnumerator
    WaitForFinalDialogueToFinish()
    {
        while (dialogueManager != null &&
               dialogueManager.DialogueActive)
        {
            yield return null;
        }

        dialogueInteractionLocked =
            false;

        dialogueCoroutine =
            null;

        yield return null;

        RestoreWorkStateAfterDialogue();

        CompleteFinalDialogue();
    }

    private void CompleteFinalDialogue()
    {
        SetInteractionAvailable(false);

        if (animator == null)
        {
            FinishClient();
            return;
        }

        dialogueStage =
            ClientDialogueStage
                .TakeSon3AnimationRunning;

        animator.ResetTrigger(
            takeSon3TriggerName
        );

        animator.SetTrigger(
            takeSon3TriggerName
        );

        if (takeSon3Coroutine != null)
        {
            StopCoroutine(
                takeSon3Coroutine
            );
        }

        takeSon3Coroutine =
            StartCoroutine(
                WaitForTakeSon3AnimationToFinish()
            );
    }

    private IEnumerator
    WaitForTakeSon3AnimationToFinish()
    {
        int stateHash =
            Animator.StringToHash(
                takeSon3TriggerName
            );

        float elapsed = 0f;
        bool enteredState = false;

        // Ждём, пока Animator действительно
        // запустит состояние Take_SON3.
        while (elapsed <
               takeSon3StartTimeout)
        {
            AnimatorStateInfo currentState =
                animator
                    .GetCurrentAnimatorStateInfo(
                        animatorLayerIndex
                    );

            AnimatorStateInfo nextState =
                animator
                    .GetNextAnimatorStateInfo(
                        animatorLayerIndex
                    );

            bool currentMatches =
                currentState.shortNameHash ==
                    stateHash ||
                currentState.IsName(
                    takeSon3TriggerName
                );

            bool nextMatches =
                nextState.shortNameHash ==
                    stateHash ||
                nextState.IsName(
                    takeSon3TriggerName
                );

            if (currentMatches ||
                nextMatches)
            {
                enteredState = true;
                break;
            }

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        // Не оставляем клиента навечно
        // зависшим из-за ошибки Animator.
        if (!enteredState)
        {
            takeSon3Coroutine = null;

            FinishClient();
            yield break;
        }

        // Ждём полного окончания анимации.
        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator
                    .GetCurrentAnimatorStateInfo(
                        animatorLayerIndex
                    );

            bool isTakeSon3State =
                stateInfo.shortNameHash ==
                    stateHash ||
                stateInfo.IsName(
                    takeSon3TriggerName
                );

            bool isTransitioning =
                animator.IsInTransition(
                    animatorLayerIndex
                );

            if (isTakeSon3State &&
                stateInfo.normalizedTime >= 1f &&
                !isTransitioning)
            {
                break;
            }

            if (!isTakeSon3State &&
                !isTransitioning)
            {
                break;
            }

            yield return null;
        }

        takeSon3Coroutine = null;

        FinishClient();
    }

    private void FinishClient()
    {
        dialogueStage =
            ClientDialogueStage.Completed;

        SetInteractionAvailable(false);

        ClientFinished?.Invoke(
            this
        );
    }

    private void ApplyClientData()
    {
        if (visitorData == null)
        {
            activeVariant = null;
            return;
        }

        activeVariant =
            visitorData.GetVariant(
                activeVariantIndex
            );

        ApplyClientInformation();
        ApplyVoiceSettings();

        if (questionDialogueController !=
            null)
        {
            questionDialogueController
                .Configure(
                    activeVariant
                );
        }
    }

    private void ApplyVoiceSettings()
    {
        if (dialogueManager == null ||
            voiceAudioSource == null)
        {
            return;
        }

        if (visitorData != null &&
            visitorData.VoiceClip != null)
        {
            voiceAudioSource.clip =
                visitorData.VoiceClip;
        }

        dialogueManager
            .defaultVoiceAudioSource =
            voiceAudioSource;
    }

    private void ResetRuntimeStateForNewClient()
    {
        if (giveSon3Coroutine != null)
        {
            StopCoroutine(
                giveSon3Coroutine
            );

            giveSon3Coroutine =
                null;
        }

        giveSon3AnimationReady =
            false;

        if (takeSon3Coroutine != null)
        {
            StopCoroutine(
                takeSon3Coroutine
            );

            takeSon3Coroutine = null;
        }

        if (questionDialogueController !=
            null &&
            questionDialogueController.IsOpen)
        {
            questionDialogueController
                .CloseDialogue();
        }

        approachStarted = false;
        interactionAvailable = false;
        dialogueInteractionLocked = false;
        directionTabOpened = false;
        directionSubmitted = false;
        waitingForSon3Return = false;

        if (son3 != null)
        {
            son3.ReturnedToOriginalPlace -=
                HandleSon3Returned;
        }

        dialogueStage =
            ClientDialogueStage
                .WaitingForApproach;

        SetInteractionAvailable(false);
    }

    private void RestoreWorkStateAfterDialogue()
    {
        WorkSessionManager workSession =
            WorkSessionManager.Instance;

        if (workSession == null ||
            !workSession.IsSeated)
        {
            return;
        }

        if (workSession.seatController !=
            null)
        {
            workSession.seatController
                .RestoreWorkControlAfterPause();
        }

        if (workSession.cursorController !=
            null)
        {
            workSession.cursorController
                .ShowWorkCursor();
        }
    }

    private void FindReferences()
    {
        FindAnimator();
        FindInteractionReferences();
        FindSon3Tray();
        FindDialogueManagerByExactName();
        FindQuestionDialogueController();
        FindComputerNavigation();
        FindVoiceAudioSource();
    }

    private void FindAnimator()
    {
        if (animator != null)
            return;

        animator =
            GetComponent<Animator>();

        if (animator == null)
        {
            animator =
                GetComponentInChildren
                    <Animator>(true);
        }
    }

    private void FindInteractionReferences()
    {
        // =====================================================
        // SON-3
        // =====================================================

        // SON-3 ищем только если ссылка вообще отсутствует.
        //
        // После передачи игроку SON-3 специально
        // перестаёт быть дочерним объектом NPC,
        // поэтому проверять IsChildOf здесь НЕЛЬЗЯ.
        if (son3 == null)
        {
            son3 =
                GetComponentInChildren
                    <Son3DragController>(
                        true
                    );
        }


        // =====================================================
        // COLLIDER NPC
        // =====================================================

        if (interactionCollider != null)
            return;


        Collider[] colliders =
            GetComponentsInChildren
                <Collider>(true);


        // Сначала ищем точный
        // ClientInteractionCollider.
        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            if (colliders[i] == null)
                continue;

            if (colliders[i].gameObject.name ==
                clientColliderObjectName)
            {
                interactionCollider =
                    colliders[i];

                return;
            }
        }


        // Запасной поиск.
        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider currentCollider =
                colliders[i];

            if (currentCollider == null)
                continue;


            // Collider SON-3 нельзя принять
            // за Collider самого NPC.
            if (son3 != null &&
                currentCollider.transform
                    .IsChildOf(
                        son3.transform
                    ))
            {
                continue;
            }


            interactionCollider =
                currentCollider;

            return;
        }
    }

    private void FindSon3Tray()
    {
        if (son3Tray != null)
            return;

        son3Tray =
            FindFirstObjectByType
                <WorkSon3TrayController>(
                    FindObjectsInactive.Include
                );
    }

    private void FindDialogueManagerByExactName()
    {
        if (dialogueManager != null &&
            dialogueManager.gameObject.name ==
            dialogueManagerObjectName)
        {
            return;
        }

        dialogueManager = null;

        GameObject dialogueManagerObject =
            GameObject.Find(
                dialogueManagerObjectName
            );

        if (dialogueManagerObject != null)
        {
            dialogueManager =
                dialogueManagerObject
                    .GetComponent
                        <DialogueManager>();
        }

        if (dialogueManager == null)
        {
            dialogueManager =
                FindFirstObjectByType
                    <DialogueManager>(
                        FindObjectsInactive
                            .Include
                    );
        }
    }

    private void FindQuestionDialogueController()
    {
        if (questionDialogueController !=
            null)
        {
            return;
        }

        questionDialogueController =
            FindFirstObjectByType
                <ClientQuestionDialogueController>(
                    FindObjectsInactive
                        .Include
                );
    }

    private void FindComputerNavigation()
    {
        if (computerNavigation != null)
            return;

        computerNavigation =
            FindFirstObjectByType
                <ComputerInterfaceNavigation>(
                    FindObjectsInactive
                        .Include
                );
    }

    private void FindVoiceAudioSource()
    {
        if (voiceAudioSource != null)
            return;

        voiceAudioSource =
            GetComponent<AudioSource>();

        if (voiceAudioSource == null)
        {
            voiceAudioSource =
                GetComponentInChildren
                    <AudioSource>(true);
        }
    }

    private void SubscribeToComputerNavigation()
    {
        if (computerNavigation == null)
            return;

        computerNavigation
            .ElectronicDirectionOpened -=
            HandleElectronicDirectionOpened;

        computerNavigation
            .ElectronicDirectionOpened +=
            HandleElectronicDirectionOpened;
    }

    private void UnsubscribeFromComputerNavigation()
    {
        if (computerNavigation == null)
            return;

        computerNavigation
            .ElectronicDirectionOpened -=
            HandleElectronicDirectionOpened;
    }
}
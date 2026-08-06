using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientNPCController : MonoBehaviour, IInteractable
{
    [Header("Данные клиента")]
    [Tooltip("Карточка данных этого конкретного клиента.")]
    [SerializeField] private VisitorCaseData visitorData;

    [Tooltip("Панель информации клиента на экране направления.")]
    [SerializeField] private ClientInfoPanelController clientInfoPanel;

    [Header("Анимация")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string approachTriggerName = "Podhodit";

    [SerializeField]
    private string giveSon3TriggerName = "Give_SON3";

    [Header("SON-3")]
    [SerializeField] private Son3DragController son3;
    [SerializeField] private WorkSon3TrayController son3Tray;
    [SerializeField] private Transform workItemsRoot;

    [InspectorName("Индекс реплики для Give_SON3")]
    [SerializeField]
    private int giveSon3DialogueIndex = 1;

    [SerializeField]
    private int animatorLayerIndex = 0;

    [Tooltip("Максимальное время ожидания входа в состояние подхода. ")]
    [SerializeField]
    private float approachStartTimeout = 5f;

    [Header("Взаимодействие")]
    [SerializeField]
    private Collider interactionCollider;

    [SerializeField]
    private string defaultLayerName = "Default";

    [SerializeField]
    private string interactableLayerName = "Interactable";

    [SerializeField]
    private string clientColliderObjectName = "ClientInteractionCollider";

    [Header("Диалог")]
    [SerializeField]
    private DialogueManager dialogueManager;

    [SerializeField]
    private string dialogueManagerObjectName = "DialogueManager";

    [SerializeField]
    private List<DialogueManager.DialogueLine> firstDialogue =
        new List<DialogueManager.DialogueLine>();

    [Header("Текущее состояние")]
    [SerializeField]
    private bool approachStarted;

    [SerializeField]
    private bool interactionAvailable;

    [SerializeField]
    private bool dialogueInteractionLocked;
    private Coroutine approachCoroutine;
    private Coroutine dialogueCoroutine;

    private void Awake()
    {
        FindReferences();
        SetInteractionAvailable(false);
    }

    private void Reset()
    {
        FindReferences();
    }

    private void OnDisable()
    {
        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
            approachCoroutine = null;
        }

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        dialogueInteractionLocked = false;
    }

    private void FindReferences()
    {
        FindAnimator();
        FindInteractionReferences();
        FindDialogueManagerByExactName();
    }

    private void FindAnimator()
    {
        if (animator != null)
            return;

        animator = GetComponent<Animator>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(
                true
            );
        }
    }

    private void FindInteractionReferences()
    {
        // Находим сам SON-3, чтобы управлять им после диалога.
        if (son3 == null)
        {
            son3 =
                GetComponentInChildren<Son3DragController>(
                    true
                );
        }

        if (interactionCollider != null)
            return;

        Collider[] colliders =
            GetComponentsInChildren<Collider>(
                true
            );

        // Ищем коллайдер клиента по точному имени.
        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            if (colliders[i].gameObject.name ==
                clientColliderObjectName)
            {
                interactionCollider =
                    colliders[i];

                return;
            }
        }

        // Запасной поиск без коллайдеров внутри SON-3.
        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider currentCollider =
                colliders[i];

            if (currentCollider == null)
                continue;

            if (son3 != null &&
                currentCollider.transform.IsChildOf(
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

        if (dialogueManagerObject == null)
        {
            return;
        }

        dialogueManager =
            dialogueManagerObject.GetComponent<DialogueManager>();
    }


    /// <summary>
    /// Вызывается после завершения загрузочной заставки компьютера.
    /// </summary>
    public void StartApproach()
    {
        if (approachStarted)
            return;

        FindReferences();
        ApplyClientInformation();

        if (animator == null)
        {
            return;
        }

        if (interactionCollider == null)
        {
            return;
        }

        approachStarted = true;

        // Пока клиент идёт, нажимать на него нельзя.
        SetInteractionAvailable(false);

        animator.ResetTrigger(
            approachTriggerName
        );

        animator.SetTrigger(
            approachTriggerName
        );

        if (approachCoroutine != null)
            StopCoroutine(approachCoroutine);

        approachCoroutine =
            StartCoroutine(
                WaitForApproachToFinish()
            );
    }


    private IEnumerator WaitForApproachToFinish()
    {
        int approachStateHash =
            Animator.StringToHash(approachTriggerName);

        float elapsed = 0f;
        bool enteredApproachState = false;

        /*
         * Ждём, пока Animator действительно
         * войдёт в состояние Podhodit.
         */
        while (elapsed < approachStartTimeout)
        {
            AnimatorStateInfo currentState =
                animator.GetCurrentAnimatorStateInfo(
                    animatorLayerIndex
                );

            AnimatorStateInfo nextState =
                animator.GetNextAnimatorStateInfo(
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
                enteredApproachState = true;
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
        /*
         * Если Podhodit пока находится только
         * в Next State, ждём, пока он станет Current State.
         */
        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(
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
                animator.GetCurrentAnimatorStateInfo(
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
                stateInfo.normalizedTime >= 1f &&
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

        SetInteractionAvailable(true);

        approachCoroutine = null;
    }


    public void Interact()
    {
        if (!interactionAvailable)
            return;
        /*
         * Защита от повторного запуска диалога
         * тем же кликом, которым закрылась последняя реплика.
         */
        if (dialogueInteractionLocked)
            return;

        FindDialogueManagerByExactName();

        if (dialogueManager == null)
        {
            return;
        }
        /*
         * Не начинаем диалог, пока идёт любой другой диалог.
         */
        if (DialogueManager.AnyDialogueActive)
            return;

        if (firstDialogue == null ||
            firstDialogue.Count == 0)
        {
            return;
        }

        dialogueInteractionLocked = true;

        dialogueManager.StartDialogue(
            firstDialogue,
            false
        );

        if (!dialogueManager.DialogueActive)
        {
            dialogueInteractionLocked = false;
            return;
        }

        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine =
            StartCoroutine(
                WaitForDialogueToFinish()
            );
    }

    public void ApplyClientInformation()
    {
        if (visitorData == null ||
            clientInfoPanel == null)
        {
            return;
        }

        clientInfoPanel.ShowClient(
            visitorData
        );
    }

    private IEnumerator WaitForDialogueToFinish()
    {
        bool giveSon3Triggered = false;

        while (dialogueManager != null &&
               dialogueManager.DialogueActive)
        {
            // На второй реплике с индексом 1 запускаем передачу SON-3.
            if (!giveSon3Triggered &&
                giveSon3DialogueIndex >= 0 &&
                dialogueManager.CurrentLineIndex == giveSon3DialogueIndex)

            {
                giveSon3Triggered = true;

                animator.ResetTrigger(giveSon3TriggerName);
                animator.SetTrigger(giveSon3TriggerName);
            }

            yield return null;
        }

        // После диалога SON-3 становится доступен для клика.
        if (son3 != null &&
            son3Tray != null)
        {
            son3.PrepareForPlayer(
                workItemsRoot,
                son3Tray
            );
            son3Tray.EnablePlacement();
        }

        // После диалога клиент больше не интерактивен.
        SetInteractionAvailable(false);

        yield return null;
        RestoreWorkStateAfterDialogue();

        yield return null;
        RestoreWorkStateAfterDialogue();

        dialogueInteractionLocked = false;
        dialogueCoroutine = null;
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
        /*
         * Возвращаем правильное управление игроком
         */
        if (workSession.seatController != null)
        {
            workSession.seatController
                .RestoreWorkControlAfterPause();
        }
        /*
         * Возвращаем рабочий курсор.
         */
        if (workSession.cursorController != null)
        {
            workSession.cursorController
                .ShowWorkCursor();
        }
    }

    public void SetInteractionAvailable(
        bool available)
    {
        FindInteractionReferences();

        interactionAvailable = available;

        if (interactionCollider == null)
        {
            return;
        }

        string layerName =
            available
                ? interactableLayerName
                : defaultLayerName;

        int targetLayer =
            LayerMask.NameToLayer(
                layerName
            );

        if (targetLayer < 0)
        {
            return;
        }

        /*
         * Меняем Layer именно у дочернего объекта,
         * на котором расположен Collider.
         */
        interactionCollider.gameObject.layer =
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
}
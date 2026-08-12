using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossDoor : MonoBehaviour, IInteractable
{
    [Header("Door Animators")]
    public List<Animator> doorAnimators = new List<Animator>();

    [Header("Forced Animator States")]
    public string initialClosedState = "doorStatic";

    [Header("Animator Triggers")]
    public string openTrigger = "Open";
    public string closeTrigger = "Close";

    [Header("Animator State Names")]
    [Tooltip("Первое открытие: стук + открывание")]
    public string firstOpenState = "OpenBossDoor";

    [Tooltip("Общее состояние закрывания")]
    public string closeState = "CloseHallLeftDoor";

    [Tooltip("Второе обычное открытие без стука")]
    public string secondOpenState = "OpenHallLeftDoor";

    [Header("Animation Waiting")]
    [Tooltip("Защита от вечного ожидания, если название состояния указано неверно")]
    public float maximumAnimationWait = 20f;

    [Header("Interaction")]
    public bool availableOnStart = true;

    [Tooltip("Объект, которому назначается слой Interactable")]
    public Transform interactionRoot;

    public bool setLayerRecursively = true;

    [Header("Audio")]
    public AudioSource audioSource;

    [Tooltip("Можно оставить пустым, если стук уже находится в анимации")]
    public AudioClip firstOpenSound;

    public AudioClip secondOpenSound;
    public AudioClip closeSound;

    public bool IsAvailable => isAvailable;
    public bool IsFinished => stage == DoorStage.Finished;

    public bool IsOpen =>
        stage == DoorStage.FirstOpen ||
        stage == DoorStage.SecondOpen;

    public bool IsAnimating =>
        stage == DoorStage.FirstOpening ||
        stage == DoorStage.FirstClosing ||
        stage == DoorStage.SecondOpening ||
        stage == DoorStage.FinalClosing;

    private enum DoorStage
    {
        WaitingFirstOpen,
        FirstOpening,
        FirstOpen,
        FirstClosing,

        WaitingSecondOpen,
        SecondOpening,
        SecondOpen,
        FinalClosing,

        Finished
    }

    private DoorStage stage = DoorStage.WaitingFirstOpen;
    private bool isAvailable;

    private int defaultLayer;
    private int interactableLayer;
    private SaveButtonBlockController saveButtonBlockController;

    private void Start()
    {
        defaultLayer = LayerMask.NameToLayer("Default");
        interactableLayer = LayerMask.NameToLayer("Interactable");

        if (interactionRoot == null)
            interactionRoot = transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Если список не заполнен вручную,
        // попробуем найти Animator на двери и её детях.
        if (doorAnimators == null)
            doorAnimators = new List<Animator>();

        if (doorAnimators.Count == 0)
        {
            Animator[] foundAnimators =
                GetComponentsInChildren<Animator>(true);

            doorAnimators.AddRange(foundAnimators);
        }

        stage = DoorStage.WaitingFirstOpen;
        SetDoorAvailable(availableOnStart);

        SetSaveBlocked(false);
    }

    public void Interact()
    {
        if (!isAvailable || IsAnimating || IsFinished)
            return;

        switch (stage)
        {
            // Первое взаимодействие:
            // стук и открытие двери.
            case DoorStage.WaitingFirstOpen:
                StartCoroutine(FirstOpenRoutine());
                break;

            // Второе взаимодействие:
            // обычное открытие без стука.
            case DoorStage.WaitingSecondOpen:
                StartCoroutine(SecondOpenRoutine());
                break;
        }
    }

    /// <summary>
    /// Вызывается только внутренним FinishTrigger.
    /// Закрывает дверь только после первого открытия со стуком.
    /// </summary>
    public void TryCloseFromFinishTrigger()
    {
        // Во всех остальных стадиях FinishTrigger игнорируется.
        if (stage != DoorStage.FirstOpen)
            return;

        StartCoroutine(FirstCloseRoutine());
    }

    /// <summary>
    /// Вызывается только внешним EntryTrigger.
    /// Закрывает дверь только после второго обычного открытия.
    /// </summary>
    public void TryCloseFromEntryTrigger()
    {
        // До второго открытия внешний триггер ничего не делает.
        if (stage != DoorStage.SecondOpen)
            return;

        StartCoroutine(FinalCloseRoutine());
    }

    public void ForceFinishedClosedAfterBossQuest()
    {
        SetSaveBlocked(false);

        if (stage == DoorStage.FinalClosing)
        {
            SetDoorAvailable(false);
            return;
        }

        if (stage == DoorStage.SecondOpen)
        {
            StartCoroutine(
                FinalCloseRoutine()
            );

            return;
        }

        StopAllCoroutines();

        stage = DoorStage.Finished;

        SetDoorAvailable(false);

        ForceAnimatorsToClosedState();
    }

    public void ResetDoorForWalkBossActiveQuest()
    {
        StopAllCoroutines();
        SetSaveBlocked(false);

        stage = DoorStage.WaitingFirstOpen;

        ForceAnimatorsToState(initialClosedState, 0f);

        // ВАЖНО: именно true, не availableOnStart.
        SetDoorAvailable(true);

        Debug.Log("BossDoor: дверь сброшена в начало walk_to_boss и снова стала Interactable.");
    }

    private void ForceAnimatorsToClosedState()
    {
        if (doorAnimators == null)
            return;

        for (int i = 0; i < doorAnimators.Count; i++)
        {
            Animator animator = doorAnimators[i];

            if (animator == null)
                continue;

            animator.ResetTrigger(openTrigger);
            animator.ResetTrigger(closeTrigger);

            if (!string.IsNullOrEmpty(closeState))
            {
                animator.Play(closeState, 0, 1f);
                animator.Update(0f);
            }
        }
    }

    private void ForceAnimatorsToState(string stateName, float normalizedTime)
    {
        if (doorAnimators == null)
            return;

        for (int i = 0; i < doorAnimators.Count; i++)
        {
            Animator animator = doorAnimators[i];

            if (animator == null)
                continue;

            animator.ResetTrigger(openTrigger);
            animator.ResetTrigger(closeTrigger);

            if (!string.IsNullOrEmpty(stateName))
            {
                animator.Play(stateName, 0, normalizedTime);
                animator.Update(0f);
            }
        }
    }

    private IEnumerator FirstOpenRoutine()
    {
        stage = DoorStage.FirstOpening;
        SetDoorAvailable(false);

        SetSaveBlocked(true);

        PlaySound(firstOpenSound);
        PlayTriggerOnAllAnimators(openTrigger, closeTrigger);

        yield return WaitForAnimatorState(firstOpenState);

        stage = DoorStage.FirstOpen;

        // Внешний EntryTrigger всё ещё ничего не сделает.
        // Ждём только внутренний FinishTrigger.
        SetDoorAvailable(false);
    }

    private IEnumerator FirstCloseRoutine()
    {
        stage = DoorStage.FirstClosing;
        SetDoorAvailable(false);

        SetSaveBlocked(false);

        PlaySound(closeSound);
        PlayTriggerOnAllAnimators(closeTrigger, openTrigger);

        yield return WaitForAnimatorState(closeState);

        // Теперь разрешаем второе взаимодействие.
        stage = DoorStage.WaitingSecondOpen;
        SetDoorAvailable(true);
    }

    private IEnumerator SecondOpenRoutine()
    {
        stage = DoorStage.SecondOpening;
        SetDoorAvailable(false);

        SetSaveBlocked(true);

        PlaySound(secondOpenSound);
        PlayTriggerOnAllAnimators(openTrigger, closeTrigger);

        yield return WaitForAnimatorState(secondOpenState);

        stage = DoorStage.SecondOpen;

        // Теперь внутренний FinishTrigger игнорируется.
        // Ждём только внешний EntryTrigger.
        SetDoorAvailable(false);
    }

    private IEnumerator FinalCloseRoutine()
    {
        stage = DoorStage.FinalClosing;
        SetDoorAvailable(false);

        SetSaveBlocked(false);

        PlaySound(closeSound);
        PlayTriggerOnAllAnimators(closeTrigger, openTrigger);

        yield return WaitForAnimatorState(closeState);

        // Конец всей цепочки.
        stage = DoorStage.Finished;
        SetDoorAvailable(false);
    }

    private IEnumerator WaitForAnimatorState(string stateName)
    {
        Animator animator = GetPrimaryAnimator();

        if (animator == null)
        {
            Debug.LogError(
                "BossDoor: не назначен ни один Animator.",
                this
            );

            yield break;
        }

        int stateHash = Animator.StringToHash(stateName);
        float timer = 0f;
        bool stateEntered = false;

        // Сначала ждём, пока Animator действительно войдёт
        // в нужное состояние.
        while (timer < maximumAnimationWait)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.shortNameHash == stateHash)
            {
                stateEntered = true;
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (!stateEntered)
        {
            Debug.LogWarning(
                $"BossDoor: Animator не вошёл в состояние '{stateName}'. " +
                "Проверь название состояния и переходы.",
                this
            );

            yield break;
        }

        timer = 0f;

        // Теперь ждём настоящего окончания клипа.
        // Благодаря этому не нужны вручную выставленные Duration.
        while (timer < maximumAnimationWait)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(0);

            bool isCorrectState =
                stateInfo.shortNameHash == stateHash;

            bool animationFinished =
                stateInfo.normalizedTime >= 1f;

            bool isNotTransitioning =
                !animator.IsInTransition(0);

            if (isCorrectState &&
                animationFinished &&
                isNotTransitioning)
            {
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning(
            $"BossDoor: ожидание окончания '{stateName}' " +
            "превысило допустимое время.",
            this
        );
    }

    private Animator GetPrimaryAnimator()
    {
        if (doorAnimators == null)
            return null;

        foreach (Animator animator in doorAnimators)
        {
            if (animator != null)
                return animator;
        }

        return null;
    }

    private void PlayTriggerOnAllAnimators(
        string triggerToSet,
        string triggerToReset)
    {
        if (doorAnimators == null)
            return;

        foreach (Animator animator in doorAnimators)
        {
            if (animator == null)
                continue;

            animator.ResetTrigger(triggerToReset);
            animator.SetTrigger(triggerToSet);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void SetSaveBlocked(bool blocked)
    {
        if (saveButtonBlockController == null)
        {
            saveButtonBlockController =
                FindFirstObjectByType<SaveButtonBlockController>(
                    FindObjectsInactive.Include
                );
        }

        if (saveButtonBlockController != null)
        {
            saveButtonBlockController.SetTemporaryBlock(blocked);
        }
    }

    private void SetDoorAvailable(bool state)
    {
        isAvailable = state;

        if (interactionRoot == null)
            return;

        int targetLayer = state
            ? interactableLayer
            : defaultLayer;

        if (targetLayer < 0)
        {
            Debug.LogError(
                "BossDoor: не найден слой Default или Interactable.",
                this
            );

            return;
        }

        if (setLayerRecursively)
        {
            SetLayerRecursive(
                interactionRoot,
                targetLayer
            );
        }
        else
        {
            interactionRoot.gameObject.layer = targetLayer;
        }
    }

    private void SetLayerRecursive(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
        {
            SetLayerRecursive(child, layer);
        }
    }
}
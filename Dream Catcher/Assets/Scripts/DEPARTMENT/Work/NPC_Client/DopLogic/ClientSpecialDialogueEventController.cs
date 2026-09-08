using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ClientSpecialDialogueEventController :
    MonoBehaviour
{
    [Header("Клиент")]

    [Tooltip(
        "ClientNPCController этого клиента. " +
        "Если пусто — берётся автоматически " +
        "с этого же GameObject."
    )]
    [SerializeField]
    private ClientNPCController clientNPC;


    [Header("Условия")]

    [Tooltip(
        "Для какого варианта дела работает событие. " +
        "Пусто = любой вариант."
    )]
    [SerializeField]
    private string requiredVariantId =
        "variant_a";

    [Tooltip(
        "Событие сработает только если игрок " +
        "задал индивидуальный вопрос."
    )]
    [SerializeField]
    private bool requirePersonalQuestion =
        true;

    [Tooltip(
    "Какое решение должно быть принято " +
    "для запуска события. " +
    "None = решение не учитывать."
    )]
    [SerializeField]
    private DirectionDecision requiredDecision =
    DirectionDecision.None;

    [Header("Момент запуска")]

    [Tooltip(
        "Индекс реплики финального диалога, " +
        "на которой запускается событие. " +
        "Первая реплика имеет индекс 0."
    )]
    [Min(0)]
    [SerializeField]
    private int triggerLineIndex;


    [Header("Событие")]

    [Tooltip(
        "Событие, которое запускается " +
        "на нужной реплике."
    )]
    [SerializeField]
    private UnityEvent onTriggered;


    [Header("Текущее состояние")]

    [Tooltip(
        "Только для просмотра в Play Mode. " +
        "Показывает, сработало ли событие."
    )]
    [SerializeField]
    private bool triggered;


    private Coroutine watchCoroutine;

    private void OnEnable()
    {
        triggered = false;
        Subscribe();


        /*
         * Защита на случай, если компонент
         * был включён уже во время
         * финального диалога.
         */
        if (clientNPC != null &&
            clientNPC.IsFinalDialogueRunning)
        {
            BeginWatchingFinalDialogue();
        }
    }


    private void OnDisable()
    {
        Unsubscribe();


        if (watchCoroutine != null)
        {
            StopCoroutine(
                watchCoroutine
            );

            watchCoroutine = null;
        }
    }


    // =====================================================
    // СОБЫТИЕ ОТ ClientNPCController
    // =====================================================

    private void HandleFinalDialogueStarted(
        ClientNPCController startedClient)
    {
        if (triggered)
            return;


        /*
         * Каждый special-controller слушает
         * только своего собственного NPC.
         */
        if (startedClient != clientNPC)
            return;


        BeginWatchingFinalDialogue();
    }


    // =====================================================
    // ЗАПУСК КОРОТКОГО НАБЛЮДЕНИЯ
    // =====================================================

    private void BeginWatchingFinalDialogue()
    {
        if (triggered ||
            watchCoroutine != null)
        {
            return;
        }


        if (!AreStaticConditionsMet())
            return;


        DialogueManager dialogueManager =
            clientNPC != null
                ? clientNPC.DialogueManagerReference
                : null;


        if (dialogueManager == null)
            return;


        watchCoroutine =
            StartCoroutine(
                WatchFinalDialogueRoutine(
                    dialogueManager
                )
            );
    }


    // =====================================================
    // СЛЕДИМ ТОЛЬКО ПОКА ИДЁТ НУЖНЫЙ FINAL DIALOGUE
    // =====================================================

    private IEnumerator WatchFinalDialogueRoutine(
        DialogueManager dialogueManager)
    {
        /*
         * Эта coroutine существует только
         * несколько секунд во время
         * финального диалога конкретного NPC.
         *
         * В остальное время компонент
         * вообще ничего не проверяет.
         */
        while (!triggered &&
               clientNPC != null &&
               clientNPC.IsFinalDialogueRunning &&
               dialogueManager != null &&
               dialogueManager.DialogueActive)
        {
            if (ClientNPCController
                    .CurrentActiveClient !=
                clientNPC)
            {
                break;
            }


            /*
             * >= оставляем специально.
             *
             * Если игрок очень быстро
             * переключил реплики, событие
             * всё равно не будет пропущено.
             */
            if (dialogueManager.CurrentLineIndex >=
                triggerLineIndex)
            {
                triggered = true;

                /*
                 * Обнуляем ссылку ДО UnityEvent.
                 *
                 * Так безопаснее даже если
                 * вызванное событие вдруг
                 * отключит этот компонент.
                 */
                watchCoroutine = null;

                onTriggered?.Invoke();

                yield break;
            }


            yield return null;
        }


        watchCoroutine = null;
    }


    // =====================================================
    // УСЛОВИЯ, КОТОРЫЕ НЕ НУЖНО ПРОВЕРЯТЬ КАЖДЫЙ КАДР
    // =====================================================

    private bool AreStaticConditionsMet()
    {
        if (clientNPC == null)
            return false;


        if (ClientNPCController
                .CurrentActiveClient !=
            clientNPC)
        {
            return false;
        }


        if (!clientNPC.IsFinalDialogueRunning)
            return false;


        VisitorCaseData.VisitorCaseVariant
            variant =
                CurrentClientContext
                    .CurrentVariant;


        if (variant == null)
            return false;


        if (!string.IsNullOrWhiteSpace(
                requiredVariantId) &&
            variant.VariantId !=
                requiredVariantId)
        {
            return false;
        }

        if (requiredDecision !=
                DirectionDecision.None &&
            clientNPC.SubmittedDecision !=
                requiredDecision)
        {
            return false;
        }

        if (requirePersonalQuestion)
        {
            ClientQuestionDialogueController
                questionController =
                    clientNPC
                        .QuestionDialogueControllerReference;


            if (questionController == null ||
                !questionController
                    .PersonalQuestionAsked)
            {
                return false;
            }
        }


        return true;
    }


    // =====================================================
    // ПОДПИСКА
    // =====================================================

    private void Subscribe()
    {
        if (clientNPC == null)
            return;


        /*
         * Сначала -= для защиты
         * от случайной двойной подписки.
         */
        clientNPC.FinalDialogueStarted -=
            HandleFinalDialogueStarted;

        clientNPC.FinalDialogueStarted +=
            HandleFinalDialogueStarted;
    }


    private void Unsubscribe()
    {
        if (clientNPC == null)
            return;


        clientNPC.FinalDialogueStarted -=
            HandleFinalDialogueStarted;
    }


    private void OnValidate()
    {
        triggerLineIndex =
            Mathf.Max(
                0,
                triggerLineIndex
            );
    }
}
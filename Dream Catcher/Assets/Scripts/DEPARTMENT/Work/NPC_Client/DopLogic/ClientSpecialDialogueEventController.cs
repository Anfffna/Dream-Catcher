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


    private void Awake()
    {
        FindClient();
    }


    private void OnEnable()
    {
        triggered = false;

        FindClient();
    }


    private void Update()
    {
        if (triggered)
            return;


        FindClient();

        if (clientNPC == null)
            return;


        // =====================================================
        // ЭТО ДОЛЖЕН БЫТЬ ИМЕННО ТЕКУЩИЙ NPC
        // =====================================================

        if (ClientNPCController
                .CurrentActiveClient !=
            clientNPC)
        {
            return;
        }


        // =====================================================
        // СОБЫТИЕ ТОЛЬКО В ФИНАЛЬНОМ ДИАЛОГЕ
        // =====================================================

        if (!clientNPC
                .IsFinalDialogueRunning)
        {
            return;
        }


        // =====================================================
        // БЕРЁМ МЕНЕДЖЕРЫ У САМОГО NPC
        // =====================================================

        DialogueManager dialogueManager =
            clientNPC
                .DialogueManagerReference;


        ClientQuestionDialogueController
            questionDialogueController =
                clientNPC
                    .QuestionDialogueControllerReference;


        if (dialogueManager == null)
            return;


        // =====================================================
        // ВАРИАНТ ДЕЛА
        // =====================================================

        VisitorCaseData.VisitorCaseVariant
            variant =
                CurrentClientContext
                    .CurrentVariant;


        if (variant == null)
            return;


        if (!string.IsNullOrWhiteSpace(
                requiredVariantId) &&
            variant.VariantId !=
                requiredVariantId)
        {
            return;
        }


        // =====================================================
        // ДОПОЛНИТЕЛЬНЫЙ ВОПРОС
        // =====================================================

        if (requirePersonalQuestion)
        {
            if (questionDialogueController ==
                    null ||
                !questionDialogueController
                    .PersonalQuestionAsked)
            {
                return;
            }
        }


        // =====================================================
        // ДИАЛОГ ДОЛЖЕН РЕАЛЬНО ИДТИ
        // =====================================================

        if (!dialogueManager.DialogueActive)
            return;


        // =====================================================
        // НУЖНАЯ РЕПЛИКА
        // =====================================================

        /*
         * >= специально, чтобы быстрый скип
         * не мог пропустить событие.
         */
        if (dialogueManager.CurrentLineIndex <
            triggerLineIndex)
        {
            return;
        }


        // =====================================================
        // ЗАПУСК
        // =====================================================

        triggered = true;

        onTriggered?.Invoke();
    }


    private void FindClient()
    {
        if (clientNPC != null)
            return;


        clientNPC =
            GetComponent<ClientNPCController>();
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
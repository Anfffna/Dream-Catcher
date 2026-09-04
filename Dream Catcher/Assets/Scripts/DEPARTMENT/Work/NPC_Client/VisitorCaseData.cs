using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(
    fileName = "Visitor_",
    menuName = "Департамент сна/Дело клиента"
)]
public class VisitorCaseData :
    ScriptableObject
{
    [Serializable]
    public class QuestionDialogueData
    {
        [Tooltip("Технический идентификатор вопроса.")]
        [SerializeField]
        private string questionId;

        [Tooltip("Текст, отображаемый на кнопке вопроса.")]
        [SerializeField]
        private string questionText;

        [Tooltip("Реплики игрока и клиента после выбора вопроса.")]
        [SerializeField]
        private List<DialogueManager.DialogueLine>
            responseDialogue =
                new List<DialogueManager.DialogueLine>();

        public string QuestionId =>
            questionId;

        public string QuestionText =>
            questionText;

        public List<DialogueManager.DialogueLine>
            ResponseDialogue =>
                responseDialogue;

        public bool HasDialogue =>
            responseDialogue != null &&
            responseDialogue.Count > 0;
    }

    [Serializable]
    public class DirectionAnswerData
    {
        [Tooltip("Правильное итоговое решение по этому делу.")]
        [SerializeField]
        private DirectionDecision correctDecision =
            DirectionDecision.None;

        [Tooltip("Физические симптомы, которые должны быть указаны в направлении.")]
        [SerializeField]
        private List<string> correctSymptoms =
            new List<string>();

        [Tooltip("Основания, которые должны быть указаны в направлении.")]
        [SerializeField]
        private List<string> correctGrounds =
            new List<string>();

        public DirectionDecision CorrectDecision =>
            correctDecision;

        public List<string> CorrectSymptoms =>
            correctSymptoms;

        public List<string> CorrectGrounds =>
            correctGrounds;
    }

    [Serializable]
    public class ConditionalFinalDialogueData
    {
        public enum PersonalQuestionCondition
        {
            Any,
            Asked,
            NotAsked
        }

        [Header("Условия")]

        [Tooltip(
            "Условие по индивидуальному вопросу. " +
            "Any — не учитывать."
        )]
        [SerializeField]
        private PersonalQuestionCondition
            personalQuestionCondition =
                PersonalQuestionCondition.Any;

        [Tooltip(
            "Какое решение должно быть принято. " +
            "None — решение пока не учитывать."
        )]
        [SerializeField]
        private DirectionDecision requiredDecision =
            DirectionDecision.None;


        [Header("Диалог")]

        [Tooltip(
            "Финальный диалог, который используется, " +
            "если все условия выше выполнены."
        )]
        [SerializeField]
        private List<DialogueManager.DialogueLine>
            dialogue =
                new List<DialogueManager.DialogueLine>();


        public List<DialogueManager.DialogueLine>
            Dialogue =>
                dialogue;


        public bool HasDialogue =>
            dialogue != null &&
            dialogue.Count > 0;


        public bool Matches(
            bool personalQuestionAsked,
            DirectionDecision actualDecision)
        {
            bool personalMatches =
                personalQuestionCondition ==
                    PersonalQuestionCondition.Any ||
                (
                    personalQuestionCondition ==
                        PersonalQuestionCondition.Asked &&
                    personalQuestionAsked
                ) ||
                (
                    personalQuestionCondition ==
                        PersonalQuestionCondition.NotAsked &&
                    !personalQuestionAsked
                );


            bool decisionMatches =
                requiredDecision ==
                    DirectionDecision.None ||
                requiredDecision ==
                    actualDecision;


            return
                personalMatches &&
                decisionMatches;
        }
    }

    [Serializable]
    public class VisitorCaseVariant
    {
        [Header("Идентификатор варианта")]

        [Tooltip("Уникальный идентификатор варианта дела.")]
        [SerializeField]
        private string variantId =
            "variant_a";

        [Header("Информация о записи")]

        [Tooltip("Регистрационный номер именно этой записи сна.")]
        [SerializeField]
        private string registrationNumber;

        [Tooltip("Дата регистрации именно этой записи сна.")]
        [SerializeField]
        private string recordDate;

        [Header("Первый диалог")]

        [Tooltip("Диалог, который запускается при первом нажатии на клиента.")]
        [SerializeField]
        private List<DialogueManager.DialogueLine>
            firstDialogue =
                new List<DialogueManager.DialogueLine>();

        [Tooltip("Передавать ли СОН-3 во время первого диалога.")]
        [SerializeField]
        private bool giveSon3DuringFirstDialogue =
            true;

        [Tooltip("Индекс реплики, на которой запускается анимация передачи СОН-3.")]
        [SerializeField]
        private int giveSon3DialogueIndex =
            1;

        [Header("Вариативный диалог")]

        [Tooltip("Реплика клиента, которая появляется перед двумя вопросами.")]
        [SerializeField]
        private DialogueManager.DialogueLine
            questionMenuOpeningLine =
                new DialogueManager.DialogueLine();

        [Tooltip("Первый вопрос. Обычно «Жалобы?».")]
        [SerializeField]
        private QuestionDialogueData
            complaintsQuestion =
                new QuestionDialogueData();

        [Tooltip("Второй индивидуальный вопрос клиента.")]
        [SerializeField]
        private QuestionDialogueData
            personalQuestion =
                new QuestionDialogueData();

        [Header("Возврат СОН-3")]

        [Tooltip("Диалог, после которого игрок должен вернуть СОН-3 клиенту.")]
        [SerializeField]
        private List<DialogueManager.DialogueLine>
            giveSon3Dialogue =
                new List<DialogueManager.DialogueLine>();

        [Header("Финальный диалог")]

        [Tooltip("Последний диалог клиента. Запускается автоматически после возврата СОН-3.")]
        [SerializeField]
        private List<DialogueManager.DialogueLine>
            finalDialogue =
                new List<DialogueManager.DialogueLine>();

        [Tooltip(
            "Особые варианты финального диалога. " +
            "Проверяются сверху вниз. " +
            "Если ни один не подходит — используется обычный Final Dialogue."
        )]
        [SerializeField]
        private List<ConditionalFinalDialogueData>
            conditionalFinalDialogues =
                new List<ConditionalFinalDialogueData>();

        [Header("Запись сна")]

        [Tooltip("Видео сна для этого варианта. Пока можно оставить пустым.")]
        [SerializeField]
        private VideoClip dreamVideoClip;

        [Header("Правильный ответ")]

        [Tooltip("Правильное заполнение электронного направления для этого варианта дела.")]
        [SerializeField]
        private DirectionAnswerData correctDirection =
            new DirectionAnswerData();

        public string VariantId =>
            variantId;

        public string RegistrationNumber =>
            registrationNumber;

        public string RecordDate =>
            recordDate;

        public List<DialogueManager.DialogueLine>
            FirstDialogue =>
                firstDialogue;

        public List<DialogueManager.DialogueLine>
            ResolveFinalDialogue(
                bool personalQuestionAsked,
                DirectionDecision actualDecision =
                    DirectionDecision.None)
        {
            if (conditionalFinalDialogues != null)
            {
                for (int i = 0;
                     i < conditionalFinalDialogues.Count;
                     i++)
                {
                    ConditionalFinalDialogueData
                        conditionalDialogue =
                            conditionalFinalDialogues[i];

                    if (conditionalDialogue == null ||
                        !conditionalDialogue.HasDialogue)
                    {
                        continue;
                    }

                    if (conditionalDialogue.Matches(
                            personalQuestionAsked,
                            actualDecision))
                    {
                        return
                            conditionalDialogue.Dialogue;
                    }
                }
            }

            return finalDialogue;
        }

        public bool GiveSon3DuringFirstDialogue =>
            giveSon3DuringFirstDialogue;

        public int GiveSon3DialogueIndex =>
            giveSon3DialogueIndex;

        public DialogueManager.DialogueLine
            QuestionMenuOpeningLine =>
                questionMenuOpeningLine;

        public QuestionDialogueData
            ComplaintsQuestion =>
                complaintsQuestion;

        public QuestionDialogueData
            PersonalQuestion =>
                personalQuestion;

        public List<DialogueManager.DialogueLine>
            GiveSon3Dialogue =>
                giveSon3Dialogue;

        public List<DialogueManager.DialogueLine>
            FinalDialogue =>
                finalDialogue;

        public VideoClip DreamVideoClip =>
            dreamVideoClip;

        public DirectionAnswerData CorrectDirection =>
            correctDirection;
    }

    [Header("Основные данные человека")]

    [Tooltip("Уникальный технический идентификатор клиента.")]
    [SerializeField]
    private string visitorId =
        "visitor_001";

    [Tooltip("Имя клиента, отображаемое в его деле.")]
    [SerializeField]
    private string clientName;

    [Tooltip("Должность или место работы клиента.")]
    [SerializeField]
    private string occupation;

    [Tooltip("Зацикленная голосовая дорожка этого клиента.")]
    [SerializeField]
    private AudioClip voiceClip;

    [Header("Варианты дела")]

    [Tooltip("Один вариант у обычного клиента или два у вариативного.")]
    [SerializeField]
    private List<VisitorCaseVariant>
        caseVariants =
            new List<VisitorCaseVariant>();

    [SerializeField]
    [HideInInspector]
    private string registrationNumber;

    [SerializeField]
    [HideInInspector]
    private string recordDate;

    public string VisitorId =>
        visitorId;

    public string ClientName =>
        clientName;

    public string Occupation =>
        occupation;

    public AudioClip VoiceClip =>
        voiceClip;

    public int VariantCount =>
        caseVariants == null
            ? 0
            : caseVariants.Count;

    public VisitorCaseVariant GetVariant(
        int variantIndex)
    {
        if (caseVariants == null ||
            caseVariants.Count == 0)
        {
            return null;
        }

        int safeIndex =
            Mathf.Clamp(
                variantIndex,
                0,
                caseVariants.Count - 1
            );

        return caseVariants[safeIndex];
    }

    public int GetRandomVariantIndex()
    {
        if (caseVariants == null ||
            caseVariants.Count <= 1)
        {
            return 0;
        }

        return UnityEngine.Random.Range(
            0,
            caseVariants.Count
        );
    }

    public string ResolveRegistrationNumber(
        VisitorCaseVariant variant)
    {
        if (variant != null &&
            !string.IsNullOrWhiteSpace(
                variant.RegistrationNumber))
        {
            return variant.RegistrationNumber;
        }

        return registrationNumber;
    }

    public string ResolveRecordDate(
        VisitorCaseVariant variant)
    {
        if (variant != null &&
            !string.IsNullOrWhiteSpace(
                variant.RecordDate))
        {
            return variant.RecordDate;
        }

        return recordDate;
    }
}
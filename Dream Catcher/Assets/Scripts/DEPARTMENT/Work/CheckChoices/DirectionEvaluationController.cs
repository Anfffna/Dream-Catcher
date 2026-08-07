using System;
using System.Collections.Generic;
using UnityEngine;

public class DirectionEvaluationController :
    MonoBehaviour
{
    [Serializable]
    public class EvaluationResult
    {
        public string VisitorId
        {
            get;
            private set;
        }

        public string ClientName
        {
            get;
            private set;
        }

        public string VariantId
        {
            get;
            private set;
        }

        public DirectionDecision SelectedDecision
        {
            get;
            private set;
        }

        public DirectionDecision CorrectDecision
        {
            get;
            private set;
        }

        public bool SelectedSymptomsEnabled
        {
            get;
            private set;
        }

        public bool CorrectSymptomsEnabled
        {
            get;
            private set;
        }

        public List<string> SelectedSymptoms
        {
            get;
            private set;
        }

        public List<string> CorrectSymptoms
        {
            get;
            private set;
        }

        public List<string> SelectedGrounds
        {
            get;
            private set;
        }

        public List<string> CorrectGrounds
        {
            get;
            private set;
        }

        public bool DecisionCorrect
        {
            get;
            private set;
        }

        public bool SymptomsCorrect
        {
            get;
            private set;
        }

        public bool GroundsCorrect
        {
            get;
            private set;
        }

        public bool IsCorrect =>
            DecisionCorrect &&
            SymptomsCorrect &&
            GroundsCorrect;

        public EvaluationResult(
            string visitorId,
            string clientName,
            string variantId,
            DirectionDecision selectedDecision,
            DirectionDecision correctDecision,
            bool selectedSymptomsEnabled,
            bool correctSymptomsEnabled,
            List<string> selectedSymptoms,
            List<string> correctSymptoms,
            List<string> selectedGrounds,
            List<string> correctGrounds,
            bool decisionCorrect,
            bool symptomsCorrect,
            bool groundsCorrect)
        {
            VisitorId = visitorId;
            ClientName = clientName;
            VariantId = variantId;

            SelectedDecision =
                selectedDecision;

            CorrectDecision =
                correctDecision;

            SelectedSymptomsEnabled =
                selectedSymptomsEnabled;

            CorrectSymptomsEnabled =
                correctSymptomsEnabled;

            SelectedSymptoms =
                selectedSymptoms;

            CorrectSymptoms =
                correctSymptoms;

            SelectedGrounds =
                selectedGrounds;

            CorrectGrounds =
                correctGrounds;

            DecisionCorrect =
                decisionCorrect;

            SymptomsCorrect =
                symptomsCorrect;

            GroundsCorrect =
                groundsCorrect;
        }
    }

    [Header("Форма направления")]

    [Tooltip("Контроллер, который собирает текущий выбор игрока.")]
    [SerializeField]
    private DirectionFormController
        formController;

    private void Awake()
    {
        FindReferences();
    }

    public bool TryEvaluate(
        out EvaluationResult result)
    {
        result = null;

        FindReferences();

        if (formController == null)
            return false;

        VisitorCaseData currentClient =
            CurrentClientContext
                .CurrentClient;

        VisitorCaseData.VisitorCaseVariant
            currentVariant =
                CurrentClientContext
                    .CurrentVariant;

        if (currentClient == null ||
            currentVariant == null)
        {
            return false;
        }

        VisitorCaseData.DirectionAnswerData
            correctAnswer =
                currentVariant
                    .CorrectDirection;

        if (correctAnswer == null)
            return false;

        DirectionDecision selectedDecision =
            formController
                .GetSelectedDecision();

        List<string> selectedSymptoms =
            formController
                .GetSelectedSymptoms();

        List<string> selectedGrounds =
            formController
                .GetSelectedGrounds();

        List<string> correctSymptoms =
            CopyList(
                correctAnswer
                    .CorrectSymptoms
            );

        List<string> correctGrounds =
            CopyList(
                correctAnswer
                    .CorrectGrounds
            );

        bool selectedSymptomsEnabled =
            formController
                .GetPhysicalSymptomsEnabled();

        bool correctSymptomsEnabled =
            correctSymptoms.Count > 0;

        bool decisionCorrect =
            selectedDecision ==
            correctAnswer
                .CorrectDecision;

        bool symptomsCorrect =
            selectedSymptomsEnabled ==
                correctSymptomsEnabled &&
            AreSetsEqual(
                selectedSymptoms,
                correctSymptoms
            );

        bool groundsCorrect =
            AreSetsEqual(
                selectedGrounds,
                correctGrounds
            );

        result =
            new EvaluationResult(
                currentClient.VisitorId,
                currentClient.ClientName,
                currentVariant.VariantId,
                selectedDecision,
                correctAnswer.CorrectDecision,
                selectedSymptomsEnabled,
                correctSymptomsEnabled,
                selectedSymptoms,
                correctSymptoms,
                selectedGrounds,
                correctGrounds,
                decisionCorrect,
                symptomsCorrect,
                groundsCorrect
            );

        return true;
    }

    private bool AreSetsEqual(
        List<string> first,
        List<string> second)
    {
        HashSet<string> firstSet =
            BuildNormalizedSet(first);

        HashSet<string> secondSet =
            BuildNormalizedSet(second);

        return firstSet.SetEquals(
            secondSet
        );
    }

    private HashSet<string> BuildNormalizedSet(
        List<string> source)
    {
        HashSet<string> result =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        if (source == null)
            return result;

        for (int i = 0;
             i < source.Count;
             i++)
        {
            string value =
                source[i];

            if (string.IsNullOrWhiteSpace(
                    value))
            {
                continue;
            }

            result.Add(
                value.Trim()
            );
        }

        return result;
    }

    private List<string> CopyList(
        List<string> source)
    {
        if (source == null)
        {
            return new List<string>();
        }

        return new List<string>(
            source
        );
    }

    private void FindReferences()
    {
        if (formController == null)
        {
            formController =
                FindFirstObjectByType
                    <DirectionFormController>(
                        FindObjectsInactive.Include
                    );
        }
    }
}
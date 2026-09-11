using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceController : MonoBehaviour
{
    [Header("Общий объект плашек")]
    [SerializeField]
    private GameObject choicesRoot;


    [Header("Первая плашка")]
    [SerializeField]
    private Button firstButton;

    [SerializeField]
    private TMP_Text firstButtonText;


    [Header("Вторая плашка")]
    [SerializeField]
    private Button secondButton;

    [SerializeField]
    private TMP_Text secondButtonText;


    [Header("Dialogue Manager")]
    [Tooltip(
        "Если пусто — найдётся автоматически."
    )]
    [SerializeField]
    private DialogueManager dialogueManager;



    public static bool AnyChoiceOpen
    {
        get;
        private set;
    }


    public bool IsOpen =>
        isOpen;



    private UnityEngine.Object currentOwner;

    private Action<int> selectedCallback;

    private bool isOpen;

    private bool firstAvailable;

    private bool secondAvailable;



    private void Awake()
    {
        FindReferences();

        BindButtons();

        HideInternal();
    }


    private void OnEnable()
    {
        FindReferences();
    }


    private void OnDestroy()
    {
        UnbindButtons();

        HideInternal();
    }



    // =====================================================
    // ПОКАЗ ВАРИАНТОВ
    // =====================================================

    public bool ShowChoices(
        UnityEngine.Object owner,
        string firstText,
        string secondText,
        Action<int> callback)
    {
        FindReferences();


        if (owner == null ||
            callback == null)
        {
            return false;
        }


        if (isOpen &&
            currentOwner != owner)
        {
            return false;
        }



        firstAvailable =
            !string.IsNullOrWhiteSpace(
                firstText
            );


        secondAvailable =
            !string.IsNullOrWhiteSpace(
                secondText
            );



        if (!firstAvailable &&
            !secondAvailable)
        {
            return false;
        }



        currentOwner = owner;

        selectedCallback = callback;



        SetupButton(
            firstButton,
            firstButtonText,
            firstText,
            firstAvailable
        );


        SetupButton(
            secondButton,
            secondButtonText,
            secondText,
            secondAvailable
        );



        if (choicesRoot != null)
            choicesRoot.SetActive(true);



        isOpen = true;

        AnyChoiceOpen = true;



        return true;
    }



    // =====================================================
    // НАЖАТИЕ
    // =====================================================

    private void HandleFirstPressed()
    {
        Select(0);
    }


    private void HandleSecondPressed()
    {
        Select(1);
    }



    private void Select(int index)
    {
        if (!isOpen)
            return;


        bool available =
            index == 0
            ? firstAvailable
            : secondAvailable;


        if (!available)
            return;



        Action<int> callback =
            selectedCallback;



        /*
         * ВАЖНО:
         *
         * Сначала убираем только кнопки.
         *
         * DialogueManager всё ещё держит
         * последнюю реплику на экране.
         */


        HideInternal();



        /*
         * Теперь завершаем ChoicePrompt.
         *
         * true = оставить диалоговую панель.
         *
         * Следующий ответ телефона
         * сразу заменит текст.
         */


        if (dialogueManager != null &&
            dialogueManager.DialogueActive &&
            dialogueManager.ChoicePromptReady)
        {
            dialogueManager
                .FinishChoicePrompt(true);
        }



        callback?.Invoke(index);
    }




    // =====================================================
    // СКРЫТИЕ
    // =====================================================

    public void HideChoices(
        UnityEngine.Object owner)
    {
        if (!isOpen)
            return;


        if (owner != null &&
            currentOwner != owner)
        {
            return;
        }


        HideInternal();
    }



    private void HideInternal()
    {
        isOpen = false;

        AnyChoiceOpen = false;


        currentOwner = null;

        selectedCallback = null;


        firstAvailable = false;

        secondAvailable = false;



        if (choicesRoot != null)
            choicesRoot.SetActive(false);



        DisableButton(
            firstButton,
            firstButtonText
        );


        DisableButton(
            secondButton,
            secondButtonText
        );
    }




    private void SetupButton(
        Button button,
        TMP_Text text,
        string value,
        bool active)
    {
        if (button != null)
        {
            button.gameObject
                .SetActive(active);

            button.interactable =
                active;
        }


        if (text != null)
        {
            text.text =
                active
                ? value
                : "";
        }
    }



    private void DisableButton(
        Button button,
        TMP_Text text)
    {
        if (button != null)
        {
            button.interactable = false;
            button.gameObject.SetActive(false);
        }


        if (text != null)
            text.text = "";
    }



    // =====================================================
    // BUTTON EVENTS
    // =====================================================

    private void BindButtons()
    {
        if (firstButton != null)
        {
            firstButton.onClick
                .RemoveListener(
                    HandleFirstPressed
                );

            firstButton.onClick
                .AddListener(
                    HandleFirstPressed
                );
        }


        if (secondButton != null)
        {
            secondButton.onClick
                .RemoveListener(
                    HandleSecondPressed
                );

            secondButton.onClick
                .AddListener(
                    HandleSecondPressed
                );
        }
    }



    private void UnbindButtons()
    {
        if (firstButton != null)
        {
            firstButton.onClick
                .RemoveListener(
                    HandleFirstPressed
                );
        }


        if (secondButton != null)
        {
            secondButton.onClick
                .RemoveListener(
                    HandleSecondPressed
                );
        }
    }



    // =====================================================
    // REFERENCES
    // =====================================================

    private void FindReferences()
    {
        if (dialogueManager != null)
            return;


        dialogueManager =
            FindFirstObjectByType
            <DialogueManager>(
                FindObjectsInactive.Include
            );
    }
}
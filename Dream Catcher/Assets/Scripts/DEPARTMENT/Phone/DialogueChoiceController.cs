using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueChoiceController : MonoBehaviour
{
    public sealed class ChoiceOption
    {
        public int Index;
        public string Text;
        public bool Interactable = true;
        public bool OverrideTextColor;
        public Color TextColor;
    }

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

    [Header("Страницы — нужны при трёх и более вариантах")]
    [SerializeField]
    private Button previousPageButton;

    [SerializeField]
    private Button nextPageButton;

    [Header("Dialogue Manager")]
    [SerializeField]
    private DialogueManager dialogueManager;

    private static DialogueChoiceController activeController;

    public static bool AnyChoiceOpen =>
        activeController != null && activeController.isOpen;

    public static int LastClosedFrame { get; private set; } = -1;

    public static bool BlockWorldInteraction =>
        AnyChoiceOpen || LastClosedFrame == Time.frameCount;

    public bool IsOpen => isOpen;

    private readonly List<ChoiceOption> options =
        new List<ChoiceOption>(8);

    private UnityEngine.Object currentOwner;
    private Action<int> selectedCallback;

    private bool initialized;
    private bool isOpen;
    private int page;

    private Color firstDefaultTextColor;
    private Color secondDefaultTextColor;

    private int PageCount => (options.Count + 1) / 2;

    private void Awake()
    {
        EnsureInitialized();

        if (!isOpen)
            HideInternal();
    }

    private void OnEnable()
    {
        FindReferences();
    }

    private void OnDisable()
    {
        if (isOpen)
            HideInternal();
    }

    private void OnDestroy()
    {
        UnbindButtons();

        if (activeController == this)
        {
            activeController = null;
            LastClosedFrame = Time.frameCount;
        }
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        FindReferences();

        firstDefaultTextColor =
            firstButtonText != null ? firstButtonText.color : Color.white;

        secondDefaultTextColor =
            secondButtonText != null ? secondButtonText.color : Color.white;

        BindButtons();
    }

    public bool IsOwnedBy(UnityEngine.Object owner)
    {
        return isOpen && owner != null && currentOwner == owner;
    }

    public bool CanDisplayOptionCount(int count)
    {
        EnsureInitialized();

        if (count <= 0)
            return false;

        if (firstButton == null || firstButtonText == null)
            return false;

        if (count > 1 &&
            (secondButton == null || secondButtonText == null))
        {
            return false;
        }

        if (count > 2 &&
            (previousPageButton == null || nextPageButton == null))
        {
            return false;
        }

        return true;
    }

    public bool ShowChoices(
        UnityEngine.Object owner,
        string firstText,
        string secondText,
        Action<int> callback)
    {
        var values = new List<ChoiceOption>(2);

        if (!string.IsNullOrWhiteSpace(firstText))
        {
            values.Add(new ChoiceOption
            {
                Index = 0,
                Text = firstText
            });
        }

        if (!string.IsNullOrWhiteSpace(secondText))
        {
            values.Add(new ChoiceOption
            {
                Index = 1,
                Text = secondText
            });
        }

        return ShowOptions(owner, values, callback);
    }

    public bool ShowOptions(
        UnityEngine.Object owner,
        IList<ChoiceOption> values,
        Action<int> callback)
    {
        EnsureInitialized();
        FindReferences();

        if (owner == null || callback == null || values == null)
            return false;

        if (AnyChoiceOpen && activeController != this)
            return false;

        if (isOpen && currentOwner != owner)
            return false;

        int count = 0;

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] != null &&
                !string.IsNullOrWhiteSpace(values[i].Text))
            {
                count++;
            }
        }

        if (!CanDisplayOptionCount(count))
        {
            Debug.LogError(
                "DialogueChoiceController: проверь кнопки, тексты " +
                "и кнопки страниц для трёх и более вариантов.", this);
            return false;
        }

        options.Clear();

        for (int i = 0; i < values.Count; i++)
        {
            ChoiceOption option = values[i];

            if (option != null &&
                !string.IsNullOrWhiteSpace(option.Text))
            {
                options.Add(option);
            }
        }

        currentOwner = owner;
        selectedCallback = callback;
        page = 0;
        isOpen = true;
        activeController = this;

        if (choicesRoot != null)
            choicesRoot.SetActive(true);

        RefreshPage();
        return true;
    }

    public void HideChoices(UnityEngine.Object owner)
    {
        if (!isOpen)
            return;

        if (owner != null && currentOwner != owner)
            return;

        HideInternal();
    }

    private void HandleFirstPressed()
    {
        SelectSlot(0);
    }

    private void HandleSecondPressed()
    {
        SelectSlot(1);
    }

    private void SelectSlot(int slot)
    {
        if (!isOpen)
            return;

        int position = page * 2 + slot;

        if (position < 0 || position >= options.Count)
            return;

        ChoiceOption option = options[position];

        if (!option.Interactable)
            return;

        Action<int> callback = selectedCallback;
        int selectedIndex = option.Index;

        HideInternal();

        if (dialogueManager != null &&
            dialogueManager.DialogueActive &&
            dialogueManager.ChoicePromptReady)
        {
            dialogueManager.FinishChoicePrompt(true);
        }

        callback?.Invoke(selectedIndex);
    }

    private void PreviousPage()
    {
        if (!isOpen || page <= 0)
            return;

        page--;
        RefreshPage();
    }

    private void NextPage()
    {
        if (!isOpen || page + 1 >= PageCount)
            return;

        page++;
        RefreshPage();
    }

    private void RefreshPage()
    {
        SetupSlot(
            firstButton,
            firstButtonText,
            page * 2,
            firstDefaultTextColor);

        SetupSlot(
            secondButton,
            secondButtonText,
            page * 2 + 1,
            secondDefaultTextColor);

        bool hasPages = PageCount > 1;

        if (previousPageButton != null)
        {
            previousPageButton.gameObject.SetActive(hasPages);
            previousPageButton.interactable = page > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.gameObject.SetActive(hasPages);
            nextPageButton.interactable = page + 1 < PageCount;
        }
    }

    private void SetupSlot(
        Button button,
        TMP_Text text,
        int position,
        Color defaultColor)
    {
        bool exists = position >= 0 && position < options.Count;

        if (button != null)
        {
            button.gameObject.SetActive(exists);
            button.interactable =
                exists && options[position].Interactable;
        }

        if (text == null)
            return;

        text.text = exists ? options[position].Text : "";

        text.color =
            exists && options[position].OverrideTextColor
                ? options[position].TextColor
                : defaultColor;
    }

    private void HideInternal()
    {
        bool wasOpen = isOpen;

        isOpen = false;
        currentOwner = null;
        selectedCallback = null;
        options.Clear();
        page = 0;

        if (activeController == this)
            activeController = null;

        if (wasOpen)
            LastClosedFrame = Time.frameCount;

        SetupSlot(
            firstButton, firstButtonText, -1, firstDefaultTextColor);

        SetupSlot(
            secondButton, secondButtonText, -1, secondDefaultTextColor);

        if (previousPageButton != null)
            previousPageButton.gameObject.SetActive(false);

        if (nextPageButton != null)
            nextPageButton.gameObject.SetActive(false);

        if (choicesRoot != null)
            choicesRoot.SetActive(false);
    }

    private void BindButtons()
    {
        if (firstButton != null)
        {
            firstButton.onClick.RemoveListener(HandleFirstPressed);
            firstButton.onClick.AddListener(HandleFirstPressed);
        }

        if (secondButton != null)
        {
            secondButton.onClick.RemoveListener(HandleSecondPressed);
            secondButton.onClick.AddListener(HandleSecondPressed);
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(PreviousPage);
            previousPageButton.onClick.AddListener(PreviousPage);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(NextPage);
            nextPageButton.onClick.AddListener(NextPage);
        }
    }

    private void UnbindButtons()
    {
        if (firstButton != null)
            firstButton.onClick.RemoveListener(HandleFirstPressed);

        if (secondButton != null)
            secondButton.onClick.RemoveListener(HandleSecondPressed);

        if (previousPageButton != null)
            previousPageButton.onClick.RemoveListener(PreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.RemoveListener(NextPage);
    }

    private void FindReferences()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>(
                FindObjectsInactive.Include);
        }
    }
}
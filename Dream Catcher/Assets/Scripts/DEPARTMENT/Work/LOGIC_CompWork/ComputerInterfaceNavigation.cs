using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComputerInterfaceNavigation :
    MonoBehaviour
{
    private enum MainTab
    {
        SleepRecording,
        ElectronicDirection
    }

    [System.Serializable]
    private class TabEntry
    {
        [Header("Кнопка")]
        [Tooltip("Постоянный объект кнопки.")]
        public Button button;

        [Tooltip("Image на этом же объекте кнопки.")]
        public Image buttonImage;

        [Tooltip("Дочерний текст кнопки.")]
        public TMP_Text buttonText;

        [Header("Страница")]
        [Tooltip("Содержимое соответствующей вкладки.")]
        public GameObject page;

        [Header("Обычное состояние")]
        [Tooltip("PNG невыбранной вкладки.")]
        public Sprite normalSprite;

        [Tooltip("Цвет текста невыбранной вкладки.")]
        public Color normalTextColor =
            Color.white;

        [Header("Выбранное состояние")]
        [Tooltip("PNG выбранной вкладки.")]
        public Sprite selectedSprite;

        [Tooltip("Цвет текста выбранной вкладки.")]
        public Color selectedTextColor =
            Color.black;
    }

    [System.Serializable]
    private class PopupEntry
    {
        [Tooltip("Кнопка открытия окна.")]
        public Button button;

        [Tooltip("Само открываемое окно.")]
        public GameObject window;
    }

    [Header("Основные вкладки")]
    [SerializeField]
    private TabEntry sleepRecordingTab;

    [SerializeField]
    private TabEntry electronicDirectionTab;

    [Header("Дополнительные окна")]
    [SerializeField]
    private PopupEntry directivesPopup;

    [SerializeField]
    private PopupEntry instructionPopup;

    [Tooltip("Закрывать инструкцию при любом следующем клике.")]
    [SerializeField]
    private bool closeInstructionOnAnyClick =
        true;

    private MainTab currentTab;

    private bool directivesOpen;
    private bool instructionOpen;

    private int popupButtonHandledFrame =
        -1;

    public event Action
        ElectronicDirectionOpened;

    public bool IsElectronicDirectionSelected =>
        currentTab ==
        MainTab.ElectronicDirection;

    private void Awake()
    {
        FindMissingTabReferences();
        AddButtonListeners();

        currentTab =
            MainTab.SleepRecording;

        directivesOpen = false;
        instructionOpen = false;

        ApplyMainTabState();
        ApplyPopupState();
    }

    private void LateUpdate()
    {
        if (!closeInstructionOnAnyClick ||
            !instructionOpen ||
            !Input.GetMouseButtonUp(0))
        {
            return;
        }

        if (popupButtonHandledFrame ==
            Time.frameCount)
        {
            return;
        }

        instructionOpen = false;

        ApplyPopupState();
    }

    private void OnDisable()
    {
        directivesOpen = false;
        instructionOpen = false;
        popupButtonHandledFrame = -1;

        ApplyPopupState();
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
    }

    public void ShowSleepRecordingTab()
    {
        SetMainTab(
            MainTab.SleepRecording
        );
    }

    public void ShowElectronicDirectionTab()
    {
        SetMainTab(
            MainTab.ElectronicDirection
        );
    }

    public void ToggleDirectives()
    {
        if (!HasWindow(directivesPopup))
            return;

        popupButtonHandledFrame =
            Time.frameCount;

        if (directivesOpen)
        {
            directivesOpen = false;
            instructionOpen = false;
        }
        else
        {
            directivesOpen = true;
            instructionOpen = false;
        }

        ApplyPopupState();
    }

    public void ToggleInstruction()
    {
        if (!HasWindow(instructionPopup))
            return;

        popupButtonHandledFrame =
            Time.frameCount;

        instructionOpen =
            !instructionOpen;

        ApplyPopupState();
    }

    public void CloseAllPopups()
    {
        directivesOpen = false;
        instructionOpen = false;
        popupButtonHandledFrame = -1;

        ApplyPopupState();
    }

    private void SetMainTab(
        MainTab newTab)
    {
        bool tabChanged =
            currentTab != newTab;

        currentTab = newTab;

        ApplyMainTabState();

        if (!tabChanged)
            return;

        CloseAllPopups();

        if (newTab ==
            MainTab.ElectronicDirection)
        {
            ElectronicDirectionOpened
                ?.Invoke();
        }
    }

    private void ApplyMainTabState()
    {
        bool sleepRecordingSelected =
            currentTab ==
            MainTab.SleepRecording;

        ApplyTabState(
            sleepRecordingTab,
            sleepRecordingSelected
        );

        ApplyTabState(
            electronicDirectionTab,
            !sleepRecordingSelected
        );
    }

    private void ApplyTabState(
        TabEntry tab,
        bool selected)
    {
        if (tab == null)
            return;

        if (tab.page != null)
        {
            tab.page.SetActive(
                selected
            );
        }

        if (tab.buttonImage != null)
        {
            Sprite targetSprite =
                selected
                    ? tab.selectedSprite
                    : tab.normalSprite;

            if (targetSprite != null)
            {
                tab.buttonImage.sprite =
                    targetSprite;
            }
        }

        if (tab.buttonText != null)
        {
            tab.buttonText.color =
                selected
                    ? tab.selectedTextColor
                    : tab.normalTextColor;
        }
    }

    private void ApplyPopupState()
    {
        SetWindowActive(
            directivesPopup,
            directivesOpen
        );

        SetWindowActive(
            instructionPopup,
            instructionOpen
        );
    }

    private void SetWindowActive(
        PopupEntry popup,
        bool active)
    {
        if (!HasWindow(popup))
            return;

        if (popup.window.activeSelf ==
            active)
        {
            return;
        }

        popup.window.SetActive(active);
    }

    private bool HasWindow(
        PopupEntry popup)
    {
        return popup != null &&
               popup.window != null;
    }

    private void FindMissingTabReferences()
    {
        FindTabReferences(
            sleepRecordingTab
        );

        FindTabReferences(
            electronicDirectionTab
        );
    }

    private void FindTabReferences(
        TabEntry tab)
    {
        if (tab == null ||
            tab.button == null)
        {
            return;
        }

        if (tab.buttonImage == null)
        {
            tab.buttonImage =
                tab.button
                    .GetComponent<Image>();
        }

        if (tab.buttonText == null)
        {
            tab.buttonText =
                tab.button
                    .GetComponentInChildren
                        <TMP_Text>(true);
        }
    }

    private void AddButtonListeners()
    {
        if (sleepRecordingTab != null &&
            sleepRecordingTab.button != null)
        {
            sleepRecordingTab.button.onClick
                .RemoveListener(
                    ShowSleepRecordingTab
                );

            sleepRecordingTab.button.onClick
                .AddListener(
                    ShowSleepRecordingTab
                );
        }

        if (electronicDirectionTab != null &&
            electronicDirectionTab.button != null)
        {
            electronicDirectionTab.button.onClick
                .RemoveListener(
                    ShowElectronicDirectionTab
                );

            electronicDirectionTab.button.onClick
                .AddListener(
                    ShowElectronicDirectionTab
                );
        }

        if (directivesPopup != null &&
            directivesPopup.button != null)
        {
            directivesPopup.button.onClick
                .RemoveListener(
                    ToggleDirectives
                );

            directivesPopup.button.onClick
                .AddListener(
                    ToggleDirectives
                );
        }

        if (instructionPopup != null &&
            instructionPopup.button != null)
        {
            instructionPopup.button.onClick
                .RemoveListener(
                    ToggleInstruction
                );

            instructionPopup.button.onClick
                .AddListener(
                    ToggleInstruction
                );
        }
    }

    private void RemoveButtonListeners()
    {
        if (sleepRecordingTab != null &&
            sleepRecordingTab.button != null)
        {
            sleepRecordingTab.button.onClick
                .RemoveListener(
                    ShowSleepRecordingTab
                );
        }

        if (electronicDirectionTab != null &&
            electronicDirectionTab.button != null)
        {
            electronicDirectionTab.button.onClick
                .RemoveListener(
                    ShowElectronicDirectionTab
                );
        }

        if (directivesPopup != null &&
            directivesPopup.button != null)
        {
            directivesPopup.button.onClick
                .RemoveListener(
                    ToggleDirectives
                );
        }

        if (instructionPopup != null &&
            instructionPopup.button != null)
        {
            instructionPopup.button.onClick
                .RemoveListener(
                    ToggleInstruction
                );
        }
    }
}
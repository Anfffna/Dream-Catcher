using UnityEngine;

public class MainMenuRemoteController : MonoBehaviour
{
    private enum PendingAction
    {
        None,
        NewGame,
        Continue,
        Load,
        Settings,
        Quit
    }


    [Header("Аниматор руки с пультом")]
    [Tooltip("Animator руки и пульта.")]
    [SerializeField] private Animator remoteAnimator;

    [Tooltip(
        "Точное название состояния покоя. " +
        "У тебя это default_state."
    )]
    [SerializeField] private string defaultStateName = "default_state";


    [Header("Рабочая логика меню")]
    [Tooltip("Существующий MainMenuController.")]
    [SerializeField] private MainMenuController mainMenuController;

    [Tooltip("Существующий MainMenuTVZoomController.")]
    [SerializeField] private MainMenuTVZoomController tvZoomController;

    [Tooltip(
        "Моргание при Новой игре и Продолжить."
    )]
    [SerializeField] private MainMenuBlinkTransition blinkTransition;


    [Header("Названия Trigger в Animator")]
    [SerializeField] private string continueTrigger = "continue";
    [SerializeField] private string newGameTrigger = "new_game";
    [SerializeField] private string loadTrigger = "download";
    [SerializeField] private string settingsTrigger = "settings";
    [SerializeField] private string quitTrigger = "vkl_vikl";


    private PendingAction pendingAction =
        PendingAction.None;

    private bool buttonAnimationInProgress;

    private bool actionAlreadyExecuted;

    private bool hasLeftDefaultState;

    private int defaultStateHash;


    private void Awake()
    {
        defaultStateHash =
            Animator.StringToHash(
                defaultStateName
            );

        FindBlinkTransition();
    }


    private void Update()
    {
        CheckAnimationFinished();
    }


    // =========================================================
    // КНОПКИ
    // =========================================================

    public void PressNewGame()
    {
        StartButtonAnimation(
            PendingAction.NewGame,
            newGameTrigger
        );
    }


    public void PressContinue()
    {
        StartButtonAnimation(
            PendingAction.Continue,
            continueTrigger
        );
    }


    public void PressLoad()
    {
        StartButtonAnimation(
            PendingAction.Load,
            loadTrigger
        );
    }


    public void PressSettings()
    {
        StartButtonAnimation(
            PendingAction.Settings,
            settingsTrigger
        );
    }


    public void PressQuit()
    {
        StartButtonAnimation(
            PendingAction.Quit,
            quitTrigger
        );
    }


    // =========================================================
    // ЗАПУСК АНИМАЦИИ
    // =========================================================

    private void StartButtonAnimation(
        PendingAction action,
        string triggerName)
    {
        if (buttonAnimationInProgress)
            return;

        if (remoteAnimator == null)
            return;

        if (string.IsNullOrEmpty(triggerName))
            return;


        buttonAnimationInProgress = true;

        actionAlreadyExecuted = false;
        hasLeftDefaultState = false;

        pendingAction = action;


        ResetAllTriggers();

        remoteAnimator.SetTrigger(
            triggerName
        );
    }


    private void ResetAllTriggers()
    {
        if (remoteAnimator == null)
            return;


        if (!string.IsNullOrEmpty(continueTrigger))
            remoteAnimator.ResetTrigger(continueTrigger);

        if (!string.IsNullOrEmpty(newGameTrigger))
            remoteAnimator.ResetTrigger(newGameTrigger);

        if (!string.IsNullOrEmpty(loadTrigger))
            remoteAnimator.ResetTrigger(loadTrigger);

        if (!string.IsNullOrEmpty(settingsTrigger))
            remoteAnimator.ResetTrigger(settingsTrigger);

        if (!string.IsNullOrEmpty(quitTrigger))
            remoteAnimator.ResetTrigger(quitTrigger);
    }


    // =========================================================
    // ANIMATION EVENT
    // =========================================================

    public void ExecutePendingAction()
    {
        if (!buttonAnimationInProgress)
            return;

        if (actionAlreadyExecuted)
            return;


        actionAlreadyExecuted = true;


        switch (pendingAction)
        {
            case PendingAction.NewGame:

                if (FindBlinkTransition())
                {
                    blinkTransition
                        .PlayNewGame();
                }

                break;


            case PendingAction.Continue:

                if (FindBlinkTransition())
                {
                    blinkTransition
                        .PlayContinue();
                }

                break;


            case PendingAction.Load:

                if (tvZoomController != null)
                {
                    tvZoomController
                        .OpenLoad();
                }

                break;


            case PendingAction.Settings:

                if (tvZoomController != null)
                {
                    tvZoomController
                        .OpenSettings();
                }

                break;


            case PendingAction.Quit:

                if (tvZoomController != null)
                {
                    tvZoomController
                        .OpenQuit();
                }

                break;
        }
    }


    // =========================================================
    // ПРОВЕРКА ОКОНЧАНИЯ АНИМАЦИИ
    // =========================================================

    private void CheckAnimationFinished()
    {
        if (!buttonAnimationInProgress)
            return;

        if (remoteAnimator == null)
            return;


        AnimatorStateInfo stateInfo =
            remoteAnimator
                .GetCurrentAnimatorStateInfo(0);


        if (!hasLeftDefaultState)
        {
            if (stateInfo.shortNameHash !=
                    defaultStateHash ||
                remoteAnimator.IsInTransition(0))
            {
                hasLeftDefaultState = true;
            }

            return;
        }


        if (remoteAnimator.IsInTransition(0))
            return;


        if (stateInfo.shortNameHash !=
            defaultStateHash)
        {
            return;
        }


        /*
         * Страховка на случай,
         * если импортированный FBX пропустил Event.
         */
        if (!actionAlreadyExecuted &&
            pendingAction != PendingAction.None)
        {
            ExecutePendingAction();
        }


        FinishCurrentButtonAnimation();
    }

    private bool FindBlinkTransition()
    {
        if (blinkTransition != null)
            return true;

        blinkTransition =
            FindFirstObjectByType<MainMenuBlinkTransition>(
                FindObjectsInactive.Include
            );

        return blinkTransition != null;
    }

    private void FinishCurrentButtonAnimation()
    {
        buttonAnimationInProgress = false;

        actionAlreadyExecuted = false;
        hasLeftDefaultState = false;

        pendingAction =
            PendingAction.None;
    }


    public void FinishButtonAnimation()
    {
        /*
         * Старые Finish Events могут оставаться
         * в FBX. Они больше ничего не решают.
         */
    }
}
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
        "Точное название состояния покоя в Animator. " +
        "У тебя это default_state."
    )]
    [SerializeField] private string defaultStateName = "default_state";


    [Header("Рабочая логика меню")]
    [Tooltip("Существующий MainMenuController.")]
    [SerializeField] private MainMenuController mainMenuController;

    [Tooltip("Существующий MainMenuTVZoomController.")]
    [SerializeField] private MainMenuTVZoomController tvZoomController;


    [Header("Названия Trigger в Animator")]
    [SerializeField] private string continueTrigger = "continue";
    [SerializeField] private string newGameTrigger = "new_game";
    [SerializeField] private string loadTrigger = "download";
    [SerializeField] private string settingsTrigger = "settings";
    [SerializeField] private string quitTrigger = "vkl_vikl";


    private PendingAction pendingAction =
        PendingAction.None;

    private bool buttonAnimationInProgress = false;

    /*
     * Показывает, было ли реальное действие меню
     * уже выполнено Animation Event'ом.
     */
    private bool actionAlreadyExecuted = false;

    /*
     * Нельзя разблокировать кнопку сразу после SetTrigger,
     * потому что Animator ещё несколько мгновений
     * может находиться в default_state.
     */
    private bool hasLeftDefaultState = false;

    private int defaultStateHash;


    private void Awake()
    {
        defaultStateHash =
            Animator.StringToHash(defaultStateName);
    }


    private void Update()
    {
        CheckAnimationFinished();
    }


    // =========================================================
    // КНОПКИ ПУЛЬТА
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
        /*
         * Текущее нажатие нельзя перебить
         * другим нажатием.
         */
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

        remoteAnimator.SetTrigger(triggerName);
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

    /*
     * Этот метод должен находиться
     * на Animation Event в момент,
     * когда палец физически нажимает кнопку.
     */
    public void ExecutePendingAction()
    {
        if (!buttonAnimationInProgress)
            return;

        /*
         * Если Event каким-то образом
         * был вызван дважды, действие
         * второй раз не выполняем.
         */
        if (actionAlreadyExecuted)
            return;


        actionAlreadyExecuted = true;


        switch (pendingAction)
        {
            case PendingAction.NewGame:

                if (mainMenuController != null)
                {
                    mainMenuController.StartGame();
                }

                break;


            case PendingAction.Continue:

                if (mainMenuController != null)
                {
                    mainMenuController
                        .OnContinueButton();
                }

                break;


            case PendingAction.Load:

                if (tvZoomController != null)
                {
                    tvZoomController.OpenLoad();
                }

                break;


            case PendingAction.Settings:

                if (tvZoomController != null)
                {
                    tvZoomController.OpenSettings();
                }

                break;


            case PendingAction.Quit:

                if (tvZoomController != null)
                {
                    tvZoomController.OpenQuit();
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


        /*
         * Сначала ждём, пока Animator
         * действительно покинет default_state.
         */
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


        /*
         * Пока идёт переход между состояниями,
         * ничего не заканчиваем.
         */
        if (remoteAnimator.IsInTransition(0))
            return;


        /*
         * Ждём настоящего возвращения
         * в default_state.
         */
        if (stateInfo.shortNameHash !=
            defaultStateHash)
        {
            return;
        }


        /*
         * ВАЖНАЯ СТРАХОВКА.
         *
         * Если Animation Event по какой-то причине
         * не сработал до возврата в default_state,
         * всё равно выполняем действие.
         *
         * В нормальной ситуации этот блок
         * вообще ничего делать не будет,
         * потому что actionAlreadyExecuted
         * уже станет true в нужный кадр.
         */
        if (!actionAlreadyExecuted &&
            pendingAction != PendingAction.None)
        {
            ExecutePendingAction();
        }


        FinishCurrentButtonAnimation();
    }


    private void FinishCurrentButtonAnimation()
    {
        buttonAnimationInProgress = false;
        actionAlreadyExecuted = false;
        hasLeftDefaultState = false;

        pendingAction =
            PendingAction.None;
    }


    // =========================================================
    // СТАРЫЙ FINISH EVENT
    // =========================================================

    /*
     * Оставляем метод на случай,
     * если FinishButtonAnimation всё ещё
     * стоит в импортированных клипах.
     *
     * Разблокировка больше от него
     * вообще не зависит.
     */
    public void FinishButtonAnimation()
    {
        // Намеренно пусто.
    }
}
using UnityEngine;
using System.Collections;

public class MainMenuTVZoomController : MonoBehaviour
{
    private enum MenuViewMode
    {
        Overview,
        Load,
        Settings,
        Quit
    }

    [Header("Камера")]
    [Tooltip("Основная камера сцены MainMenu.")]
    [SerializeField] private Camera menuCamera;

    [Tooltip(
        "Точка в центре экрана телевизора, " +
        "на которую камера смотрит при приближении. " +
        "Это НЕ позиция камеры."
    )]
    [SerializeField] private Transform tvLookTarget;


    [Header("Небольшое движение головы")]
    [Tooltip(
        "Разрешает немного осматриваться мышью " +
        "в обычном состоянии главного меню."
    )]
    [SerializeField] private bool enableSmallHeadLook = true;

    [Tooltip(
        "Чувствительность небольшого движения головы. " +
        "Начни примерно с 0.25-0.4."
    )]
    [Range(0.01f, 2f)]
    [SerializeField] private float headLookSensitivity = 0.3f;

    [Tooltip(
        "Максимальный поворот головы влево/вправо в градусах. " +
        "Для очень небольшого движения обычно 2-4 градуса."
    )]
    [Range(0f, 15f)]
    [SerializeField] private float maxHeadYaw = 3f;

    [Tooltip(
        "Максимальный поворот головы вверх/вниз в градусах. " +
        "Для очень небольшого движения обычно 1-3 градуса."
    )]
    [Range(0f, 10f)]
    [SerializeField] private float maxHeadPitch = 2f;

    [Tooltip(
        "Насколько плавно камера догоняет движение мыши."
    )]
    [Range(1f, 30f)]
    [SerializeField] private float headLookSmoothSpeed = 10f;

    [Tooltip("Инвертировать вертикальное движение.")]
    [SerializeField] private bool invertHeadLookY = false;


    [Header("TV Zoom — как в StartDay")]
    [Tooltip("Чем меньше FOV, тем сильнее приближение.")]
    [SerializeField] private float tvZoomFOV = 35f;

    [Tooltip("Продолжительность приближения к телевизору.")]
    [SerializeField] private float tvZoomDuration = 1.5f;

    [Tooltip("Продолжительность возврата обратно.")]
    [SerializeField] private float returnDuration = 1.5f;


    [Header("Главные кнопки / будущий пульт")]
    [Tooltip(
        "CanvasGroup общего родителя ВСЕХ пяти кнопок: " +
        "Новая игра / Продолжить / Загрузить / Настройки / Выйти."
    )]
    [SerializeField] private CanvasGroup mainButtonsCanvasGroup;

    [Tooltip("Сколько секунд кнопки исчезают и появляются.")]
    [SerializeField] private float buttonsFadeDuration = 0.25f;


    [Header("Существующее меню")]
    [Tooltip("Существующий MainMenuController.")]
    [SerializeField] private MainMenuController mainMenuController;

    [Tooltip("LoadPanel на World Space Canvas телевизора.")]
    [SerializeField] private GameObject loadPanel;

    [Tooltip("SettingsPanel на World Space Canvas телевизора.")]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip("ExitPanel на World Space Canvas телевизора.")]
    [SerializeField] private GameObject quitPanel;


    [Header("Стрелка Назад")]
    [Tooltip("Корневой объект кнопки Назад на телевизоре.")]
    [SerializeField] private GameObject backButton;

    [Tooltip("CanvasGroup кнопки Назад.")]
    [SerializeField] private CanvasGroup backButtonCanvasGroup;

    [Tooltip("Время появления и исчезновения стрелки.")]
    [SerializeField] private float backButtonFadeDuration = 0.5f;


    private MenuViewMode currentMode =
        MenuViewMode.Overview;

    private bool isTransitioning;


    // =========================================================
    // ИСХОДНАЯ ПОЗА КАМЕРЫ
    // =========================================================

    private Vector3 overviewCameraPosition;
    private Quaternion overviewBaseRotation;
    private float overviewCameraFOV;

    /*
     * Поза, в которой игрок находился прямо
     * перед нажатием Загрузить / Настройки / Выйти.
     *
     * Если он немного повернул голову,
     * после Назад мы вернём именно туда.
     */
    private Quaternion overviewRotationBeforeZoom;


    // =========================================================
    // SMALL HEAD LOOK
    // =========================================================

    private float currentHeadYaw;
    private float currentHeadPitch;


    // =========================================================
    // UI
    // =========================================================

    private Coroutine buttonsFadeCoroutine;


    private void Start()
    {
        if (menuCamera == null)
            menuCamera = Camera.main;


        if (backButtonCanvasGroup == null &&
            backButton != null)
        {
            backButtonCanvasGroup =
                backButton.GetComponent<CanvasGroup>();
        }


        CaptureOverviewCameraPose();

        SetMainButtonsImmediate(true);
        HideBackButtonImmediate();


        StartCoroutine(
            RegisterCursorEventsWhenReady()
        );
    }


    private void Update()
    {
        UpdateSmallHeadLook();
    }


    // =========================================================
    // НЕБОЛЬШОЕ ДВИЖЕНИЕ ГОЛОВЫ
    // =========================================================

    private void UpdateSmallHeadLook()
    {
        if (!enableSmallHeadLook)
            return;

        if (menuCamera == null)
            return;

        /*
         * Головой можно двигать только
         * в обычном состоянии меню.
         *
         * Во время Zoom и работы с TV
         * камера полностью неподвижна.
         */
        if (currentMode != MenuViewMode.Overview)
            return;

        if (isTransitioning)
            return;


        float mouseX =
            Input.GetAxisRaw("Mouse X");

        float mouseY =
            Input.GetAxisRaw("Mouse Y");


        currentHeadYaw +=
            mouseX *
            headLookSensitivity;


        float verticalInput =
            invertHeadLookY
                ? mouseY
                : -mouseY;


        currentHeadPitch +=
            verticalInput *
            headLookSensitivity;


        /*
         * Очень маленькие ограничения.
         *
         * Именно они определяют,
         * насколько далеко вообще
         * разрешено "качать головой".
         */
        currentHeadYaw =
            Mathf.Clamp(
                currentHeadYaw,
                -maxHeadYaw,
                maxHeadYaw
            );


        currentHeadPitch =
            Mathf.Clamp(
                currentHeadPitch,
                -maxHeadPitch,
                maxHeadPitch
            );


        Quaternion targetRotation =
            overviewBaseRotation *
            Quaternion.Euler(
                currentHeadPitch,
                currentHeadYaw,
                0f
            );


        float smoothFactor =
            1f -
            Mathf.Exp(
                -headLookSmoothSpeed *
                Time.unscaledDeltaTime
            );


        /*
         * POSITION НИКОГДА НЕ МЕНЯЕМ.
         */
        menuCamera.transform.position =
            overviewCameraPosition;


        menuCamera.transform.rotation =
            Quaternion.Slerp(
                menuCamera.transform.rotation,
                targetRotation,
                smoothFactor
            );


        /*
         * В обычном режиме FOV тоже
         * всегда остаётся исходным.
         */
        menuCamera.fieldOfView =
            overviewCameraFOV;
    }


    // =========================================================
    // ПУБЛИЧНЫЕ КНОПКИ
    // =========================================================

    public void OpenLoad()
    {
        if (!CanOpenTVMode())
            return;


        StartCoroutine(
            OpenTVMode(
                MenuViewMode.Load
            )
        );
    }


    public void OpenSettings()
    {
        if (!CanOpenTVMode())
            return;


        StartCoroutine(
            OpenTVMode(
                MenuViewMode.Settings
            )
        );
    }


    public void OpenQuit()
    {
        if (!CanOpenTVMode())
            return;


        StartCoroutine(
            OpenTVMode(
                MenuViewMode.Quit
            )
        );
    }


    public void BackToOverview()
    {
        if (isTransitioning)
            return;


        if (currentMode ==
            MenuViewMode.Overview)
        {
            return;
        }


        StartCoroutine(
            ReturnFromTV()
        );
    }


    private bool CanOpenTVMode()
    {
        if (isTransitioning)
            return false;


        if (currentMode !=
            MenuViewMode.Overview)
        {
            return false;
        }


        return true;
    }


    // =========================================================
    // ОТКРЫТИЕ TV
    // =========================================================

    private IEnumerator OpenTVMode(
        MenuViewMode targetMode)
    {
        if (menuCamera == null ||
            tvLookTarget == null)
        {
            yield break;
        }


        isTransitioning = true;


        /*
         * Запоминаем текущий небольшой
         * поворот головы.
         *
         * Назад вернёт именно его.
         */
        overviewRotationBeforeZoom =
            menuCamera.transform.rotation;


        currentMode =
            targetMode;


        /*
         * Сразу запрещаем повторные клики.
         */
        SetMainButtonsInteractable(false);


        /*
         * СРАЗУ:
         *
         * - проигрывается звук;
         * - появляется соответствующая панель TV.
         *
         * Никакого ожидания завершения zoom.
         */
        OpenCurrentTVPanel(
            targetMode
        );


        /*
         * Кнопки / будущий пульт
         * начинают исчезать одновременно.
         */
        StartMainButtonsFade(0f);


        /*
         * И одновременно начинается
         * Rotation + FOV zoom.
         */
        yield return ZoomToTV();


        /*
         * После окончания движения
         * появляется Назад.
         */
        yield return FadeBackButtonIn();


        isTransitioning = false;
    }


    private void OpenCurrentTVPanel(
        MenuViewMode targetMode)
    {
        if (mainMenuController == null)
            return;


        switch (targetMode)
        {
            case MenuViewMode.Load:

                mainMenuController
                    .OnLoadButton();

                break;


            case MenuViewMode.Settings:

                mainMenuController
                    .OnSettingsButton();

                break;


            case MenuViewMode.Quit:

                mainMenuController
                    .OnQuitButton();

                break;
        }
    }


    // =========================================================
    // ZOOM К TV
    // =========================================================

    private IEnumerator ZoomToTV()
    {
        if (menuCamera == null ||
            tvLookTarget == null)
        {
            yield break;
        }


        Transform cameraTransform =
            menuCamera.transform;


        /*
         * POSITION камеры фиксирован
         * на всё время существования меню.
         */
        Vector3 fixedPosition =
            overviewCameraPosition;


        Quaternion startRotation =
            cameraTransform.rotation;


        Quaternion targetRotation =
            GetLookRotationToTV();


        float startFOV =
            menuCamera.fieldOfView;


        float timer = 0f;


        if (tvZoomDuration <= 0f)
        {
            cameraTransform.position =
                fixedPosition;


            cameraTransform.rotation =
                targetRotation;


            menuCamera.fieldOfView =
                tvZoomFOV;


            yield break;
        }


        while (timer <
               tvZoomDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float k =
                Mathf.Clamp01(
                    timer /
                    tvZoomDuration
                );


            /*
             * ТОЧНО такой же Ease Out,
             * как в StartDay.
             */
            float smoothK =
                1f -
                (1f - k) *
                (1f - k);


            /*
             * Position остаётся
             * абсолютно одинаковым.
             */
            cameraTransform.position =
                fixedPosition;


            cameraTransform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    smoothK
                );


            menuCamera.fieldOfView =
                Mathf.Lerp(
                    startFOV,
                    tvZoomFOV,
                    smoothK
                );


            yield return null;
        }


        cameraTransform.position =
            fixedPosition;


        cameraTransform.rotation =
            targetRotation;


        menuCamera.fieldOfView =
            tvZoomFOV;
    }


    // =========================================================
    // ВОЗВРАТ
    // =========================================================

    private IEnumerator ReturnFromTV()
    {
        if (menuCamera == null)
            yield break;


        isTransitioning = true;


        /*
         * Сразу убираем текущую
         * панель телевизора.
         */
        HideCurrentTVPanel();


        /*
         * Стрелочка исчезает параллельно.
         */
        StartCoroutine(
            FadeBackButtonOut()
        );


        /*
         * Возвращаем Rotation + FOV.
         */
        yield return
            ReturnCameraToOverview();


        currentMode =
            MenuViewMode.Overview;


        /*
         * Возвращаем кнопки /
         * будущий пульт.
         */
        yield return
            FadeMainButtons(1f);


        SetMainButtonsInteractable(
            true
        );


        isTransitioning = false;
    }


    private IEnumerator
        ReturnCameraToOverview()
    {
        if (menuCamera == null)
            yield break;


        Transform cameraTransform =
            menuCamera.transform;


        Quaternion startRotation =
            cameraTransform.rotation;


        float startFOV =
            menuCamera.fieldOfView;


        float timer = 0f;


        if (returnDuration <= 0f)
        {
            cameraTransform.position =
                overviewCameraPosition;


            cameraTransform.rotation =
                overviewRotationBeforeZoom;


            menuCamera.fieldOfView =
                overviewCameraFOV;


            yield break;
        }


        while (timer <
               returnDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float k =
                Mathf.Clamp01(
                    timer /
                    returnDuration
                );


            float smoothK =
                1f -
                (1f - k) *
                (1f - k);


            /*
             * Position всё ещё
             * вообще не двигается.
             */
            cameraTransform.position =
                overviewCameraPosition;


            cameraTransform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    overviewRotationBeforeZoom,
                    smoothK
                );


            menuCamera.fieldOfView =
                Mathf.Lerp(
                    startFOV,
                    overviewCameraFOV,
                    smoothK
                );


            yield return null;
        }


        cameraTransform.position =
            overviewCameraPosition;


        cameraTransform.rotation =
            overviewRotationBeforeZoom;


        menuCamera.fieldOfView =
            overviewCameraFOV;
    }


    // =========================================================
    // TV LOOK TARGET
    // =========================================================

    private Quaternion GetLookRotationToTV()
    {
        Vector3 direction =
            tvLookTarget.position -
            overviewCameraPosition;


        if (direction.sqrMagnitude <
            0.0001f)
        {
            return
                menuCamera.transform.rotation;
        }


        return Quaternion.LookRotation(
            direction.normalized,
            Vector3.up
        );
    }


    // =========================================================
    // ИСХОДНАЯ ПОЗА
    // =========================================================

    private void CaptureOverviewCameraPose()
    {
        if (menuCamera == null)
            return;


        overviewCameraPosition =
            menuCamera.transform.position;


        overviewBaseRotation =
            menuCamera.transform.rotation;


        overviewRotationBeforeZoom =
            overviewBaseRotation;


        overviewCameraFOV =
            menuCamera.fieldOfView;


        currentHeadYaw = 0f;
        currentHeadPitch = 0f;
    }


    // =========================================================
    // ГЛАВНЫЕ КНОПКИ
    // =========================================================

    private void StartMainButtonsFade(
        float targetAlpha)
    {
        if (mainButtonsCanvasGroup == null)
            return;


        if (buttonsFadeCoroutine != null)
        {
            StopCoroutine(
                buttonsFadeCoroutine
            );
        }


        buttonsFadeCoroutine =
            StartCoroutine(
                FadeMainButtons(
                    targetAlpha
                )
            );
    }


    private IEnumerator FadeMainButtons(
        float targetAlpha)
    {
        if (mainButtonsCanvasGroup == null)
            yield break;


        float startAlpha =
            mainButtonsCanvasGroup.alpha;


        float elapsed = 0f;


        if (buttonsFadeDuration <= 0f)
        {
            mainButtonsCanvasGroup.alpha =
                targetAlpha;


            buttonsFadeCoroutine = null;


            yield break;
        }


        while (elapsed <
               buttonsFadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    buttonsFadeDuration
                );


            float smoothT =
                t * t *
                (3f - 2f * t);


            mainButtonsCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    smoothT
                );


            yield return null;
        }


        mainButtonsCanvasGroup.alpha =
            targetAlpha;


        buttonsFadeCoroutine = null;
    }


    private void
        SetMainButtonsInteractable(
            bool value)
    {
        if (mainButtonsCanvasGroup == null)
            return;


        mainButtonsCanvasGroup.interactable =
            value;


        mainButtonsCanvasGroup.blocksRaycasts =
            value;
    }


    private void SetMainButtonsImmediate(
        bool visible)
    {
        if (mainButtonsCanvasGroup == null)
            return;


        mainButtonsCanvasGroup.alpha =
            visible ? 1f : 0f;


        mainButtonsCanvasGroup.interactable =
            visible;


        mainButtonsCanvasGroup.blocksRaycasts =
            visible;
    }


    // =========================================================
    // ПАНЕЛИ TV
    // =========================================================

    private void HideCurrentTVPanel()
    {
        /*
         * У панели выхода уже есть
         * отдельный нормальный CancelQuit(),
         * который НЕ проигрывает звук
         * кнопки выхода повторно.
         */
        if (currentMode ==
            MenuViewMode.Quit)
        {
            if (mainMenuController != null)
            {
                mainMenuController
                    .CancelQuit();
            }

            return;
        }


        GameObject panel = null;


        if (currentMode ==
            MenuViewMode.Load)
        {
            panel = loadPanel;
        }
        else if (currentMode ==
                 MenuViewMode.Settings)
        {
            panel = settingsPanel;
        }


        if (panel == null)
            return;


        CanvasGroup canvasGroup =
            panel.GetComponent<CanvasGroup>();


        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }


        panel.SetActive(false);
    }


    // =========================================================
    // НАЗАД
    // =========================================================

    private IEnumerator
        FadeBackButtonIn()
    {
        if (backButton == null)
            yield break;


        backButton.SetActive(true);


        if (backButtonCanvasGroup == null)
            yield break;


        backButtonCanvasGroup.alpha = 0f;
        backButtonCanvasGroup.interactable = false;
        backButtonCanvasGroup.blocksRaycasts = false;


        yield return
            FadeBackButtonCanvasGroup(
                0f,
                1f
            );


        backButtonCanvasGroup.alpha = 1f;
        backButtonCanvasGroup.interactable = true;
        backButtonCanvasGroup.blocksRaycasts = true;
    }


    private IEnumerator
        FadeBackButtonOut()
    {
        if (backButton == null)
            yield break;


        if (backButtonCanvasGroup == null)
        {
            backButton.SetActive(false);
            yield break;
        }


        backButtonCanvasGroup.interactable = false;
        backButtonCanvasGroup.blocksRaycasts = false;


        float startAlpha =
            backButtonCanvasGroup.alpha;


        yield return
            FadeBackButtonCanvasGroup(
                startAlpha,
                0f
            );


        backButtonCanvasGroup.alpha = 0f;

        backButton.SetActive(false);
    }


    private IEnumerator
        FadeBackButtonCanvasGroup(
            float from,
            float to)
    {
        if (backButtonCanvasGroup == null)
            yield break;


        if (backButtonFadeDuration <= 0f)
        {
            backButtonCanvasGroup.alpha =
                to;

            yield break;
        }


        float elapsed = 0f;


        while (elapsed <
               backButtonFadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    backButtonFadeDuration
                );


            float smoothT =
                t * t *
                (3f - 2f * t);


            backButtonCanvasGroup.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    smoothT
                );


            yield return null;
        }


        backButtonCanvasGroup.alpha =
            to;
    }


    private void HideBackButtonImmediate()
    {
        if (backButton == null)
            return;


        if (backButtonCanvasGroup != null)
        {
            backButtonCanvasGroup.alpha = 0f;
            backButtonCanvasGroup.interactable = false;
            backButtonCanvasGroup.blocksRaycasts = false;
        }


        backButton.SetActive(false);
    }


    // =========================================================
    // CURSOR
    // =========================================================

    private IEnumerator
        RegisterCursorEventsWhenReady()
    {
        while (PauseManager.Instance == null)
            yield return null;


        if (mainButtonsCanvasGroup != null)
        {
            PauseManager.Instance
                .AddCursorEventsToButtons(
                    mainButtonsCanvasGroup.transform
                );
        }


        if (loadPanel != null)
        {
            PauseManager.Instance
                .AddCursorEventsToButtons(
                    loadPanel.transform
                );
        }


        if (settingsPanel != null)
        {
            PauseManager.Instance
                .AddCursorEventsToButtons(
                    settingsPanel.transform
                );
        }


        if (quitPanel != null)
        {
            PauseManager.Instance
                .AddCursorEventsToButtons(
                    quitPanel.transform
                );
        }


        if (backButton != null)
        {
            PauseManager.Instance
                .AddCursorEventsToButtons(
                    backButton.transform
                );
        }
    }
}
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Panels")]
    public RectTransform leftPanel;
    public CanvasGroup savePanelCG;
    public CanvasGroup downloadPanelCG;
    public CanvasGroup settingsPanelCG;

    [Header("Blur")]
    public Volume blurVolume;
    public float blurFadeDuration = 0.5f;

    [Header("Animation")]
    public float slideDuration = 0.3f;
    public float leftStartX = 0f;
    public float leftTargetX = -382f;

    [Header("Work HUD")]
    public WorkHUDManager workHUDManager;

    [Header("Pause Panel Fade")]
    public CanvasGroup leftPanelCG;
    public float pausePanelFadeDuration = 0.3f;

    private Coroutine pausePanelFadeCoroutine;

    [Header("Key")]
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("Custom Cursor")]
    public Texture2D defaultCursor;
    public Texture2D interactCursor;
    public Vector2 defaultCursorHotspot = Vector2.zero;
    public Vector2 interactCursorHotspot = Vector2.zero;

    [Header("Player Auto Find")]
    public bool autoFindReferences = true;
    public string playerObjectName = "Player";

    private PlayerController playerController;
    private bool isPaused = false;
    private bool isTransitioning = false;
    private CanvasGroup currentRightPanel = null;

    public bool IsPaused => isPaused;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        FindReferences();
        FindLeftPanelCanvasGroup();

        leftPanel.gameObject.SetActive(false);

        if (leftPanelCG != null)
        {
            leftPanelCG.alpha = 0f;
            leftPanelCG.interactable = false;
            leftPanelCG.blocksRaycasts = false;
        }

        SetPanelActive(savePanelCG, false);
        SetPanelActive(downloadPanelCG, false);
        SetPanelActive(settingsPanelCG, false);

        if (blurVolume != null)
        {
            blurVolume.weight = 0f;
            blurVolume.gameObject.SetActive(true);
            blurVolume.enabled = true;
        }

        leftPanel.anchoredPosition = new Vector2(leftStartX, leftPanel.anchoredPosition.y);
    }

    void Update()
    {
        /*если игрок сидит в рабочем режиме, курсор нельзя блокировать в центре*/
        if (DialogueManager.AnyDialogueActive)
        {
            bool playerIsSeated =
                WorkSessionManager.Instance != null &&
                WorkSessionManager.Instance.IsSeated;

            if (!playerIsSeated)
                ForceGameplayCursorLocked();
            return;
        }

        // Вариативный диалог блокирует паузу,
        // но рабочий курсор остаётся доступным.
        if (ClientQuestionDialogueController
            .AnyQuestionDialogueOpen)
        {
            return;
        }


        if (IsPauseBlocked())
        {
            ForceGameplayCursorLocked();

            if (Input.GetKeyDown(pauseKey))
                StartCoroutine(ForceGameplayCursorLockedNextFrame());

            return;
        }

        // Если панель задач открыта – игнорируем Esc
        if (TaskPanelController.Instance != null && TaskPanelController.Instance.IsPanelOpen)
            return;

        if (Input.GetKeyDown(pauseKey) && !isTransitioning)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (DialogueManager.AnyDialogueActive)
        {
            return;
        }

        if (ClientQuestionDialogueController
        .AnyQuestionDialogueOpen)
        {
            return;
        }

        if (IsPauseBlocked())
        {
            ForceGameplayCursorLocked();
            StartCoroutine(ForceGameplayCursorLockedNextFrame());
            return;
        }

        FindReferences();
        FindLeftPanelCanvasGroup();

        if (isPaused) return;
        InteractionOutlineAutoHider.SetForceVisible(false);

        isPaused = true;
        isTransitioning = true;

        if (workHUDManager != null)
            workHUDManager.SetPauseBlocked(true);

        Time.timeScale = 0f;

        if (playerController != null)
            playerController.canControl = false;

        // ---- Мгновенно включаем блюр ----
        if (blurVolume != null)
        {
            blurVolume.weight = 1f;
        }

        // ---- Показываем левую панель через fade ----
        leftPanel.gameObject.SetActive(true);
        leftPanel.anchoredPosition = new Vector2(leftStartX, leftPanel.anchoredPosition.y);

        HideRightPanelInstantly();

        if (leftPanelCG != null)
        {
            leftPanelCG.alpha = 0f;
            leftPanelCG.interactable = false;
            leftPanelCG.blocksRaycasts = false;
        }

        IndicatorHover.ResetSelection();

        AddCursorEventsToAllButtons();

        CursorCenterHelper.ShowCursorCentered(this, defaultCursor, defaultCursorHotspot);

        if (pausePanelFadeCoroutine != null)
            StopCoroutine(pausePanelFadeCoroutine);

        pausePanelFadeCoroutine = StartCoroutine(FadePausePanelIn());
    }

    public void ResumeGame()
    {
        FindReferences();
        FindLeftPanelCanvasGroup();

        if (!isPaused) return;

        if (workHUDManager != null)
            workHUDManager.SetPauseBlocked(false);

        InteractionOutlineAutoHider.SetForceVisible(true);

        if (pausePanelFadeCoroutine != null)
            StopCoroutine(pausePanelFadeCoroutine);

        pausePanelFadeCoroutine = StartCoroutine(FadePausePanelOutAndResume());
    }

    public void HidePauseMenuBeforeLoading()
    {
        FindReferences();
        FindLeftPanelCanvasGroup();

        if (pausePanelFadeCoroutine != null)
        {
            StopCoroutine(pausePanelFadeCoroutine);
            pausePanelFadeCoroutine = null;
        }

        isPaused = false;
        isTransitioning = false;

        Time.timeScale = 1f;

        if (playerController != null)
            playerController.canControl = false;

        if (leftPanel != null)
            leftPanel.gameObject.SetActive(false);

        if (leftPanelCG != null)
        {
            leftPanelCG.alpha = 0f;
            leftPanelCG.interactable = false;
            leftPanelCG.blocksRaycasts = false;
        }

        SetPanelActive(savePanelCG, false);
        SetPanelActive(downloadPanelCG, false);
        SetPanelActive(settingsPanelCG, false);

        currentRightPanel = null;

        if (leftPanel != null)
            leftPanel.anchoredPosition = new Vector2(leftStartX, leftPanel.anchoredPosition.y);

        if (blurVolume != null)
            blurVolume.weight = 0f;

        Cursor.SetCursor(defaultCursor, defaultCursorHotspot, CursorMode.ForceSoftware);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void EnableGameplayAfterLoading()
    {
        isPaused = false;
        isTransitioning = false;

        Time.timeScale = 1f;

        FindReferences();
        FindLeftPanelCanvasGroup();

        if (pausePanelFadeCoroutine != null)
        {
            StopCoroutine(pausePanelFadeCoroutine);
            pausePanelFadeCoroutine = null;
        }

        if (playerController != null)
            playerController.canControl = true;

        if (leftPanel != null)
            leftPanel.gameObject.SetActive(false);

        if (leftPanelCG != null)
        {
            leftPanelCG.alpha = 0f;
            leftPanelCG.interactable = false;
            leftPanelCG.blocksRaycasts = false;
        }

        SetPanelActive(savePanelCG, false);
        SetPanelActive(downloadPanelCG, false);
        SetPanelActive(settingsPanelCG, false);

        currentRightPanel = null;

        if (leftPanel != null)
            leftPanel.anchoredPosition = new Vector2(leftStartX, leftPanel.anchoredPosition.y);

        if (blurVolume != null)
            blurVolume.weight = 0f;

        Cursor.SetCursor(defaultCursor, defaultCursorHotspot, CursorMode.ForceSoftware);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ----- Открытие правых панелей (из кнопок) -----

    public void ShowSavePanel()
    {
        if (savePanelCG != null)
        {
            SavePanelController controller = savePanelCG.GetComponent<SavePanelController>();

            if (controller == null)
                controller = savePanelCG.GetComponentInChildren<SavePanelController>(true);

            if (controller != null)
                controller.PrepareSavePanel();
        }

        ShowRightPanel(savePanelCG);
    }

    public void ShowDownloadPanel()
    {
        if (downloadPanelCG != null)
        {
            LoadPanelController controller = downloadPanelCG.GetComponent<LoadPanelController>();

            if (controller == null)
                controller = downloadPanelCG.GetComponentInChildren<LoadPanelController>(true);

            if (controller != null)
                controller.PrepareLoadPanel();
        }

        ShowRightPanel(downloadPanelCG);
    }

    public void ShowSettingsPanel() => ShowRightPanel(settingsPanelCG);

    private void ShowRightPanel(CanvasGroup panelCG)
    {
        if (isTransitioning) return;
        if (panelCG == null) return;

        if (currentRightPanel == panelCG && panelCG.gameObject.activeSelf)
        {
            HideRightPanel();
            return;
        }

        if (currentRightPanel != null)
        {
            currentRightPanel.gameObject.SetActive(false);
            currentRightPanel.alpha = 0f;
            currentRightPanel = null;
        }

        StartCoroutine(SlideAndShow(panelCG));
    }

    public void HideRightPanel()
    {
        if (isTransitioning) return;
        if (currentRightPanel == null) return;
        StartCoroutine(SlideAndHide());
    }

    // ----- Корутины анимации -----

    private IEnumerator SlideAndShow(CanvasGroup panelCG)
    {
        isTransitioning = true;
        currentRightPanel = panelCG;

        panelCG.gameObject.SetActive(true);
        panelCG.alpha = 0f;
        panelCG.interactable = false;
        panelCG.blocksRaycasts = false;

        Vector2 startLeftPos = leftPanel.anchoredPosition;
        Vector2 targetLeftPos = new Vector2(leftTargetX, startLeftPos.y);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float ease = t * t * (3f - 2f * t);

            leftPanel.anchoredPosition = Vector2.Lerp(startLeftPos, targetLeftPos, ease);
            panelCG.alpha = ease;

            yield return null;
        }

        leftPanel.anchoredPosition = targetLeftPos;
        panelCG.alpha = 1f;
        panelCG.interactable = true;
        panelCG.blocksRaycasts = true;
        isTransitioning = false;
    }

    private IEnumerator SlideAndHide()
    {
        isTransitioning = true;
        CanvasGroup panelCG = currentRightPanel;
        if (panelCG == null) { isTransitioning = false; yield break; }

        panelCG.interactable = false;
        panelCG.blocksRaycasts = false;

        Vector2 startLeftPos = leftPanel.anchoredPosition;
        Vector2 targetLeftPos = new Vector2(leftStartX, startLeftPos.y);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float ease = t * t * (3f - 2f * t);

            leftPanel.anchoredPosition = Vector2.Lerp(startLeftPos, targetLeftPos, ease);
            panelCG.alpha = 1f - ease;

            yield return null;
        }

        leftPanel.anchoredPosition = targetLeftPos;
        panelCG.alpha = 0f;
        panelCG.gameObject.SetActive(false);

        panelCG.interactable = false;
        panelCG.blocksRaycasts = false;

        currentRightPanel = null;
        isTransitioning = false;
    }

    private IEnumerator FadePausePanelIn()
    {
        float elapsed = 0f;

        if (leftPanelCG == null)
        {
            isTransitioning = false;
            yield break;
        }

        while (elapsed < pausePanelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / pausePanelFadeDuration);
            float ease = t * t * (3f - 2f * t);

            leftPanelCG.alpha = ease;

            yield return null;
        }

        leftPanelCG.alpha = 1f;
        leftPanelCG.interactable = true;
        leftPanelCG.blocksRaycasts = true;

        isTransitioning = false;
        pausePanelFadeCoroutine = null;
    }

    private IEnumerator FadePausePanelOutAndResume()
    {
        isTransitioning = true;

        if (WorkSessionManager.Instance == null ||
            !WorkSessionManager.Instance.IsWorkModeActive)
        {
            Cursor.SetCursor(
                defaultCursor,
                defaultCursorHotspot,
                CursorMode.ForceSoftware
            );

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (leftPanelCG == null)
        {
            FinishResumeInstantly();
            yield break;
        }

        leftPanelCG.interactable = false;
        leftPanelCG.blocksRaycasts = false;

        // Запоминаем правую панель, если она была открыта.
        CanvasGroup rightPanelToFade = currentRightPanel;

        if (rightPanelToFade != null)
        {
            rightPanelToFade.interactable = false;
            rightPanelToFade.blocksRaycasts = false;
        }

        // Плавно скрываем блюр.
        if (blurVolume != null)
            StartCoroutine(FadeBlur(blurVolume.weight, 0f, blurFadeDuration));

        float startLeftAlpha = leftPanelCG.alpha;
        float startRightAlpha = rightPanelToFade != null ? rightPanelToFade.alpha : 0f;

        float elapsed = 0f;

        while (elapsed < pausePanelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / pausePanelFadeDuration);
            float ease = t * t * (3f - 2f * t);

            leftPanelCG.alpha = Mathf.Lerp(startLeftAlpha, 0f, ease);

            if (rightPanelToFade != null)
                rightPanelToFade.alpha = Mathf.Lerp(startRightAlpha, 0f, ease);

            yield return null;
        }

        leftPanelCG.alpha = 0f;

        if (rightPanelToFade != null)
        {
            rightPanelToFade.alpha = 0f;
            rightPanelToFade.gameObject.SetActive(false);
        }

        currentRightPanel = null;

        if (leftPanel != null)
        {
            leftPanel.gameObject.SetActive(false);

            // ВАЖНО:
            // возвращаем в центр только ПОСЛЕ скрытия,
            // поэтому игрок не увидит обратный сдвиг.
            leftPanel.anchoredPosition = new Vector2(leftStartX, leftPanel.anchoredPosition.y);
        }

        FinishResumeInstantly();
    }

    private void FinishResumeInstantly()
    {
        isPaused = false;
        isTransitioning = false;

        Time.timeScale = 1f;

        if (playerController != null)
            playerController.canControl = true;

        if (WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsWorkModeActive)
        {
            WorkSessionManager.Instance.RestoreAfterPause();
        }

        pausePanelFadeCoroutine = null;
        InteractionOutlineAutoHider.SetForceVisible(false);
    }

    private void FindLeftPanelCanvasGroup()
    {
        if (leftPanelCG != null)
            return;

        if (leftPanel != null)
            leftPanelCG = leftPanel.GetComponent<CanvasGroup>();
    }

    private void HideRightPanelInstantly(bool resetLeftPanelPosition = true)
    {
        SetPanelActive(savePanelCG, false);
        SetPanelActive(downloadPanelCG, false);
        SetPanelActive(settingsPanelCG, false);

        currentRightPanel = null;

        if (resetLeftPanelPosition && leftPanel != null)
            leftPanel.anchoredPosition = new Vector2(leftStartX, leftPanel.anchoredPosition.y);
    }

    private void SetPanelActive(CanvasGroup cg, bool active)
    {
        if (cg == null)
            return;

        cg.gameObject.SetActive(active);
        cg.alpha = active ? 1f : 0f;
        cg.interactable = active;
        cg.blocksRaycasts = active;
    }

    private IEnumerator FadeBlur(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            blurVolume.weight = Mathf.Lerp(from, to, t);
            yield return null;
        }
        blurVolume.weight = to;
    }

    // ----- Управление кастомным курсором -----

    private void SetDefaultCursor()
    {
        Cursor.SetCursor(defaultCursor, defaultCursorHotspot, CursorMode.ForceSoftware);
    }

    private void SetInteractCursor()
    {
        Cursor.SetCursor(interactCursor, interactCursorHotspot, CursorMode.ForceSoftware);
    }

    private void AddCursorEventsToAllButtons()
    {
        // Находим все кнопки на левой панели и на правых панелях (если активны)
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            // Добавляем EventTrigger, если его нет
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = btn.gameObject.AddComponent<EventTrigger>();

            // Проверяем, есть ли уже событие PointerEnter, чтобы не дублировать
            bool hasEnter = false;
            bool hasExit = false;
            foreach (var entry in trigger.triggers)
            {
                if (entry.eventID == EventTriggerType.PointerEnter) hasEnter = true;
                if (entry.eventID == EventTriggerType.PointerExit) hasExit = true;
            }

            if (!hasEnter)
            {
                var entryEnter = new EventTrigger.Entry();
                entryEnter.eventID = EventTriggerType.PointerEnter;
                entryEnter.callback.AddListener((data) => { SetInteractCursor(); });
                trigger.triggers.Add(entryEnter);
            }

            if (!hasExit)
            {
                var entryExit = new EventTrigger.Entry();
                entryExit.eventID = EventTriggerType.PointerExit;
                entryExit.callback.AddListener((data) => { SetDefaultCursor(); });
                trigger.triggers.Add(entryExit);
            }
        }
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (playerController == null)
        {
            GameObject playerObj = GameObject.Find(playerObjectName);

            if (playerObj != null)
                playerController = playerObj.GetComponent<PlayerController>();
        }

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (workHUDManager == null)
        {
            workHUDManager =
                FindFirstObjectByType<WorkHUDManager>(
                    FindObjectsInactive.Include
                );
        }
    }

    private bool IsPauseBlocked()
    {
        if (StartDay.IntroBlocksPause)
            return true;

        if (NewsDialogue.NewsBlocksPause)
            return true;

        if (LoadingManager.IsLoadingScreenBlockingPause())
            return true;

        return false;
    }

    private void ForceGameplayCursorLocked()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private IEnumerator ForceGameplayCursorLockedNextFrame()
    {
        yield return null;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        yield return null;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
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

    [Header("Key")]
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("Custom Cursor")]
    public Texture2D defaultCursor;
    public Texture2D interactCursor;
    public Vector2 defaultCursorHotspot = Vector2.zero;
    public Vector2 interactCursorHotspot = Vector2.zero;

    private bool isPaused = false;
    private bool isTransitioning = false;
    private CanvasGroup currentRightPanel = null;
    private PlayerController playerController;

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
        playerController = FindObjectOfType<PlayerController>();

        leftPanel.gameObject.SetActive(false);
        SetPanelActive(savePanelCG, false);
        SetPanelActive(downloadPanelCG, false);
        SetPanelActive(settingsPanelCG, false);

        if (blurVolume != null)
        {
            blurVolume.weight = 0f;
            blurVolume.gameObject.SetActive(true); // всегда активен
            blurVolume.enabled = true;
        }

        leftPanel.anchoredPosition = new Vector2(leftStartX, leftPanel.anchoredPosition.y);
    }

    void Update()
    {
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
        if (isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        if (playerController != null) playerController.canControl = false;

        // ---- Мгновенно включаем блюр ----
        if (blurVolume != null)
        {
            blurVolume.weight = 1f;
        }

        // ---- Показываем курсор ----
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetDefaultCursor();

        // ---- Показываем левую панель ----
        leftPanel.gameObject.SetActive(true);
        leftPanel.anchoredPosition = new Vector2(leftStartX, leftPanel.anchoredPosition.y);
        HideRightPanelInstantly();

        // ---- СБРАСЫВАЕМ ВЫДЕЛЕННЫЙ ИНДИКАТОР ПРИ ОТКРЫТИИ ПАУЗЫ ----
        IndicatorHover.ResetSelection();

        // ---- Навешиваем события курсора на все кнопки ----
        AddCursorEventsToAllButtons();
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        if (playerController != null) playerController.canControl = true;

        // ---- Плавно скрываем блюр ----
        if (blurVolume != null)
            StartCoroutine(FadeBlur(blurVolume.weight, 0f, blurFadeDuration));

        // ---- Скрываем панели ----
        leftPanel.gameObject.SetActive(false);
        HideRightPanelInstantly();

        // ---- Скрываем курсор ----
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        isTransitioning = false;
    }

    private IEnumerator SlideAndHide()
    {
        isTransitioning = true;
        CanvasGroup panelCG = currentRightPanel;
        if (panelCG == null) { isTransitioning = false; yield break; }

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
        currentRightPanel = null;
        isTransitioning = false;
    }

    private void HideRightPanelInstantly()
    {
        SetPanelActive(savePanelCG, false);
        SetPanelActive(downloadPanelCG, false);
        SetPanelActive(settingsPanelCG, false);
        currentRightPanel = null;
        leftPanel.anchoredPosition = new Vector2(leftStartX, leftPanel.anchoredPosition.y);
    }

    private void SetPanelActive(CanvasGroup cg, bool active)
    {
        if (cg == null) return;
        cg.gameObject.SetActive(active);
        cg.alpha = active ? 1f : 0f;
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
}
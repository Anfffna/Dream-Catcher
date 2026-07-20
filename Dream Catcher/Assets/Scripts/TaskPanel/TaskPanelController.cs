using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class TaskPanelController : MonoBehaviour
{
    public static TaskPanelController Instance { get; private set; }

    [Header("Panel")]
    public GameObject taskPanel;
    public CanvasGroup taskPanelCanvasGroup;

    [Header("Fade")]
    public float fadeDuration = 0.25f;

    [Header("Blur / Post Process")]
    public Volume blurVolume;
    [Range(0f, 1f)] public float blurOpenWeight = 1f;
    public bool disableBlurWhenClosed = true;

    [Header("Task Update Toast")]
    public TaskUpdateToast taskUpdateToast;

    [Header("UI Blockers")]
    public CanvasGroup[] blockingCanvasGroups;
    public GameObject[] blockingObjects;

    [Header("Player")]
    public PlayerController playerController;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string playerObjectName = "Player";

    [Header("Custom Cursors")]
    public Texture2D defaultCursor;
    public Texture2D interactCursor;

    [Header("Cursor Hotspot")]
    public Vector2 defaultCursorHotspot = Vector2.zero;
    public Vector2 interactCursorHotspot = Vector2.zero;

    public bool IsPanelOpen => isPanelOpen;

    private bool isPanelOpen = false;
    private bool cursorIsDefault = false;
    private bool cursorIsInteract = false;

    private Coroutine fadeCoroutine;

    [Header("Unlock")]
    public bool panelUnlocked = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (blurVolume == null)
            blurVolume = FindObjectOfType<Volume>();
    }

    void Start()
    {
        if (taskPanel != null)
            taskPanel.SetActive(true);

        if (taskPanelCanvasGroup == null && taskPanel != null)
            taskPanelCanvasGroup = taskPanel.GetComponent<CanvasGroup>();

        if (taskPanelCanvasGroup == null && taskPanel != null)
            taskPanelCanvasGroup = taskPanel.AddComponent<CanvasGroup>();

        if (blurVolume != null)
        {
            blurVolume.weight = 0f;
            blurVolume.gameObject.SetActive(true); // ‚ÒÂ„‰‡ ‡ÍÚË‚ÂÌ
        }

        ClosePanelInstant();
    }

    void Update()
    {
        // ≈ÒÎË Ô‡ÛÁ‡ ‡ÍÚË‚Ì‡ ñ Ë„ÌÓËÛÂÏ Tab
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        if (panelUnlocked && Input.GetKeyDown(KeyCode.Tab))
        {
            if (isPanelOpen)
            {
                ClosePanel();
                return;
            }

            if (IsOtherUIBlockingTaskPanel())
                return;

            if (taskUpdateToast != null && taskUpdateToast.IsShowing)
                taskUpdateToast.HideToastNow();

            OpenPanel();
        }

        if (WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsWorkModeActive)
        {
            return;
        }
    }

    bool IsOtherUIBlockingTaskPanel()
    {
        if (blockingCanvasGroups != null)
        {
            for (int i = 0; i < blockingCanvasGroups.Length; i++)
            {
                CanvasGroup group = blockingCanvasGroups[i];

                if (group == null) continue;

                if (group.gameObject == taskPanel) continue;

                if (group.gameObject.activeInHierarchy && group.alpha > 0.01f)
                    return true;
            }
        }

        if (blockingObjects != null)
        {
            for (int i = 0; i < blockingObjects.Length; i++)
            {
                GameObject obj = blockingObjects[i];

                if (obj == null) continue;

                if (obj == taskPanel) continue;

                if (taskUpdateToast != null && obj == taskUpdateToast.gameObject)
                    continue;

                if (obj.activeInHierarchy)
                    return true;
            }
        }

        return false;
    }

    public void ResetForNewGame()
    {
        panelUnlocked = false;
        isPanelOpen = false;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (taskPanel != null)
            taskPanel.SetActive(true);

        if (taskPanelCanvasGroup == null && taskPanel != null)
            taskPanelCanvasGroup = taskPanel.GetComponent<CanvasGroup>();

        if (taskPanelCanvasGroup != null)
        {
            taskPanelCanvasGroup.alpha = 0f;
            taskPanelCanvasGroup.interactable = false;
            taskPanelCanvasGroup.blocksRaycasts = false;
        }

        if (blurVolume != null)
            blurVolume.weight = 0f;

        cursorIsDefault = false;
        cursorIsInteract = false;
    }

    public void OpenPanel()
    {
        FindReferences();

        isPanelOpen = true;

        if (taskPanel != null)
            taskPanel.SetActive(true);

        if (taskPanelCanvasGroup != null)
        {
            taskPanelCanvasGroup.interactable = true;
            taskPanelCanvasGroup.blocksRaycasts = true;
        }

        cursorIsDefault = true;
        cursorIsInteract = false;

        if (playerController != null)
            playerController.canControl = false;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadePanelAndBlur(1f, blurOpenWeight));

        CursorCenterHelper.ShowCursorCentered(this, defaultCursor, defaultCursorHotspot);
    }

    public void ClosePanel()
    {
        FindReferences();

        isPanelOpen = false;

        if (taskPanelCanvasGroup != null)
        {
            taskPanelCanvasGroup.interactable = false;
            taskPanelCanvasGroup.blocksRaycasts = false;
        }

        // --- œ–¿¬»À‹Õ€… œŒ–ﬂƒŒ  — –€“»ﬂ  ”–—Œ–¿ ---
        Cursor.SetCursor(defaultCursor, defaultCursorHotspot, CursorMode.ForceSoftware);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        // ------------------------------------------

        cursorIsDefault = false;
        cursorIsInteract = false;

        if (playerController != null &&
            (PauseManager.Instance == null || !PauseManager.Instance.IsPaused))
        {
            playerController.canControl = true;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadePanelAndBlur(0f, 0f));
    }

    private void ClosePanelInstant()
    {
        isPanelOpen = false;

        if (taskPanelCanvasGroup != null)
        {
            taskPanelCanvasGroup.alpha = 0f;
            taskPanelCanvasGroup.interactable = false;
            taskPanelCanvasGroup.blocksRaycasts = false;
        }

        Cursor.SetCursor(defaultCursor, defaultCursorHotspot, CursorMode.ForceSoftware);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        cursorIsDefault = false;
        cursorIsInteract = false;

        if (playerController != null)
            playerController.canControl = true;
    }

    private IEnumerator FadePanelAndBlur(float targetPanelAlpha, float targetBlurWeight)
    {
        float startPanelAlpha = taskPanelCanvasGroup != null ? taskPanelCanvasGroup.alpha : 0f;

        float startBlurWeight = 0f;

        if (blurVolume != null)
        {
            startBlurWeight = blurVolume.weight;
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = fadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(timer / fadeDuration);

            float smoothT = t * t * (3f - 2f * t);

            if (taskPanelCanvasGroup != null)
                taskPanelCanvasGroup.alpha = Mathf.Lerp(startPanelAlpha, targetPanelAlpha, smoothT);

            if (blurVolume != null)
                blurVolume.weight = Mathf.Lerp(startBlurWeight, targetBlurWeight, smoothT);

            yield return null;
        }

        if (taskPanelCanvasGroup != null)
            taskPanelCanvasGroup.alpha = targetPanelAlpha;

        if (blurVolume != null)
        {
            blurVolume.weight = targetBlurWeight;
        }

        fadeCoroutine = null;
    }

    public void SetDefaultCursor()
    {
        if (!isPanelOpen) return;
        if (cursorIsDefault) return;

        Cursor.SetCursor(defaultCursor, defaultCursorHotspot, CursorMode.ForceSoftware);

        cursorIsDefault = true;
        cursorIsInteract = false;
    }

    public void SetInteractCursor()
    {
        if (!isPanelOpen) return;
        if (cursorIsInteract) return;

        Cursor.SetCursor(interactCursor, interactCursorHotspot, CursorMode.ForceSoftware);

        cursorIsInteract = true;
        cursorIsDefault = false;
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
    }
}
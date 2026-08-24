using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance
    {
        get;
        private set;
    }

    public static bool LoadingBlocksPause
    {
        get;
        private set;
    }

    [Header("Случайные варианты загрузки")]

    [Tooltip("Все варианты фона загрузки. " + "При каждой загрузке будет выбран один случайный.")]
    [SerializeField]
    private GameObject[] loadingBackgroundVariants;

    [Tooltip("Все варианты изображения загрузки. " + "При каждой загрузке будет выбран один случайный.")]
    [SerializeField]
    private GameObject[] loadingImageVariants;

    [Header("Цветной экран загрузки")]
    [Tooltip("Общий родитель цветного интерфейса загрузки.")]
    [SerializeField] private GameObject loadingRoot;

    [Tooltip("Canvas Group общего родителя загрузки.")]
    [SerializeField] private CanvasGroup loadingRootCanvasGroup;

    [Tooltip("Цветной фон загрузки.")]
    [SerializeField] private GameObject loadingBackground;

    [Tooltip("Canvas Group цветного фона.")]
    [SerializeField] private CanvasGroup loadingBackgroundCanvasGroup;

    [Tooltip("Цветное изображение поверх фона.")]
    [SerializeField] private GameObject loadingImage;

    [Tooltip("Canvas Group цветного изображения.")]
    [SerializeField] private CanvasGroup loadingImageCanvasGroup;

    [Header("Индикатор загрузки")]
    [SerializeField] private LoadingSpinnerController loadingSpinner;

    [Header("Плавность")]
    [Tooltip("Время появления и исчезновения каждого слоя.")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Игрок")]
    [SerializeField] private PlayerController playerController;

    [Header("Автоматический поиск")]
    [SerializeField] private bool autoFindReferences = true;

    [SerializeField] private string playerObjectName = "Player";

    private Coroutine loadingCoroutine;
    private bool isLoading;

    public bool IsLoading => isLoading;

    public bool IsLoadingBackgroundReady
    {
        get
        {
            return
                isLoading &&
                loadingBackgroundCanvasGroup != null &&
                loadingBackgroundCanvasGroup.alpha >= 0.999f;
        }
    }

    public static bool IsLoadingScreenBlockingPause()
    {
        if (LoadingBlocksPause)
            return true;

        if (Instance == null)
            return false;

        if (Instance.isLoading)
            return true;

        if (Instance.loadingRootCanvasGroup != null &&
            Instance.loadingRootCanvasGroup.alpha > 0.001f &&
            Instance.loadingRootCanvasGroup.blocksRaycasts)
        {
            return true;
        }

        return false;
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        FindReferences();
        HideLoadingVisualsInstantly();
    }

    public void StartLoading(
        string sceneName)
    {
        StartLoadingInternal(
            sceneName,
            null,
            null,
            0f
        );
    }

    public void StartLoading(
        string sceneName,
        DialogueManager dialogueManager,
        List<DialogueManager.DialogueLine>
            dialogueLines,
        float showImageDelay = 1f)
    {
        StartLoadingInternal(
            sceneName,
            dialogueManager,
            dialogueLines,
            showImageDelay
        );
    }

    private void StartLoadingInternal(
        string sceneName,
        DialogueManager dialogueManager,
        List<DialogueManager.DialogueLine>
            dialogueLines,
        float showImageDelay)
    {
        if (isLoading ||
            string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        if (PauseManager.Instance != null)
            PauseManager.Instance.SetCursorBlocked(true);

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }

        loadingCoroutine =
            StartCoroutine(
                LoadingSequence(
                    sceneName,
                    dialogueManager,
                    dialogueLines,
                    showImageDelay
                )
            );
    }

    private IEnumerator LoadingSequence(
    string sceneName,
    DialogueManager dialogueManager,
    List<DialogueManager.DialogueLine>
        dialogueLines,
    float showImageDelay)
    {
        isLoading = true;
        LoadingBlocksPause = true;

        FindReferences();
        SelectRandomLoadingVisuals();
        PrepareLoadingVisuals();

        // Сначала плавно показываем цветной фон.
        yield return StartCoroutine(
            FadeIn(
                loadingBackgroundCanvasGroup
            )
        );

        // Затем плавно показываем цветную картинку.
        yield return StartCoroutine(
            FadeIn(
                loadingImageCanvasGroup
            )
        );

        DisablePlayerFootsteps();

        AsyncOperation asyncLoad =
            SceneManager.LoadSceneAsync(
                sceneName
            );

        if (asyncLoad == null)
        {
            FinishLoadingImmediately();
            yield break;
        }

        asyncLoad.allowSceneActivation = false;

        if (showImageDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    showImageDelay
                );
        }

        bool hasLoadingDialogue =
            dialogueManager != null &&
            dialogueLines != null &&
            dialogueLines.Count > 0;

        if (hasLoadingDialogue)
        {
            dialogueManager.StartDialogue(
                dialogueLines
            );

            yield return new WaitUntil(
                () =>
                    dialogueManager == null ||
                    !dialogueManager.DialogueActive
            );
        }

        if (loadingSpinner != null)
            loadingSpinner.Show();

        while (asyncLoad.progress < 0.9f)
            yield return null;

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
            yield return null;

        // Если это загрузка сохранения — ждём полного восстановления.
        while (SaveManager.Instance != null &&
               SaveManager.Instance.IsLoadingSave)
        {
            yield return null;
        }

        // Новая сцена уже активна, но loading screen всё ещё полностью виден.
        // Даём Awake / Start / камере / игроку закончить инициализацию.
        yield return null;
        yield return new WaitForEndOfFrame();
        yield return null;

        FindReferences();

        if (loadingSpinner != null)
            loadingSpinner.HideSmooth();

        // Только теперь плавно открываем уже загруженную сцену.
        yield return StartCoroutine(
            FadeOut(loadingRootCanvasGroup)
        );

        HideLoadingVisualsInstantly();
        EnablePlayerFootsteps();

        // Loading уже невидим, но ещё один кадр
        // не разрешаем другим системам показывать курсор.
        yield return null;

        LoadingBlocksPause = false;
        isLoading = false;
        loadingCoroutine = null;

        if (PauseManager.Instance != null)
            PauseManager.Instance.SetCursorBlocked(false);
    }

    private void SelectRandomLoadingVisuals()
    {
        SelectRandomVariant(
            loadingBackgroundVariants
        );

        SelectRandomVariant(
            loadingImageVariants
        );
    }

    private void SelectRandomVariant(
        GameObject[] variants)
    {
        if (variants == null ||
            variants.Length == 0)
        {
            return;
        }

        // Сначала выключаем все варианты.
        for (int i = 0;
             i < variants.Length;
             i++)
        {
            if (variants[i] != null)
            {
                variants[i].SetActive(
                    false
                );
            }
        }

        // Выбираем случайный.
        int randomIndex =
            Random.Range(
                0,
                variants.Length
            );

        if (variants[randomIndex] != null)
        {
            variants[randomIndex].SetActive(
                true
            );
        }
    }

    private void PrepareLoadingVisuals()
    {
        EnsureLoadingHierarchyActive();

        if (loadingRootCanvasGroup != null)
        {
            loadingRootCanvasGroup.alpha = 1f;
            loadingRootCanvasGroup.interactable = false;
            loadingRootCanvasGroup.blocksRaycasts = true;
        }

        PrepareLayer(
            loadingBackgroundCanvasGroup
        );

        PrepareLayer(
            loadingImageCanvasGroup
        );
    }

    private void PrepareLayer(
    CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator FadeIn(
    CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            yield break;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        float duration =
            Mathf.Max(0f, fadeDuration);

        if (duration <= 0f)
        {
            canvasGroup.alpha = 1f;
            yield break;
        }

        float startAlpha =
            canvasGroup.alpha;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Mathf.Min(
                Time.unscaledDeltaTime,
                0.05f
            );

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    1f,
                    smoothT
                );

            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            yield break;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        float duration = Mathf.Max(0f, fadeDuration);

        if (duration <= 0f)
        {
            HideLayer(canvasGroup);
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // После тяжёлой загрузки unscaledDeltaTime
            // может оказаться огромным.
            // Не даём одному кадру съесть весь fade.
            float frameDelta = Mathf.Min(
                Time.unscaledDeltaTime,
                0.05f
            );

            elapsed += frameDelta;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            float smoothT = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                0f,
                smoothT
            );

            yield return null;
        }

        HideLayer(canvasGroup);
    }

    private void HideLoadingVisualsInstantly()
    {
        EnsureLoadingHierarchyActive();

        HideLayer(
            loadingImageCanvasGroup
        );

        HideLayer(
            loadingBackgroundCanvasGroup
        );

        if (loadingRootCanvasGroup != null)
        {
            loadingRootCanvasGroup.alpha = 0f;
            loadingRootCanvasGroup.interactable = false;
            loadingRootCanvasGroup.blocksRaycasts = false;
        }

        if (loadingSpinner != null)
            loadingSpinner.Hide();
    }

    public void QuitWithLoadingBackground()
    {
        if (isLoading)
            return;

        StartCoroutine(QuitWithLoadingBackgroundRoutine());
    }

    private IEnumerator QuitWithLoadingBackgroundRoutine()
    {
        isLoading = true;
        LoadingBlocksPause = true;

        if (PauseManager.Instance != null)
            PauseManager.Instance.SetCursorBlocked(true);

        EnsureLoadingHierarchyActive();

        if (loadingRootCanvasGroup != null)
        {
            loadingRootCanvasGroup.alpha = 1f;
            loadingRootCanvasGroup.interactable = false;
            loadingRootCanvasGroup.blocksRaycasts = true;
        }

        // Выбираем случайный фон.
        SelectRandomVariant(loadingBackgroundVariants);

        // Loading Image вообще не показываем.
        if (loadingImageCanvasGroup != null)
            loadingImageCanvasGroup.alpha = 0f;

        // Сам фон начинаем с нуля.
        if (loadingBackgroundCanvasGroup != null)
            loadingBackgroundCanvasGroup.alpha = 0f;

        if (loadingSpinner != null)
            loadingSpinner.Hide();

        // Плавно показываем только background.
        yield return StartCoroutine(
            FadeIn(loadingBackgroundCanvasGroup)
        );

        // Один кадр гарантированно показываем полностью готовый фон.
        yield return new WaitForEndOfFrame();

#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EnsureLoadingHierarchyActive()
    {
        if (loadingRoot != null &&
            !loadingRoot.activeSelf)
        {
            loadingRoot.SetActive(true);
        }

        if (loadingBackground != null &&
            !loadingBackground.activeSelf)
        {
            loadingBackground.SetActive(true);
        }

        if (loadingImage != null &&
            !loadingImage.activeSelf)
        {
            loadingImage.SetActive(true);
        }

        if (loadingSpinner == null)
            return;

        Transform current =
            loadingSpinner.transform;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            if (loadingRoot != null &&
                current.gameObject == loadingRoot)
            {
                break;
            }

            current = current.parent;
        }
    }

    private void HideLayer(
    CanvasGroup canvasGroup)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void DisablePlayerFootsteps()
    {
        FindReferences();

        if (playerController == null ||
            playerController.footstepSource == null)
        {
            return;
        }

        playerController
            .footstepSource
            .Stop();

        playerController
            .footstepSource
            .enabled = false;
    }

    private void EnablePlayerFootsteps()
    {
        FindReferences();

        if (playerController == null ||
            playerController.footstepSource == null)
        {
            return;
        }

        playerController
            .footstepSource
            .enabled = true;
    }

    private void FinishLoadingImmediately()
    {
        HideLoadingVisualsInstantly();
        EnablePlayerFootsteps();

        LoadingBlocksPause = false;
        isLoading = false;
        loadingCoroutine = null;

        if (PauseManager.Instance != null)
            PauseManager.Instance.SetCursorBlocked(false);
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (playerController == null)
        {
            GameObject playerObject =
                GameObject.Find(
                    playerObjectName
                );

            if (playerObject != null)
            {
                playerController =
                    playerObject
                        .GetComponent
                            <PlayerController>();
            }
        }

        if (playerController == null)
        {
            playerController =
                FindFirstObjectByType
                    <PlayerController>(
                        FindObjectsInactive.Include
                    );
        }

        if (loadingSpinner == null)
        {
            loadingSpinner =
                FindFirstObjectByType
                    <LoadingSpinnerController>(
                        FindObjectsInactive.Include
                    );
        }

        if (loadingRootCanvasGroup == null &&
            loadingRoot != null)
        {
            loadingRootCanvasGroup =
                loadingRoot
                    .GetComponent<CanvasGroup>();
        }

        if (loadingBackgroundCanvasGroup == null &&
            loadingBackground != null)
        {
            loadingBackgroundCanvasGroup =
                loadingBackground
                    .GetComponent<CanvasGroup>();
        }

        if (loadingImageCanvasGroup == null &&
            loadingImage != null)
        {
            loadingImageCanvasGroup =
                loadingImage
                    .GetComponent<CanvasGroup>();
        }
    }

    private void OnDestroy()
    {
        // Уничтожение дубликата не должно менять состояние
        // настоящего глобального менеджера.
        if (Instance != this)
            return;

        LoadingBlocksPause = false;
        isLoading = false;
        Instance = null;
    }
}
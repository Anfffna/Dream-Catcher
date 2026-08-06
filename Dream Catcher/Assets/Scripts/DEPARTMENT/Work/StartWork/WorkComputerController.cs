using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.Events;

public class WorkComputerController :
    MonoBehaviour,
    IInteractable
{
    public enum ComputerState
    {
        Off,
        Ready,
        Booting,
        Desktop
    }

    [Header("Current Day Settings")]
    [Tooltip("Доступен ли компьютер в текущий день.")]
    public bool availableThisDay = true;

    [Tooltip("Показывать ли обучающую обводку в текущий день.")]
    public bool showGuidanceOutlineThisDay = true;

    [Tooltip("Выключать компьютер, когда игрок покидает рабочее место.")]
    public bool resetWhenLeavingSeat = true;

    [Header("Interaction")]
    [Tooltip("Отдельный коллайдер для клика по компьютеру.")]
    public Collider interactionCollider;

    [Tooltip("Объект, которому меняется слой. Обычно объект с Interaction Collider.")]
    public GameObject layerTarget;

    public string defaultLayerName = "Default";
    public string interactableLayerName = "Interactable";

    [Tooltip("Обычно выключено. Включай только если всем детям тоже нужен этот слой.")]
    public bool applyLayerToChildren = false;

    [Header("Outline")]
    public InteractionOutline interactionOutline;

    [Header("Video")]
    public VideoPlayer videoPlayer;

    public RenderTexture targetTexture;

    [Tooltip("Видео загрузки. Его последний кадр должен быть рабочим столом.")]
    public VideoClip startupClip;

    [Tooltip("Сколько максимум ждать подготовки видео.")]
    public float prepareTimeout = 10f;

    [Header("Events")]
    [Tooltip("Вызывается после полного завершения видео загрузки.")]
    public UnityEvent onStartupFinished;

    [Header("Computer Screen")]
    [Tooltip("Обычный меш выключенного экрана, который нужно скрыть при запуске.")]
    public GameObject physicalScreenObject;

    [Header("Рабочий интерфейс")]
    [Tooltip("World Space Canvas с интерфейсом компьютера.")]
    [SerializeField] private GameObject computerWorldCanvas;

    [Tooltip("Canvas Group рабочего интерфейса.")]
    [SerializeField] private CanvasGroup computerWorldCanvasGroup;

    [Tooltip("Время плавного появления рабочего интерфейса.")]
    [SerializeField] private float computerCanvasFadeDuration = 0.5f;

    private Coroutine computerCanvasFadeCoroutine;

    [Header("Зум компьютера")]
    [SerializeField] private ZoomComputerWork zoomComputerWork;

    [SerializeField] private bool zoomClickAvailable;

    [Tooltip("Quad, на котором отображается Render Texture с видео.")]
    public GameObject videoScreenQuad;

    [Tooltip("Renderer именно поверхности экрана монитора.")]
    public Renderer screenRenderer;

    [Tooltip("Имя объекта экрана для автоматического поиска.")]
    public string screenObjectName = "Screen";

    [Header("Optional Power Sound")]
    public AudioSource powerAudioSource;
    public AudioClip powerButtonClip;

    [Header("Screen Off")]
    public Color screenOffColor = Color.black;

    [Header("Current State")]
    [SerializeField]
    private ComputerState currentState =
        ComputerState.Off;

    private Material screenMaterialInstance;

    private Coroutine bootCoroutine;
    private Coroutine outlineCoroutine;

    private bool videoHadError;

    public ComputerState CurrentState =>
        currentState;

    private void Awake()
    {
        FindReferences();
        SetupScreenMaterial();
        SubscribeToVideoEvents();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeToVideoEvents();
    }

    private void Start()
    {
        ResetComputer();
    }

    private void OnDisable()
    {
        if (computerCanvasFadeCoroutine != null)
        {
            StopCoroutine(computerCanvasFadeCoroutine);
            computerCanvasFadeCoroutine = null;
        }

        UnsubscribeFromVideoEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromVideoEvents();
    }

    private void Update()
    {
        bool isSeated =
            WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsSeated;

        // Компьютер недоступен в этом дне.
        if (!availableThisDay)
        {
            if (currentState != ComputerState.Off)
                ResetComputer();

            return;
        }

        // Игрок не сидит за рабочим столом.
        if (!isSeated)
        {
            if (resetWhenLeavingSeat &&
                currentState != ComputerState.Off)
            {
                ResetComputer();
            }

            return;
        }

        // Игрок только что сел.
        if (currentState == ComputerState.Off)
            MakeComputerReady();
    }

    public void Interact()
    {
        bool isSeated =
            WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsSeated;

        if (!isSeated ||
            !availableThisDay)
        {
            return;
        }

        // После установки SON-3 клик по монитору запускает зум.
        if (currentState == ComputerState.Desktop)
        {
            if (!zoomClickAvailable ||
                zoomComputerWork == null)
            {
                return;
            }

            zoomClickAvailable = false;

            SetInteractionLayer(false);

            if (interactionOutline != null)
                interactionOutline.HideOutline();

            zoomComputerWork.StartZoom();

            return;
        }

        // Первый клик запускает загрузочное видео.
        if (currentState != ComputerState.Ready)
            return;

        ShowVideoScreen();

        currentState =
            ComputerState.Booting;

        SetInteractionAvailable(false);

        if (powerAudioSource != null &&
            powerButtonClip != null)
        {
            powerAudioSource.PlayOneShot(
                powerButtonClip
            );
        }

        if (bootCoroutine != null)
            StopCoroutine(bootCoroutine);

        bootCoroutine =
            StartCoroutine(BootComputer());
    }

    private IEnumerator BootComputer()
    {
        FindReferences();
        SetupScreenMaterial();
        SubscribeToVideoEvents();

        if (videoPlayer == null)
        {
            Debug.LogError(
                "WorkComputerController: не назначен VideoPlayer."
            );

            ReturnToReadyState();
            yield break;
        }

        if (targetTexture == null)
        {
            Debug.LogError(
                "WorkComputerController: не назначена Render Texture."
            );

            ReturnToReadyState();
            yield break;
        }

        if (startupClip == null)
        {
            Debug.LogError(
                "WorkComputerController: не назначено видео загрузки."
            );

            ReturnToReadyState();
            yield break;
        }

        ApplyScreenTexture(targetTexture);
        ClearScreen();

        videoHadError = false;

        videoPlayer.Stop();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        videoPlayer.renderMode =
            VideoRenderMode.RenderTexture;

        videoPlayer.targetTexture =
            targetTexture;

        videoPlayer.clip =
            startupClip;

        videoPlayer.Prepare();

        float elapsed = 0f;

        while (!videoPlayer.isPrepared &&
               !videoHadError &&
               elapsed < prepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (videoHadError)
        {
            Debug.LogError(
                "WorkComputerController: ошибка подготовки видео."
            );

            ReturnToReadyState();
            yield break;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError(
                "WorkComputerController: видео не подготовилось за отведённое время."
            );

            ReturnToReadyState();
            yield break;
        }

        videoPlayer.Play();

        // Ждём событие loopPointReached.
        while (currentState ==
               ComputerState.Booting)
        {
            if (videoHadError)
            {
                ReturnToReadyState();
                yield break;
            }

            yield return null;
        }

        bootCoroutine = null;
    }

    private void OnStartupVideoFinished(
        VideoPlayer finishedPlayer)
    {
        if (currentState != ComputerState.Booting)
            return;

        currentState = ComputerState.Desktop;

        finishedPlayer.Pause();
        bootCoroutine = null;

        onStartupFinished?.Invoke();

        Debug.Log(
            "WorkComputerController: загрузка закончена, последний кадр оставлен на экране."
        );
    }

    private void OnVideoError(
        VideoPlayer source,
        string message)
    {
        videoHadError = true;

        Debug.LogError(
            "WorkComputerController: ошибка VideoPlayer: " +
            message
        );
    }

    private void MakeComputerReady()
    {
        FindReferences();
        SetupScreenMaterial();

        currentState = ComputerState.Ready;

        // Пока компьютер не включён,
        // экран остаётся чёрным.
        ApplyScreenTexture(targetTexture);
        ClearScreen();

        SetInteractionAvailable(true);
    }

    private void ReturnToReadyState()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        currentState = ComputerState.Ready;

        ClearScreen();
        SetInteractionAvailable(true);

        bootCoroutine = null;
    }

    public void ResetComputer()
    {
        if (bootCoroutine != null)
        {
            StopCoroutine(bootCoroutine);
            bootCoroutine = null;
        }

        if (outlineCoroutine != null)
        {
            StopCoroutine(outlineCoroutine);
            outlineCoroutine = null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.isLooping = false;
            videoPlayer.time = 0;
        }

        currentState = ComputerState.Off;
        zoomClickAvailable = false;
        ShowPhysicalScreen();

        SetInteractionAvailable(false);

        ApplyScreenTexture(targetTexture);
        ClearScreen();
    }

    private void SetInteractionAvailable(
        bool available)
    {
        SetInteractionLayer(available);

        if (outlineCoroutine != null)
        {
            StopCoroutine(outlineCoroutine);
            outlineCoroutine = null;
        }

        FindOutline();

        if (interactionOutline == null)
            return;

        // Слой Interactable может быть включён,
        // даже если обучающая обводка выключена.
        if (available &&
            showGuidanceOutlineThisDay)
        {
            outlineCoroutine =
                StartCoroutine(
                    ShowOutlineNextFrames()
                );
        }
        else
        {
            interactionOutline.HideOutline();
        }
    }

    private IEnumerator ShowOutlineNextFrames()
    {
        // Даём рабочему режиму полностью завершить посадку.
        yield return new WaitForSecondsRealtime(0.1f);

        bool isSeated =
            WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance.IsSeated;

        if (!isSeated)
        {
            outlineCoroutine = null;
            yield break;
        }

        if (currentState != ComputerState.Ready)
        {
            outlineCoroutine = null;
            yield break;
        }

        if (!showGuidanceOutlineThisDay)
        {
            outlineCoroutine = null;
            yield break;
        }

        FindOutline();

        if (interactionOutline == null)
        {
            Debug.LogWarning(
                "WorkComputerController: InteractionOutline не найден."
            );

            outlineCoroutine = null;
            yield break;
        }

        interactionOutline.enabled = true;

        if (!interactionOutline.gameObject.activeSelf)
            interactionOutline.gameObject.SetActive(true);

        interactionOutline.ForceRedrawOutline();

        Debug.Log(
            "WorkComputerController: обводка компьютера показана."
        );

        outlineCoroutine = null;
    }

    private void SetInteractionLayer(
        bool interactable)
    {
        string targetLayerName =
            interactable
                ? interactableLayerName
                : defaultLayerName;

        int targetLayer =
            LayerMask.NameToLayer(
                targetLayerName
            );

        if (targetLayer < 0)
        {
            Debug.LogWarning(
                "WorkComputerController: слой не найден: " +
                targetLayerName
            );

            return;
        }

        GameObject target = layerTarget;

        if (target == null &&
            interactionCollider != null)
        {
            target =
                interactionCollider.gameObject;
        }

        if (target == null)
            target = gameObject;

        if (applyLayerToChildren)
        {
            SetLayerRecursively(
                target,
                targetLayer
            );
        }
        else
        {
            target.layer = targetLayer;
        }
    }

    private void SetLayerRecursively(
        GameObject target,
        int layer)
    {
        if (target == null)
            return;

        target.layer = layer;

        for (int i = 0;
             i < target.transform.childCount;
             i++)
        {
            Transform child =
                target.transform.GetChild(i);

            if (child != null)
            {
                SetLayerRecursively(
                    child.gameObject,
                    layer
                );
            }
        }
    }

    public void PrepareCanvasForTest()
    {
        // Останавливаем возможную загрузку компьютера.
        if (bootCoroutine != null)
        {
            StopCoroutine(bootCoroutine);
            bootCoroutine = null;
        }

        if (outlineCoroutine != null)
        {
            StopCoroutine(outlineCoroutine);
            outlineCoroutine = null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.isLooping = false;
        }

        // Сразу устанавливаем компьютер в конечное рабочее состояние.
        currentState = ComputerState.Desktop;

        // В тестовом режиме полностью скрываем предыдущие экраны.
        if (physicalScreenObject != null)
            physicalScreenObject.SetActive(false);

        if (videoScreenQuad != null)
            videoScreenQuad.SetActive(false);

        // Показываем готовый World Space Canvas.
        if (computerWorldCanvas != null)
            computerWorldCanvas.SetActive(true);

        if (computerWorldCanvasGroup != null)
        {
            computerWorldCanvasGroup.alpha = 1f;

            // Кнопки включатся только после завершения зума.
            computerWorldCanvasGroup.interactable = false;
            computerWorldCanvasGroup.blocksRaycasts = false;
        }

        zoomClickAvailable = false;

        // Монитор больше не должен ловить 3D-клики.
        SetInteractionLayer(false);

        if (interactionOutline != null)
            interactionOutline.HideOutline();
    }

    private void ShowPhysicalScreen()
    {
        if (physicalScreenObject != null)
            physicalScreenObject.SetActive(true);

        if (videoScreenQuad != null)
            videoScreenQuad.SetActive(false);

        HideComputerCanvas();
    }

    public void ShowWorkCanvasAfterSon3()
    {
        if (currentState != ComputerState.Desktop)
            return;

        if (computerCanvasFadeCoroutine != null)
        {
            StopCoroutine(computerCanvasFadeCoroutine);
            computerCanvasFadeCoroutine = null;
        }

        // Video Quad остаётся включённым под рабочим Canvas.
        if (computerWorldCanvas != null)
            computerWorldCanvas.SetActive(true);

        if (computerWorldCanvasGroup != null)
        {
            computerWorldCanvasGroup.alpha = 0f;
            computerWorldCanvasGroup.interactable = false;
            computerWorldCanvasGroup.blocksRaycasts = false;

            computerCanvasFadeCoroutine =
                StartCoroutine(FadeInComputerCanvas());
        }
        else
        {
            // Запасной вариант, если Canvas Group не назначен.
            EnableMonitorZoomClick();
        }

        if (interactionOutline != null)
            interactionOutline.HideOutline();
    }

    private IEnumerator FadeInComputerCanvas()
    {
        float duration =
            Mathf.Max(0f, computerCanvasFadeDuration);

        if (duration <= 0f)
        {
            computerWorldCanvasGroup.alpha = 1f;
        }
        else
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(elapsed / duration);

                float smoothT =
                    Mathf.SmoothStep(0f, 1f, t);

                computerWorldCanvasGroup.alpha =
                    Mathf.Lerp(0f, 1f, smoothT);

                yield return null;
            }

            computerWorldCanvasGroup.alpha = 1f;
        }

        computerCanvasFadeCoroutine = null;

        // После появления Canvas разрешаем клик по монитору для зума.
        EnableMonitorZoomClick();
    }

    private void EnableMonitorZoomClick()
    {
        zoomClickAvailable = true;

        // Монитор снова получает слой Interactable.
        SetInteractionLayer(true);

        if (interactionOutline != null)
            interactionOutline.HideOutline();
    }

    private void HideComputerCanvas()
    {
        if (computerCanvasFadeCoroutine != null)
        {
            StopCoroutine(computerCanvasFadeCoroutine);
            computerCanvasFadeCoroutine = null;
        }

        if (computerWorldCanvasGroup != null)
        {
            computerWorldCanvasGroup.alpha = 0f;
            computerWorldCanvasGroup.interactable = false;
            computerWorldCanvasGroup.blocksRaycasts = false;
        }

        if (computerWorldCanvas != null)
            computerWorldCanvas.SetActive(false);

        zoomClickAvailable = false;
    }

    public void EnableWorkCanvasInteraction()
    {
        if (computerWorldCanvasGroup == null)
            return;

        // После зума разрешаем работу с интерфейсом.
        computerWorldCanvasGroup.interactable = true;
        computerWorldCanvasGroup.blocksRaycasts = true;
    }

    public void BeginReturnToWorkView()
    {
        if (currentState !=
            ComputerState.Desktop)
        {
            return;
        }

        if (computerCanvasFadeCoroutine != null)
        {
            StopCoroutine(
                computerCanvasFadeCoroutine
            );

            computerCanvasFadeCoroutine = null;
        }

        // Интерфейс остаётся виден на поверхности монитора,
        // но больше не перехватывает клики издалека.
        if (computerWorldCanvasGroup != null)
        {
            computerWorldCanvasGroup.alpha =
                1f;

            computerWorldCanvasGroup.interactable =
                false;

            computerWorldCanvasGroup.blocksRaycasts =
                false;
        }

        zoomClickAvailable = false;

        // Пока камера отъезжает, монитор ещё нельзя нажать.
        SetInteractionLayer(false);

        if (interactionOutline != null)
            interactionOutline.HideOutline();
    }

    public void CompleteReturnToWorkView()
    {
        if (currentState !=
            ComputerState.Desktop)
        {
            return;
        }

        // Canvas остаётся видимым на мониторе.
        if (computerWorldCanvas != null)
        {
            computerWorldCanvas.SetActive(
                true
            );
        }

        if (computerWorldCanvasGroup != null)
        {
            computerWorldCanvasGroup.alpha =
                1f;

            computerWorldCanvasGroup.interactable =
                false;

            computerWorldCanvasGroup.blocksRaycasts =
                false;
        }

        // После завершения перехода монитор снова
        // можно нажать для нового приближения.
        EnableMonitorZoomClick();
    }

    private void ShowVideoScreen()
    {
        if (physicalScreenObject != null)
            physicalScreenObject.SetActive(false);

        if (videoScreenQuad != null)
            videoScreenQuad.SetActive(true);

        HideComputerCanvas();
    }

    public void RestorePhysicalScreen()
    {
        ShowPhysicalScreen();
    }

    public void ClearScreen()
    {
        if (targetTexture == null)
            return;

        if (!targetTexture.IsCreated())
            targetTexture.Create();

        RenderTexture previous =
            RenderTexture.active;

        RenderTexture.active =
            targetTexture;

        GL.Clear(
            true,
            true,
            screenOffColor
        );

        RenderTexture.active =
            previous;
    }

    private void SetupScreenMaterial()
    {
        if (videoPlayer != null &&
            targetTexture == null)
        {
            targetTexture =
                videoPlayer.targetTexture;
        }

        if (screenRenderer == null)
            return;

        // Создаёт отдельный экземпляр материала
        // только для этого экрана.
        if (screenMaterialInstance == null)
        {
            screenMaterialInstance =
                screenRenderer.material;
        }

        ApplyScreenTexture(
            targetTexture
        );
    }

    private void ApplyScreenTexture(
        Texture texture)
    {
        if (screenRenderer == null ||
            texture == null)
        {
            return;
        }

        if (screenMaterialInstance == null)
        {
            screenMaterialInstance =
                screenRenderer.material;
        }

        screenMaterialInstance.mainTexture =
            texture;

        if (screenMaterialInstance.HasProperty(
            "_BaseMap"))
        {
            screenMaterialInstance.SetTexture(
                "_BaseMap",
                texture
            );
        }

        if (screenMaterialInstance.HasProperty(
            "_EmissionMap"))
        {
            screenMaterialInstance.SetTexture(
                "_EmissionMap",
                texture
            );
        }
    }

    private void FindOutline()
    {
        if (interactionOutline != null)
            return;

        interactionOutline =
            GetComponent<InteractionOutline>();

        if (interactionOutline == null)
        {
            interactionOutline =
                GetComponentInChildren<InteractionOutline>(
                    true
                );
        }

        if (interactionOutline == null)
        {
            interactionOutline =
                GetComponentInParent<InteractionOutline>();
        }
    }

    private void FindReferences()
    {
        if (videoPlayer == null)
        {
            videoPlayer =
                GetComponent<VideoPlayer>();
        }

        if (videoPlayer != null &&
            targetTexture == null)
        {
            targetTexture =
                videoPlayer.targetTexture;
        }

        if (interactionCollider == null)
        {
            interactionCollider =
                GetComponentInChildren<Collider>(
                    true
                );
        }

        if (layerTarget == null &&
            interactionCollider != null)
        {
            layerTarget =
                interactionCollider.gameObject;
        }

        FindOutline();

        if (screenRenderer == null)
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(
                    true
                );

            for (int i = 0;
                 i < renderers.Length;
                 i++)
            {
                Renderer currentRenderer =
                    renderers[i];

                if (currentRenderer == null)
                    continue;

                if (currentRenderer.gameObject.name ==
                    screenObjectName)
                {
                    screenRenderer =
                        currentRenderer;

                    break;
                }
            }
        }

        if (powerAudioSource == null)
        {
            powerAudioSource =
                GetComponent<AudioSource>();
        }

        if (computerWorldCanvasGroup == null &&
            computerWorldCanvas != null)
        {
            computerWorldCanvasGroup =
                computerWorldCanvas.GetComponent<CanvasGroup>();

            if (computerWorldCanvasGroup == null)
            {
                computerWorldCanvasGroup =
                    computerWorldCanvas
                        .GetComponentInChildren<CanvasGroup>(true);
            }
        }

        if (zoomComputerWork == null)
        {
            zoomComputerWork =
                FindFirstObjectByType<ZoomComputerWork>(
                    FindObjectsInactive.Include
                );
        }
    }

    private void SubscribeToVideoEvents()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached -=
            OnStartupVideoFinished;

        videoPlayer.loopPointReached +=
            OnStartupVideoFinished;

        videoPlayer.errorReceived -=
            OnVideoError;

        videoPlayer.errorReceived +=
            OnVideoError;
    }

    private void UnsubscribeFromVideoEvents()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached -=
            OnStartupVideoFinished;

        videoPlayer.errorReceived -=
            OnVideoError;
    }

    /*
     * НА БУДУЩЕЕ:
     * будущий DayManager сможет вызвать:
     *
     * computer.ConfigureForDay(currentDay);
     */
    public void ConfigureForDay(
        int dayNumber)
    {
        availableThisDay = true;

        // Обводка только в первый день.
        showGuidanceOutlineThisDay =
            dayNumber == 1;

        if (currentState ==
            ComputerState.Ready)
        {
            SetInteractionAvailable(true);
        }
    }
}
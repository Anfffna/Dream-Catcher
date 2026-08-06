using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonTextHoverScale :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Текст")]
    [Tooltip("Дочерний текст, который будет увеличиваться.")]
    [SerializeField] private TMP_Text targetText;

    [Header("Увеличение")]
    [Tooltip("Множитель размера текста при наведении.")]
    [SerializeField] private float hoverScaleMultiplier = 1.08f;

    [Tooltip("Время плавного увеличения и уменьшения.")]
    [SerializeField] private float animationDuration = 0.12f;

    [Header("Состояние кнопки")]
    [Tooltip("Не реагировать, если кнопка недоступна.")]
    [SerializeField] private bool respectButtonInteractable = true;

    [Header("Курсор")]
    [Tooltip("Рабочий контроллер курсора. Можно оставить пустым.")]
    [SerializeField] private WorkCursorController cursorController;

    private Button button;
    private RectTransform textRectTransform;

    private Vector3 normalScale;
    private Vector3 targetScale;
    private Vector3 animationStartScale;

    private float animationElapsed;
    private bool animationActive;
    private bool pointerInside;

    private void Awake()
    {
        FindReferences();
        SaveNormalScale();
    }

    private void OnEnable()
    {
        FindReferences();

        if (textRectTransform == null)
            return;

        normalScale =
            textRectTransform.localScale;

        targetScale =
            normalScale;

        animationActive = false;
        pointerInside = false;
    }

    private void Update()
    {
        if (!animationActive ||
            textRectTransform == null)
        {
            return;
        }

        if (animationDuration <= 0f)
        {
            textRectTransform.localScale =
                targetScale;

            animationActive = false;
            return;
        }

        animationElapsed +=
            Time.unscaledDeltaTime;

        float t =
            Mathf.Clamp01(
                animationElapsed /
                animationDuration
            );

        float smoothT =
            Mathf.SmoothStep(
                0f,
                1f,
                t
            );

        textRectTransform.localScale =
            Vector3.Lerp(
                animationStartScale,
                targetScale,
                smoothT
            );

        if (t >= 1f)
        {
            textRectTransform.localScale =
                targetScale;

            animationActive = false;
        }
    }

    private void OnDisable()
    {
        pointerInside = false;
        animationActive = false;

        if (textRectTransform != null)
        {
            textRectTransform.localScale =
                normalScale;
        }

        SetDefaultCursor();
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        pointerInside = true;

        if (!CanAnimate())
            return;

        SetInteractCursor();

        StartScaleAnimation(
            normalScale *
            hoverScaleMultiplier
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        pointerInside = false;

        SetDefaultCursor();

        StartScaleAnimation(
            normalScale
        );
    }

    public void RefreshButtonState()
    {
        if (pointerInside &&
            CanAnimate())
        {
            SetInteractCursor();

            StartScaleAnimation(
                normalScale *
                hoverScaleMultiplier
            );
        }
        else
        {
            SetDefaultCursor();

            StartScaleAnimation(
                normalScale
            );
        }
    }

    private void StartScaleAnimation(
        Vector3 newTargetScale)
    {
        if (textRectTransform == null)
            return;

        animationStartScale =
            textRectTransform.localScale;

        targetScale =
            newTargetScale;

        animationElapsed = 0f;
        animationActive = true;
    }

    private bool CanAnimate()
    {
        if (!respectButtonInteractable)
            return true;

        return button != null &&
               button.interactable;
    }

    private void SetInteractCursor()
    {
        FindCursorController();

        if (cursorController != null)
        {
            cursorController
                .SetInteractCursor();
        }
    }

    private void SetDefaultCursor()
    {
        FindCursorController();

        if (cursorController != null)
        {
            cursorController
                .SetDefaultCursor();
        }
    }

    private void FindReferences()
    {
        if (button == null)
        {
            button =
                GetComponent<Button>();
        }

        if (targetText == null)
        {
            targetText =
                GetComponentInChildren<TMP_Text>(
                    true
                );
        }

        if (targetText != null)
        {
            textRectTransform =
                targetText.rectTransform;
        }

        FindCursorController();
    }

    private void FindCursorController()
    {
        if (cursorController != null)
            return;

        if (WorkSessionManager.Instance != null)
        {
            cursorController =
                WorkSessionManager.Instance
                    .cursorController;
        }

        if (cursorController == null)
        {
            cursorController =
                FindFirstObjectByType
                    <WorkCursorController>(
                        FindObjectsInactive.Include
                    );
        }
    }

    private void SaveNormalScale()
    {
        if (textRectTransform == null)
            return;

        normalScale =
            textRectTransform.localScale;

        targetScale =
            normalScale;
    }

    private void OnValidate()
    {
        hoverScaleMultiplier =
            Mathf.Max(
                0f,
                hoverScaleMultiplier
            );

        animationDuration =
            Mathf.Max(
                0f,
                animationDuration
            );
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SettingsControlsScroll :
    MonoBehaviour
{
    [Header("Основные объекты")]

    [Tooltip("Общий Content, внутри которого находятся настройки и управление.")]
    [SerializeField]
    private RectTransform content;

    [Tooltip("Прозрачная кнопка на строке 'управление'.")]
    [SerializeField]
    private Button controlButton;

    [Tooltip("Стрелка рядом со словом 'управление'.")]
    [SerializeField]
    private RectTransform arrow;


    [Header("Позиции Content")]

    [Tooltip("Pos Y Content в обычном состоянии настроек.")]
    [SerializeField]
    private float settingsPositionY = 0f;

    [Tooltip("Pos Y Content, когда открыта страница управления.")]
    [SerializeField]
    private float controlsPositionY = 500f;


    [Header("Анимация")]

    [Tooltip("Длительность плавной прокрутки.")]
    [SerializeField]
    private float animationDuration = 0.45f;

    [Tooltip("Поворот стрелки в закрытом состоянии.")]
    [SerializeField]
    private float closedArrowAngle = 0f;

    [Tooltip("Поворот стрелки в открытом состоянии.")]
    [SerializeField]
    private float openedArrowAngle = 180f;


    private Coroutine animationCoroutine;

    private bool controlsOpened;


    private void Awake()
    {
        if (controlButton != null)
        {
            controlButton.onClick.RemoveListener(
                ToggleControls
            );

            controlButton.onClick.AddListener(
                ToggleControls
            );
        }

        SetStateInstantly(false);
    }


    private void OnDestroy()
    {
        if (controlButton != null)
        {
            controlButton.onClick.RemoveListener(
                ToggleControls
            );
        }
    }


    public void ToggleControls()
    {
        controlsOpened =
            !controlsOpened;

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );
        }

        animationCoroutine =
            StartCoroutine(
                AnimateState()
            );
    }


    private IEnumerator AnimateState()
    {
        if (content == null)
        {
            animationCoroutine = null;
            yield break;
        }


        Vector2 startPosition =
            content.anchoredPosition;

        Vector2 targetPosition =
            startPosition;

        targetPosition.y =
            controlsOpened
                ? controlsPositionY
                : settingsPositionY;


        float startArrowAngle =
            arrow != null
                ? arrow.localEulerAngles.z
                : 0f;

        float targetArrowAngle =
            controlsOpened
                ? openedArrowAngle
                : closedArrowAngle;


        if (animationDuration <= 0f)
        {
            content.anchoredPosition =
                targetPosition;

            SetArrowAngle(
                targetArrowAngle
            );

            animationCoroutine = null;
            yield break;
        }


        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    animationDuration
                );

            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            content.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    smoothT
                );


            if (arrow != null)
            {
                float angle =
                    Mathf.LerpAngle(
                        startArrowAngle,
                        targetArrowAngle,
                        smoothT
                    );

                SetArrowAngle(
                    angle
                );
            }


            yield return null;
        }


        content.anchoredPosition =
            targetPosition;

        SetArrowAngle(
            targetArrowAngle
        );

        animationCoroutine = null;
    }


    private void SetStateInstantly(
        bool opened)
    {
        controlsOpened = opened;

        if (content != null)
        {
            Vector2 position =
                content.anchoredPosition;

            position.y =
                opened
                    ? controlsPositionY
                    : settingsPositionY;

            content.anchoredPosition =
                position;
        }

        SetArrowAngle(
            opened
                ? openedArrowAngle
                : closedArrowAngle
        );
    }


    private void SetArrowAngle(
        float angle)
    {
        if (arrow == null)
            return;

        Vector3 rotation =
            arrow.localEulerAngles;

        rotation.z = angle;

        arrow.localEulerAngles =
            rotation;
    }


    private void OnValidate()
    {
        animationDuration =
            Mathf.Max(
                0f,
                animationDuration
            );
    }
}
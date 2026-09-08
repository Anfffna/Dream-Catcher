using System.Collections;
using TMPro;
using UnityEngine;

public class ClickInteractionHint :
    MonoBehaviour
{
    [Header("UI")]

    [Tooltip(
        "CanvasGroup общей плашки подсказки."
    )]
    [SerializeField]
    private CanvasGroup hintCanvasGroup;

    [Tooltip(
        "TMP-текст внутри общей плашки."
    )]
    [SerializeField]
    private TMP_Text hintTextComponent;


    [Header("Закрытие")]

    [Tooltip(
        "Закрывать подсказку следующим ЛКМ."
    )]
    [SerializeField]
    private bool hideOnLeftClick =
        true;

    [Tooltip(
        "Блокировать UI-raycast, " +
        "пока подсказка видима."
    )]
    [SerializeField]
    private bool blockRaycastsWhileVisible =
        true;


    [Header("Плавность")]

    [Tooltip(
        "Время плавного появления."
    )]
    [Min(0f)]
    [SerializeField]
    private float fadeInDuration =
        0.35f;

    [Tooltip(
        "Время плавного исчезновения."
    )]
    [Min(0f)]
    [SerializeField]
    private float fadeOutDuration =
        0.25f;


    public bool IsVisible =>
        isVisible;

    public bool DismissRequested =>
        dismissRequested;


    private bool isVisible;
    private bool dismissRequested;
    private bool currentHideOnLeftClick;

    private int shownFrame =
        -1;

    private Coroutine fadeCoroutine;


    private void Awake()
    {
        HideImmediate();

        /*
         * Update нужен только тогда,
         * когда подсказка реально открыта.
         */
        enabled = false;
    }


    private void Update()
    {
        if (!isVisible ||
    !       currentHideOnLeftClick)
        {
            return;
        }


        /*
         * Клик, которым подсказку
         * только что вызвали,
         * не должен тут же её закрыть.
         */
        if (Time.frameCount <=
            shownFrame)
        {
            return;
        }


        if (Input.GetMouseButtonDown(0))
        {
            Hide();
        }
    }


    // =====================================================
    // ПОКАЗ
    // =====================================================

    public void Show(
    string text)
    {
        Show(
            text,
            hideOnLeftClick
        );
    }


    public void Show(
        string text,
        bool closeOnLeftClick)
    {
        if (hintCanvasGroup == null)
            return;


        currentHideOnLeftClick =
            closeOnLeftClick;


        if (hintTextComponent != null)
        {
            hintTextComponent.text =
                text;
        }


        dismissRequested =
            false;

        shownFrame =
            Time.frameCount;


        hintCanvasGroup
            .gameObject
            .SetActive(true);


        hintCanvasGroup.blocksRaycasts =
            blockRaycastsWhileVisible;

        hintCanvasGroup.interactable =
            blockRaycastsWhileVisible;


        isVisible =
            true;

        enabled =
            true;


        StartFade(
            1f,
            fadeInDuration,
            false
        );
    }


    // =====================================================
    // СКРЫТИЕ
    // =====================================================

    public void Hide()
    {
        if (!isVisible)
            return;


        dismissRequested =
            true;


        StartFade(
            0f,
            fadeOutDuration,
            true
        );
    }


    private void StartFade(
        float targetAlpha,
        float duration,
        bool finishHide)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );
        }


        fadeCoroutine =
            StartCoroutine(
                FadeRoutine(
                    targetAlpha,
                    duration,
                    finishHide
                )
            );
    }


    private IEnumerator FadeRoutine(
        float targetAlpha,
        float duration,
        bool finishHide)
    {
        if (hintCanvasGroup == null)
        {
            fadeCoroutine =
                null;

            yield break;
        }


        float startAlpha =
            hintCanvasGroup.alpha;


        if (duration <= 0f)
        {
            hintCanvasGroup.alpha =
                targetAlpha;
        }
        else
        {
            float elapsed =
                0f;


            while (elapsed <
                   duration)
            {
                elapsed +=
                    Time.deltaTime;


                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );


                float smoothT =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );


                hintCanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        targetAlpha,
                        smoothT
                    );


                yield return null;
            }


            hintCanvasGroup.alpha =
                targetAlpha;
        }


        fadeCoroutine =
            null;


        if (!finishHide)
            yield break;


        hintCanvasGroup.alpha =
            0f;

        hintCanvasGroup.blocksRaycasts =
            false;

        hintCanvasGroup.interactable =
            false;


        isVisible =
            false;

        enabled =
            false;
    }


    private void HideImmediate()
    {
        if (hintCanvasGroup == null)
            return;


        hintCanvasGroup.alpha =
            0f;

        hintCanvasGroup.blocksRaycasts =
            false;

        hintCanvasGroup.interactable =
            false;


        isVisible =
            false;

        dismissRequested =
            false;
    }


    private void OnValidate()
    {
        fadeInDuration =
            Mathf.Max(
                0f,
                fadeInDuration
            );

        fadeOutDuration =
            Mathf.Max(
                0f,
                fadeOutDuration
            );
    }
}
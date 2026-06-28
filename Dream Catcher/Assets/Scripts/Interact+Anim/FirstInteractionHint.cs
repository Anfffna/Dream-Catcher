using UnityEngine;
using System.Collections;

public class FirstInteractionHint : MonoBehaviour
{
    [Header("UI Hint")]
    public CanvasGroup hintCanvasGroup;  // сюда перетащи CanvasGroup плашки

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;    // время появления/исчезновения

    private static bool hasBeenShown = false;
    private bool isShowing = false;

    void Start()
    {
        if (hintCanvasGroup != null)
            hintCanvasGroup.alpha = 0f;
    }

    public void TryShowHint()
    {
        if (hasBeenShown) return;
        if (isShowing) return;
        if (hintCanvasGroup == null) return;

        hasBeenShown = true;
        isShowing = true;
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        // Делаем плашку кликабельной (блокируем лучи)
        hintCanvasGroup.blocksRaycasts = true;
        hintCanvasGroup.interactable = true;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            hintCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        hintCanvasGroup.alpha = 1f;

        // Ждём, пока игрок не кликнет куда-нибудь (любая кнопка мыши)
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

        // Начинаем скрытие
        StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            hintCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        hintCanvasGroup.alpha = 0f;
        hintCanvasGroup.blocksRaycasts = false;
        hintCanvasGroup.interactable = false;
        isShowing = false;
    }
}
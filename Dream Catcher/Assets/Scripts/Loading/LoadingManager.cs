using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("Loading Screen UI")]
    public GameObject loadingBackground;
    public GameObject loadingImage;
    public CanvasGroup loadingBackgroundCanvasGroup;
    public CanvasGroup loadingImageCanvasGroup;

    [Header("Loading Spinner")]
    public LoadingSpinnerController loadingSpinner; 

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    [Header("Player")]
    public PlayerController playerController;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ищем спиннер автоматически, если ссылка не задана или объект уничтожен
        if (loadingSpinner == null)
        {
            loadingSpinner = FindObjectOfType<LoadingSpinnerController>();
            if (loadingSpinner != null)
                Debug.Log("Спиннер найден автоматически: " + loadingSpinner.name);
            else
                Debug.LogWarning("Спиннер не найден!");
        }
    }

    public void StartLoading(
        string sceneName,
        DialogueManager dialogueManager,
        List<DialogueManager.DialogueLine> dialogueLines,
        float showImageDelay = 1f
    )
    {
        if (loadingSpinner == null)
            loadingSpinner = FindObjectOfType<LoadingSpinnerController>();

        StartCoroutine(LoadingSequence(sceneName, dialogueManager, dialogueLines, showImageDelay));
    }

    private IEnumerator LoadingSequence(
        string sceneName,
        DialogueManager dialogueManager,
        List<DialogueManager.DialogueLine> dialogueLines,
        float showImageDelay
    )
    {
        // 1. Показываем фон и картинку
        yield return StartCoroutine(FadeIn(loadingBackgroundCanvasGroup, loadingBackground));
        yield return StartCoroutine(FadeIn(loadingImageCanvasGroup, loadingImage));

        // 2. Отключаем звуки шагов
        if (playerController != null && playerController.footstepSource != null)
        {
            playerController.footstepSource.Stop();
            playerController.footstepSource.enabled = false;
        }

        // 3. Запускаем асинхронную загрузку
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // 4. Ждём задержку перед диалогом
        yield return new WaitForSeconds(showImageDelay);

        // 5. Запускаем диалог загрузки (если есть)
        if (dialogueManager != null && dialogueLines != null && dialogueLines.Count > 0)
        {
            dialogueManager.StartDialogue(dialogueLines);
            yield return new WaitUntil(() => !dialogueManager.DialogueActive);
        }

        // Показываем спиннер (плавно)
        if (loadingSpinner != null)
            loadingSpinner.Show();

        // 6. Ждём, пока сцена не загрузится
        while (asyncLoad.progress < 0.9f)
            yield return null;

        // 7. Активируем новую сцену
        asyncLoad.allowSceneActivation = true;

        // Скрываем спиннер (плавно)
        if (loadingSpinner != null)
            loadingSpinner.Hide();

        // 8. Плавно скрываем картинки
        yield return StartCoroutine(FadeOut(loadingImageCanvasGroup, loadingImage));
        yield return StartCoroutine(FadeOut(loadingBackgroundCanvasGroup, loadingBackground));

        // 9. Включаем звуки шагов обратно
        if (playerController != null && playerController.footstepSource != null)
            playerController.footstepSource.enabled = true;
    }

    private IEnumerator FadeIn(CanvasGroup canvasGroup, GameObject obj)
    {
        if (canvasGroup == null || obj == null) yield break;

        obj.SetActive(true);
        canvasGroup.alpha = 0f;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut(CanvasGroup canvasGroup, GameObject obj)
    {
        if (canvasGroup == null || obj == null) yield break;

        float timer = 0f;
        float startAlpha = canvasGroup.alpha;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        obj.SetActive(false);
    }
}
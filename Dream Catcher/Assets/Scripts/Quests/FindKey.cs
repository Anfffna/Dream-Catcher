using UnityEngine;
using System.Collections;

public class FindKey : MonoBehaviour, IInteractable
{
    [Header("Quest Settings")]
    public QuestUIManager questUIManager;
    public string questId = "find_the_key";

    [Header("Interaction")]
    public float fadeDuration = 0.5f;   // длительность плавного исчезновения

    [Header("Audio")]
    public AudioSource audioSource;    // сюда перетащи AudioSource (клип уже вставлен)

    private bool isAvailable = false;
    private bool isPickedUp = false;

    void Start()
    {
        // Изначально объект не интерактивен (слой Default)
        gameObject.layer = LayerMask.NameToLayer("Default");
        isAvailable = false;

        // Если AudioSource не назначен, пробуем найти на объекте
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Если ключ ещё не доступен, проверяем, активно ли задание
        if (!isAvailable && !isPickedUp && questUIManager != null)
        {
            if (questUIManager.IsQuestActive(questId))
            {
                EnableKey();
            }
        }
    }

    private void EnableKey()
    {
        isAvailable = true;
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        Debug.Log("Ключ теперь можно подобрать");
    }

    public void Interact()
    {
        if (!isAvailable || isPickedUp) return;

        // Воспроизводим звук подбора
        if (audioSource != null)
            audioSource.Play();

        StartCoroutine(PickupRoutine());
    }

    private IEnumerator PickupRoutine()
    {
        isAvailable = false;
        isPickedUp = true;

        // Блокируем повторное взаимодействие
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Плавное исчезновение
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Renderer rend = GetComponent<Renderer>();
        Color startColor = Color.white;
        if (rend != null) startColor = rend.material.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            // Уменьшаем масштаб
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (rend != null)
            {
                Color newColor = startColor;
                newColor.a = Mathf.Lerp(1f, 0f, t);
                rend.material.color = newColor;
            }
            yield return null;
        }

        transform.localScale = Vector3.zero;
        if (rend != null) rend.enabled = false;

        // Завершаем задание
        if (questUIManager != null)
            questUIManager.CompleteQuest(questId);

        // Уничтожаем объект (звук продолжит играть, даже если объект удалён)
        Destroy(gameObject, 0.1f);
    }
}
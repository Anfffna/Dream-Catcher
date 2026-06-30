using UnityEngine;
using System.Collections;
using System.Collections.Generic;  // для списка

public class FindKey : MonoBehaviour, IInteractable
{
    [Header("Quest Settings")]
    public QuestUIManager questUIManager;
    public string questId = "find_the_key";
    public string nextQuestId = "check_the_mailbox";

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string questUIManagerObjectName = "QuestUIManager";

    [Header("Interaction")]
    public float fadeDuration = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Appearance")]
    public float brightness = 0.9f; // Яркость (0 – чёрный, 1 – белый)

    // Список всех рендереров на этом объекте и его дочерних
    private List<Renderer> allRenderers = new List<Renderer>();
    // Список начальных цветов для каждого рендерера (чтобы при исчезновении знать, с какого цвета начинать)
    private List<Color> startColors = new List<Color>();

    private bool isAvailable = false;
    private bool isPickedUp = false;

    void Start()
    {
        FindReferences();
        // --- 1. Настройка слоя ---
        gameObject.layer = LayerMask.NameToLayer("Default");
        isAvailable = false;

        // --- 2. AudioSource ---
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // --- 3. Находим ВСЕ рендереры на этом объекте и всех дочерних ---
        allRenderers.Clear();
        allRenderers.AddRange(GetComponentsInChildren<Renderer>(true)); // true – включает неактивные

        // Если рендереров нет, выдаём предупреждение
        if (allRenderers.Count == 0)
        {
            Debug.LogWarning("На объекте и его дочерних элементах нет Renderer'ов!");
            return;
        }

        // --- 4. Устанавливаем цвет для каждого рендерера ---
        foreach (Renderer rend in allRenderers)
        {
            // Создаём новый цвет с нужной яркостью и полной непрозрачностью
            Color newColor = new Color(brightness, brightness, brightness, 1f);
            // Если у материала есть текстура, этот цвет умножится на неё – получится приглушённый оттенок
            rend.material.color = newColor;
        }
    }

    // Этот метод вызывается, когда задание становится активным
    private void EnableKey()
    {
        isAvailable = true;
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        Debug.Log("Ключ теперь можно подобрать");
    }

    void Update()
    {
        if (questUIManager == null)
            FindReferences();

        if (!isAvailable && !isPickedUp && questUIManager != null)
        {
            if (questUIManager.IsQuestActive(questId))
            {
                EnableKey();
            }
        }
    }

    public void Interact()
    {
        if (!isAvailable || isPickedUp) return;

        if (audioSource != null)
            audioSource.Play();

        StartCoroutine(PickupRoutine());
    }

    private IEnumerator PickupRoutine()
    {
        isAvailable = false;
        isPickedUp = true;
        gameObject.layer = LayerMask.NameToLayer("Default");

        // Запоминаем начальные масштабы (на случай, если у дочерних объектов свои масштабы – мы будем менять масштаб родителя, но можно и дочерние)
        Vector3 startScale = transform.localScale;

        // Запоминаем начальные цвета для каждого рендерера (на случай, если они уже были изменены)
        startColors.Clear();
        foreach (Renderer rend in allRenderers)
        {
            startColors.Add(rend.material.color);
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // Уменьшаем масштаб всего объекта (все дочерние уменьшатся пропорционально)
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Для каждого рендерера плавно уменьшаем прозрачность
            for (int i = 0; i < allRenderers.Count; i++)
            {
                Renderer rend = allRenderers[i];
                if (rend != null)
                {
                    Color currentColor = startColors[i];
                    currentColor.a = Mathf.Lerp(1f, 0f, t);
                    rend.material.color = currentColor;
                }
            }

            yield return null;
        }

        // Финализация
        transform.localScale = Vector3.zero;
        foreach (Renderer rend in allRenderers)
        {
            if (rend != null)
            {
                rend.enabled = false; // отключаем рендеринг на всякий случай
            }
        }

        FindReferences();

        // Завершаем задание
        if (questUIManager != null)
        {
            questUIManager.CompleteQuest(questId);

            if (!string.IsNullOrEmpty(nextQuestId))
                questUIManager.AddQuest(nextQuestId);
        }

        // Уничтожаем объект (звук продолжит играть)
        Destroy(gameObject, 0.1f);
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (questUIManager == null)
            questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
        {
            GameObject obj = GameObject.Find(questUIManagerObjectName);

            if (obj != null)
                questUIManager = obj.GetComponent<QuestUIManager>();
        }

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();
    }
}
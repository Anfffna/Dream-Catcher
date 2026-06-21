using UnityEngine;

public class ArchiveToggle : MonoBehaviour
{
    [Header("Archive")]
    public GameObject archiveScrollView;

    [Header("Quest UI")]
    public QuestUIManager questUIManager;

    [Header("Arrow")]
    public RectTransform arrowTransform;

    [Header("Open Arrow Offset")]
    public float openXOffset = -2.5f;

    private bool isOpen = false;
    private Vector2 closedArrowPosition;

    void Start()
    {
        // Если ссылка на менеджер не назначена, ищем автоматически
        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();

        if (questUIManager == null)
            Debug.LogWarning("QuestUIManager не найден! Архив не будет обновлять резюме.");

        if (arrowTransform != null)
        {
            closedArrowPosition = arrowTransform.anchoredPosition;

            arrowTransform.localEulerAngles = Vector3.zero;
            arrowTransform.anchoredPosition = closedArrowPosition;
        }

        if (archiveScrollView != null)
            archiveScrollView.SetActive(false);
    }

    public void ToggleArchive()
    {
        Debug.Log("ToggleArchive вызван на " + gameObject.name);

        isOpen = !isOpen;

        if (archiveScrollView != null)
            archiveScrollView.SetActive(isOpen);

        if (!isOpen && questUIManager != null)
            questUIManager.ClearSummaryIfFromArchive();

        if (arrowTransform != null)
        {
            arrowTransform.localEulerAngles = isOpen
                ? new Vector3(0f, 0f, -90f)
                : Vector3.zero;

            arrowTransform.anchoredPosition = isOpen
                ? closedArrowPosition + new Vector2(openXOffset, 0f)
                : closedArrowPosition;
        }
    }
}
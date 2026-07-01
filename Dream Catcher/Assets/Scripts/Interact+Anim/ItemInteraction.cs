using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(InteractionOutline))]
public class ItemInteraction : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> dialogueLines;

    [Header("One Time Outline")]
    [Tooltip("Уникальный ID предмета. Если пустой, будет взят outlineId из InteractionOutline.")]
    public string itemId;

    public bool hideOutlineAfterFirstInteraction = true;

    [Header("Auto Find")]
    public bool autoFindReferences = true;
    public string dialogueManagerObjectName = "DialogueManager";

    private InteractionOutline outline;
    private bool wasInspected = false;
    private static FirstInteractionHint hintManager;

    void Start()
    {
        outline = GetComponent<InteractionOutline>();

        ResolveItemId();
        FindReferences();

        if (hintManager == null)
            hintManager = FindObjectOfType<FirstInteractionHint>();

        RefreshInspectedState();
    }

    public void Interact()
    {
        FindReferences();
        ResolveItemId();

        if (dialogueManager != null && dialogueLines != null && dialogueLines.Count > 0)
        {
            // Запускаем диалог
            dialogueManager.StartDialogue(dialogueLines);

            // Помечаем предмет как осмотренный и скрываем обводку.
            MarkItemInspectedOnce();

            // Запускаем ожидание окончания диалога, затем показываем подсказку
            StartCoroutine(ShowHintAfterDialogue());
        }
        else
        {
            Debug.LogWarning("На " + gameObject.name + " не заданы диалоговые строки или DialogueManager", this);
        }
    }

    public void RefreshInspectedState()
    {
        ResolveItemId();

        if (string.IsNullOrEmpty(itemId))
            return;

        wasInspected = ItemInteractionState.IsInspected(itemId);

        if (wasInspected && hideOutlineAfterFirstInteraction)
            HideItemOutline();
    }

    private void MarkItemInspectedOnce()
    {
        ResolveItemId();

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("ItemInteraction: itemId пустой на объекте " + gameObject.name, this);
            return;
        }

        // Если предмет уже был осмотрен в сохранении — просто синхронизируем локальное состояние.
        if (ItemInteractionState.IsInspected(itemId))
        {
            wasInspected = true;

            if (hideOutlineAfterFirstInteraction)
                HideItemOutline();

            return;
        }

        if (wasInspected)
            return;

        wasInspected = true;
        ItemInteractionState.MarkInspected(itemId);

        if (hideOutlineAfterFirstInteraction)
            HideItemOutline();
    }

    private void HideItemOutline()
    {
        string outlineId = GetOutlineId();

        if (!string.IsNullOrEmpty(outlineId))
            InteractionOutlineRegistry.Hide(outlineId);

        if (outline != null)
            outline.HideOutline();
    }

    private string GetOutlineId()
    {
        if (outline != null && !string.IsNullOrEmpty(outline.outlineId))
            return outline.outlineId;

        return itemId;
    }

    private void ResolveItemId()
    {
        if (outline == null)
            outline = GetComponent<InteractionOutline>();

        if (string.IsNullOrEmpty(itemId) && outline != null)
            itemId = outline.outlineId;
    }

    private IEnumerator ShowHintAfterDialogue()
    {
        // Ждём, пока диалог не завершится
        while (dialogueManager != null && dialogueManager.DialogueActive)
            yield return null;

        // Показываем подсказку
        if (hintManager != null)
            hintManager.TryShowHint();
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        // DialogueManager — ВАЖНО: ищем именно обычный DialogueManager, не LoadingDialogueManager
        if (dialogueManager == null || dialogueManager.gameObject.name != dialogueManagerObjectName)
        {
            GameObject obj = GameObject.Find(dialogueManagerObjectName);

            if (obj != null)
                dialogueManager = obj.GetComponent<DialogueManager>();
        }

        // Запасной вариант: среди всех DialogueManager берём только тот, у которого имя DialogueManager
        if (dialogueManager == null)
        {
            DialogueManager[] managers = FindObjectsOfType<DialogueManager>();

            foreach (DialogueManager manager in managers)
            {
                if (manager.gameObject.name == dialogueManagerObjectName)
                {
                    dialogueManager = manager;
                    break;
                }
            }
        }
    }
}
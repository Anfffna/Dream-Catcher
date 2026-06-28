using UnityEngine;
using System.Collections.Generic;

public class HomeItemsActivator : MonoBehaviour
{
    [Header("Items to Activate")]
    public GameObject[] itemsToActivate; // сюда перетащи объекты с ItemInteraction и InteractionOutline

    public void ActivateItems()
    {
        foreach (GameObject item in itemsToActivate)
        {
            if (item == null) continue;

            // Меняем слой на Interactable
            item.layer = LayerMask.NameToLayer("Interactable");

            // Включаем обводку (если есть)
            InteractionOutline outline = item.GetComponent<InteractionOutline>();
            if (outline != null)
            {
                outline.ShowOutline();
            }
            else
            {
                // Если нет компонента, можно добавить или просто игнорировать
                Debug.LogWarning($"На объекте {item.name} нет InteractionOutline", item);
            }

            // Убедимся, что скрипт ItemInteraction есть и включён
            ItemInteraction interaction = item.GetComponent<ItemInteraction>();
            if (interaction != null)
            {
                interaction.enabled = true;
                // Можно также передать ссылку на диалог менеджер, если не задан в инспекторе
            }
        }
    }
}
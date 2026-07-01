using UnityEngine;

public class HomeItemsActivator : MonoBehaviour
{
    [Header("Items to Activate")]
    public GameObject[] itemsToActivate;

    [Header("Layer")]
    public string interactableLayerName = "Interactable";

    public void ActivateItems()
    {
        int interactableLayer = LayerMask.NameToLayer(interactableLayerName);

        for (int i = 0; i < itemsToActivate.Length; i++)
        {
            GameObject item = itemsToActivate[i];

            if (item == null)
                continue;

            item.SetActive(true);

            if (interactableLayer != -1)
                SetLayerRecursively(item, interactableLayer);

            ItemInteraction interaction = item.GetComponent<ItemInteraction>();

            if (interaction != null)
                interaction.enabled = true;
                interaction.RefreshInspectedState();

            InteractionOutline outline = item.GetComponent<InteractionOutline>();

            if (outline == null)
            {
                Debug.LogWarning("На объекте " + item.name + " нет InteractionOutline", item);
                continue;
            }

            string outlineId = outline.outlineId;

            if (string.IsNullOrEmpty(outlineId))
            {
                Debug.LogWarning("На объекте " + item.name + " пустой outlineId", item);
                continue;
            }

            if (ItemInteractionState.IsInspected(outlineId))
            {
                InteractionOutlineRegistry.Hide(outlineId);
                outline.HideOutline();
            }
            else
            {
                InteractionOutlineRegistry.Show(outlineId);
                outline.ForceRedrawOutline();
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            Transform child = obj.transform.GetChild(i);

            if (child != null)
                SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void DeactivateItems()
    {
        int defaultLayer = LayerMask.NameToLayer("Default");

        for (int i = 0; i < itemsToActivate.Length; i++)
        {
            GameObject item = itemsToActivate[i];

            if (item == null)
                continue;

            InteractionOutline outline = item.GetComponent<InteractionOutline>();

            if (outline != null)
            {
                string outlineId = outline.outlineId;

                if (!string.IsNullOrEmpty(outlineId))
                    InteractionOutlineRegistry.Hide(outlineId);

                outline.HideOutline();
            }

            ItemInteraction interaction = item.GetComponent<ItemInteraction>();

            if (interaction != null)
                interaction.enabled = false;

            if (defaultLayer != -1)
                SetLayerRecursively(item, defaultLayer);

            // Если эти предметы вообще не должны существовать до завершения света,
            // раскомментируй строку ниже:
            // item.SetActive(false);
        }
    }
}
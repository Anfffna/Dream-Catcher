using System.Collections.Generic;

public static class ItemInteractionState
{
    private static HashSet<string> inspectedItemIds = new HashSet<string>();

    public static void MarkInspected(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        inspectedItemIds.Add(itemId);
    }

    public static bool IsInspected(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && inspectedItemIds.Contains(itemId);
    }

    public static List<string> GetInspectedItemIds()
    {
        return new List<string>(inspectedItemIds);
    }

    public static void Restore(List<string> ids)
    {
        inspectedItemIds.Clear();

        if (ids == null)
            return;

        for (int i = 0; i < ids.Count; i++)
        {
            if (!string.IsNullOrEmpty(ids[i]))
                inspectedItemIds.Add(ids[i]);
        }
    }

    public static void Clear()
    {
        inspectedItemIds.Clear();
    }
}
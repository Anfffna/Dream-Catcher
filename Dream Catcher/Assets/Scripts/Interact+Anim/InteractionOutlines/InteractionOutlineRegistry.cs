using UnityEngine;
using System.Collections.Generic;

public static class InteractionOutlineRegistry
{
    private static Dictionary<string, List<InteractionOutline>> outlinesById =
        new Dictionary<string, List<InteractionOutline>>();

    private static HashSet<string> visibleIds = new HashSet<string>();

    public static void Register(string id, InteractionOutline outline)
    {
        if (string.IsNullOrEmpty(id) || outline == null)
            return;

        if (!outlinesById.ContainsKey(id))
            outlinesById[id] = new List<InteractionOutline>();

        if (!outlinesById[id].Contains(outline))
            outlinesById[id].Add(outline);

        if (visibleIds.Contains(id))
            outline.ForceRedrawOutline();
    }

    public static void Unregister(string id, InteractionOutline outline)
    {
        if (string.IsNullOrEmpty(id) || outline == null)
            return;

        if (!outlinesById.ContainsKey(id))
            return;

        outlinesById[id].Remove(outline);

        if (outlinesById[id].Count == 0)
            outlinesById.Remove(id);
    }

    public static void Show(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        visibleIds.Add(id);

        if (!outlinesById.ContainsKey(id))
            return;

        List<InteractionOutline> outlines = outlinesById[id];

        for (int i = 0; i < outlines.Count; i++)
        {
            if (outlines[i] != null)
                outlines[i].ForceRedrawOutline();
        }
    }

    public static void Hide(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        visibleIds.Remove(id);

        if (!outlinesById.ContainsKey(id))
            return;

        List<InteractionOutline> outlines = outlinesById[id];

        for (int i = 0; i < outlines.Count; i++)
        {
            if (outlines[i] != null)
                outlines[i].HideOutline();
        }
    }

    public static bool ShouldBeVisible(string id)
    {
        return !string.IsNullOrEmpty(id) && visibleIds.Contains(id);
    }

    public static void RedrawVisibleOutlines()
    {
        foreach (string id in visibleIds)
        {
            if (!outlinesById.ContainsKey(id))
                continue;

            List<InteractionOutline> outlines = outlinesById[id];

            for (int i = 0; i < outlines.Count; i++)
            {
                if (outlines[i] != null)
                    outlines[i].ForceRedrawOutline();
            }
        }
    }
}
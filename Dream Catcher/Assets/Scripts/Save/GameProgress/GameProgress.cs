using UnityEngine;
using System.Collections.Generic;

public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance { get; private set; }

    private HashSet<string> doneEvents = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void MarkEventDone(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("GameProgress: пустой eventId.");
            return;
        }

        doneEvents.Add(id);
    }

    public bool IsEventDone(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        return doneEvents.Contains(id);
    }

    public List<string> GetDoneEvents()
    {
        return new List<string>(doneEvents);
    }

    public void SetDoneEvents(List<string> ids)
    {
        doneEvents.Clear();

        if (ids == null)
            return;

        for (int i = 0; i < ids.Count; i++)
        {
            if (!string.IsNullOrEmpty(ids[i]))
                doneEvents.Add(ids[i]);
        }
    }

    public void ClearProgress()
    {
        doneEvents.Clear();
    }
}
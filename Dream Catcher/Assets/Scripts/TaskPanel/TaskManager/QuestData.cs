using UnityEngine;

[System.Serializable]
public class QuestData
{
    public string questId;

    public string title;

    [TextArea(3, 8)]
    public string description;

    public bool showInArchive = true;
}
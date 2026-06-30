using UnityEngine;

public enum QuestTag
{
    ֱûע,
    ׁ‏זוע
}

[System.Serializable]
public class QuestData
{
    [Header("ID")]
    public string questId;

    [Header("Tag")]
    public QuestTag tag;

    [Header("Text")]
    public string title;

    [TextArea(3, 8)]
    public string description;

    [Header("Quest Outline IDs")]
    public string[] outlineIds;
}
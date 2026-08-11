using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public string saveName;
    public string sceneName;

    public float posX, posY, posZ;

    public string dateTime;

    // Активные задания
    public List<string> activeQuestIds = new List<string>();

    // Завершённые задания
    public List<string> completedQuestIds = new List<string>();

    public List<string> inspectedItemIds = new List<string>(); //сохранение взаимодействия с не квестовыми объектами

    // ХАРАКТЕРИСТИКИ ИГРОКА
    // 0 = старый сейв без характеристик.
    // 1 = сохранён рассудок.
    public int playerStatsVersion = 0;
    public int sanity = 100;
    public int experience = 0;
}
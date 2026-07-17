using UnityEngine;

public class QuestDescriptionImagesController : MonoBehaviour
{
    [System.Serializable]
    public class QuestImageGroup
    {
        public string questId;
        public GameObject imagesRoot;
    }

    [Header("Quest Image Groups")]
    public QuestImageGroup[] questImageGroups;

    public void ShowImagesForQuest(string questId)
    {
        HideAllImages();

        if (string.IsNullOrEmpty(questId))
            return;

        if (questImageGroups == null)
            return;

        for (int i = 0; i < questImageGroups.Length; i++)
        {
            QuestImageGroup group = questImageGroups[i];

            if (group == null)
                continue;

            if (group.imagesRoot == null)
                continue;

            if (group.questId == questId)
            {
                group.imagesRoot.SetActive(true);
                return;
            }
        }
    }

    public void HideAllImages()
    {
        if (questImageGroups == null)
            return;

        for (int i = 0; i < questImageGroups.Length; i++)
        {
            QuestImageGroup group = questImageGroups[i];

            if (group == null)
                continue;

            if (group.imagesRoot != null)
                group.imagesRoot.SetActive(false);
        }
    }
}
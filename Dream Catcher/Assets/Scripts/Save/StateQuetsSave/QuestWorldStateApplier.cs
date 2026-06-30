using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class QuestWorldStateApplier : MonoBehaviour
{
    public enum QuestState
    {
        NotStarted,
        Active,
        Completed
    }

    [Header("Quest")]
    public string questId;

    [Header("Timing")]
    public bool applyOnStart = true;
    public bool applyOneFrameLater = true;

    [Header("When NOT STARTED")]
    public GameObject[] enableWhenNotStarted;
    public GameObject[] disableWhenNotStarted;
    public Behaviour[] enableBehavioursWhenNotStarted;
    public Behaviour[] disableBehavioursWhenNotStarted;
    public Collider[] enableCollidersWhenNotStarted;
    public Collider[] disableCollidersWhenNotStarted;
    public UnityEvent onNotStarted;

    [Header("When ACTIVE")]
    public GameObject[] enableWhenActive;
    public GameObject[] disableWhenActive;
    public Behaviour[] enableBehavioursWhenActive;
    public Behaviour[] disableBehavioursWhenActive;
    public Collider[] enableCollidersWhenActive;
    public Collider[] disableCollidersWhenActive;
    public UnityEvent onActive;

    [Header("When COMPLETED")]
    public GameObject[] enableWhenCompleted;
    public GameObject[] disableWhenCompleted;
    public Behaviour[] enableBehavioursWhenCompleted;
    public Behaviour[] disableBehavioursWhenCompleted;
    public Collider[] enableCollidersWhenCompleted;
    public Collider[] disableCollidersWhenCompleted;
    public UnityEvent onCompleted;

    private void Start()
    {
        if (!applyOnStart)
            return;

        if (applyOneFrameLater)
            StartCoroutine(ApplyNextFrame());
        else
            Apply();
    }

    private IEnumerator ApplyNextFrame()
    {
        yield return null;
        Apply();
    }

    public void Apply()
    {
        QuestState state = GetQuestState();

        switch (state)
        {
            case QuestState.NotStarted:
                ApplyNotStarted();
                break;

            case QuestState.Active:
                ApplyActive();
                break;

            case QuestState.Completed:
                ApplyCompleted();
                break;
        }
    }

    private QuestState GetQuestState()
    {
        if (string.IsNullOrEmpty(questId))
        {
            Debug.LogWarning("QuestWorldStateApplier: questId пустой на объекте " + gameObject.name);
            return QuestState.NotStarted;
        }

        QuestUIManager questManager = QuestUIManager.Instance;

        if (questManager == null)
            questManager = FindObjectOfType<QuestUIManager>();

        if (questManager == null)
        {
            Debug.LogWarning("QuestWorldStateApplier: QuestUIManager не найден на объекте " + gameObject.name);
            return QuestState.NotStarted;
        }

        // Completed важнее Active.
        // Если вдруг из-за ошибки questId есть и там, и там,
        // считаем его завершённым.
        if (questManager.IsQuestCompleted(questId))
            return QuestState.Completed;

        if (questManager.IsQuestActive(questId))
            return QuestState.Active;

        return QuestState.NotStarted;
    }

    private void ApplyNotStarted()
    {
        SetGameObjects(enableWhenNotStarted, true);
        SetGameObjects(disableWhenNotStarted, false);

        SetBehaviours(enableBehavioursWhenNotStarted, true);
        SetBehaviours(disableBehavioursWhenNotStarted, false);

        SetColliders(enableCollidersWhenNotStarted, true);
        SetColliders(disableCollidersWhenNotStarted, false);

        onNotStarted?.Invoke();
    }

    private void ApplyActive()
    {
        SetGameObjects(enableWhenActive, true);
        SetGameObjects(disableWhenActive, false);

        SetBehaviours(enableBehavioursWhenActive, true);
        SetBehaviours(disableBehavioursWhenActive, false);

        SetColliders(enableCollidersWhenActive, true);
        SetColliders(disableCollidersWhenActive, false);

        onActive?.Invoke();
    }

    private void ApplyCompleted()
    {
        SetGameObjects(enableWhenCompleted, true);
        SetGameObjects(disableWhenCompleted, false);

        SetBehaviours(enableBehavioursWhenCompleted, true);
        SetBehaviours(disableBehavioursWhenCompleted, false);

        SetColliders(enableCollidersWhenCompleted, true);
        SetColliders(disableCollidersWhenCompleted, false);

        onCompleted?.Invoke();
    }

    private void SetGameObjects(GameObject[] objects, bool state)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(state);
        }
    }

    private void SetBehaviours(Behaviour[] behaviours, bool state)
    {
        if (behaviours == null)
            return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = state;
        }
    }

    private void SetColliders(Collider[] colliders, bool state)
    {
        if (colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = state;
        }
    }

    public static void ApplyAllInScene()
    {
        QuestWorldStateApplier[] appliers = FindObjectsOfType<QuestWorldStateApplier>(true);

        for (int i = 0; i < appliers.Length; i++)
        {
            if (appliers[i] != null)
                appliers[i].Apply();
        }
    }
}
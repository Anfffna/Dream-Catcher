using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Lights")]
    public Light roomLight1;
    public Light roomLight2;


    [Header("Lamp — Material Switch")]

    [Tooltip(
        "Renderer объекта лампы, " +
        "у которого нужно менять материал."
    )]
    [SerializeField]
    private Renderer lampRenderer;

    [Tooltip(
        "Материал лампы, когда свет включён."
    )]
    [SerializeField]
    private Material lampOnMaterial;

    [Tooltip(
        "Материал лампы, когда свет выключен."
    )]
    [SerializeField]
    private Material lampOffMaterial;


    [Header("Quest")]
    public QuestUIManager questUIManager;
    public string questIdToComplete = "turn_on_light";


    [Header("Outline")]
    public string outlineIdToHideAfterFirstInteraction = "obj_light_switch";
    public bool hideOutlineAfterFirstSuccessfulInteraction = true;


    [Header("Invite Door")]
    public InviteDoor inviteDoor;


    [Header("Audio")]
    public AudioSource audioSource;


    private bool isOn = false;
    private bool sequenceStarted = false;
    private bool outlineHiddenAfterInteraction = false;


    void Start()
    {
        if (roomLight1 != null)
            isOn = roomLight1.enabled;
        else if (roomLight2 != null)
            isOn = roomLight2.enabled;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        FindQuestManager();

        // Синхронизируем материал лампы
        // с исходным состоянием света.
        SetLampMaterial(isOn);
    }


    public void Interact()
    {
        isOn = !isOn;

        if (roomLight1 != null)
            roomLight1.enabled = isOn;

        if (roomLight2 != null)
            roomLight2.enabled = isOn;

        // Материал лампы переключается
        // одновременно со светом.
        SetLampMaterial(isOn);

        if (audioSource != null)
            audioSource.Play();

        // Если игрок выключил свет — цепочку не запускаем.
        if (!isOn)
            return;

        // Если цепочка уже запускалась — второй раз не запускаем.
        if (sequenceStarted)
            return;

        FindQuestManager();

        // Запускаем дверную цепочку только если задание реально активно.
        if (questUIManager != null &&
            !questUIManager.IsQuestActive(questIdToComplete))
            return;

        // ВАЖНО:
        // Квест ещё НЕ завершаем,
        // но обводку света уже убираем,
        // потому что игрок сделал нужное первое действие.
        HideLightOutlineAfterFirstInteraction();

        if (questUIManager != null)
            questUIManager.HideActiveQuestVisual(questIdToComplete);

        sequenceStarted = true;

        if (inviteDoor != null)
            inviteDoor.StartInviteDoorSequence();
        else
            Debug.LogWarning("InviteDoor не назначен в LightSwitch");
    }


    // =====================================================
    // ЛАМПА — ЗАМЕНА МАТЕРИАЛА
    // =====================================================

    private void SetLampMaterial(bool enabled)
    {
        if (lampRenderer == null)
            return;

        Material targetMaterial =
            enabled
                ? lampOnMaterial
                : lampOffMaterial;

        if (targetMaterial == null)
            return;

        lampRenderer.sharedMaterial =
            targetMaterial;
    }


    // =====================================================
    // OUTLINE
    // =====================================================

    private void HideLightOutlineAfterFirstInteraction()
    {
        if (!hideOutlineAfterFirstSuccessfulInteraction)
            return;

        if (outlineHiddenAfterInteraction)
            return;

        if (string.IsNullOrEmpty(outlineIdToHideAfterFirstInteraction))
            return;

        InteractionOutlineRegistry.Hide(
            outlineIdToHideAfterFirstInteraction
        );

        outlineHiddenAfterInteraction = true;
    }


    // =====================================================
    // QUEST MANAGER
    // =====================================================

    private void FindQuestManager()
    {
        if (questUIManager == null)
            questUIManager = QuestUIManager.Instance;

        if (questUIManager == null)
            questUIManager = FindObjectOfType<QuestUIManager>();
    }


    // =====================================================
    // RESET
    // =====================================================

    // Можно вызвать из QuestWorldStateApplier,
    // если хочешь жёстко сбрасывать свет
    // при восстановлении active-состояния задания.
    public void ResetForQuestStart()
    {
        sequenceStarted = false;
        outlineHiddenAfterInteraction = false;

        isOn = false;

        if (roomLight1 != null)
            roomLight1.enabled = false;

        if (roomLight2 != null)
            roomLight2.enabled = false;

        // При сбросе задания возвращаем
        // материал выключенной лампы.
        SetLampMaterial(false);
    }
}
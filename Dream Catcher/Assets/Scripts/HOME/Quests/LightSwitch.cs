using UnityEngine;

public class LightSwitch :
    MonoBehaviour,
    IInteractable
{
    [Header("Lights")]

    public Light roomLight1;
    public Light roomLight2;


    [Header("Lamp — Emission")]

    [Tooltip(
        "Renderer лампы. " +
        "Цвет Emission берётся из её основного материала автоматически."
    )]
    [SerializeField]
    private Renderer lampRenderer;


    [Header("Quest")]

    public QuestUIManager questUIManager;

    public string questIdToComplete =
        "turn_on_light";


    [Header("Outline")]

    public string outlineIdToHideAfterFirstInteraction =
        "obj_light_switch";

    public bool hideOutlineAfterFirstSuccessfulInteraction =
        true;


    [Header("Invite Door")]

    public InviteDoor inviteDoor;


    [Header("Audio")]

    public AudioSource audioSource;


    private bool isOn = false;

    private bool sequenceStarted = false;

    private bool outlineHiddenAfterInteraction =
        false;


    private MaterialPropertyBlock
        lampPropertyBlock;

    private Color originalEmissionColor =
        Color.white;


    private static readonly int
        EmissionColorId =
            Shader.PropertyToID(
                "_EmissionColor"
            );


    private void Awake()
    {
        lampPropertyBlock =
            new MaterialPropertyBlock();

        RememberEmissionColor();
    }


    private void Start()
    {
        if (roomLight1 != null)
        {
            isOn =
                roomLight1.enabled;
        }
        else if (roomLight2 != null)
        {
            isOn =
                roomLight2.enabled;
        }

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        FindQuestManager();

        // Синхронизируем Emission лампы
        // с исходным состоянием света.
        SetLampEmission(isOn);
    }


    public void Interact()
    {
        isOn = !isOn;

        if (roomLight1 != null)
        {
            roomLight1.enabled =
                isOn;
        }

        if (roomLight2 != null)
        {
            roomLight2.enabled =
                isOn;
        }

        // Материал не заменяется.
        // Меняется только Emission
        // конкретного Renderer.
        SetLampEmission(isOn);

        if (audioSource != null)
        {
            audioSource.Play();
        }

        // Если игрок выключил свет —
        // цепочку не запускаем.
        if (!isOn)
            return;

        // Если цепочка уже запускалась —
        // второй раз не запускаем.
        if (sequenceStarted)
            return;

        FindQuestManager();

        // Запускаем дверную цепочку
        // только если задание реально активно.
        if (questUIManager != null &&
            !questUIManager.IsQuestActive(
                questIdToComplete))
        {
            return;
        }

        // Квест ещё НЕ завершаем,
        // но обводку света уже убираем,
        // потому что игрок сделал
        // нужное первое действие.
        HideLightOutlineAfterFirstInteraction();

        if (questUIManager != null)
        {
            questUIManager.HideActiveQuestVisual(
                questIdToComplete
            );
        }

        sequenceStarted = true;

        if (inviteDoor != null)
        {
            inviteDoor.StartInviteDoorSequence();
        }
        else
        {
            Debug.LogWarning(
                "InviteDoor не назначен в LightSwitch"
            );
        }
    }


    // =====================================================
    // ЛАМПА — EMISSION
    // =====================================================

    private void RememberEmissionColor()
    {
        if (lampRenderer == null)
            return;

        Material material =
            lampRenderer.sharedMaterial;

        if (material == null)
            return;

        if (!material.HasProperty(
                EmissionColorId))
        {
            return;
        }

        // Запоминаем исходный HDR Emission Color
        // непосредственно из материала.
        originalEmissionColor =
            material.GetColor(
                EmissionColorId
            );
    }


    private void SetLampEmission(
        bool enabled)
    {
        if (lampRenderer == null)
            return;

        if (lampPropertyBlock == null)
        {
            lampPropertyBlock =
                new MaterialPropertyBlock();
        }

        lampRenderer.GetPropertyBlock(
            lampPropertyBlock
        );

        lampPropertyBlock.SetColor(
            EmissionColorId,
            enabled
                ? originalEmissionColor
                : Color.black
        );

        lampRenderer.SetPropertyBlock(
            lampPropertyBlock
        );
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

        if (string.IsNullOrEmpty(
                outlineIdToHideAfterFirstInteraction))
        {
            return;
        }

        InteractionOutlineRegistry.Hide(
            outlineIdToHideAfterFirstInteraction
        );

        outlineHiddenAfterInteraction =
            true;
    }


    // =====================================================
    // QUEST MANAGER
    // =====================================================

    private void FindQuestManager()
    {
        if (questUIManager == null)
        {
            questUIManager =
                QuestUIManager.Instance;
        }

        if (questUIManager == null)
        {
            questUIManager =
                FindObjectOfType<QuestUIManager>();
        }
    }


    // =====================================================
    // RESET
    // =====================================================

    // Можно вызвать из QuestWorldStateApplier,
    // если нужно жёстко сбрасывать свет
    // при восстановлении active-состояния задания.
    public void ResetForQuestStart()
    {
        sequenceStarted = false;

        outlineHiddenAfterInteraction =
            false;

        isOn = false;

        if (roomLight1 != null)
        {
            roomLight1.enabled =
                false;
        }

        if (roomLight2 != null)
        {
            roomLight2.enabled =
                false;
        }

        // Материал не меняем.
        // Просто визуально гасим Emission
        // конкретной лампы.
        SetLampEmission(false);
    }
}
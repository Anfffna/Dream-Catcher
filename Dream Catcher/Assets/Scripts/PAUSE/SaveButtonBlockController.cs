using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveButtonBlockController :
    MonoBehaviour
{
    [Header("Кнопка сохранения")]

    [Tooltip(
        "Кнопка «Сохранить». " +
        "Если пусто — будет найдена на этом объекте."
    )]
    [SerializeField]
    private Button saveButton;

    [Tooltip(
        "Текст кнопки «Сохранить»."
    )]
    [SerializeField]
    private TMP_Text saveButtonText;

    [Tooltip(
        "IndicatorHover этой кнопки."
    )]
    [SerializeField]
    private IndicatorHover indicatorHover;

    [Header("Заблокированный вид")]

    [Tooltip(
        "Цвет текста, когда сохранение недоступно."
    )]
    [SerializeField]
    private Color blockedTextColor =
        new Color32(
            62,
            62,
            62,
            255
        );

    [Header("Блокировка во время работы")]

    [Tooltip(
        "Запрещать сохранение, пока игрок находится " +
        "в рабочем режиме."
    )]
    [SerializeField]
    private bool blockDuringWork =
        true;

    private Color normalTextColor;

    private bool temporaryBlock;
    private bool currentlyBlocked;

    public bool IsBlocked =>
        currentlyBlocked;

    private void Awake()
    {
        FindReferences();

        if (saveButtonText != null)
        {
            normalTextColor =
                saveButtonText.color;
        }

        ApplyBlockedState(
            false,
            true
        );
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded +=
            HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -=
            HandleSceneLoaded;
    }

    private void Update()
    {
        RefreshState();
    }

    private void RefreshState()
    {
        bool blockedByWork =
            blockDuringWork &&
            WorkSessionManager.Instance != null &&
            WorkSessionManager.Instance
                .IsWorkModeActive;

        bool shouldBeBlocked =
            blockedByWork ||
            temporaryBlock;

        ApplyBlockedState(
            shouldBeBlocked
        );
    }

    private void ApplyBlockedState(
        bool blocked,
        bool force = false)
    {
        if (!force &&
            currentlyBlocked == blocked)
        {
            return;
        }

        currentlyBlocked =
            blocked;

        if (saveButton != null)
        {
            saveButton.interactable =
                !blocked;
        }

        if (saveButtonText != null)
        {
            saveButtonText.color =
                blocked
                    ? blockedTextColor
                    : normalTextColor;
        }

        if (indicatorHover != null)
        {
            indicatorHover
                .SetInteractionEnabled(
                    !blocked
                );
        }
    }

    // =====================================================
    // ДОПОЛНИТЕЛЬНАЯ ВРЕМЕННАЯ БЛОКИРОВКА
    // =====================================================

    /// <summary>
    /// Можно использовать позже из другой механики,
    /// если сохранение временно нужно запретить.
    /// </summary>
    public void SetTemporaryBlock(
        bool blocked)
    {
        temporaryBlock =
            blocked;

        RefreshState();
    }

    public void BlockTemporarily()
    {
        SetTemporaryBlock(
            true
        );
    }

    public void UnblockTemporarily()
    {
        SetTemporaryBlock(
            false
        );
    }

    // =====================================================
    // СМЕНА СЦЕНЫ
    // =====================================================

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        // Любая временная блокировка,
        // оставшаяся от старой сцены,
        // не переносится дальше.
        temporaryBlock =
            false;

        RefreshState();
    }

    // =====================================================
    // ПОИСК
    // =====================================================

    private void FindReferences()
    {
        if (saveButton == null)
        {
            saveButton =
                GetComponent<Button>();
        }

        if (saveButtonText == null)
        {
            saveButtonText =
                GetComponentInChildren
                    <TMP_Text>(true);
        }

        if (indicatorHover == null)
        {
            indicatorHover =
                GetComponent<IndicatorHover>();

            if (indicatorHover == null)
            {
                indicatorHover =
                    GetComponentInChildren
                        <IndicatorHover>(true);
            }
        }
    }
}
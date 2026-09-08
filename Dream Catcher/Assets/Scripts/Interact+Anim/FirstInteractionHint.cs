using UnityEngine;

public class FirstInteractionHint :
    MonoBehaviour
{
    [Header("Общая UI-плашка")]

    [Tooltip(
        "Универсальный ClickInteractionHint, " +
        "который управляет самой плашкой."
    )]
    [SerializeField]
    private ClickInteractionHint interactionHint;


    [Header("Текст этой подсказки")]

    [TextArea(2, 5)]
    [SerializeField]
    private string hintText =
        "Удерживайте Z, чтобы закрыть подсказку.";


    [Header("Первый показ")]

    [SerializeField]
    private bool showOnlyOnce =
        true;


    [Header("Закрытие")]

    [Tooltip(
        "Клавиша, которую нужно удерживать."
    )]
    [SerializeField]
    private KeyCode hideKey =
        KeyCode.Z;

    [Tooltip(
        "Сколько секунд нужно удерживать клавишу."
    )]
    [SerializeField]
    private float holdDurationToHide =
        1f;


    private bool hasBeenShown;
    private bool isWaitingForHide;

    private float holdTimer;


    private void Update()
    {
        if (!isWaitingForHide)
            return;


        if (interactionHint == null ||
            !interactionHint.IsVisible)
        {
            isWaitingForHide = false;
            holdTimer = 0f;

            enabled = false;

            return;
        }


        if (Input.GetKey(hideKey))
        {
            holdTimer +=
                Time.deltaTime;


            if (holdTimer >=
                holdDurationToHide)
            {
                interactionHint.Hide();

                isWaitingForHide =
                    false;

                holdTimer =
                    0f;

                enabled =
                    false;
            }
        }
        else
        {
            holdTimer =
                0f;
        }
    }


    public void TryShowHint()
    {
        if (interactionHint == null)
            return;


        if (showOnlyOnce &&
            hasBeenShown)
        {
            return;
        }


        hasBeenShown =
            true;

        holdTimer =
            0f;

        isWaitingForHide =
            true;


        /*
         * FALSE:
         * эта конкретная подсказка
         * НЕ закрывается ЛКМ.
         */
        interactionHint.Show(
            hintText,
            false
        );


        /*
         * Update нужен только пока
         * ждём удержание Z.
         */
        enabled =
            true;
    }


    public void ResetHint()
    {
        hasBeenShown =
            false;
    }


    private void Awake()
    {
        enabled =
            false;
    }


    private void OnValidate()
    {
        holdDurationToHide =
            Mathf.Max(
                0f,
                holdDurationToHide
            );
    }
}
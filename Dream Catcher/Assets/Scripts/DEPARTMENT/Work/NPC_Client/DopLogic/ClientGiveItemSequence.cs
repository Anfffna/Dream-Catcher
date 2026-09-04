using System.Collections;
using UnityEngine;

public class ClientGiveItemSequence :
    MonoBehaviour
{
    [Header("Клиент")]

    [SerializeField]
    private ClientNPCController clientNPC;

    [SerializeField]
    private Animator animator;


    [Header("Анимация передачи")]

    [Tooltip(
        "Имя Trigger и Animator State. " +
        "Например Give."
    )]
    [SerializeField]
    private string giveAnimationName =
        "Give";

    [SerializeField]
    private int animatorLayerIndex;

    [Tooltip(
        "Максимальное время ожидания " +
        "реального запуска Give."
    )]
    [SerializeField]
    private float animationStartTimeout =
        2f;

    [Tooltip(
        "На каком этапе Give предмет " +
        "отцепляется от руки NPC."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float detachNormalizedTime =
        0.65f;


    [Header("Предмет")]

    [Tooltip(
        "Предмет, который изначально выключен " +
        "и находится в руке NPC."
    )]
    [SerializeField]
    private DeskCarryItemController giftItem;

    [Tooltip(
        "Родитель предмета после отсоединения " +
        "от руки NPC."
    )]
    [SerializeField]
    private Transform releasedItemsRoot;

    [Tooltip(
        "Необязательная точка, куда предмет " +
        "ставится после отсоединения. " +
        "Если пусто — останется в месте руки."
    )]
    [SerializeField]
    private Transform presentationPoint;


    [Header("Финал клиента")]

    [Tooltip(
        "Не позволять Take_SON3 перебить " +
        "анимацию Give."
    )]
    [SerializeField]
    private bool blockFinalCompletion =
        true;


    private Coroutine sequenceCoroutine;

    private bool played;
    private bool finalBlockApplied;


    private void Awake()
    {
        if (clientNPC == null)
        {
            clientNPC =
                GetComponent<ClientNPCController>();
        }

        if (animator == null)
        {
            animator =
                GetComponent<Animator>();

            if (animator == null)
            {
                animator =
                    GetComponentInChildren
                        <Animator>(true);
            }
        }
    }


    public void Play()
    {
        if (played ||
            sequenceCoroutine != null)
        {
            return;
        }

        played = true;


        if (blockFinalCompletion &&
            clientNPC != null)
        {
            clientNPC.BlockFinalCompletion(
                this
            );

            finalBlockApplied = true;
        }


        if (animator == null ||
            string.IsNullOrWhiteSpace(
                giveAnimationName))
        {
            ShowAndReleaseGift();

            ReleaseFinalBlock();

            return;
        }


        animator.ResetTrigger(
            giveAnimationName
        );

        animator.SetTrigger(
            giveAnimationName
        );


        sequenceCoroutine =
            StartCoroutine(
                GiveRoutine()
            );
    }


    private IEnumerator GiveRoutine()
    {
        int stateHash =
            Animator.StringToHash(
                giveAnimationName
            );

        float elapsed = 0f;

        bool enteredState = false;
        bool giftShown = false;
        bool giftReleased = false;


        // =====================================================
        // ЖДЁМ РЕАЛЬНОГО НАЧАЛА GIVE
        // =====================================================

        while (elapsed <
               animationStartTimeout)
        {
            AnimatorStateInfo currentState =
                animator
                    .GetCurrentAnimatorStateInfo(
                        animatorLayerIndex
                    );

            AnimatorStateInfo nextState =
                animator
                    .GetNextAnimatorStateInfo(
                        animatorLayerIndex
                    );


            bool currentMatches =
                currentState.shortNameHash ==
                    stateHash ||
                currentState.IsName(
                    giveAnimationName
                );

            bool nextMatches =
                nextState.shortNameHash ==
                    stateHash ||
                nextState.IsName(
                    giveAnimationName
                );


            if (currentMatches ||
                nextMatches)
            {
                enteredState = true;
                break;
            }


            elapsed +=
                Time.deltaTime;

            yield return null;
        }


        if (!enteredState)
        {
            ShowAndReleaseGift();

            sequenceCoroutine = null;

            ReleaseFinalBlock();

            yield break;
        }


        // =====================================================
        // GIVE РЕАЛЬНО НАЧАЛАСЬ
        // =====================================================

        /*
         * Именно здесь предмет становится видимым.
         *
         * Он всё ещё ребёнок руки NPC,
         * поэтому дальше автоматически едет
         * вместе с анимацией руки.
         */
        if (giftItem != null)
        {
            giftItem.ShowInHand();

            giftShown = true;
        }


        // =====================================================
        // ЖДЁМ МОМЕНТ ОТПУСКАНИЯ
        // =====================================================

        while (true)
        {
            AnimatorStateInfo stateInfo =
                animator
                    .GetCurrentAnimatorStateInfo(
                        animatorLayerIndex
                    );

            bool isGiveState =
                stateInfo.shortNameHash ==
                    stateHash ||
                stateInfo.IsName(
                    giveAnimationName
                );

            bool isTransitioning =
                animator.IsInTransition(
                    animatorLayerIndex
                );


            if (isGiveState &&
                !giftReleased &&
                stateInfo.normalizedTime >=
                    detachNormalizedTime)
            {
                giftReleased = true;

                if (giftItem != null)
                {
                    giftItem.ReleaseFromAnimation(
                        releasedItemsRoot,
                        presentationPoint
                    );
                }
            }


            if (isGiveState &&
                stateInfo.normalizedTime >= 1f &&
                !isTransitioning)
            {
                break;
            }


            if (!isGiveState &&
                !isTransitioning)
            {
                break;
            }


            yield return null;
        }


        /*
         * Защита на случай очень короткой
         * или странно настроенной анимации.
         */
        if (!giftShown &&
            giftItem != null)
        {
            giftItem.ShowInHand();
        }


        if (!giftReleased &&
            giftItem != null)
        {
            giftItem.ReleaseFromAnimation(
                releasedItemsRoot,
                presentationPoint
            );
        }


        sequenceCoroutine = null;

        ReleaseFinalBlock();
    }


    private void ShowAndReleaseGift()
    {
        if (giftItem == null)
            return;

        giftItem.ShowInHand();

        giftItem.ReleaseFromAnimation(
            releasedItemsRoot,
            presentationPoint
        );
    }


    private void ReleaseFinalBlock()
    {
        if (!finalBlockApplied)
            return;

        if (clientNPC != null)
        {
            clientNPC.ReleaseFinalCompletion(
                this
            );
        }

        finalBlockApplied = false;
    }


    private void OnDisable()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(
                sequenceCoroutine
            );

            sequenceCoroutine = null;
        }

        ReleaseFinalBlock();
    }


    private void OnValidate()
    {
        animationStartTimeout =
            Mathf.Max(
                0f,
                animationStartTimeout
            );
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InviteDoor : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip knockClip;
    public bool loopKnock = true;

    [Header("Timing")]
    public float delayAfterLight = 3f;
    public float delayAfterKnock = 1.5f;

    [Header("Dialogue")]
    public DialogueManager dialogueManager;
    public List<DialogueManager.DialogueLine> ggLines = new List<DialogueManager.DialogueLine>();

    [Header("Door")]
    public InviteDoorInteractable doorInteractable;

    private bool started = false;

    public void StartInviteDoorSequence()
    {
        if (started) return;

        started = true;
        StartCoroutine(InviteRoutine());
    }

    private IEnumerator InviteRoutine()
    {
        if (doorInteractable != null)
            doorInteractable.SetDoorAvailable(false);

        yield return new WaitForSeconds(delayAfterLight);

        StartKnock();

        yield return new WaitForSeconds(delayAfterKnock);

        if (dialogueManager != null && ggLines != null && ggLines.Count > 0)
        {
            dialogueManager.StartDialogue(ggLines);

            yield return new WaitUntil(() => dialogueManager.DialogueActive == false);
        }

        if (doorInteractable != null)
            doorInteractable.SetDoorAvailable(true);
    }

    private void StartKnock()
    {
        if (audioSource == null || knockClip == null) return;

        audioSource.clip = knockClip;
        audioSource.loop = loopKnock;
        audioSource.Play();
    }

    public void StopKnock()
    {
        if (audioSource == null) return;

        audioSource.Stop();
        audioSource.loop = false;
    }
}
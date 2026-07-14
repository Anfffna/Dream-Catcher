using UnityEngine;
using System.Collections;

public class ScenePlayerSettings : MonoBehaviour
{
    [Header("Player Speed In This Scene")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;

    [Header("Apply Settings")]
    public bool applyOnStart = true;
    public int waitFramesBeforeApply = 3;

    private void Start()
    {
        if (applyOnStart)
            StartCoroutine(ApplyAfterSceneReady());
    }

    private IEnumerator ApplyAfterSceneReady()
    {
        for (int i = 0; i < waitFramesBeforeApply; i++)
            yield return null;

        ApplySettings();
    }

    [ContextMenu("Apply Settings Now")]
    public void ApplySettings()
    {
        PlayerController player = FindRealPlayer();

        if (player == null)
        {
            Debug.LogWarning("ScenePlayerSettings: PlayerController не найден.");
            return;
        }

        player.walkSpeed = walkSpeed;
        player.runSpeed = runSpeed;

        Debug.Log(
            "ScenePlayerSettings применил скорость к: " + player.gameObject.name +
            " | scene: " + player.gameObject.scene.name +
            " | walkSpeed = " + player.walkSpeed +
            " | runSpeed = " + player.runSpeed,
            player
        );
    }

    private PlayerController FindRealPlayer()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>(true);

        PlayerController activePlayer = null;

        for (int i = 0; i < players.Length; i++)
        {
            PlayerController player = players[i];

            if (player == null)
                continue;

            if (!player.gameObject.activeInHierarchy)
                continue;

            // Берём активного игрока. После нескольких кадров дубликат уже должен быть удалён.
            activePlayer = player;
        }

        return activePlayer;
    }
}
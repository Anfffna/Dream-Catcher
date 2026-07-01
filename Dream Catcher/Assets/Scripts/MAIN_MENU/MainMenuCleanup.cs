using UnityEngine;

public class MainMenuCleanup : MonoBehaviour
{
    private void Start()
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>(true);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
                Destroy(players[i].gameObject);
        }
    }
}
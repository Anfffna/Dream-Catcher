using UnityEngine;
using System.Collections.Generic;

public class CorridorLightController : MonoBehaviour
{
    [Header("Коридор")]

    [Tooltip("Trigger-коллайдер, который покрывает область коридора.")]
    [SerializeField]
    private Collider corridorTrigger;


    [Header("Свет")]

    [Tooltip("Свет, который должен гореть только пока игрок находится в коридоре.")]
    [SerializeField]
    private Light corridorLight;


    [Header("Игрок")]

    [SerializeField]
    private string playerTag = "Player";


    // Храним коллайдеры игрока внутри зоны.
    // Это защищает от ситуации, когда у игрока несколько Collider.
    private readonly HashSet<Collider> playerCollidersInside =
        new HashSet<Collider>();


    private void Awake()
    {
        if (corridorLight != null)
            corridorLight.enabled = false;

        if (corridorTrigger == null)
        {
            Debug.LogWarning(
                "CorridorLightController: не назначен Corridor Trigger.",
                this
            );

            return;
        }

        corridorTrigger.isTrigger = true;

        // Автоматически добавляем обработчик на сам Trigger.
        CorridorLightTriggerRelay relay =
            corridorTrigger.GetComponent<CorridorLightTriggerRelay>();

        if (relay == null)
            relay =
                corridorTrigger.gameObject.AddComponent<
                    CorridorLightTriggerRelay
                >();

        relay.Initialize(this);
    }


    public void PlayerEntered(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerCollidersInside.Add(other);

        UpdateLight();
    }


    public void PlayerExited(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerCollidersInside.Remove(other);

        UpdateLight();
    }


    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        Transform root = other.transform.root;

        return root != null && root.CompareTag(playerTag);
    }


    private void UpdateLight()
    {
        if (corridorLight == null)
            return;

        corridorLight.enabled =
            playerCollidersInside.Count > 0;
    }


    private void OnDisable()
    {
        playerCollidersInside.Clear();

        if (corridorLight != null)
            corridorLight.enabled = false;
    }
}


// ============================================================
// Служебная часть.
// Никуда вручную добавлять НЕ НАДО.
// CorridorLightController сам поставит её на Trigger.
// ============================================================

public class CorridorLightTriggerRelay : MonoBehaviour
{
    private CorridorLightController controller;


    public void Initialize(
        CorridorLightController newController
    )
    {
        controller = newController;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (controller != null)
            controller.PlayerEntered(other);
    }


    private void OnTriggerExit(Collider other)
    {
        if (controller != null)
            controller.PlayerExited(other);
    }
}
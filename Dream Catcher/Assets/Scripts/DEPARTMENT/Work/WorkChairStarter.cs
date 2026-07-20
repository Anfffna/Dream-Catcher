using UnityEngine;

public class WorkChairStarter : MonoBehaviour, IInteractable
{
    [Header("Work")]
    public WorkSessionManager workSessionManager;

    [Header("Layer Object")]
    [Tooltip("Объект, которому нужно менять слой. Обычно это объект с Collider.")]
    public GameObject layerObject;

    [Header("Layers")]
    public string defaultLayerName = "Default";
    public string interactableLayerName = "Interactable";

    [Header("Auto Find")]
    public bool autoFindReferences = true;

    private int defaultLayer;
    private int interactableLayer;
    private bool isAvailable;

    private void Awake()
    {
        defaultLayer = LayerMask.NameToLayer(defaultLayerName);
        interactableLayer = LayerMask.NameToLayer(interactableLayerName);

        if (layerObject == null)
            layerObject = gameObject;

        SetAvailable(false);
    }

    public void Interact()
    {
        if (!isAvailable)
            return;

        FindReferences();

        if (workSessionManager == null)
        {
            Debug.LogWarning(
                "WorkChairStarter: WorkSessionManager не найден."
            );
            return;
        }

        workSessionManager.StartWork();
    }

    public void SetAvailable(bool available)
    {
        isAvailable = available;

        if (layerObject == null)
            layerObject = gameObject;

        int targetLayer = available
            ? interactableLayer
            : defaultLayer;

        if (targetLayer < 0)
        {
            Debug.LogWarning(
                "WorkChairStarter: не найден требуемый слой."
            );
            return;
        }

        layerObject.layer = targetLayer;
    }

    private void FindReferences()
    {
        if (!autoFindReferences)
            return;

        if (workSessionManager == null)
            workSessionManager = WorkSessionManager.Instance;

        if (workSessionManager == null)
            workSessionManager =
                FindObjectOfType<WorkSessionManager>();
    }
}
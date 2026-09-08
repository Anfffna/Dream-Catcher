using UnityEngine;

public class VisitorVariantDebugOverride :
    MonoBehaviour
{
    [Header("Тест варианта дела")]

    [Tooltip(
        "Если включено, для этого NPC " +
        "вместо случайного варианта будет " +
        "использован указанный ниже."
    )]
    [SerializeField]
    private bool forceVariant;

    [Tooltip(
        "ID варианта, например variant_a или variant_b."
    )]
    [SerializeField]
    private string forcedVariantId =
        "variant_a";


    public bool ForceVariant =>
        forceVariant;

    public string ForcedVariantId =>
        forcedVariantId;
}
using Robust.Shared.GameStates;

namespace Content.Shared._NC.Decryption.Components;

// Physical raw data carrier brought from POIs.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RawDataComponent : Component
{
    // Data points stored in this raw carrier.
    [DataField("dataPoints"), AutoNetworkedField]
    public int DataPoints = 100;

    // Integrity after decryption flow.
    [DataField("currentIntegrity"), AutoNetworkedField]
    public int CurrentIntegrity = 100;

    // Marks that this carrier has already been decrypted.
    [DataField("isDecrypted"), AutoNetworkedField]
    public bool IsDecrypted;

    // Decrypted technology payload assigned by decryption system.
    [DataField("decryptedTechnologyId"), AutoNetworkedField]
    public string DecryptedTechnologyId = string.Empty;

    // Remaining uses for generated technology payload.
    [DataField("remainingTechnologyUses"), AutoNetworkedField]
    public int RemainingTechnologyUses;
}

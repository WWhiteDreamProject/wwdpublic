using Content.Shared.Containers.ItemSlots;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Decryption.Components;

// Terminal component with one slot for a technology carrier.
[RegisterComponent, NetworkedComponent]
public sealed partial class DecryptionTerminalComponent : Component
{
    public const string DataSlotId = "decryption_data_slot";

    [DataField("dataSlot")]
    public ItemSlot DataSlot = new()
    {
        Name = "Technology",
        Whitelist = new EntityWhitelist
        {
            Components = new[] { "DecryptionTechnology" }
        }
    };

    // Burned media spawned on hard fail.
    [DataField("burnedMediaPrototype")]
    public string BurnedMediaPrototype = "NCRawDataBurned";

    // Matrix width for one decryption session.
    [DataField("matrixWidth")]
    public int MatrixWidth = 24;

    // Matrix height for one decryption session.
    [DataField("matrixHeight")]
    public int MatrixHeight = 22;

    // Total attempts available before ICE burn.
    [DataField("maxAttempts")]
    public int MaxAttempts = 4;

    // Integrity damage applied per incorrect word attempt.
    [DataField("attemptIntegrityDamage")]
    public int AttemptIntegrityDamage = 25;

    // Maximum number of lines kept in terminal log.
    [DataField("logLineLimit")]
    public int LogLineLimit = 20;

    // Active ICE timeout in seconds. 0 disables timeout.
    [DataField("activeIceTimeoutSeconds")]
    public int ActiveIceTimeoutSeconds = 30;

    // Enables active ICE timeout only for legendary tier.
    [DataField("activeIceLegendaryOnly")]
    public bool ActiveIceLegendaryOnly = false;
}

[Serializable, NetSerializable]
public enum DecryptionUiKey : byte
{
    Key
}


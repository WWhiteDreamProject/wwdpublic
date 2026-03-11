using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared._NC.Forensics;

/// <summary>
/// UI ключи для баллистического сканера.
/// </summary>
[Serializable, NetSerializable]
public enum BallisticAnalyzerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class BallisticAnalyzerBuiState : BoundUserInterfaceState
{
    public readonly string? BulletHash;
    public readonly string? WeaponHash;
    public readonly bool IsAnalyzing;
    public readonly BallisticMatchResult Result;

    public BallisticAnalyzerBuiState(string? bulletHash, string? weaponHash, bool isAnalyzing, BallisticMatchResult result)
    {
        BulletHash = bulletHash;
        WeaponHash = weaponHash;
        IsAnalyzing = isAnalyzing;
        Result = result;
    }
}

[Serializable, NetSerializable]
public enum BallisticMatchResult : byte
{
    None,
    Match,
    NoMatch
}

[Serializable, NetSerializable]
public sealed class BallisticAnalyzerStartMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Компонент для баллистического сканера (консоли).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BallisticAnalyzerComponent : Component
{
    [DataField("bulletSlotId")]
    public string BulletSlotId = "bullet_slot";

    [DataField("weaponSlotId")]
    public string WeaponSlotId = "weapon_slot";
}

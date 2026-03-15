using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared._NC.Citation;

/// <summary>
/// DoAfter событие для принудительного штрафа (взлом карты оглушенной цели).
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CitationForceDoAfterEvent : SimpleDoAfterEvent
{
    public readonly int Amount;
    
    public CitationForceDoAfterEvent(int amount)
    {
        Amount = amount;
    }
}
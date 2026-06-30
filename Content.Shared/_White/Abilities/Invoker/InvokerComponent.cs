using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared._White.Abilities.Invoker;

[Serializable, NetSerializable]
public enum OrbType : byte
{
    None = 0,
    Quas = 1,
    Wex = 2,
    Exort = 3
}

[RegisterComponent]
public sealed partial class InvokerComponent : Component
{
    [DataField]
    public List<OrbType> CurrentOrbs = new();

    [DataField]
    public List<EntityUid> OrbEntities = new();

    [DataField]
    public List<Vector2> OrbOffsets = new()
    {
        new Vector2(-0.5f, 0.75f),
        new Vector2(0f, 1.0f),
        new Vector2(0.5f, 0.75f)
    };

    [DataField]
    public ProtoId<InvokerSpellPoolPrototype> SpellPool = "DefaultInvokerSpellPool";

    [DataField]
    public int MaxActiveSpells = 2;

    [DataField]
    public List<EntityUid?> ActiveSpellActions = new();

    [DataField]
    public Dictionary<string, TimeSpan> CooldownHistory = new();

    [DataField]
    public List<string>? LastSpellComponents;
}

using Content.Shared.Ghost;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared._White.CustomGhostSystem;

[Prototype("customGhost")]
public sealed class CustomGhostPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [ViewVariables]
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [ViewVariables]
    [ParentDataFieldAttribute(typeof(AbstractPrototypeIdArraySerializer<CustomGhostPrototype>))]
    public string[]? Parents { get; }

    [DataField]
    public string Category { get; private set; } = "Misc";

    [DataField]
    public List<CustomGhostRestriction>? Restrictions { get; private set; }

    [DataField("proto", required: true)]
    public EntProtoId<GhostComponent> GhostEntityPrototype { get; private set; } = default!;

    /// <summary>
    /// If null, the default of "custom-ghost-[id]-name" will be used.
    /// </summary>
    [DataField("name")]
    public string? Name { get; private set; }

    public string DisplayName => Loc.GetString(Name ?? $"custom-ghost-{ID.ToLower()}-name");
    public string DisplayDesc => Loc.GetString(Description ?? $"custom-ghost-{ID.ToLower()}-desc");

    /// <summary>
    /// If null, the default of "custom-ghost-[id]-desc" will be used.
    /// </summary>
    [DataField("desc")]
    public string? Description { get; private set; }
}


public abstract class CustomGhostRestriction
{
    public virtual bool HideOnFail => false;

    public abstract bool CanUse(ICommonSession player, [NotNullWhen(false)] out string? failReason);
}

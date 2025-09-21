using System.Threading;
using Content.Shared.Antag;
using Content.Shared.FixedPoint;
using Content.Shared.Language;
using Content.Shared.Mind;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.BloodCult.BloodCultist;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultistComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultMember";

    [DataField]
    public ProtoId<LanguagePrototype> CultLanguageId { get; set; } = "Eldritch";

    [ViewVariables, NonSerialized]
    public Entity<MindComponent>? OriginalMind;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 StoredBloodAmount = FixedPoint2.Zero;

    public Color OriginalEyeColor = Color.White;

    public CancellationTokenSource? DeconvertToken { get; set; }
}

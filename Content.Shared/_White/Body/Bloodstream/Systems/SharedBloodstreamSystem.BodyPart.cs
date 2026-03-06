using Content.Shared._White.Body.Bloodstream.Components;
using Content.Shared._White.Body.Systems;

namespace Content.Shared._White.Body.Bloodstream.Systems;

public abstract partial class SharedBloodstreamSystem
{
    private void InitializeBodyPart() =>
        SubscribeLocalEvent<BleedingBodyPartComponent, BodyPartRelayedEvent<GetBleedEvent>>(OnBodyPartGetBleed);

    #region Event Handling

    private void OnBodyPartGetBleed(Entity<BleedingBodyPartComponent> ent, ref BodyPartRelayedEvent<GetBleedEvent> args)
    {
        ent.Comp.Bleeding = 0f;
        foreach (var wound in _wound.GetWounds<BleedingWoundComponent>(ent, scar: true))
            ent.Comp.Bleeding += GetWoundBleed(wound.AsNullable());

        Dirty(ent);

        args.Args = new (args.Args.Bleeding + ent.Comp.Bleeding);
    }

    #endregion
}

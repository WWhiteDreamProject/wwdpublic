using Content.Shared._White.Nutrition.Systems;
using Content.Shared.Chemistry.Components;

namespace Content.Shared._White.Chemistry.Systems;

public sealed class PillSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PillComponent, BeforeTryIngestEvent>(OnBeforeTryIngest);
    }

    #region Event Handling

    private void OnBeforeTryIngest(Entity<PillComponent> ent, ref BeforeTryIngestEvent args)
    {
        args.Min = args.Solution.Volume;
    }

    #endregion
}

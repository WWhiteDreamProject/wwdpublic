using Content.Server._White.Body.Systems;
using Content.Server._White.Gibbing;
using Content.Server.Actions;
using Content.Shared._EE.Shadowling;


namespace Content.Server._EE.Shadowling;


/// <summary>
/// This handles the Annihilate abiltiy logic.
/// Gib from afar!
/// </summary>
public sealed class ShadowlingAnnihilateSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!; // WD EDIT
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowlingAnnihilateComponent, AnnihilateEvent>(OnAnnihilate);
    }

    private void OnAnnihilate(EntityUid uid, ShadowlingAnnihilateComponent component, AnnihilateEvent args)
    {
        // The gibbening
        var target = args.Target;
        if (HasComp<ShadowlingComponent>(target))
            return;

        _gibbing.GibBody(target); // WD EDIT

        _actions.StartUseDelay(args.Action);
    }
}

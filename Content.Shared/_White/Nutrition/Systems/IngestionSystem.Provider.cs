using Content.Shared._White.Body.Systems;
using Content.Shared._White.Nutrition.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;

namespace Content.Shared._White.Nutrition.Systems;

public sealed partial class IngestionSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<IngestionProviderComponent, BodyRelayedEvent<IngestEvent>>(OnIngest);
        SubscribeLocalEvent<IngestionProviderComponent, BodyRelayedEvent<IngestingDoAfterEvent>>(OnIngestingDoAfter);
    }

    #region Event Handling

    private void OnIngest(Entity<IngestionProviderComponent> ent, ref BodyRelayedEvent<IngestEvent> args)
    {
        if (args.Args.Handled)
            return;

        if (!CanConsume(args.Args.Ingestible, args.Args.User, args.Args.Target))
            return;

        var forceFeed = args.Args.User != args.Args.Target;
        var doAfterArgs = new DoAfterArgs(EntityManager, args.Args.User, args.Args.Ingestible.Comp.Delay, new IngestingDoAfterEvent(), args.Args.Target, args.Args.Ingestible)
        {
            BreakOnHandChange = false,
            BreakOnMove = forceFeed,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = MaxFeedDistance,
            NeedHand = forceFeed || _hands.IsHolding(args.Args.User, args.Args.Ingestible),
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        args.Args = args.Args with { Handled = true };

        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution, out var solution))
            return;

        if (!forceFeed)
        {
            _adminLog.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.Args.Target):target} is eating {ToPrettyString(args.Args.Ingestible):food} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
            return;
        }

        var message = Loc.GetString("edible-force-feed", ("user", Identity.Entity(args.Args.User, EntityManager)), ("verb", GetEdibleVerb(food)));
        _popup.PopupEntity(message, args.Args.User, args.Args.Target);

        _adminLog.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(args.Args.User):user} is forcing {ToPrettyString(args.Args.Target):target} to eat {ToPrettyString(args.Args.Ingestible):food} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
    }

    #endregion
}

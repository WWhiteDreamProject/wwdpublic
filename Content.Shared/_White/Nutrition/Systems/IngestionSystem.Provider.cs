using Content.Shared._White.Body.Systems;
using Content.Shared._White.Nutrition.Components;
using Content.Shared._White.Random;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;

namespace Content.Shared._White.Nutrition.Systems;

public sealed partial class IngestionSystem
{
    private void InitializeProvider()
    {
        SubscribeLocalEvent<IngestionProviderComponent, BodyRelayedEvent<IngestEvent>>(OnIngest);
        SubscribeLocalEvent<IngestionProviderComponent, IngestingDoAfterEvent>(OnIngestingDoAfter);
    }

    #region Event Handling

    private void OnIngest(Entity<IngestionProviderComponent> ent, ref BodyRelayedEvent<IngestEvent> args)
    {
        if (args.Args.Handled)
            return;

        if (!CanConsume(args.Args.Ingestible, args.Args.User, args.Args.Target, out _))
            return;

        var forceFeed = args.Args.User != args.Args.Target;
        var doAfterArgs = new DoAfterArgs(EntityManager, args.Args.User, args.Args.Ingestible.Comp.Delay, new IngestingDoAfterEvent(), ent, args.Args.Target, args.Args.Ingestible)
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
            _adminLog.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.Args.Target):target} is ingesting {ToPrettyString(args.Args.Ingestible):ingestible} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
            return;
        }

        var message = Loc.GetString("ingestion-force-feed", ("user", Identity.Entity(args.Args.User, EntityManager)), ("verb", Loc.GetString(args.Args.Ingestible.Comp.Verb)));
        _popup.PopupEntity(message, args.Args.User, args.Args.Target);

        _adminLog.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(args.Args.User):user} is forcing {ToPrettyString(args.Args.Target):target} to ingest {ToPrettyString(args.Args.Ingestible):ingestible} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
    }

    private void OnIngestingDoAfter(Entity<IngestionProviderComponent> ent, ref IngestingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not {} target || args.Used is not {} used)
            return;

        if (!_ingestibleQuery.TryComp(used, out var ingestibleComp))
            return;

        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.SolutionName, ref ent.Comp.Solution))
            return;

        if (!_solutionContainer.ResolveSolution(used, ingestibleComp.SolutionName, ref ingestibleComp.Solution, out var solution))
            return;

        if (!CanConsume((used, ingestibleComp), args.Args.User, target, out var utensils))
            return;

        var forceFeed = args.Target != args.User;
        var verb = Loc.GetString(ingestibleComp.Verb);

        var transfer = ingestibleComp.TransferAmount != null ? FixedPoint2.Min(ingestibleComp.TransferAmount.Value, solution.Volume) : solution.Volume;
        if (transfer == FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("ingestion-cannot-ingest-more", ("verb", verb)), target, target);
            if (!forceFeed)
                return;

            _popup.PopupClient(Loc.GetString("ingestion-cannot-ingest-more-user", ("target", target), ("verb", verb)), target, args.User);
            return;
        }

        var split = _solutionContainer.SplitSolution(ingestibleComp.Solution.Value, transfer);

        _reactive.DoEntityReaction(args.Target.Value, solution, ReactionMethod.Ingestion);
        _solutionContainer.TryAddSolution(ent.Comp.Solution.Value, solution);

        var flavors = _flavorProfile.GetLocalizedFlavorsMessage(used, target, split);
        var satiated = transfer >= solution.Volume;

        if (forceFeed)
        {
            var targetMessage = Loc.GetString("ingestion-force-feed-success", ("user", Identity.Entity(args.User, EntityManager)), ("verb", verb), ("flavors", flavors), ("satiated", satiated));
            _popup.PopupEntity(targetMessage, target, target);

            var userMessage = Loc.GetString("ingestion-force-feed-success-user", ("target", Identity.Entity(target, EntityManager)), ("verb", verb));
            _popup.PopupClient(userMessage, args.User, args.User);

            _adminLog.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(ent):user} forced {ToPrettyString(args.User):target} to ingest {ToPrettyString(ent):ingestible}");
        }
        else
        {
            var message = Loc.GetString(ingestibleComp.Message, ("ingestible", ent.Owner), ("flavors", flavors), ("satiated", satiated));
            _popup.PopupPredicted(message, Loc.GetString(ingestibleComp.OtherMessage), args.User, args.User);

            _adminLog.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} ingest {ToPrettyString(ent):ingestible}");
        }

        _audio.PlayPredicted(ingestibleComp.Sound, target, args.User);

        foreach (var utensil in utensils)
        {
            if (!_random.Prob(utensil, utensil.Comp.BreakChance))
                continue;

            _audio.PlayPredicted(utensil.Comp.BreakSound, args.User, args.User);
            PredictedDel(utensil.Owner);
        }

        if (solution.Volume == FixedPoint2.Zero)
        {
            DeleteAndSpawnTrash((used, ingestibleComp), args.User);
            return;
        }

        var transferDnaEv = new TransferDnaEvent
        {
            Donor = target,
            Recipient = ent,
            CanDnaBeCleaned = false,
        };
        RaiseLocalEvent(target, ref transferDnaEv);

        args.Repeat = !forceFeed;
    }

    #endregion
}

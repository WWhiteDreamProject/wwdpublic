using Content.Server._White.AspectsSystem.Aspects.Components;
using Content.Server._White.AspectsSystem.Base;
using Content.Server.Access.Systems;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking;
using Content.Server.PDA;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._White.AspectsSystem.Aspects;

public sealed class RenameCrewAspect : AspectSystem<RenameCrewAspectComponent>
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConsoleHost _con = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;

    public override void Initialize()
    {
        base.Initialize();
        //SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<PlayerProfileAdjustEvent>(OnPlayerProfileAdjust);
        SubscribeLocalEvent<RenameCrewAspectComponent, ComponentInit>(OnCompInit);
        //SubscribeLocalEvent<CleanUp>(OnCompInit);
    }

    //protected override void Ended(EntityUid uid, RenameCrewAspectComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    //{
    //    base.Ended(uid, component, gameRule, args);
    //}
    //
    //public override void Shutdown()
    //{
    //    base.Shutdown();
    //}

    private void OnCompInit(EntityUid uid, RenameCrewAspectComponent comp, ComponentInit args)
    {
        for (int i = 0; i < comp.FirstNames.Count; i++)
        {
            for (int j = 0; j < comp.LastNames.Count; j++)
            {
                comp.RegularNameCombos.Add((i, j));
            }
        }
    }

    private bool TryPickName(RenameCrewAspectComponent comp, [NotNullWhen(true)] out string? name)
    {
        if (_rng.Prob(comp.SpecialNameChance) && comp.SpecialNames.Count > 0)
        {
            name = _rng.PickAndTake(comp.SpecialNames);
            DebugTools.Assert(name.Length < IdCardConsoleComponent.MaxFullNameLength);
            return true;
        }

        if (comp.RegularNameCombos.Count > 0)
        {
            (int first, int last) = _rng.PickAndTake(comp.RegularNameCombos);
            name = string.Join(' ', comp.FirstNames[first], comp.LastNames[last]);
            DebugTools.Assert(name.Length < IdCardConsoleComponent.MaxFullNameLength);
            return true;
        }

        name = null;
        return false;
    }

    private void OnPlayerProfileAdjust(ref PlayerProfileAdjustEvent args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var ruleEntity, out var renameAspect, out var gameRule))
        {
            if (!TryPickName(renameAspect, out var newName))
                return;
            args.Profile = args.Profile.WithName(newName);
        }
    }
}


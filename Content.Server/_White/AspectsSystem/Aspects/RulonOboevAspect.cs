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

public sealed class RulonOboevAspect : AspectSystem<RenameCrewAspectComponent>
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConsoleHost _con = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;

    private EntityUid? _existing = null;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RenameCrewAspectComponent, ComponentInit>(OnCompInit);
    }

    protected override void Ended(EntityUid uid, RenameCrewAspectComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
        _existing = null;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _existing = null;
    }

    private void OnCompInit(EntityUid uid, RenameCrewAspectComponent comp, ComponentInit args)
    {
        if (_existing.HasValue) {
            //Sawmill.Error("RulonOboev aspect should only exist as a single gamerule. Stop spamming shit.");
            return; // fuck off we're full
        }
        _existing = uid;
        for (int i = 0; i < comp.FirstNames.Count; i++)
        {
            for (int j = 0; j < comp.LastNames.Count; j++)
            {
                comp.RegularNameCombos.Add((i, j));
            }
        }
    }

    private bool PickName(RenameCrewAspectComponent comp, [NotNullWhen(true)] out string? name)
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

    /// <summary>
    /// copypasted from RenameCommand
    /// </summary>
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!_existing.HasValue)
            return;

        var comp = Comp<RenameCrewAspectComponent>(_existing.Value);
        if (!PickName(comp, out var newName))
            return;

        _con.ExecuteCommand($"rename {args.Mob} {newName}");
    }
}


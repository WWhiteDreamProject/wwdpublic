using System.Diagnostics.CodeAnalysis;
using Content.Server._War.StationEngine;
using Content.Server._War.TotalWar.Factions;
using Content.Server._War.TotalWar.Factions.Members;
using Content.Server.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Maps;
using Content.Server.RoundEnd;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._War.TotalWar.GameRule;


/// <summary>
/// TODO: ftl shit. 15 minute timer and shit from research and marinades
/// </summary>
public sealed class TotalWarRuleSystem : GameRuleSystem<TotalWarRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly NpcFactionSystem _npc = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _map = default!;


    private Dictionary<ProtoId<GameMapPrototype>, ProtoId<WarFactionPrototype>> _mapsRegistration = new();
    private Dictionary<EntityUid, WarFactionPrototype> _factionMaps = new();
    private Dictionary<ProtoId<WarFactionPrototype>, HashSet<ProtoId<WarFactionPrototype>>> _allowedFTLToFaction = new();

    private ISawmill _sawmill = default!;

    public bool IsWar;

    private TimeSpan _lastRoundEndCheck = TimeSpan.MaxValue;

    private TimeSpan _roundEndCheckInterval = TimeSpan.FromSeconds(30);

    private string _roundEndText = "";

    private TimeSpan _ftlCheck = TimeSpan.MaxValue;

    private TimeSpan _gracePeriod = TimeSpan.FromMinutes(15);


    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _log.GetSawmill("war.rule");

        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<LoadingMapsEvent>(OnLoadingMaps);
        SubscribeLocalEvent<DestructionStationEngineEvent>(OnStationEngineDestruction);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = QueryActiveRules();
        var active = false;
        while (query.MoveNext(out _, out _, out _))
        {
            active = true;
            break;
        }

        if (!active)
            return;

        TimedCheckRoundEnd();

        CheckFTL();
    }

    #region Events

    protected override void Started(EntityUid uid, TotalWarRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        //_cfg.SetCVar(CCVars.EconomyEnabled, false);

        _ftlCheck = _timing.CurTime + _gracePeriod;
    }


    protected override void Added(EntityUid uid, TotalWarRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        IsWar = true;
    }


    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _mapsRegistration.Clear();
        _factionMaps.Clear();
        _allowedFTLToFaction.Clear();
        IsWar = false;
    }


    private void OnPostGameMapLoad(PostGameMapLoad args)
    {
        if (!_mapsRegistration.TryGetValue(args.GameMap.ID, out var faction))
            return;

        //var warMapComp = AddComp<WarFactionMapComponent>(_map.GetMap(args.Map));
        //warMapComp.Faction = faction;

        _factionMaps.Add(_map.GetMap(args.Map), _prototype.Index(faction));
        _sawmill.Debug($"Map {args.Map} is faction map of faction {faction}");
    }


    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!IsWar)
            return;

        if (!FindWarRule(out var rule)) // TODO: debug assert if rule is not found
            return;

        if (args.JobId == null || !_proto.TryIndex<JobPrototype>(args.JobId, out var jobProto))
            return;

        var warFaction = new WarFactionMemberComponent
        {
            Faction = jobProto.Faction
        };

        rule.FactionMembersCounter.TryAdd(jobProto.Faction, 0);
        rule.FactionMembersCounter[jobProto.Faction] += 1;

        AddComp(args.Mob, warFaction);

        if (!_proto.TryIndex<WarFactionPrototype>(jobProto.Faction, out var factionProto))
            return;

        foreach (var factionNpc in factionProto.NpcFactions)
        {
            _npc.AddFaction(args.Mob, factionNpc);
        }

        _sawmill.Debug($"Associated Player Mob {args.Player.Name}:{args.Mob} is faction {warFaction.Faction}");
    }


    private void OnLoadingMaps(LoadingMapsEvent ev)
    {
        if (!FindWarRule(out var warRule))
            return;

        foreach (var faction in warRule.Factions)
        {
            var factionProto = _proto.Index(faction);

            foreach (var poolId in factionProto.Maps)
            {
                var pool = _proto.Index(poolId);
                var map = _random.Pick(pool.Maps);

                if (!_proto.TryIndex<GameMapPrototype>(map, out var mapProto))
                    continue;

                RegisterFactionMap(map, faction);

                ev.Maps.Add(mapProto);
            }
        }
    }

    private void OnStationEngineDestruction(DestructionStationEngineEvent ev)
    {
        if (!FindWarRule(out var ruleComp))
            return;

        var parent = ev.Parent;
        int currentCycle = 0;
        const int maxCycles = 10;
        while (parent.IsValid() && currentCycle < maxCycles)
        {
            currentCycle += 1;

            if (!_factionMaps.TryGetValue(parent, out var factionPrototype))
            {
                parent = Transform(parent).ParentUid;
                continue;
            }

            ruleComp.DefeatedFactions.Add(factionPrototype.ID);

            CheckRoundEnd();
        }

        // get the faction by map or by station, start the countdown, end the round

    }

    protected override void AppendRoundEndText(EntityUid uid, TotalWarRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        /*var winText = Loc.GetString($"nukeops-{component.WinType.ToString().ToLower()}");
        args.AddLine(winText);

        foreach (var cond in component.WinConditions)
        {
            var text = Loc.GetString($"nukeops-cond-{cond.ToString().ToLower()}");
            args.AddLine(text);
        }

        args.AddLine(Loc.GetString("nukeops-list-start"));

        var antags =_antag.GetAntagIdentifiers(uid);

        foreach (var (_, sessionData, name) in antags)
        {
            args.AddLine(Loc.GetString("nukeops-list-name-user", ("name", name), ("user", sessionData.UserName)));
        }*/
    }


    #endregion

    private void CheckFTL()
    {
        if (_timing.CurTime < _ftlCheck)
            return;

        AllowFTLToFaction("Nanotrasen", "Syndicate");
    }

    private void TimedCheckRoundEnd()
    {
        if (_timing.CurTime < _lastRoundEndCheck)
            return;

        CheckRoundEnd();

        _lastRoundEndCheck = _timing.CurTime + _roundEndCheckInterval;
    }


    private void CheckRoundEnd()
    {
        if (!FindWarRule(out var ruleComp))
            return;

        if (!ruleComp.EndRoundOnOneFaction)
            return;

        var aliveFactions = new List<ProtoId<WarFactionPrototype>>();
        foreach (var faction in ruleComp.Factions)
        {
            if (IsFactionAlive(faction, ruleComp))
                aliveFactions.Add(faction);
        }

        if (aliveFactions.Count == 1)
        {
            _roundEnd.EndRound();
        }
    }


    /// <summary>
    /// TODO: count cuffed as dead
    /// </summary>
    /// <param name="factionId">faction protoid</param>
    /// <param name="ruleComp"></param>
    /// <returns>true if faction is alive, false if not</returns>
    private bool IsFactionAlive(string factionId, TotalWarRuleComponent ruleComp)
    {
        if (ruleComp.DefeatedFactions.Contains(factionId))
            return false;

        if (!ruleComp.FactionMembersCounter.TryGetValue(factionId, out var memberCount))
            return true; // TODO: false

        var deadCounter = 0;

        var query = EntityQueryEnumerator<WarFactionMemberComponent>();
        while (query.MoveNext(out var uid, out var memberComp))
        {
            if (memberComp.Faction != factionId)
                continue;

            if (!TryComp<MobStateComponent>(uid, out var mobState))
                continue;

            if (mobState.CurrentState != MobState.Dead)
                continue;

            deadCounter += 1;
        }

        if (deadCounter / memberCount > 0.7) // TODO: cvar or something
            return false;

        return true;
    }


    public bool FindWarRule([NotNullWhen(true)] out TotalWarRuleComponent? ruleComp)
    {
        ruleComp = null;
        var query = EntityQueryEnumerator<TotalWarRuleComponent>();
        while (query.MoveNext(out _, out var rule))
        {
            ruleComp = rule;
            return true;
        }

        return false;
    }


    public void RegisterFactionMap(string mapId, string factionId)
    {
        _mapsRegistration.TryAdd(mapId, factionId);
        _sawmill.Debug($"Registered Faction Map. {factionId}:{mapId}");
    }


    #region FTL

    public bool CanFTLToFaction(string factionId, string targetFractionId)
    {
        return _allowedFTLToFaction[factionId].Contains(targetFractionId);
    }


    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="targetFractionId"></param>
    public void AllowFTLToFaction(string factionId, string targetFractionId)
    {
        if (!_allowedFTLToFaction.ContainsKey(factionId))
            return;

        _allowedFTLToFaction[factionId].Add(targetFractionId);
    }


    #endregion

}

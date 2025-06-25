using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.PDA.Ringer;
using Content.Server.Roles;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Roles.RoleCodeword;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;
using Content.Shared.Mood;
using Content.Server.Preferences.Managers;
using Robust.Shared.Log;

namespace Content.Server.GameTicking.Rules;

public sealed class TraitorRuleSystem : GameRuleSystem<TraitorRuleComponent>
{
    private static readonly Color TraitorCodewordColor = Color.FromHex("#cc3b3b");
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRoleCodewordSystem _roleCodewordSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!; // WD EDIT
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitorRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<TraitorRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
    }

    protected override void Added(EntityUid uid, TraitorRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
        MakeCodewords(component);
    }

    private void AfterEntitySelected(Entity<TraitorRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeTraitor(args.EntityUid, ent);
    }

    private void MakeCodewords(TraitorRuleComponent component)
    {
        var adjectives = _prototypeManager.Index(component.CodewordAdjectives).Values;
        var verbs = _prototypeManager.Index(component.CodewordVerbs).Values;
        var codewordPool = adjectives.Concat(verbs).ToList();
        var finalCodewordCount = Math.Min(component.CodewordCount, codewordPool.Count);
        component.Codewords = new string[finalCodewordCount];
        for (var i = 0; i < finalCodewordCount; i++)
        {
            component.Codewords[i] = _random.PickAndTake(codewordPool);
        }
    }

    public bool MakeTraitor(EntityUid traitor, TraitorRuleComponent component)
    {
        //Grab the mind if it wasn't provided
        if (!_mindSystem.TryGetMind(traitor, out var mindId, out var mind))
            return false;

        component.SelectionStatus = TraitorRuleComponent.SelectionState.Started; // WD EDIT

        var briefing = new StringBuilder();

        if (component.GiveCodewords)
            briefing.AppendLine(Loc.GetString("traitor-role-codewords-short", ("codewords", string.Join(", ", component.Codewords))));

        var issuer = _random.Pick(_prototypeManager.Index(component.ObjectiveIssuers).Values);

        Note[]? code = null;

        if (component.GiveUplink)
        {
            var uplinkPref = GetUplinkPreference(mind);
            var startingBalance = GetStartingBalance(component, mind, uplinkPref);

            if (_uplink.AddUplink(traitor, startingBalance, uplinkPref: uplinkPref, giveDiscounts: true))
            {
                briefing.AppendLine(GetUplinkBriefing(traitor, uplinkPref, out code));
            }
        }

        _antag.SendBriefing(traitor, GenerateBriefing(component.Codewords, code, issuer, GetUplinkPreference(mind)), null, component.GreetSoundNotification);

        component.TraitorMinds.Add(mindId);

        // Assign briefing
        //Since this provides neither an antag/job prototype, nor antag status/roletype,
        //and is intrinsically related to the traitor role
        //it does not need to be a separate Mind Role Entity
        _roleSystem.MindHasRole<TraitorRoleComponent>(mindId, out var traitorRole);
        if (traitorRole is not null)
        {
            AddComp<RoleBriefingComponent>(traitorRole.Value.Owner);
            Comp<RoleBriefingComponent>(traitorRole.Value.Owner).Briefing = briefing.ToString();
        }

        // Send codewords to only the traitor client
        var color = TraitorCodewordColor; // Fall back to a dark red Syndicate color if a prototype is not found

        RoleCodewordComponent codewordComp = EnsureComp<RoleCodewordComponent>(mindId);
        _roleCodewordSystem.SetRoleCodewords(codewordComp, "traitor", component.Codewords.ToList(), color);

        // Don't change the faction, this was stupid.
        //_npcFaction.RemoveFaction(traitor, component.NanoTrasenFaction, false);
        //_npcFaction.AddFaction(traitor, component.SyndicateFaction);

        RaiseLocalEvent(traitor, new MoodEffectEvent("TraitorFocused"));
        return true;
    }

    private UplinkPreference GetUplinkPreference(MindComponent mind)
    {
        if (mind.Session != null)
        {
            var prefs = _prefs.GetPreferencesOrNull(mind.Session.UserId);
            if (prefs != null && prefs.SelectedCharacter is HumanoidCharacterProfile profile)
            {
                return profile.Uplink;
            }
        }
        return UplinkPreference.PDA;
    }

    private FixedPoint2 GetStartingBalance(TraitorRuleComponent component, MindComponent mind, UplinkPreference uplinkPref)
    {
        var startingBalance = component.StartingBalance;
        if (_jobs.MindTryGetJob(mind.Owner, out var prototype))
        {
            var newBalance = startingBalance - prototype.AntagAdvantage;
            if (newBalance < 0)
                startingBalance = 0;
            else
                startingBalance = newBalance;
        }

        switch (uplinkPref)
        {
            case UplinkPreference.Implant:
                return component.ImplantBalance;
            case UplinkPreference.Radio:
                return component.RadioBalance;
            default:
                return startingBalance;
        }
    }

    private string GetUplinkBriefing(EntityUid traitor, UplinkPreference uplinkPref, out Note[]? code)
    {
        code = null;
        switch (uplinkPref)
        {
            case UplinkPreference.PDA:
                var pda = _uplink.FindUplinkTarget(traitor);
                if (pda.HasValue && TryComp<RingerUplinkComponent>(pda, out var ringerComp))
                {
                    code = ringerComp.Code;
                    return Loc.GetString("traitor-role-uplink-code-short", ("code", string.Join("-", code).Replace("sharp", "#")));
                }
                Logger.Error($"Could not find PDA or RingerUplinkComponent for {ToPrettyString(traitor)} after adding PDA uplink.");
                return string.Empty;
            case UplinkPreference.Implant:
                return Loc.GetString("traitor-role-uplink-implant-short");
            case UplinkPreference.Radio:
                return Loc.GetString("traitor-role-uplink-radio-short");
            default:
                Logger.Error($"Unsupported uplink preference in GetUplinkBriefing: {uplinkPref}");
                return string.Empty;
        }
    }

    private void OnObjectivesTextPrepend(EntityUid uid, TraitorRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        args.Text += "\n" + Loc.GetString("traitor-round-end-codewords", ("codewords", string.Join(", ", comp.Codewords)));
    }

    private string GenerateBriefing(string[]? codewords, Note[]? uplinkCode, string? objectiveIssuer = null, UplinkPreference uplinkPref = UplinkPreference.PDA)
    {
        var sb = new StringBuilder();

        // Use different greeting based on uplink type
        switch (uplinkPref)
        {
            case UplinkPreference.PDA:
                sb.AppendLine(Loc.GetString("traitor-role-greeting-pda", ("corporation", objectiveIssuer ?? Loc.GetString("objective-issuer-unknown"))));
                break;
            case UplinkPreference.Implant:
                sb.AppendLine(Loc.GetString("traitor-role-greeting-implant", ("corporation", objectiveIssuer ?? Loc.GetString("objective-issuer-unknown"))));
                break;
            case UplinkPreference.Radio:
                sb.AppendLine(Loc.GetString("traitor-role-greeting-radio", ("corporation", objectiveIssuer ?? Loc.GetString("objective-issuer-unknown"))));
                break;
            default:
                // Fallback to generic greeting
                sb.AppendLine(Loc.GetString("traitor-role-greeting", ("corporation", objectiveIssuer ?? Loc.GetString("objective-issuer-unknown"))));
                break;
        }

        if (codewords != null)
            sb.AppendLine(Loc.GetString("traitor-role-codewords", ("codewords", string.Join(", ", codewords))));

        // Add appropriate message based on uplink type
        switch (uplinkPref)
        {
            case UplinkPreference.PDA:
                if (uplinkCode != null)
                    sb.AppendLine(Loc.GetString("traitor-role-uplink-code", ("code", string.Join("-", uplinkCode).Replace("sharp", "#"))));
                break;

            case UplinkPreference.Implant:
                sb.AppendLine(Loc.GetString("traitor-role-uplink-implant"));
                break;

            case UplinkPreference.Radio:
                sb.AppendLine(Loc.GetString("traitor-role-uplink-radio"));
                break;

            default:
                // Fallback for any other cases, though this shouldn't be hit with the new logic.
                break;
        }

        return sb.ToString();
    }

    public List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind)
    {
        List<(EntityUid Id, MindComponent Mind)> allTraitors = new();

        var query = EntityQueryEnumerator<TraitorRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor))
        {
            foreach (var role in GetOtherTraitorMindsAliveAndConnected(ourMind, (uid, traitor)))
            {
                if (!allTraitors.Contains(role))
                    allTraitors.Add(role);
            }
        }

        return allTraitors;
    }

    private List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind, Entity<TraitorRuleComponent> rule)
    {
        var traitors = new List<(EntityUid Id, MindComponent Mind)>();
        foreach (var mind in _antag.GetAntagMinds(rule.Owner))
        {
            if (mind.Comp == ourMind)
                continue;

            traitors.Add((mind, mind));
        }

        return traitors;
    }

    // WD EDIT START
    public List<Entity<MindComponent>> GetAllLivingConnectedTraitors()
    {
        var traitors = new List<Entity<MindComponent>>();

        var query = EntityQueryEnumerator<TraitorRuleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            traitors.AddRange(_antag.GetAntagMinds(uid));
        }

        return traitors;
    }
    // WD EDIT END
}

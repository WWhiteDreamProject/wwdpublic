using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared._White.Implants.MindSlave;
using Content.Shared._White.Implants.NeuroStabilization;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Tag;

namespace Content.Server._White.Implants;

public sealed class ImplantsSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly JobSystem _job = default!;

    [ValidatePrototypeId<TagPrototype>]
    private const string MindShieldTag = "MindShield";

    [ValidatePrototypeId<TagPrototype>]
    private const string NeuroStabilizationTag = "NeuroStabilization";

    [ValidatePrototypeId<TagPrototype>]
    private const string MindSlaveTag = "MindSlave";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, SubdermalImplantInserted>(OnImplantInserted);
        SubscribeLocalEvent<SubdermalImplantComponent, SubdermalImplantRemoved>(OnImplantRemoved);
        SubscribeLocalEvent<SubdermalImplantComponent, AttemptSubdermalImplantInserted>(AttemptImplantInserted);
    }

    private void OnImplantInserted(EntityUid uid, SubdermalImplantComponent component, SubdermalImplantInserted args)
    {
        if (_tag.HasTag(uid, MindShieldTag)
            && MindShieldCheck(uid, args.Target))
            EnsureComp<MindShieldComponent>(args.Target);

        if (_tag.HasTag(uid, NeuroStabilizationTag))
            EnsureComp<NeuroStabilizationComponent>(args.Target);

        if (_tag.HasTag(uid, MindSlaveTag))
            MindSlaveInserted(args.User, args.Target);
    }

    private void OnImplantRemoved(EntityUid uid, SubdermalImplantComponent component, SubdermalImplantRemoved args)
    {
        if (_tag.HasTag(uid, MindShieldTag))
            RemComp<MindShieldComponent>(args.Target);

        if (_tag.HasTag(uid, NeuroStabilizationTag))
            RemComp<NeuroStabilizationComponent>(args.Target);

        if (_tag.HasTag(uid, MindSlaveTag))
            MindSlaveRemoved(args.User, args.Target);
    }

    private void AttemptImplantInserted(EntityUid uid, SubdermalImplantComponent component, ref AttemptSubdermalImplantInserted args)
    {
        if (_tag.HasTag(uid, MindSlaveTag)
            && !MindSlaveCheck(args.User, args.Target))
        {
            args.Popup = false;
            args.Cancel();
        }
    }

    #region MindShield

    /// <summary>
    /// Checks if the implanted person was a Rev or Head Rev and remove role or destroy mindshield respectively.
    /// </summary>
    private bool MindShieldCheck(EntityUid uid, EntityUid target)
    {
        if (HasComp<HeadRevolutionaryComponent>(target)
            || (TryComp<MindSlaveComponent>(target, out var mindSlave)
                && mindSlave.Master.HasValue))
        {
            _popup.PopupEntity(Loc.GetString("head-rev-break-mindshield"), target);
            QueueDel(uid);
            return false;
        }

        if (_mind.TryGetMind(target, out var mindId, out _)
            && _role.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId))
        {
            _adminLog.Add(LogType.Mind, LogImpact.Medium,
            $"{ToPrettyString(target)} was deconverted due to being implanted with a Mindshield.");
        }

        return true;
    }

    #endregion

    #region MindSlave

    private void MindSlaveInserted(EntityUid user, EntityUid target)
    {
        var slaveComponent = EnsureComp<MindSlaveComponent>(target);
        slaveComponent.Master = GetNetEntity(user);

        var masterComponent = EnsureComp<MindSlaveComponent>(user);
        masterComponent.Slaves.Add(GetNetEntity(target));

        Dirty(user, masterComponent);
        Dirty(target, slaveComponent);

        if (!_mind.TryGetMind(target, out var targetMindId, out var targetMind)
            || targetMind.Session is null)
            return;

        var jobName = _job.MindTryGetJobName(user);

        // send message to chat
        var message = Loc.GetString("mindslave-chat-message", ("player", user), ("role", jobName));
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false,
            targetMind.Session.Channel, Color.FromHex("#5e9cff"));

        // add briefing in character menu
        if (TryComp<RoleBriefingComponent>(targetMindId, out var roleBriefing))
        {
            roleBriefing.Briefing += Loc.GetString("mindslave-briefing", ("player", user), ("role", jobName));
            Dirty(targetMindId, roleBriefing);
        }
        else
        {
            _role.MindAddRole(targetMindId, new RoleBriefingComponent
            {
                Briefing = Loc.GetString("mindslave-briefing", ("player", user), ("role", jobName))
            }, targetMind);
        }

        _adminLog.Add(LogType.Mind, LogImpact.High,
            $"{ToPrettyString(user)} MindSlaved {ToPrettyString(target)}");
    }

    private void MindSlaveRemoved(EntityUid user, EntityUid target)
    {
        if (!TryComp(target, out MindSlaveComponent? mindslave)
            || !mindslave.Master.HasValue)
            return;

        var master = GetEntity(mindslave.Master.Value);
        if (_mind.TryGetMind(target, out var mindId, out _))
        {
            _role.MindTryRemoveRole<RoleBriefingComponent>(mindId);
            _popup.PopupEntity(Loc.GetString("mindslave-freed", ("player", master)), target, target);
        }

        if (TryComp(master, out MindSlaveComponent? masterMindslave))
        {
            masterMindslave.Slaves.Remove(GetNetEntity(target));
            if (masterMindslave.Slaves.Count == 0)
                RemComp<MindSlaveComponent>(master);
        }

        RemComp<MindSlaveComponent>(target);

        _adminLog.Add(LogType.Mind, LogImpact.High,
            $"{ToPrettyString(user)} UnMindSlaved {ToPrettyString(target)}");
    }

    /// <summary>
    /// Checks if the target can be an MindSlaved
    /// </summary>
    private bool MindSlaveCheck(EntityUid user, EntityUid target)
    {
        string? message = null;
        if (target == user)
            message = Loc.GetString("mindslave-target-self");

        if (HasComp<MindShieldComponent>(target)
            || HasComp<RevolutionaryComponent>(target)
            || !_mind.TryGetMind(target, out _, out _)
            || (TryComp<MindSlaveComponent>(target, out var mindSlave)
                && mindSlave.Master.HasValue))
            message = Loc.GetString("mindslave-cant-insert");

        if (message == null)
            return true;

        _popup.PopupEntity(message, target, user);
        return false;
    }

    #endregion
}

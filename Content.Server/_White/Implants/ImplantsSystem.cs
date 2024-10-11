using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared._White.Implants.NeuroStabilization;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Tag;

namespace Content.Server._White.Implants;

public sealed class ImplantsSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    [ValidatePrototypeId<TagPrototype>]
    private const string MindShieldTag = "MindShield";

    [ValidatePrototypeId<TagPrototype>]
    private const string NeuroStabilizationTag = "NeuroStabilization";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, SubdermalImplantInserted>(OnImplantInserted);
        SubscribeLocalEvent<SubdermalImplantComponent, SubdermalImplantRemoved>(OnImplantRemoved);
    }

    private void OnImplantInserted(EntityUid uid, SubdermalImplantComponent component, SubdermalImplantInserted args)
    {
        if (_tag.HasTag(uid, MindShieldTag)
            && RevolutionCheck(uid, args.Target))
            EnsureComp<MindShieldComponent>(args.Target);

        if (_tag.HasTag(uid, NeuroStabilizationTag))
            EnsureComp<NeuroStabilizationComponent>(args.Target);
    }

    private void OnImplantRemoved(EntityUid uid, SubdermalImplantComponent component, SubdermalImplantRemoved args)
    {
        if (_tag.HasTag(uid, MindShieldTag))
            RemComp<MindShieldComponent>(args.Target);

        if (_tag.HasTag(uid, NeuroStabilizationTag))
            RemComp<NeuroStabilizationComponent>(args.Target);
    }

    /// <summary>
    /// Checks if the implanted person was a Rev or Head Rev and remove role or destroy mindshield respectively.
    /// </summary>
    private bool RevolutionCheck(EntityUid uid, EntityUid target)
    {
        if (HasComp<HeadRevolutionaryComponent>(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), target);
            QueueDel(uid);
            return false;
        }

        if (_mindSystem.TryGetMind(target, out var mindId, out _)
            && _roleSystem.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId))
        {
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium,
            $"{ToPrettyString(target)} was deconverted due to being implanted with a Mindshield.");
        }

        return true;
    }
}

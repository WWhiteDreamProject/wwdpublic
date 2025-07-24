using Content.Server.CriminalRecords.Systems;
using Content.Server.Popups;
using Content.Server.StationRecords.Systems;
using Content.Shared._White.Glasses.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Content.Server.Station.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.CriminalRecords.Systems;

namespace Content.Server._White.Glasses.Systems;

public sealed class SecurityGlassesWantedStatusSystem : SharedSecurityGlassesWantedStatusSystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedCriminalRecordsSystem _sharedCriminalRecords = default!;

    private const string SecurityAccessTag = "Security";
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeNetworkEvent<SecurityGlassesChangeStatusEvent>(OnChangeStatus);
    }
    protected override void TryOpenUi(EntityUid user, EntityUid target)
    {
        if (!CheckSecurityAccess(user))
        {
            _popup.PopupEntity(Loc.GetString("security-glasses-wanted-status-access-denied"), user, user);
            return;
        }

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        var targetNet = _entityManager.GetNetEntity(target);
        var userNet = _entityManager.GetNetEntity(user);

        RaiseNetworkEvent(new SecurityGlassesWantedStatusOpenEvent(targetNet, userNet), actor.PlayerSession);
    }

    private void OnChangeStatus(SecurityGlassesChangeStatusEvent ev, EntitySessionEventArgs args)
    {
        if (!_entityManager.TryGetEntity(ev.User, out var userEntity) ||
            args.SenderSession.AttachedEntity == null || 
            args.SenderSession.AttachedEntity.Value != userEntity)
            return;

        if (!_entityManager.TryGetEntity(ev.Target, out var targetEntity))
            return;

        var user = userEntity.Value;
        var target = targetEntity.Value;
        
        if (!CheckSecurityAccess(user))
        {
            _popup.PopupEntity(Loc.GetString("security-glasses-wanted-status-access-denied"), user, user);
            return;
        }

        var targetName = Identity.Name(target, EntityManager);

        var station = FindTargetStation(target);
        if (station == null)
        {
            _popup.PopupEntity(Loc.GetString("security-glasses-wanted-status-no-record"), user, user);
            return;
        }

        var recordId = _stationRecords.GetRecordByName(station.Value, targetName);
        if (recordId == null)
        {
            _popup.PopupEntity(Loc.GetString("security-glasses-wanted-status-no-record"), user, user);
            return;
        }

        var recordKey = new StationRecordKey(recordId.Value, station.Value);
        
        var status = (SecurityStatus)ev.Status;
        var reason = Loc.GetString("security-glasses-wanted-status-reason", ("user", user));
        if (_criminalRecords.TryChangeStatus(recordKey, status, reason))
        {
            _popup.PopupEntity(Loc.GetString("security-glasses-wanted-status-changed-success", 
                ("target", target), ("status", status)), user, user);

            _sharedCriminalRecords.UpdateCriminalIdentity(targetName, status);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("security-glasses-wanted-status-changed-failed"), user, user);
        }
    }

    private EntityUid? FindTargetStation(EntityUid target)
    {
        var station = _stationSystem.GetOwningStation(target);
        if (station != null)
            return station;

        return _stationSystem.GetStationInMap(Transform(target).MapID);
    }

    public override bool CheckSecurityAccess(EntityUid user)
    {
        var accessTags = _accessReader.FindAccessTags(user);
        return accessTags.Contains(SecurityAccessTag);
    }
} 
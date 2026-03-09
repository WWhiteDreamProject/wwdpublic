using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._NC.CitiNet;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NC.CitiNet;

public sealed class CitiNetNodeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<CitiNetNodeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CitiNetNodeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CitiNetNodeComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<CitiNetNodeComponent, PowerChangedEvent>(OnPowerChanged);
        
        SubscribeLocalEvent<CitiNetNodeComponent, CitiNetNodeConnectDoAfterEvent>(OnConnectDoAfter);
        SubscribeLocalEvent<CitiNetNodeComponent, CitiNetNodeEmergencyDoAfterEvent>(OnEmergencyDoAfter);
        
        // Используем Subs.BuiEvents, это 100% рабочий вариант для вашего проекта
        Subs.BuiEvents<CitiNetNodeComponent>(CitiNetNodeUiKey.Key, subs => {
            subs.Event<CitiNetNodeEmergencyExtractionMessage>(OnEmergencyMessage);
        });
    }

    private void OnInit(EntityUid uid, CitiNetNodeComponent component, ComponentInit args)
    {
        UpdatePowerDraw(uid, component);
        UpdateAppearance(uid, component);
    }

    private void OnInteractHand(EntityUid uid, CitiNetNodeComponent component, InteractHandEvent args)
    {
        _ui.OpenUi(uid, CitiNetNodeUiKey.Key, args.User);
    }

    private void OnEmergencyMessage(EntityUid uid, CitiNetNodeComponent component, CitiNetNodeEmergencyExtractionMessage args)
    {
        if (component.State != CitiNetNodeState.Downloading)
            return;

        if (args.Actor is not { Valid: true } user)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(component.EmergencyDelay), new CitiNetNodeEmergencyDoAfterEvent(), uid, target: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupEntity(Loc.GetString("citinet-popup-emergency-start"), uid, user);
    }

    private void OnInteractUsing(EntityUid uid, CitiNetNodeComponent component, InteractUsingEvent args)
    {
        if (component.State != CitiNetNodeState.Idle)
            return;

        if (!HasComp<CitiNetBlankDriveComponent>(args.Used))
        {
            _popup.PopupEntity(Loc.GetString("citinet-popup-not-blank-drive"), uid, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(component.ConnectDelay), new CitiNetNodeConnectDoAfterEvent(), uid, target: uid, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupEntity(Loc.GetString("citinet-popup-connect-start"), uid, args.User);
    }

    private void OnConnectDoAfter(EntityUid uid, CitiNetNodeComponent component, CitiNetNodeConnectDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Used == null)
            return;

        StartDownloading(uid, component, args.Args.User, args.Args.Used.Value);
        args.Handled = true;
    }

    private void OnEmergencyDoAfter(EntityUid uid, CitiNetNodeComponent component, CitiNetNodeEmergencyDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || component.State != CitiNetNodeState.Downloading)
            return;

        FailDownload(uid, component, Loc.GetString("citinet-popup-emergency-fail"));
        args.Handled = true;
    }

    private void OnPowerChanged(EntityUid uid, CitiNetNodeComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered && component.State == CitiNetNodeState.Downloading)
        {
            FailDownload(uid, component, Loc.GetString("citinet-popup-power-lost"));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CitiNetNodeComponent>();
        while (query.MoveNext(out var uid, out var node))
        {
            switch (node.State)
            {
                case CitiNetNodeState.Downloading:
                    UpdateDownloading(uid, node, frameTime);
                    break;
                case CitiNetNodeState.Cooldown:
                    UpdateCooldown(uid, node, frameTime);
                    break;
            }
        }
    }

    private void UpdateDownloading(EntityUid uid, CitiNetNodeComponent node, float frameTime)
    {
        node.Progress += frameTime / node.DownloadDuration;
        UpdateUserInterface(uid, node);

        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver) && receiver.PowerReceived < node.ActivePower)
        {
             if (node.Progress > 0.01f)
                FailDownload(uid, node, Loc.GetString("citinet-popup-power-low"));
        }

        if (node.Progress >= 1.0f)
        {
            FinishDownload(uid, node);
        }
    }

    private void UpdateCooldown(EntityUid uid, CitiNetNodeComponent node, float frameTime)
    {
        node.RemainingTime -= frameTime;
        UpdateUserInterface(uid, node);

        if (node.RemainingTime <= 0)
        {
            SetState(uid, node, CitiNetNodeState.Idle);
        }
    }

    private void SetState(EntityUid uid, CitiNetNodeComponent component, CitiNetNodeState state)
    {
        component.State = state;
        UpdatePowerDraw(uid, component);
        UpdateUserInterface(uid, component);
        UpdateAppearance(uid, component);
    }

    private void UpdatePowerDraw(EntityUid uid, CitiNetNodeComponent component)
    {
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            return;

        float targetPower = component.State switch
        {
            CitiNetNodeState.Idle => component.IdlePower,
            CitiNetNodeState.Downloading => component.ActivePower,
            _ => 100f
        };

        receiver.Load = targetPower;
    }

    private void UpdateUserInterface(EntityUid uid, CitiNetNodeComponent component)
    {
        if (!_ui.HasUi(uid, CitiNetNodeUiKey.Key))
            return;

        bool isPowered = TryComp<ApcPowerReceiverComponent>(uid, out var receiver) && receiver.Powered;

        _ui.SetUiState(uid, CitiNetNodeUiKey.Key, new CitiNetNodeBoundUserInterfaceState(
            component.State,
            component.Progress,
            component.RemainingTime,
            isPowered));
    }

    private void UpdateAppearance(EntityUid uid, CitiNetNodeComponent component)
    {
        _appearance.SetData(uid, CitiNetNodeVisuals.State, component.State);
    }

    private void StartDownloading(EntityUid uid, CitiNetNodeComponent component, EntityUid user, EntityUid drive)
    {
        EntityManager.DeleteEntity(drive);
        component.Progress = 0f;
        SetState(uid, component, CitiNetNodeState.Downloading);
        _audio.PlayPvs(component.SoundConnect, uid);
    }

    private void FailDownload(EntityUid uid, CitiNetNodeComponent component, string reason)
    {
        Spawn(component.BurnedDiskPrototype, _transform.GetMapCoordinates(uid));
        component.RemainingTime = component.CooldownDuration / 2;
        SetState(uid, component, CitiNetNodeState.Cooldown);
        _audio.PlayPvs(component.SoundError, uid);
        _popup.PopupEntity(reason, uid);
    }

    private void FinishDownload(EntityUid uid, CitiNetNodeComponent component)
    {
        string prototypeId;
        Dictionary<string, float> poolToUse;

        if (_random.Prob(component.CleanTechChance))
        {
            poolToUse = component.CleanTechPool;
        }
        else
        {
            poolToUse = component.RawDataPool;
        }

        prototypeId = PickFromWeightedPool(poolToUse);

        Spawn(prototypeId, _transform.GetMapCoordinates(uid));
        component.RemainingTime = component.CooldownDuration;
        SetState(uid, component, CitiNetNodeState.Cooldown);
        _audio.PlayPvs(component.SoundSuccess, uid);
    }

    private string PickFromWeightedPool(Dictionary<string, float> pool)
    {
        if (pool == null || pool.Count == 0)
            return "NCRawDataTier1"; // Fallback

        float totalWeight = 0f;
        foreach (var weight in pool.Values)
        {
            totalWeight += weight;
        }

        float randomVal = _random.NextFloat() * totalWeight;
        float currentWeight = 0f;

        foreach (var kvp in pool)
        {
            currentWeight += kvp.Value;
            if (randomVal <= currentWeight)
                return kvp.Key;
        }

        // Fallback in case of floating point precision issues
        foreach (var kvp in pool) return kvp.Key; 
        return "NCRawDataTier1";
    }
}

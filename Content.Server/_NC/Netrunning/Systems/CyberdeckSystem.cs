using Content.Server.Power.Components;
using Content.Shared._NC.Netrunning.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Wieldable;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Timing;

namespace Content.Server._NC.Netrunning.Systems;

public sealed class CyberdeckSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!; // Added dependency

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberdeckComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<CyberdeckComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<CyberdeckComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);
        SubscribeLocalEvent<CyberdeckComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CyberdeckComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<CyberdeckComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<CyberdeckComponent, CyberdeckProgramRequestMessage>(OnProgramRequest);
    }

    private void OnContainerModified(EntityUid uid, CyberdeckComponent component, ContainerModifiedMessage args)
    {
        UpdateUi(uid, component);
    }

    private void OnProgramRequest(EntityUid uid, CyberdeckComponent component, CyberdeckProgramRequestMessage args)
    {
        // TODO: Validate user range, blindness, etc.
        var programUid = GetEntity(args.ProgramId);
        if (!TryComp<NetProgramComponent>(programUid, out var program))
            return;

        if (!TryUseRam(uid, program.RamCost, component))
            return;

        // Execute program logic here
        // For now, just a popup
        // Use a proper dependency for popups if needed, or simple distinct log
    }

    private void OnStartup(EntityUid uid, CyberdeckComponent component, ComponentStartup args)
    {
        UpdateUi(uid, component);
    }

    private void OnWielded(EntityUid uid, CyberdeckComponent component, ref ItemWieldedEvent args)
    {
        // Battery Check
        if (TryComp<BatteryComponent>(uid, out var battery) && battery.CurrentCharge <= 0)
            return; // No power, UI won't open

        _uiSystem.OpenUi(uid, CyberdeckUiKey.Key, args.User);
    }

    private void OnUnwielded(EntityUid uid, CyberdeckComponent component, ref ItemUnwieldedEvent args)
    {
        _uiSystem.CloseUi(uid, CyberdeckUiKey.Key, args.User);
    }

    private void AddOpenUiVerb(EntityUid uid, CyberdeckComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new ActivationVerb
        {
            Text = "Open Cyberdeck",
            Act = () => _uiSystem.TryToggleUi(uid, CyberdeckUiKey.Key, args.User)
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CyberdeckComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out var deck, out var battery))
        {
            if (deck.CurrentRam >= deck.MaxRam)
                continue;

            // Only regenerate if battery has charge
            if (battery.CurrentCharge <= 0)
                continue;

            deck.RecoveryAccumulator += frameTime * deck.RecoverySpeed;
            if (deck.RecoveryAccumulator >= 1.0f)
            {
                var amount = (int) deck.RecoveryAccumulator;
                deck.RecoveryAccumulator -= amount;

                deck.CurrentRam = System.Math.Min(deck.CurrentRam + amount, deck.MaxRam);

                Dirty(uid, deck);
                UpdateUi(uid, deck);
            }
        }
    }

    public bool TryUseRam(EntityUid uid, int cost, CyberdeckComponent? deck = null)
    {
        if (!Resolve(uid, ref deck))
            return false;

        if (deck.CurrentRam >= cost)
        {
            deck.CurrentRam -= cost;
            Dirty(uid, deck);
            UpdateUi(uid, deck);
            return true;
        }

        return false;
    }

    private void UpdateUi(EntityUid uid, CyberdeckComponent deck)
    {
        var programs = new Dictionary<NetEntity, NetProgramData>();

        // Scan all containers for programs
        foreach (var container in _containerSystem.GetAllContainers(uid))
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (TryComp<NetProgramComponent>(entity, out var program))
                {
                    programs[GetNetEntity(entity)] = new NetProgramData(program.ProgramType, program.RamCost, program.EnergyCost);
                }
            }
        }

        _uiSystem.SetUiState(uid, CyberdeckUiKey.Key, new CyberdeckBoundUiState(deck.CurrentRam, deck.MaxRam, programs));
    }
}

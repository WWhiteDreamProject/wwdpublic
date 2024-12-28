using Content.Server.Aliens.Components;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Aliens.Components;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;
using AlienInfectedComponent = Content.Shared.Aliens.Components.AlienInfectedComponent;

namespace Content.Server.Aliens.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class FacehuggerSystem : EntitySystem
{
    /// <inheritdoc/>
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FacehuggerComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<FacehuggerComponent, GotUnequippedEvent>(OnUnequipped);
    }

    public void OnEquipped(EntityUid uid, FacehuggerComponent component, GotEquippedEvent args)
    {
        if(!component.Active || args.Slot != "mask" || !TryComp(uid, out MobStateComponent? mobStateComponent) || mobStateComponent.CurrentState == MobState.Dead)
            return;
        _stun.TryParalyze(args.Equipee, TimeSpan.FromSeconds(15), false);
        component.Equipped = true;
        component.Equipee = args.Equipee;
        component.Active = false;
        var curTime = _timing.CurTime;
        component.GrowTime = curTime + TimeSpan.FromSeconds(component.EmbryoTime);
        _popup.PopupEntity(Loc.GetString("facehugger-equipped-entity-other"),
            uid, PopupType.Medium);
    }

    private static void OnUnequipped(EntityUid uid, FacehuggerComponent component, GotUnequippedEvent args)
    {
        component.Equipped = false;
        component.Active = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FacehuggerComponent>();
        var growedLarva = new Dictionary<EntityUid, FacehuggerComponent>();
        while (query.MoveNext(out var uid, out var facehugger))
        {
            if(_timing.CurTime < facehugger.GrowTime || !facehugger.Equipped)
                continue;
            growedLarva.TryAdd(uid, facehugger);
        }

        foreach (var facehugger in growedLarva)
        {
            if(!HasComp<AlienInfectedComponent>(facehugger.Value.Equipee))
                AddComp<AlienInfectedComponent>(facehugger.Value.Equipee);
            _polymorph.PolymorphEntity(facehugger.Key, facehugger.Value.FacehuggerPolymorphPrototype);
        }
    }
}

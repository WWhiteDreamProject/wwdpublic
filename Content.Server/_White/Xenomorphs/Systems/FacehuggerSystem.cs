using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared._White.Xenomorphs.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server._White.Xenomorphs.Systems;

/// <summary>
/// This handles Facehugger interactions.
/// </summary>
public sealed class FacehuggerSystem : EntitySystem
{
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobstate = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FacehuggerComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<FacehuggerComponent, GotUnequippedEvent>(OnUnequipped);
    }

    public void OnEquipped(EntityUid uid, FacehuggerComponent component, GotEquippedEvent args)
    {
        if (!component.Active || args.Slot != "mask" || _mobstate.IsDead(uid))
            return;

        _stun.TryParalyze(args.Equipee, TimeSpan.FromSeconds(15), false);
        component.Equipped = true;
        component.Equipee = args.Equipee;
        component.Active = false;

        var curTime = _timing.CurTime;
        component.GrowTime = curTime + TimeSpan.FromSeconds(component.EmbryoTime);

        _popup.PopupEntity(Loc.GetString("facehugger-equipped-entity-other"), uid, PopupType.Medium);
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
            if (_timing.CurTime < facehugger.GrowTime || !facehugger.Equipped)
                continue;

            growedLarva.TryAdd(uid, facehugger);
        }

        foreach (var facehugger in growedLarva)
        {
            var uid = facehugger.Key;
            var component = facehugger.Value;

            EnsureComp<AlienInfectedComponent>(component.Equipee);
            _polymorph.PolymorphEntity(uid, component.FacehuggerPolymorphPrototype);
        }
    }
}

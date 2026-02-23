using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Bed.Components;
using Content.Server.Cloning.Components;
using Content.Shared._White.Body;
using Content.Shared._White.Body.Components;
using Content.Shared._White.Body.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed class IgniteFromGasSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    // All ignitions tick at the same time because FlammableSystem is also the same
    private const float UpdateTimer = 1f;
    private float _timer;

    public override void Initialize()
    {
        SubscribeLocalEvent<FlammableComponent, BodyPartAddedEvent>(OnBodyPartAdded);

        SubscribeLocalEvent<IgniteFromGasComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);

        SubscribeLocalEvent<IgniteFromGasImmunityComponent, GotEquippedEvent>(OnIgniteFromGasImmunityEquipped);
        SubscribeLocalEvent<IgniteFromGasImmunityComponent, GotUnequippedEvent>(OnIgniteFromGasImmunityUnequipped);
    }

    // WD EDIT START
    private void OnBodyPartAdded(Entity<FlammableComponent> ent, ref BodyPartAddedEvent args)
    {
        if (!TryComp<IgniteFromGasPartComponent>(args.Part, out var ignitePart))
            return;

        if (!TryComp<IgniteFromGasComponent>(ent, out var ignite))
        {
            ignite = EnsureComp<IgniteFromGasComponent>(ent);
            ignite.Gas = ignitePart.Gas;
        }

        ignite.IgnitableBodyParts[args.Part.Comp.Type] = ignitePart.FireStacks;

        UpdateIgniteImmunity((ent, ignite));
    }

    private void OnBodyPartRemoved(Entity<IgniteFromGasComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (!HasComp<IgniteFromGasPartComponent>(args.Part))
            return;

        ent.Comp.IgnitableBodyParts.Remove(args.Part.Comp.Type);

        if (ent.Comp.IgnitableBodyParts.Count == 0)
        {
            RemCompDeferred<IgniteFromGasComponent>(ent);
            return;
        }

        UpdateIgniteImmunity((ent, ent.Comp));
    }
    // WD EDIT END

    private void OnIgniteFromGasImmunityEquipped(Entity<IgniteFromGasImmunityComponent> ent, ref GotEquippedEvent args) =>
        UpdateIgniteImmunity(args.Equipee);
    private void OnIgniteFromGasImmunityUnequipped(Entity<IgniteFromGasImmunityComponent> ent, ref GotUnequippedEvent args) =>
        UpdateIgniteImmunity(args.Equipee);

    public void UpdateIgniteImmunity(Entity<IgniteFromGasComponent?, InventoryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return;

        var exposedBodyParts = new Dictionary<BodyPartType, float>(ent.Comp1.IgnitableBodyParts); // WD EDIT

        var containerSlotEnumerator = _inventory.GetSlotEnumerator((ent, ent.Comp2));
        while (containerSlotEnumerator.NextItem(out var item, out _))
        {
            if (!TryComp<IgniteFromGasImmunityComponent>(item, out var immunity))
                continue;

            foreach (var immunePart in immunity.Parts)
                exposedBodyParts.Remove(immunePart);
        }

        if (exposedBodyParts.Count == 0)
        {
            ent.Comp1.FireStacksPerUpdate = 0;
            return;
        }

        ent.Comp1.FireStacksPerUpdate = ent.Comp1.BaseFireStacksPerUpdate + exposedBodyParts.Values.Sum();
    }

    public override void Update(float frameTime)
    {
        _timer += frameTime;
        if (_timer < UpdateTimer)
            return;
        _timer -= UpdateTimer;

        var enumerator = EntityQueryEnumerator<IgniteFromGasComponent, MobStateComponent, FlammableComponent>();
        while (enumerator.MoveNext(out var uid, out var ignite, out var mobState, out var flammable))
        {
            if (ignite.FireStacksPerUpdate == 0 ||
                mobState.CurrentState is MobState.Dead ||
                HasComp<InStasisComponent>(uid) ||
                HasComp<BeingClonedComponent>(uid) ||
                _atmos.GetContainingMixture(uid, excite: true) is not { } gas ||
                gas[(int) ignite.Gas] < ignite.MolesToIgnite
                )
                continue;

            _flammable.AdjustFireStacks(uid, ignite.FireStacksPerUpdate, flammable);
            _flammable.Ignite(uid, uid, flammable, ignoreFireProtection: true);
        }
    }
}

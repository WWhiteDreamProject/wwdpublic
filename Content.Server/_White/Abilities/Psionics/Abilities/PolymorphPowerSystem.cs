using Content.Server.Abilities.Psionics;
using Content.Server.Humanoid;
using Content.Shared._White.Actions.Events;
using Content.Shared._White.Psionics.Abilities;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Humanoid;
using Robust.Server.GameObjects;

namespace Content.Server._White.Abilities.Psionics.Abilities;

public sealed class PolymorphPowerSystem : EntitySystem
{
    [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly TransformSystem _transform = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphPowerActionEvent>(OnPowerUsed);
        SubscribeLocalEvent<PolymorphPowerComponent, PolymorphPowerRevertActionEvent>(OnUsed);
        SubscribeLocalEvent<PolymorphPowerComponent, ComponentStartup>(ComponentStart);
        SubscribeLocalEvent<PolymorphPowerComponent, DispelledEvent>(OnDispelled);
    }

    public void OnPowerUsed(PolymorphPowerActionEvent args)
    {
        if (!_psionics.OnAttemptPowerUse(args.Performer, args.Target, "polymorph", true))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out var targetHumanoid))
            return;

        var target = args.Target;
        var user = args.Performer;

        if (TryComp<HumanoidAppearanceComponent>(user, out var humanoid))
        {
            _appearance.CloneAppearance(target, user);
        }

        var targetMeta = MetaData(target);
        _meta.SetEntityName(user, targetMeta.EntityName);
        _meta.SetEntityDescription(user, targetMeta.EntityDescription);
        _psionics.LogPowerUsed(args.Performer, "polymorph");
        args.Handled = true;
    }

    public void OnUsed(EntityUid uid, PolymorphPowerComponent comp, PolymorphPowerRevertActionEvent args)
    {
        ReturnAppearance(uid, comp);

        args.Handled = true;
    }

    private void OnDispelled(EntityUid uid, PolymorphPowerComponent component, DispelledEvent args)
    {
        ReturnAppearance(uid, component);

        args.Handled = true;
    }

    public void ReturnAppearance(EntityUid uid, PolymorphPowerComponent comp)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        var effect = Spawn("PsionicPolymorphEffect", _transform.GetMapCoordinates(uid));
        _transform.SetParent(effect, uid);

        _meta.SetEntityName(uid, comp.OriginalName);
        _meta.SetEntityDescription(uid, comp.OriginalDescription);
        _appearance.LoadProfile(uid, comp.OriginalProfile);
    }

    public void ComponentStart(EntityUid uid, PolymorphPowerComponent component, ComponentStartup args)
    {
        var meta = MetaData(uid);
        component.OriginalName = meta.EntityName;
        component.OriginalDescription = meta.EntityDescription;

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        component.OriginalProfile = humanoid.LastProfileLoaded;
    }
}

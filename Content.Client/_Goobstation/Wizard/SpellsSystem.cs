using System.Numerics;
using Content.Client.Animations;
using Content.Shared._Goobstation.Wizard;
using Content.Shared._Goobstation.Wizard.SupermatterHalberd;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;


namespace Content.Client._Goobstation.Wizard;


public sealed class SpellsSystem : SharedSpellsSystem
{
    [Dependency] private readonly RaysSystem _rays = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WizardComponent, GetStatusIconsEvent>(GetWizardIcon);
        SubscribeLocalEvent<ApprenticeComponent, GetStatusIconsEvent>(GetApprenticeIcon);

        SubscribeAllEvent<ChargeSpellRaysEffectEvent>(OnChargeEffect);
    }

    private void OnChargeEffect(ChargeSpellRaysEffectEvent ev)
    {
        var uid = GetEntity(ev.Uid);

        CreateChargeEffect(uid, ev);
    }

    public override void CreateChargeEffect(EntityUid uid, ChargeSpellRaysEffectEvent ev)
    {
        if (!Timing.IsFirstTimePredicted || uid == EntityUid.Invalid)
            return;

        var rays = _rays.DoRays(TransformSystem.GetMapCoordinates(uid),
            Color.Yellow,
            Color.Fuchsia,
            10,
            15,
            minMaxRadius: new Vector2(3f, 6f),
            proto: "EffectRayCharge",
            server: false);

        if (rays == null)
            return;

        var track = EnsureComp<TrackUserComponent>(rays.Value);
        track.User = uid;
    }

    private void GetWizardIcon(Entity<WizardComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetApprenticeIcon(Entity<ApprenticeComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}

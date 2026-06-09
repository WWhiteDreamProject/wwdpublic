// Updated by White Dream by FckMoth

using Content.Server.Light.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Tag;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Light.EntitySystems;

[UsedImplicitly]
public sealed class ExpendableLightSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendableLightComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ExpendableLightComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ExpendableLightComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveExpendableLightComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!TryComp(uid, out ExpendableLightComponent? light))
            {
                RemCompDeferred<ActiveExpendableLightComponent>(uid);
                continue;
            }

            light.StateExpiryTime -= frameTime;
            if (light.StateExpiryTime > 0f)
                continue;

            UpdateLight((uid, light));
        }
    }

    private void OnInit(EntityUid uid, ExpendableLightComponent component, ComponentInit args)
    {
        if (TryComp<ItemComponent>(uid, out var item))
            _item.SetHeldPrefix(uid, "unlit", component: item);

        component.CurrentState = ExpendableLightState.BrandNew;
        EntityManager.EnsureComponent<PointLightComponent>(uid);
    }

    private void OnUseInHand(Entity<ExpendableLightComponent> ent, ref UseInHandEvent args)
    {
        if (!args.Handled && TryActivate(ent))
            args.Handled = true;
    }

    private void OnGetVerbs(Entity<ExpendableLightComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.CurrentState != ExpendableLightState.BrandNew)
            return;

        // Ignite the flare or make the glowstick glow.
        // Also hot damn, those are some shitty glowsticks, we need to get a refund.
        ActivationVerb verb = new()
        {
            Text = Loc.GetString("expendable-light-start-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () => TryActivate(ent)
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Enables the light if it is not active. Once active it cannot be turned off.
    /// </summary>
    [UsedImplicitly]
    public bool TryActivate(Entity<ExpendableLightComponent> ent)
    {
        var component = ent.Comp;
        if (component is not { Activated: false, CurrentState: ExpendableLightState.BrandNew, })
            return false;

        if (TryComp(ent, out ItemComponent? item))
            _item.SetHeldPrefix(ent, "lit", component: item);

        var isHotEvent = new IsHotEvent { IsHot = true, };
        RaiseLocalEvent(ent, isHotEvent);

        component.CurrentState = ExpendableLightState.Lit;
        component.StateExpiryTime = component.GlowDuration;

        UpdateSounds(ent);
        UpdateVisualizer(ent);
        EnsureComp<ActiveExpendableLightComponent>(ent);
        return true;
    }

    private void UpdateLight(Entity<ExpendableLightComponent> ent)
    {
        switch (ent.Comp.CurrentState)
        {
            case ExpendableLightState.Lit:
                ent.Comp.CurrentState = ExpendableLightState.Fading;
                ent.Comp.StateExpiryTime = ent.Comp.FadeOutDuration;
                UpdateVisualizer(ent);
                break;

            case ExpendableLightState.Fading:
                ent.Comp.CurrentState = ExpendableLightState.Dead;
                UpdateSounds(ent);
                UpdateVisualizer(ent);

                var meta = MetaData(ent);
                _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.SpentName), meta);
                _metaData.SetEntityDescription(ent, Loc.GetString(ent.Comp.SpentDesc), meta);
                _tagSystem.AddTag(ent, "Trash");

                if (TryComp(ent, out ItemComponent? item))
                    _item.SetHeldPrefix(ent, "unlit", component: item);

                if (TryComp(ent, out CartridgeAmmoComponent? ammo))
                    ammo.Spent = true;

                var isHotEvent = new IsHotEvent { IsHot = false, };
                RaiseLocalEvent(ent, isHotEvent);
                RemCompDeferred<ActiveExpendableLightComponent>(ent);
                break;

            case ExpendableLightState.Dead:
            case ExpendableLightState.BrandNew:
            default:
                break;
        }
    }

    private void UpdateVisualizer(Entity<ExpendableLightComponent> ent)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance))
            return;

        var expendableLight = ent.Comp;
        _appearance.SetData(ent, ExpendableLightVisuals.State, expendableLight.CurrentState, appearance);

        var behaviorState = expendableLight.CurrentState switch
        {
            ExpendableLightState.Lit => expendableLight.TurnOnBehaviourID,
            ExpendableLightState.Fading => expendableLight.FadeOutBehaviourID,
            _ => string.Empty
        };

        _appearance.SetData(ent, ExpendableLightVisuals.Behavior, behaviorState, appearance);
    }

    private void UpdateSounds(Entity<ExpendableLightComponent> ent)
    {
        switch (ent.Comp.CurrentState)
        {
            case ExpendableLightState.Lit:
                _audio.PlayPvs(ent.Comp.LitSound, ent);
                break;
            case ExpendableLightState.Dead:
                _audio.PlayPvs(ent.Comp.DieSound, ent);
                break;
            case ExpendableLightState.BrandNew:
            case ExpendableLightState.Fading:
            default:
                break;
        }

        if (TryComp<ClothingComponent>(ent, out var clothing))
            _clothing.SetEquippedPrefix(ent, ent.Comp.Activated ? "Activated" : string.Empty, clothing);
    }
}

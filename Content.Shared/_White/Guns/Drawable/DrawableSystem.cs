using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Guns.Drawable;

public sealed class DrawableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrawableComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DrawableComponent, EntRemovedFromContainerMessage>(OnItemRemove);
        SubscribeLocalEvent<DrawableComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<DrawableComponent, UseInHandEvent>(OnUse);
    }

    private void OnStartup(EntityUid uid, DrawableComponent component, ComponentStartup args)
    {
        component.Provider = EnsureComp<BallisticAmmoProviderComponent>(uid);
    }

    private void OnItemRemove(EntityUid uid, DrawableComponent component, EntRemovedFromContainerMessage args)
    {
        if (!component.Drawn || args.Container.ID != component.Provider.Container.ID)
            return;

        component.Drawn = false;
        UpdateDrawableAppearance(uid, component);
    }

    private void OnAttemptShoot(EntityUid uid, DrawableComponent component, ref AttemptShootEvent args)
    {
        if (!component.Drawn)
            args.Cancelled = true;
    }

    private void OnUse(EntityUid uid, DrawableComponent component, UseInHandEvent args)
    {
        if (component.Drawn || component.Provider.Count == 0)
            return;

        args.Handled = true;

        _audio.PlayPredicted(component.SoundDraw, uid, args.User);
        component.Drawn = true;

        UpdateDrawableAppearance(uid, component);
    }

    private void UpdateDrawableAppearance(EntityUid uid, DrawableComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, DrawableVisuals.Drawn, component.Drawn, appearance);
    }
}

[Serializable, NetSerializable]
public enum DrawableVisuals : byte
{
    Drawn,
    Layer
}

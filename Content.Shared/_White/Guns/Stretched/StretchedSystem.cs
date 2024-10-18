using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Guns.Stretched;

public sealed class StretchedSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StretchedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StretchedComponent, EntRemovedFromContainerMessage>(OnItemRemove);
        SubscribeLocalEvent<StretchedComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<StretchedComponent, UseInHandEvent>(OnUse);
    }

    private void OnStartup(EntityUid uid, StretchedComponent component, ComponentStartup args)
    {
        component.Provider = EnsureComp<BallisticAmmoProviderComponent>(uid);
    }

    private void OnItemRemove(EntityUid uid, StretchedComponent component, EntRemovedFromContainerMessage args)
    {
        if (!component.Stretched || args.Container.ID != component.Provider.Container.ID)
            return;

        component.Stretched = false;
        UpdateDrawableAppearance(uid, component);
    }

    private void OnAttemptShoot(EntityUid uid, StretchedComponent component, ref AttemptShootEvent args)
    {
        if (!component.Stretched)
            args.Cancelled = true;
    }

    private void OnUse(EntityUid uid, StretchedComponent component, UseInHandEvent args)
    {
        if (component.Stretched || component.Provider.Count == 0)
            return;

        args.Handled = true;

        _audio.PlayPredicted(component.SoundDraw, uid, args.User);
        component.Stretched = true;

        UpdateDrawableAppearance(uid, component);
    }

    private void UpdateDrawableAppearance(EntityUid uid, StretchedComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, StretchedVisuals.Stretched, component.Stretched, appearance);
    }
}

[Serializable, NetSerializable]
public enum StretchedVisuals : byte
{
    Stretched,
    Layer
}

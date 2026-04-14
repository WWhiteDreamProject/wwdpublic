// D:\projects\night-station\Content.Shared\_NC\Cyberware\Systems\CyberwareArmorSystem.cs
using Content.Shared._NC.Cyberware.Components;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared._NC.Cyberware.Systems;

/// <summary>
///     Handles transferring armor modifiers from installed cyberware to the host.
/// </summary>
public sealed class CyberwareArmorSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberwareComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid, CyberwareComponent component, DamageModifyEvent args)
    {
        // We iterate through all installed implants and check if they have ArmorComponent.
        // If they do, we apply their modifiers to the damage specifier.
        foreach (var implantUid in component.InstalledImplants.Values)
        {
            if (!_entManager.TryGetComponent<ArmorComponent>(implantUid, out var armor))
                continue;

            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, armor.Modifiers);
        }
    }
}

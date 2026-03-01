using Content.Shared._NC.Weapons.Ranged.NCWeapon;
using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Shared._NC.Weapons.Ranged.Systems;

/// <summary>
/// Shared-система для отображения информации об оружейном бренде при осмотре (examine).
/// Показывает: производитель, тир, прочность, статусы (заклинивание, перегрев, поломка).
/// </summary>
public sealed class NCWeaponExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NCWeaponComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    /// Обработка осмотра оружия — добавляет информацию о бренде и состоянии.
    /// </summary>
    private void OnExamined(EntityUid uid, NCWeaponComponent component, ExaminedEvent args)
    {
        // Производитель и тир
        var manufacturerName = Loc.GetString($"nc-weapon-manufacturer-{component.Manufacturer.ToString().ToLowerInvariant()}");
        var tierName = Loc.GetString($"nc-weapon-tier-{component.Tier.ToString().ToLowerInvariant()}");

        args.PushMarkup(Loc.GetString("nc-weapon-examine-brand",
            ("manufacturer", manufacturerName),
            ("tier", tierName)));

        // Прочность — цветовая индикация
        if (component.MaxDurability > 0)
        {
            var ratio = component.Durability / component.MaxDurability;
            var color = ratio switch
            {
                > 0.7f => "green",
                > 0.3f => "yellow",
                > 0f => "red",
                _ => "darkred"
            };

            args.PushMarkup(Loc.GetString("nc-weapon-examine-durability",
                ("current", MathF.Round(component.Durability)),
                ("max", MathF.Round(component.MaxDurability)),
                ("color", color)));
        }

        // Статусы
        if (component.IsBroken)
        {
            args.PushMarkup(Loc.GetString("nc-weapon-examine-broken"));
        }
        else if (component.IsJammed)
        {
            args.PushMarkup(Loc.GetString("nc-weapon-examine-jammed"));
        }

        if (component.IsOverheated)
        {
            args.PushMarkup(Loc.GetString("nc-weapon-examine-overheated"));
        }

        // Неремонтопригодность
        if (!component.IsRepairable)
        {
            args.PushMarkup(Loc.GetString("nc-weapon-examine-unrepairable"));
        }
    }
}

using Content.Server.CombatMode.Disarm;
using Content.Shared.Examine;
using Content.Shared.Wieldable;


namespace Content.Server._White.Disarm;

public sealed class DisarmMalusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisarmMalusComponent, ComponentInit>(OnMalusInit);
        SubscribeLocalEvent<DisarmMalusComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<DisarmMalusComponent, ItemUnwieldedEvent>(OnUnwielded);
        SubscribeLocalEvent<DisarmMalusComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMalusInit(EntityUid uid, DisarmMalusComponent component, ComponentInit args)
    {
        component.CurrentMalus = component.Malus;
    }

    private void OnExamined(EntityUid uid, DisarmMalusComponent component, ExaminedEvent args)
    {
        var malus = (int)(component.CurrentMalus * 100); // human readable X%
        var description = Loc.GetString("disarm-malus-examined", ("malus", malus));

        args.PushMarkup(description);
    }

    private void OnWielded(EntityUid uid, DisarmMalusComponent component, ItemWieldedEvent args)
    {
        component.CurrentMalus = component.Malus + component.WieldedBonus;
    }

    private void OnUnwielded(EntityUid uid, DisarmMalusComponent component, ItemUnwieldedEvent args)
    {
        component.CurrentMalus = component.Malus;
    }
}

using Content.Server.Popups;
using Content.Shared._White.BloodCult.Empower;
using Content.Shared._White.BloodCult.Runes.Components;

namespace Content.Server._White.BloodCult.Runes.Empower;

public sealed class CultRuneEmpowerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneEmpowerComponent, InvokeRuneEvent>(OnStrengthRuneInvoked);
    }

    private void OnStrengthRuneInvoked(Entity<CultRuneEmpowerComponent> ent, ref InvokeRuneEvent args)
    {
        if (HasComp<BloodCultEmpoweredComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-rune-empower-already-buffed"), args.User, args.User);
            args.Cancel();
            return;
        }

        AddComp<BloodCultEmpoweredComponent>(args.User);
    }
}

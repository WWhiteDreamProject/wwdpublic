using Content.Client.Popups;
using Content.Shared._White.BloodCult.Runes;
using Content.Shared._White.BloodCult.Runes.Components;
using Content.Shared.Interaction;

namespace Content.Client._White.BloodCult.Runes;

public sealed class BloodCultRuneSystem : SharedBloodCultRuneSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultRuneComponent, ActivateInWorldEvent>(OnRuneActivate);
    }

    private void OnRuneActivate(Entity<BloodCultRuneComponent> rune, ref ActivateInWorldEvent args)
    {
        args.Handled = true;

        var cultists = GatherCultists(rune, rune.Comp.RuneActivationRange);
        if (cultists.Count < rune.Comp.RequiredInvokers)
        {
            _popup.PopupClient(Loc.GetString("cult-rune-not-enough-cultists"), rune, args.User);
            return;
        }

        var tryInvokeEv = new TryInvokeCultRuneEvent(args.User, cultists);
        RaiseLocalEvent(rune, tryInvokeEv);
    }
}

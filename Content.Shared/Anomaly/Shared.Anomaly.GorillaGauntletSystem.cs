using Content.Shared.Emag.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Anomaly.GorillaGauntletSystem;

/// <summary>
/// Handles emagging Weldbots
/// </summary>
public sealed class GorillaGauntlet : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GorillaGauntletComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnEmagged(EntityUid uid, Component comp, ref GotEmaggedEvent args)
    {
        _audio.PlayPredicted(comp.EmagSparkSound, uid, args.UserUid);

        comp.IsEmagged = true;
        args.Handled = true;
    }
}

 private void OnGotEmagged(EntityUid uid, ref GotEmaggedEvent args)
    {
    
    }

        args.Repeatable = true;
        args.Handled = true;
    }

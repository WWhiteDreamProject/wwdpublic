using Content.Shared.Abilities.Psionics;
using Content.Shared.StatusEffect;
using Content.Server.Stunnable;
using Content.Server.Beam;
using Content.Shared.Actions.Events;

namespace Content.Server.Abilities.Psionics
{
    public sealed class NoosphericZapPowerSystem : EntitySystem
    {
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly BeamSystem _beam = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NoosphericZapPowerActionEvent>(OnPowerUsed);
        }

        private void OnPowerUsed(NoosphericZapPowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, args.Target, "noospheric zap", true))
                return;

            if (!TryComp<PsionicComponent>(args.Performer, out var psionic)) //wwdp start
                return;
            
            var stunTime = 1f + psionic.CurrentAmplification * 1.2f; //wwdp end

            _beam.TryCreateBeam(args.Performer, args.Target, "LightningNoospheric");

            _stunSystem.TryParalyze(args.Target, TimeSpan.FromSeconds(stunTime), false); //wwdp edit: amplification is useful
            _statusEffectsSystem.TryAddStatusEffect(args.Target, "Stutter", TimeSpan.FromSeconds(10), false, "StutteringAccent");

            _psionics.LogPowerUsed(args.Performer, "noospheric zap");
            args.Handled = true;
        }
    }
}

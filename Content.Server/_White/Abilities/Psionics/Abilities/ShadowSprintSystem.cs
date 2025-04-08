using Content.Shared.Abilities.Psionics;
using Content.Shared.Shadowkin;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared._White.Psionics.Events;

namespace Content.Server._White.Abilities.Psionics
{
    public sealed class ShadowSprintSystem : EntitySystem
    {
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShadowSprintActionEvent>(OnPowerUsed);
        }

        private void OnPowerUsed(ShadowSprintActionEvent args)
        {
            if (!TryComp<MovementSpeedModifierComponent>(args.Performer, out var movement))
            return;
            var bW = movement.BaseWalkSpeed;
            var bS = movement.BaseSprintSpeed;
            var ac = movement.Acceleration;
            _speedModifier.ChangeBaseSpeed(args.Performer, bW * 1.5f, bS * 1.5f, ac * 1.2f, movement);
            EnsureComp<EtherealComponent>(args.Performer);
            SpawnAtPosition("ShadowkinShadow", Transform(args.Performer).Coordinates);  
            SpawnAtPosition("EffectFlashShadowkinDarkSwapOn", Transform(args.Performer).Coordinates);  
            args.Performer.SpawnTimer(TimeSpan.FromSeconds(2), () =>  
            {
                if (TryComp<MovementSpeedModifierComponent>(args.Performer, out var Cmovement))
            {
                _speedModifier.ChangeBaseSpeed(args.Performer, bW, bS, ac, Cmovement);
            }

                if (HasComp<EtherealComponent>(args.Performer))   
                {  
                    SpawnAtPosition("ShadowkinShadow", Transform(args.Performer).Coordinates);  
                    SpawnAtPosition("EffectFlashShadowkinDarkSwapOff", Transform(args.Performer).Coordinates);  
                    RemComp<EtherealComponent>(args.Performer);  
                }  
            });  

            args.Handled = true;
        }
    }
}
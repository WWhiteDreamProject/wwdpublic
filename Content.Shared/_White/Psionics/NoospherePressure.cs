using Content.Shared.Abilities.Psionics;
using Content.Shared.Actions.Events;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;

namespace Content.Shared._White.Psionics;

[RegisterComponent]
public sealed partial class NoospherePressureComponent : Component 
{
    [DataField]
    public int Pressure = 0;

    [DataField]
    public bool staticPressure = true;

    [DataField]
    public int DecayRate = 2;

    [DataField]
    public float MediumPressure = 60f;

    [DataField]
    public float MaxPressure = 150f;
}

public sealed class NoospherePressuresSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NoospherePressureComponent, ActionAttemptEvent>(OnPsionicPowerUsed);
    }

    private void OnPsionicPowerUsed(Entity<NoospherePressureComponent> ent, ref ActionAttemptEvent args)
    {
        if(!TryComp<NoospherePressureComponent>(args.User, out var comp))
            return;
        
        if(TryComp<ActorComponent>(args.User, out var actor))
                comp.staticPressure = false;
        
        comp.Pressure += ent.Comp.Pressure;

        if (comp.Pressure > comp.MediumPressure)
        {
            var psionicOverloadEvent = new PsionicOverloadEvent(args.User);
            RaiseLocalEvent(args.User, psionicOverloadEvent);
        }
    }
}
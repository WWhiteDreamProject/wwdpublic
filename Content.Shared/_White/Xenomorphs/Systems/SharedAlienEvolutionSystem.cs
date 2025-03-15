using Content.Shared.Actions;

namespace Content.Shared._White.Xenomorphs.Systems;

public sealed class SharedAlienEvolutionSystem : EntitySystem
{
    public override void Initialize()
    {

    }
}

public sealed partial class AlienDroneEvolveActionEvent : InstantActionEvent { }

public sealed partial class AlienSentinelEvolveActionEvent : InstantActionEvent { }

public sealed partial class AlienPraetorianEvolveActionEvent : InstantActionEvent { }

public sealed partial class AlienHunterEvolveActionEvent : InstantActionEvent { }

public sealed partial class AlienQueenEvolveActionEvent : InstantActionEvent { }

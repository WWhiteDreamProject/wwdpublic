using Content.Shared.PDA;
using Content.Shared.UserInterface;
using Robust.Shared.Timing;

namespace Content.Shared._White.PDA.Animation;

/// <summary>
/// Shared system for animated PDA
/// </summary>
public abstract class SharedPdaAnimatedSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected void UpdateAppearance(EntityUid uid, PdaAnimatedComponent comp)
    {
        Appearance.SetData(uid, PdaVisuals.AnimationState, comp.AnimationState);
    }
}

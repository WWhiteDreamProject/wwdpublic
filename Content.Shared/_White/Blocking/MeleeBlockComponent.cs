using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared._White.Blocking;

[RegisterComponent]
public sealed partial class MeleeBlockComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(3.1);

    [DataField]
    public SoundSpecifier BlockSound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.25f)
    };
}

public sealed class MeleeBlockAttemptEvent(EntityUid attacker, DamageSpecifier damage) : HandledEntityEventArgs
{
    public EntityUid Attacker = attacker;

    public DamageSpecifier Damage = damage;
}

using Content.Shared.Flash;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components
{
    [RegisterComponent, NetworkedComponent, Access(typeof(SharedFlashSystem))]
    public sealed partial class FlashComponent : Component
    {

        [DataField("duration")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float FlashDuration { get; set; } = 5;

        /// <summary>
        /// How long a target is stunned when a melee flash is used.
        /// If null, melee flashes will not stun at all
        /// </summary>
        [DataField]
        public TimeSpan? MeleeStunDuration = TimeSpan.FromSeconds(1.5);

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Range { get; set; } = 7f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("aoeFlashDuration")]
        public float AoeFlashDuration { get; set; } = 2;

        [DataField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SlowTo { get; set; } = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField]
        public SoundSpecifier Sound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/flash.ogg")
        {
            Params = AudioParams.Default.WithVolume(1f).WithMaxDistance(3f)
        };

        public bool Flashing;

        [DataField]
        public float Probability = 1f;
    }
}

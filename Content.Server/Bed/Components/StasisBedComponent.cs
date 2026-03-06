using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed partial class StasisBedComponent : Component
    {
        [DataField]
        public float BaseMultiplier = 0.1f; // WD EDIT

        /// <summary>
        ///     What the metabolic update rate will be multiplied by (lesser = slower metabolism)
        /// </summary>
        [DataField]
        public float Multiplier = 0.1f; // WD EDIT

        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartMetabolismModifier = "Capacitor";
    }
}

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Holosign
{
    [RegisterComponent]
    public sealed partial class HolosignProjectorComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("signProto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string SignProto = "HolosignWetFloor";

        // WD EDIT START
        [DataField]
        public int MaxUses = 6;

        [DataField]
        public int Uses = 6;

        [ViewVariables(VVAccess.ReadOnly)]
        public List<EntityUid?> Signs = new();
        // WD EDIT END
    }
}

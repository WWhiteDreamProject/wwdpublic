using Content.Shared.Damage.Prototypes;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.StepTrigger.Prototypes
{
    /// <summary>
    ///  A group of <see cref="StepTriggerTypePrototype">
    ///
    /// </summary>
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class StepTriggerGroup
    {
        [DataField("Types", customTypeSerializer:typeof(PrototypeIdListSerializer<StepTriggerTypePrototype>))]
        [Access(typeof(StepTriggerSystem), Other = AccessPermissions.ReadExecute)]
        public List<string> Types = new();
    }
}

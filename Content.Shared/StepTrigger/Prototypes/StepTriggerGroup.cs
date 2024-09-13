using Content.Shared.Damage.Prototypes;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.StepTrigger.Prototypes
{
    /// <summary>
    /// A group of <see cref="StepTriggerTypePrototype">
    /// Used to determine StepTriggerTypes like Tags.
    /// Used for better work with Immunity.
    /// WD EDIT
    /// </summary>
    /// <code>
    /// stepTriggerGroup:
    ///   type:
    ///   - Lava
    ///   - Landmine
    ///   - Shard
    ///   - Liquid
    ///   - Soap
    ///   - Chasm
    ///   - Mousetrap
    ///   - Banana
    /// </code>
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class StepTriggerGroup
    {
        [DataField("types", customTypeSerializer:typeof(PrototypeIdListSerializer<StepTriggerTypePrototype>))]
        public List<string>? Types = null;
    }
}

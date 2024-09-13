using Robust.Shared.Prototypes;

namespace Content.Shared.StepTrigger.Prototypes
{
    [Prototype("StepTriggerType")]
    public sealed partial class StepTriggerTypePrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;
    }
}


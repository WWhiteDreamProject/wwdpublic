using Content.Shared._White.Body.Bloodstream;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class BloodReagentData : ReagentData
{
    [DataField]
    public BloodGroup BloodGroup = new (BloodType.O, BloodRhesusFactor.Negative);

    public override ReagentData Clone() => this;

    public override bool Equals(ReagentData? other)
    {
        if (other is not BloodReagentData data)
            return false;

        return data.BloodGroup == BloodGroup;
    }

    public override int GetHashCode() => BloodGroup.GetHashCode();
}

using Content.Shared._White.Bloodstream;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class BloodReagentData : ReagentData
{
    [DataField]
    public BloodGroup Group = new (BloodType.O, BloodRhesusFactor.Negative);

    public override ReagentData Clone()
    {
        return this;
    }

    public override bool Equals(ReagentData? other)
    {
        if (other is not BloodReagentData data)
            return false;

        return data.Group == Group;
    }

    public override int GetHashCode()
    {
        return Group.GetHashCode();
    }
}

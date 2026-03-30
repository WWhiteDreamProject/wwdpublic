using System;
using Robust.Shared.Serialization;

namespace Content.Shared._NC.Cyberware.UI;

[Serializable, NetSerializable]
public enum BiomonitorUiKey : byte
{
    Key = 0
}

[Serializable, NetSerializable]
public sealed class BiomonitorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly int HealingCount;
    public readonly int TraumaCount;
    public readonly float CurrentHumanity;
    public readonly float MaxHumanity;

    public BiomonitorBoundUserInterfaceState(int healingCount, int traumaCount, float currentHumanity, float maxHumanity)
    {
        HealingCount = healingCount;
        TraumaCount = traumaCount;
        CurrentHumanity = currentHumanity;
        MaxHumanity = maxHumanity;
    }
}

namespace Content.Shared._White.Bark;


[DataDefinition]
public sealed partial class BarkPercentageApplyData
{
    public static BarkPercentageApplyData Default => new();

    [DataField] public byte Pause { get; set; } = byte.MaxValue / 2;
    [DataField] public byte Volume { get; set; } = byte.MaxValue / 2;
    [DataField] public byte Pitch { get; set; } = byte.MaxValue / 2;
    [DataField] public byte PitchVariance { get; set; } = byte.MaxValue / 2;
}

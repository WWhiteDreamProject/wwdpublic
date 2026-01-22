using Robust.Shared.Serialization;

namespace Content.Shared._NC.Trade;

[Serializable, NetSerializable]
public sealed class ContractTargetClientData
{
    [DataField("match")]
    public PrototypeMatchMode MatchMode = PrototypeMatchMode.Exact;

    public ContractTargetClientData() { }

    public ContractTargetClientData(string targetItem, int required, int progress)
    {
        TargetItem = targetItem;
        Required = required;
        Progress = progress;
    }

    public string TargetItem { get; set; } = string.Empty;
    public int Required { get; set; }
    public int Progress { get; set; }
}

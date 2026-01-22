using Robust.Shared.Serialization;

namespace Content.Shared._NC.Trade;

[Serializable, NetSerializable]
public sealed class ContractClientData
{
    public bool Completed;
    public string Description = string.Empty;
    public string Difficulty = string.Empty;
    public string Id = string.Empty;
    public string Name = string.Empty;
    public int Progress;

    public bool Repeatable;
    public int Required;
    public List<ContractRewardData> Rewards = new();

    public string TargetItem = string.Empty;

    public List<ContractTargetClientData> Targets = new();

    public ContractClientData() { }

    public ContractClientData(
        string id,
        string name,
        string difficulty,
        string description,
        bool repeatable,
        bool completed,
        string targetItem,
        int required,
        int progress,
        List<ContractTargetClientData> targets,
        List<ContractRewardData> rewards)
    {
        Id = id;
        Name = name;
        Difficulty = difficulty;
        Description = description;
        Repeatable = repeatable;
        Completed = completed;
        TargetItem = targetItem;
        Required = required;
        Progress = progress;
        Targets = targets;
        Rewards = rewards;
    }
}

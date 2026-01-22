namespace Content.Shared._NC.Trade;

// ============================================================
// Contracts - Targets / Server contract snapshot
// ============================================================

[Serializable]
public sealed class ContractTargetServerData
{
    [DataField("match")]
    public PrototypeMatchMode MatchMode = PrototypeMatchMode.Exact;

    public string TargetItem { get; set; } = string.Empty;
    public int Required { get; set; }
    public int Progress { get; set; }
}

[Serializable]
public sealed class ContractServerData
{
    [DataField("match")]
    public PrototypeMatchMode MatchMode = PrototypeMatchMode.Exact;

    public List<ContractTargetServerData> Targets { get; set; } = new();

    public string TargetItem { get; set; } = string.Empty;
    public int Required { get; set; }
    public int Progress { get; set; }

    public bool Repeatable { get; set; } = true;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string Difficulty { get; set; } = "Easy";
    public string Description { get; set; } = string.Empty;

    public List<ContractRewardData> Rewards { get; set; } = new();

    public bool Completed
    {
        get
        {
            if (Targets.Count > 0)
            {
                var any = false;
                foreach (var t in Targets)
                {
                    if (t.Required <= 0)
                        continue;

                    any = true;
                    if (t.Progress < t.Required)
                        return false;
                }

                return any;
            }

            return Required > 0 && Progress >= Required;
        }
    }
}

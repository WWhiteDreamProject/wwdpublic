using Content.Server.Maps.NameGenerators;
using Robust.Shared.Random;

namespace Content.Server._War.TotalWar;

public sealed partial class FactionNameGenerator : StationNameGenerator
{
    [DataField("faction")] public string FactionPrefix = default!;
    [DataField("subfaction")] public string SubFactionPrefix = default!;
    private string[] SuffixCodes => new []{ "Alpha" };

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        // No way in hell am I writing custom format code just to add nice names. You can live with {0}
        return string.Format(input, $"{FactionPrefix}{SubFactionPrefix}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 999):D3}");
    }
}

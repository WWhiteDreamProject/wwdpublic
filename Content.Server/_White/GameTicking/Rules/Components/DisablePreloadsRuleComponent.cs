using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent]
public sealed partial class DisablePreloadsRuleComponent : Component
{
    [DataField] public bool OriginalArrivalsShuttles { get; set; }
    [DataField] public bool OriginalAsteroidFieldEnabled { get; set; }
    [DataField] public bool OriginalProcgenPreload { get; set; }
    [DataField] public bool OriginalGridFill { get; set; }
    [DataField] public bool OriginalPreloadGrids { get; set; }
    [DataField] public bool OriginalLavalandEnabled { get; set; }
    [DataField] public bool OriginalIsAspectsEnabled { get; set; }
}

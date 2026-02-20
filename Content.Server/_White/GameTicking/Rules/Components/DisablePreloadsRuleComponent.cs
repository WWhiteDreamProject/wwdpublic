namespace Content.Server._White.GameTicking.Rules.Components;

[RegisterComponent]
public sealed partial class DisablePreloadsRuleComponent : Component
{
    [ViewVariables]
    public bool OriginalArrivalsShuttles;

    [ViewVariables]
    public bool OriginalAsteroidFieldEnabled;

    [ViewVariables]
    public bool OriginalProcgenPreload;

    [ViewVariables]
    public bool OriginalGridFill;

    [ViewVariables]
    public bool OriginalPreloadGrids;

    [ViewVariables]
    public bool OriginalLavalandEnabled;

    [ViewVariables]
    public bool OriginalIsAspectsEnabled;
}

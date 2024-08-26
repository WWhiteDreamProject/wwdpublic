using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Intent;

[RegisterComponent, NetworkedComponent]
public sealed partial class IntentComponent : Component
{
    [DataField]
    public Intent Intent;

    #region Actions

    [DataField]
    public string HelpAction = "ActionHelpToggle";

    [DataField]
    public EntityUid? HelpActionEntity;

    [DataField]
    public string DisarmAction = "ActionDisarmToggle";

    [DataField]
    public EntityUid? DisarmActionEntity;

    [DataField]
    public string GrabAction = "ActionGrabToggle";

    [DataField]
    public EntityUid? GrabActionEntity;

    [DataField]
    public string HarmAction = "ActionHarmToggle";

    [DataField]
    public EntityUid? HarmActionEntity;

    #endregion
}

[Serializable, NetSerializable]
public enum Intent : byte
{
    Help,
    Disarm,
    Grab,
    Harm
}

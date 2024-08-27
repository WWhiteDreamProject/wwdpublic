using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Intent;

[RegisterComponent, NetworkedComponent]
public sealed partial class IntentComponent : Component
{
    [DataField]
    public Intent Intent;

    [DataField]
    public bool ToggleMouseRotator = true;

    #region Grab

    [DataField]
    public bool CanGrab = true;

    #endregion

    #region Disarm

    [DataField]
    public bool CanDisarm = true;

    [DataField]
    public SoundSpecifier DisarmSuccessSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField]
    public float BaseDisarmFailChance = 0.75f;

    #endregion

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

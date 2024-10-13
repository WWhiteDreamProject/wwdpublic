using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._White.Overlays;

[RegisterComponent, NetworkedComponent]
public abstract partial class SwitchableOverlayComponent : BaseOverlayComponent
{
    [DataField]
    public bool IsActive = true;

    [DataField]
    public virtual SoundSpecifier? ActivateSound { get; set; }

    [DataField]
    public virtual SoundSpecifier? DeactivateSound { get; set; }

    [DataField]
    public virtual string? ToggleAction { get; set; }

    [ViewVariables]
    public EntityUid? ToggleActionEntity;
}

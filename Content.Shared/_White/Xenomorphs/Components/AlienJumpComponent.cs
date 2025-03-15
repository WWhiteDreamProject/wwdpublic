using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Aliens.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class AlienJumpComponent : Component
{
    public EntProtoId Action = "ActionJumpAlien";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public float JumpTime = 1f;

    [DataField]
    public ResPath Sprite { get; set; }

    public SpriteSpecifier? OldSprite;

    public bool Hit = false;
}

public sealed partial class AlienJumpActionEvent : WorldTargetActionEvent { }

[NetSerializable]
[Serializable]
public enum JumpVisuals : byte
{
    Jumping
}

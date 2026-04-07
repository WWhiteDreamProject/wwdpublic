using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._White.Psionics.Abilities;

[RegisterComponent]
public sealed partial class PsionicHookPowerComponent : Component
{
    [DataField]
    public EntProtoId HookPrototype = "WeaponPsionicHook";

    [DataField]
    public SoundSpecifier SoundOnSpawn = new SoundPathSpecifier("/Audio/Effects/Gib2.ogg");

    [DataField]
    public SoundSpecifier SoundOnDespawn = new SoundPathSpecifier("/Audio/Effects/Gib3.ogg");

    [DataField]
    public TimeSpan BreakoutTime = TimeSpan.FromSeconds(8f);

    [DataField]
    public string UncuffPopup = "psionic-uncuff-popup";

    [DataField]
    public EntityUid? Hook;
}

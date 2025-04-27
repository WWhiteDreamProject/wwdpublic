using Robust.Shared.Serialization;

namespace Content.Shared.VoiceMask;

[Serializable, NetSerializable]
public enum VoiceMaskUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class VoiceMaskBuiState : BoundUserInterfaceState
{
    public readonly string Name;
    public readonly string Voice; // WD EDIT
    public readonly string? Verb;

    public VoiceMaskBuiState(string name, string voice, string? verb) // WD EDIT
    {
        Name = name;
        Voice = voice; // WD EDIT
        Verb = verb;
    }
}

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeNameMessage : BoundUserInterfaceMessage
{
    public readonly string Name;

    public VoiceMaskChangeNameMessage(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Change the speech verb prototype to override, or null to use the user's verb.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVerbMessage : BoundUserInterfaceMessage
{
    public readonly string? Verb;

    public VoiceMaskChangeVerbMessage(string? verb)
    {
        Verb = verb;
    }
}

// WD EDIT START
[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVoiceMessage(string voice) : BoundUserInterfaceMessage
{
    public string Voice = voice;
}
// WD EDIT END

using Robust.Shared.Serialization;

namespace Content.Shared._White.BloodCult.UI;

[Serializable, NetSerializable]
public enum NameSelectorUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class NameSelectedMessage(string name)
    : BoundUserInterfaceMessage
{
    public string Name { get; } = name;
}

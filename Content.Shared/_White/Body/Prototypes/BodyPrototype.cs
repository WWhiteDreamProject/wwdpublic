using Content.Shared._White.Body.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Body.Prototypes;

[Prototype]
public sealed partial class BodyPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField]
    public string Root { get; private set; } = string.Empty;

    [DataField]
    public Dictionary<string, BodyPartSlot> BodyParts { get; private set; } = new();

    [DataField]
    public Dictionary<string, OrganSlot> Organs { get; private set; } = new();

    private BodyPrototype() { }

    public BodyPrototype(string id, string root, Dictionary<string, BodyPartSlot> bodyParts, Dictionary<string, OrganSlot> organs)
    {
        ID = id;
        Root = root;
        BodyParts = bodyParts;
        Organs = organs;
    }
}

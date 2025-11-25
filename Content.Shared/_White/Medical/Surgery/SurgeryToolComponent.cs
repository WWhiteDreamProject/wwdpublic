using Content.Shared.Inventory;
using Content.Shared.Whitelist;

namespace Content.Shared._White.Medical.Surgery;

[RegisterComponent]
public sealed partial class SurgeryToolComponent : Component
{
    [DataField]
    public SlotFlags? SlotsToCheck;

    [DataField]
    public EntityWhitelist? ClothingWhitelist;

    [DataField]
    public EntityWhitelist? ClothingBlacklist;
}

using Content.Shared._White.Glasses.Components;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Shared.Inventory;
using Content.Shared.StationRecords;
using Content.Shared.Humanoid;

namespace Content.Shared._White.Glasses.Systems;

public abstract class SharedSecurityGlassesWantedStatusSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    
    private const string VerbIconPath = "/Textures/Interface/VerbIcons/refresh.svg.192dpi.png";
    private const int VerbPriority = 10;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(AddChangeStatusVerb);
    }

    private void AddChangeStatusVerb(GetVerbsEvent<AlternativeVerb> args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;
            
        if (!_inventory.TryGetSlotEntity(args.User, "eyes", out var glassesUid))
            return;
            
        if (!HasComp<SecurityGlassesWantedStatusComponent>(glassesUid))
            return;
            
        if (!HasComp<ActorComponent>(args.User))
            return;

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("security-glasses-verb-text"),
            Icon = new SpriteSpecifier.Texture(new(VerbIconPath)),
            Act = () => TryOpenUi(args.User, args.Target),
            Priority = VerbPriority
        };
        
        args.Verbs.Add(verb);
    }
    
    protected virtual void TryOpenUi(EntityUid user, EntityUid target)
    {
    }

    public virtual bool CheckSecurityAccess(EntityUid user)
    {
        return false;
    }
}

[Serializable, NetSerializable]
public sealed class SecurityGlassesWantedStatusOpenEvent : EntityEventArgs
{
    public NetEntity Target { get; init; }
    public NetEntity User { get; init; }
    
    public SecurityGlassesWantedStatusOpenEvent(NetEntity target, NetEntity user)
    {
        Target = target;
        User = user;
    }
}

[Serializable, NetSerializable]
public sealed class SecurityGlassesChangeStatusEvent : EntityEventArgs
{
    public NetEntity Target { get; init; }
    public NetEntity User { get; init; }
    public int Status { get; init; }
    public string? Reason { get; init; }
    
    public SecurityGlassesChangeStatusEvent(NetEntity target, NetEntity user, int status, string? reason)
    {
        Target = target;
        User = user;
        Status = status;
        Reason = reason;
    }
} 
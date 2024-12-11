using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Event;
[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class EventItemDispenserComponent : Component
{

    [DataField("dispensing"), AutoNetworkedField]
    public EntProtoId DispensingPrototype;


    [DataField, AutoNetworkedField]
    public bool CanManuallyDispose = true;

    [DataField, AutoNetworkedField]
    public bool AutoDispose = true;

    [DataField, AutoNetworkedField]
    public bool Infinite = true;

    [DataField, AutoNetworkedField]
    public int Limit = 3;

    [DataField, AutoNetworkedField]
    public bool AutoCleanUp = true;

    //[DataField] // see OnDispensedRemove in serverside system
    //public bool AutoCleanStorages = true;




    [DataField, AutoNetworkedField]
    public SoundSpecifier DispenseSound = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
    [DataField, AutoNetworkedField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
    [DataField, AutoNetworkedField]
    public SoundSpecifier ManualDisposeSound = new SoundCollectionSpecifier("trashBagRustle");


    [DataField, AutoNetworkedField]
    public bool ReplaceDisposedItems = true;
    [DataField, AutoNetworkedField]
    public EntProtoId DisposedReplacement = "EffectTeslaSparksSilent";


    /// <summary>
    /// Stores Lists with all (currently existing) items.
    /// Owners' Uids used as keys.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, List<EntityUid>> dispensedItems = new();
    /// <summary>
    /// Stores the amount of items spawned by each person in this dispenser's lifetime.
    /// Owners' Uids used as keys.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, int> dispensedItemsAmount = new();

}


/// <summary>
/// Stores relevant info about who dispensed this item to avoid having to look for it in the dispensedItems dict.
/// </summary>
[RegisterComponent]
public sealed partial class EventDispensedComponent : Component
{
    /// <summary>
    /// The person who took the item.
    /// </summary>
    [ViewVariables]
    public EntityUid ItemOwner;
    /// <summary>
    /// The dispenser which dispensed (duh) the item.
    /// </summary>
    [ViewVariables]
    public EntityUid Dispenser;
    /// <summary>
    /// 
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Slaved = new();
}


[Serializable, NetSerializable]
public enum EventItemDispenserUiKey : byte
{
    Key,
}


[Serializable, NetSerializable]
public class EventItemDispenserNewConfigBoundUserInterfaceMessage : BoundUserInterfaceMessage
{
    public string DispensingPrototype = "FoodBanana";
    public bool CanManuallyDispose;
    public bool AutoDispose;
    public bool Infinite;
    public int Limit;
    public bool AutoCleanUp;
    public bool ReplaceDisposedItems;
    public string DisposedReplacement = "EffectTeslaSparksSilent";
}


[Serializable, NetSerializable]
public class EventItemDispenserNewProtoBoundUserInterfaceMessage : BoundUserInterfaceMessage
{
    public string DispensingPrototype = "FoodBanana";
    public EventItemDispenserNewProtoBoundUserInterfaceMessage(string proto) { DispensingPrototype = proto; }
}


using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._White.Event;
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class EventItemDispenserComponent : Component
{

    [DataField("dispensing"), AutoNetworkedField]
    public EntProtoId DispensingPrototype;


    [DataField]
    public bool CanManuallyDispose = true;

    [DataField]
    public bool AutoDispose = true;

    [DataField]
    public bool Infinite = true;

    [DataField]
    public int Limit = 3;

    [DataField]
    public bool AutoCleanUp = true;

    
    [DataField]
    public SoundSpecifier DispenseSound = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
    [DataField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
    [DataField]
    public SoundSpecifier ManualDisposeSound = new SoundCollectionSpecifier("trashBagRustle");


    [DataField]
    public bool ReplaceDisposedItems = true;
    [DataField]
    public EntProtoId DisposedReplacement = "EffectTeslaSparksSilent";


    /// <summary>
    /// Stores Lists with all (currently existing) items.
    /// Owners' Uids used as keys.
    /// </summary>
    public Dictionary<EntityUid, List<EntityUid>> dispensedItems = new();
    /// <summary>
    /// Stores the amount of items spawned by each person in this dispenser's lifetime.
    /// Owners' Uids used as keys.
    /// </summary>
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
}

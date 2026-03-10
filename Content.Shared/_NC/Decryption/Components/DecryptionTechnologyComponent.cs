using Content.Shared._NC.Weapons.Ranged.NCWeapon;
using Robust.Shared.GameStates;

namespace Content.Shared._NC.Decryption.Components;

// Technology payload entity for crafting/printing terminals.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DecryptionTechnologyComponent : Component
{
    // Tier used for decryption pool selection.
    [DataField("tier"), AutoNetworkedField]
    public WeaponTier Tier = WeaponTier.Standard;

    // Crafting module prototype unlocked by this technology.
    [DataField("modulePrototype"), AutoNetworkedField]
    public string ModulePrototype = string.Empty;

    // Recipe paper prototype linked to module.
    [DataField("recipePrototype"), AutoNetworkedField]
    public string RecipePrototype = string.Empty;

    // Data points cost for module printing.
    [DataField("moduleCost"), AutoNetworkedField]
    public int ModuleCost;

    // Data points cost for recipe printing.
    [DataField("recipeCost"), AutoNetworkedField]
    public int RecipeCost;

    // Maximum usage count of this technology payload.
    [DataField("maxUses"), AutoNetworkedField]
    public int MaxUses;

    // Marks successful decryption.
    [DataField("isDecrypted"), AutoNetworkedField]
    public bool IsDecrypted;

    // Remaining uses after decryption.
    [DataField("remainingUses"), AutoNetworkedField]
    public int RemainingUses;

    // Final integrity from decryption minigame.
    [DataField("decryptedIntegrity"), AutoNetworkedField]
    public int DecryptedIntegrity = 100;
}

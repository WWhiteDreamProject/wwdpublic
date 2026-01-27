using Robust.Shared.Serialization;

namespace Content.Shared._NC.Netrunning.UI;

[Serializable, NetSerializable]
public enum HackingUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class HackingBoundUiState : BoundUserInterfaceState
{
    public NetEntity? TargetServer;
    public string TargetSlot;
    public string TargetIceName;
    public int TargetIceHealth;
    public int TargetIceMaxHealth;
    public string TargetIceType; // Enum name

    public int PlayerRam;
    public int PlayerMaxRam;

    /// <summary>
    /// Remaining time before inactivity disconnect (seconds).
    /// </summary>
    public float RemainingTime;

    // Use a list of structs/data for programs
    public List<HackingProgramData> AvailablePrograms;

    public HackingBoundUiState(NetEntity? targetServer, string targetSlot, string targetIceName, int targetIceHealth, int targetIceMaxHealth, string targetIceType, int playerRam, int playerMaxRam, float remainingTime, List<HackingProgramData> availablePrograms)
    {
        TargetServer = targetServer;
        TargetSlot = targetSlot;
        TargetIceName = targetIceName;
        TargetIceHealth = targetIceHealth;
        TargetIceMaxHealth = targetIceMaxHealth;
        TargetIceType = targetIceType;
        PlayerRam = playerRam;
        PlayerMaxRam = playerMaxRam;
        RemainingTime = remainingTime;
        AvailablePrograms = availablePrograms;
    }
}

[Serializable, NetSerializable]
public struct HackingProgramData
{
    public NetEntity Entity; // Reference to program entity in deck
    public string Name;
    public string Icon;
    public int RamCost;
    public int Damage;
    public int Defense;

    public HackingProgramData(NetEntity entity, string name, string icon, int ramCost, int damage, int defense)
    {
        Entity = entity;
        Name = name;
        Icon = icon;
        RamCost = ramCost;
        Damage = damage;
        Defense = defense;
    }
}

[Serializable, NetSerializable]
public sealed class HackingUseProgramMessage : BoundUserInterfaceMessage
{
    public NetEntity ProgramEntity;

    public HackingUseProgramMessage(NetEntity programEntity)
    {
        ProgramEntity = programEntity;
    }
}

[Serializable, NetSerializable]
public sealed class HackingPassphraseMessage : BoundUserInterfaceMessage
{
    public string Passphrase;

    public HackingPassphraseMessage(string passphrase)
    {
        Passphrase = passphrase;
    }
}

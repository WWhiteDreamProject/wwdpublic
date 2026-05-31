using System.IO;
using Content.Shared.Preferences;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._White.Preferences;

/// <summary>
/// Represents a network message sent from the client to the server to request the deletion of a character profile.
/// </summary>
public sealed class DeleteCharacterRequestMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    /// <summary>
    /// The slot index of the character profile to be deleted.
    /// </summary>
    public int Slot;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Slot = buffer.ReadInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Slot);
    }
}

/// <summary>
/// Represents a network message sent from the server to the client.
/// This message contains the player's preferences and game settings, typically sent before the client joins.
/// </summary>
public sealed class PreferencesAndSettingsResponseMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    /// <summary>
    /// The game settings relevant to the player.
    /// </summary>
    public GameSettings Settings = default!;

    /// <summary>
    /// The player's character preferences, including their profiles.
    /// </summary>
    public PlayerPreferences Preferences = default!;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();

        using (var stream = new MemoryStream())
        {
            buffer.ReadAlignedMemory(stream, length);
            serializer.DeserializeDirect(stream, out Preferences);
        }

        length = buffer.ReadVariableInt32();
        using (var stream = new MemoryStream())
        {
            buffer.ReadAlignedMemory(stream, length);
            serializer.DeserializeDirect(stream, out Settings);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        using (var stream = new MemoryStream())
        {
            serializer.SerializeDirect(stream, Preferences);
            buffer.WriteVariableInt32((int) stream.Length);
            stream.TryGetBuffer(out var segment);
            buffer.Write(segment);
        }

        using (var stream = new MemoryStream())
        {
            serializer.SerializeDirect(stream, Settings);
            buffer.WriteVariableInt32((int) stream.Length);
            stream.TryGetBuffer(out var segment);
            buffer.Write(segment);
        }
    }
}

/// <summary>
/// Represents a network message sent from the client to the server to request the selection of a character slot.
/// </summary>
public sealed class SelectCharacterRequestMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    /// <summary>
    /// The index of the character slot the client wishes to select.
    /// </summary>
    public int SelectedCharacterIndex;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        SelectedCharacterIndex = buffer.ReadVariableInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(SelectedCharacterIndex);
    }
}

/// <summary>
/// Represents a network message sent from the client to the server to update an existing character profile.
/// </summary>
public sealed class UpdateCharacterRequestMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    /// <summary>
    /// The new or updated character profile data.
    /// </summary>
    public HumanoidCharacterProfile Profile = default!;

    /// <summary>
    /// The slot index of the character profile to be updated.
    /// </summary>
    public int Slot;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Slot = buffer.ReadInt32();
        var length = buffer.ReadVariableInt32();
        using var stream = new MemoryStream(length);
        buffer.ReadAlignedMemory(stream, length);
        Profile = serializer.Deserialize<HumanoidCharacterProfile>(stream);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Slot);
        using var stream = new MemoryStream();
        serializer.Serialize(stream, Profile);
        buffer.WriteVariableInt32((int) stream.Length);
        stream.TryGetBuffer(out var segment);
        buffer.Write(segment);
    }
}

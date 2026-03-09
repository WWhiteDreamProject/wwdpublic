using Robust.Shared.Serialization;

namespace Content.Shared._NC.Decryption.UI;

[Serializable, NetSerializable]
public sealed class DecryptionMatrixCellData
{
    public char Glyph { get; }
    public string Word { get; }
    public int BackdoorId { get; }

    public DecryptionMatrixCellData(char glyph, string word, int backdoorId)
    {
        Glyph = glyph;
        Word = word;
        BackdoorId = backdoorId;
    }
}

// Full state for the decryption terminal UI.
[Serializable, NetSerializable]
public sealed class DecryptionBoundUiState : BoundUserInterfaceState
{
    public string ProtocolTitle { get; }
    public bool HasCarrier { get; }
    public bool SessionActive { get; }
    public int AttemptsRemaining { get; }
    public int MaxAttempts { get; }
    public int Integrity { get; }
    public string TierLabel { get; }
    public int TimeRemainingSeconds { get; }
    public int MatrixWidth { get; }
    public int MatrixHeight { get; }
    public List<DecryptionMatrixCellData> MatrixCells { get; }
    public List<string> LogLines { get; }

    public DecryptionBoundUiState(
        string protocolTitle,
        bool hasCarrier,
        bool sessionActive,
        int attemptsRemaining,
        int maxAttempts,
        int integrity,
        string tierLabel,
        int timeRemainingSeconds,
        int matrixWidth,
        int matrixHeight,
        List<DecryptionMatrixCellData> matrixCells,
        List<string> logLines)
    {
        ProtocolTitle = protocolTitle;
        HasCarrier = hasCarrier;
        SessionActive = sessionActive;
        AttemptsRemaining = attemptsRemaining;
        MaxAttempts = maxAttempts;
        Integrity = integrity;
        TierLabel = tierLabel;
        TimeRemainingSeconds = timeRemainingSeconds;
        MatrixWidth = matrixWidth;
        MatrixHeight = matrixHeight;
        MatrixCells = matrixCells;
        LogLines = logLines;
    }
}

[Serializable, NetSerializable]
public sealed class DecryptionStartMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class DecryptionMatrixClickMessage : BoundUserInterfaceMessage
{
    public int CellIndex { get; }

    public DecryptionMatrixClickMessage(int cellIndex)
    {
        CellIndex = cellIndex;
    }
}

[Serializable, NetSerializable]
public sealed class DecryptionEjectCarrierMessage : BoundUserInterfaceMessage { }

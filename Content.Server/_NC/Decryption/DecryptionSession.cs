using Content.Shared._NC.Weapons.Ranged.NCWeapon;

namespace Content.Server._NC.Decryption;

internal sealed class DecryptionSession
{
    public EntityUid User;
    public EntityUid Carrier;
    public WeaponTier Tier;

    public string ProtocolTitle = string.Empty;
    public string Password = string.Empty;
    public string SelectedTechnologyEntityId = string.Empty;
    public int SelectedTechnologyUses;
    public List<string> Words = new();
    public HashSet<string> RemovedWords = new();
    public List<BackdoorData> Backdoors = new();
    public List<MatrixCellData> MatrixCells = new();
    public Dictionary<int, string> WordByCell = new();
    public Dictionary<int, int> BackdoorByCell = new();
    public List<string> Log = new();

    public int MatrixWidth = 24;
    public int MatrixHeight = 22;

    public int MaxAttempts = 4;
    public int AttemptsRemaining = 4;
    public int IntegrityDamagePerMistake = 25;
    public int LogLineLimit = 20;
    public int PermanentMistakes;

    // Active ICE timer state.
    public bool TimerActive;
    public float TimerRemaining;
    public int LastDisplayedTimerSeconds = -1;

    public int Integrity => Math.Max(0, 100 - (PermanentMistakes * IntegrityDamagePerMistake));

    internal sealed class BackdoorData
    {
        public int Id;
        public string Token = string.Empty;
        public bool Used;
    }

    internal sealed class MatrixCellData
    {
        public char Glyph;
        public string Word = string.Empty;
        public int BackdoorId = -1;
    }
}

using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared._NC.Trauma
{
    // Уровни подписки Trauma Team
    [Serializable, NetSerializable]
    public enum TraumaSubscriptionTier : byte
    {
        None = 0,    // Нет подписки
        Bronze,      // Базовая (по умолчанию)
        Silver,
        Platinum     // Элита
    }

    // Уникальный ключ для открытия интерфейса
    [Serializable, NetSerializable]
    public enum TraumaComputerUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum TraumaTabletUiKey
    {
        Key
    }

    // Структура данных об одном пациенте для передачи по сети
    [Serializable, NetSerializable]
    public struct TraumaPatientData
    {
        public NetEntity EntityUid; // Сетевой ID сущности
        public string Name;         // Имя персонажа
        public string HealthStatus; // Состояние (Alive, Critical, Dead)
        public TraumaSubscriptionTier Subscription; // Текущая подписка
        public string Job;
        public string DamageInfo; // legacy or combined
        public float BruteDamage;
        public float BurnDamage;
        public float ToxinDamage;

        public NetCoordinates? TargetCoords; // Координаты для карты
    }

    // Состояние интерфейса (Сервер -> Клиент)
    // Отправляется каждый раз, когда данные меняются
    [Serializable, NetSerializable]
    public sealed class TraumaComputerState : BoundUserInterfaceState
    {
        public List<TraumaPatientData> Patients;
        public List<TraumaLogEntry> Logs;
        public HashSet<NetEntity> PendingCompletions; // Пациенты, ожидающие подтверждения завершения

        public TraumaComputerState(List<TraumaPatientData> patients, List<TraumaLogEntry> logs, HashSet<NetEntity>? pendingCompletions = null)
        {
            Patients = patients;
            Logs = logs;
            PendingCompletions = pendingCompletions ?? new HashSet<NetEntity>();
        }
    }

    [Serializable, NetSerializable]
    public struct TraumaLogEntry
    {
        public TimeSpan Time;
        public string Editor;
        public string Target;
        public TraumaSubscriptionTier OldTier;
        public TraumaSubscriptionTier NewTier;
    }

    // Сообщение о смене подписки (Клиент -> Сервер)
    // Отправляется, когда админ меняет подписку в меню
    [Serializable, NetSerializable]
    public sealed class TraumaChangeSubscriptionMsg : BoundUserInterfaceMessage
    {
        public NetEntity TargetEntity;
        public TraumaSubscriptionTier NewTier;

        public TraumaChangeSubscriptionMsg(NetEntity target, TraumaSubscriptionTier tier)
        {
            TargetEntity = target;
            NewTier = tier;
        }
    }
    [Serializable, NetSerializable]
    public sealed class TraumaDispatchMsg : BoundUserInterfaceMessage
    {
        public NetEntity TargetEntity;

        public TraumaDispatchMsg(NetEntity target)
        {
            TargetEntity = target;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TraumaOpenMapMsg : BoundUserInterfaceMessage
    {
        public NetEntity TargetEntity;

        public TraumaOpenMapMsg(NetEntity target)
        {
            TargetEntity = target;
        }
    }

    // Сообщение "Миссия выполнена" от планшета (Клиент -> Сервер)
    [Serializable, NetSerializable]
    public sealed class TraumaCompleteMissionMsg : BoundUserInterfaceMessage
    {
        public NetEntity TargetEntity;

        public TraumaCompleteMissionMsg(NetEntity target)
        {
            TargetEntity = target;
        }
    }

    // Подтверждение завершения миссии от диспетчера (Клиент -> Сервер)
    [Serializable, NetSerializable]
    public sealed class TraumaConfirmCompletionMsg : BoundUserInterfaceMessage
    {
        public NetEntity TargetEntity;

        public TraumaConfirmCompletionMsg(NetEntity target)
        {
            TargetEntity = target;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TraumaTabletState : BoundUserInterfaceState
    {
        public TraumaPatientData? ActivePatient;
        public bool IsPendingCompletion; // Ожидает подтверждения диспетчера

        public TraumaTabletState(TraumaPatientData? activePatient, bool isPendingCompletion = false)
        {
            ActivePatient = activePatient;
            IsPendingCompletion = isPendingCompletion;
        }
    }
}

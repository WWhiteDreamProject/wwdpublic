using Robust.Shared.Serialization;

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
        public string DamageInfo;
    }

    // Состояние интерфейса (Сервер -> Клиент)
    // Отправляется каждый раз, когда данные меняются
    [Serializable, NetSerializable]
    public sealed class TraumaComputerState : BoundUserInterfaceState
    {
        public List<TraumaPatientData> Patients;
        public List<TraumaLogEntry> Logs;

        public TraumaComputerState(List<TraumaPatientData> patients, List<TraumaLogEntry> logs)
        {
            Patients = patients;
            Logs = logs;
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

    [Serializable, NetSerializable]
    public sealed class TraumaTabletState : BoundUserInterfaceState
    {
        public TraumaPatientData? ActivePatient;

        public TraumaTabletState(TraumaPatientData? activePatient)
        {
            ActivePatient = activePatient;
        }
    }
}

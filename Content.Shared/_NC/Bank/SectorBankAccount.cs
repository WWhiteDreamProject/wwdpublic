namespace Content.Shared._NC.Bank
{
    /// <summary>
    /// Перечисление всех возможных счетов организаций/департаментов.
    /// </summary>
    public enum SectorBankAccount : byte
    {
        Invalid = 0,
        CityAdmin = 1,    // Городское управление (платит гражданским)
        TraumaTeam = 2,  
        Militech = 3,     
        Biotechnica = 4, 
        Ncpd = 5,
        // Добавляйте новые по мере необходимости
    }
}
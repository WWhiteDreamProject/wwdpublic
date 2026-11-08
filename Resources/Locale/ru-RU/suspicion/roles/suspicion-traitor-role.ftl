# Shown when greeted with the Suspicion role
suspicion-role-greeting = Вы { $roleName }!
# Shown when greeted with the Suspicion role
# Shown when greeted with the Suspicion role
suspicion-objective = Цель: { $objectiveText }
# Shown when greeted with the Suspicion role
# Shown when greeted with the Suspicion role
suspicion-partners-in-crime =
    { $partnersCount ->
        [zero] Ты сам по себе. Удачи!
        [one] Ваш соучастник преступления — { $partnerNames }.
       *[other] Вашими соучастниками преступления являются: { $partnerNames }.
    }

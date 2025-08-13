reagent-effect-condition-guidebook-stamina-damage-threshold =
    { $max ->
        [2147483648] у цели как минимум {NATURALFIXED($min, 2)} урона по выносливости
        *[other] { $min ->
                    [0] у цели не более {NATURALFIXED($max, 2)} урона по выносливости
                    *[other] у цели от {NATURALFIXED($min, 2)} до {NATURALFIXED($max, 2)} урона по выносливости
                 }
    }

reagent-effect-condition-guidebook-unique-bloodstream-chem-threshold =
    { $max ->
        [2147483648] { $min ->
                        [1] в кровотоке содержится как минимум {$min} реагента
                        *[other] в кровотоке содержится как минимум {$min} реагентов
                     }
        [1] { $min ->
               [0] в кровотоке содержится не более {$max} реагента
               *[other] в кровотоке содержится от {$min} до {$max} реагентов
            }
        *[other] { $min ->
                    [-1] в кровотоке содержится не более {$max} реагентов
                    *[other] в кровотоке содержится от {$min} до {$max} реагентов
                 }
    }

reagent-effect-condition-guidebook-typed-damage-threshold =
    { $inverse ->
        [true] у цели не более
        *[false] у цели как минимум
    } { $changes } урона


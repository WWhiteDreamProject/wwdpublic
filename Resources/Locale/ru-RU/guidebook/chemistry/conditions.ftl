reagent-effect-condition-guidebook-total-damage =
    { $max ->
        [2147483648] у него есть по крайней мере { NATURALFIXED($min, 2) } общего урона
       *[other]
            { $min ->
                [0] оно имеет не более { NATURALFIXED($max, 2) } общего урона
               *[other] оно имеет между { NATURALFIXED($min, 2) } и { NATURALFIXED($max, 2) } общего урона
            }
    }
reagent-effect-condition-guidebook-reagent-threshold =
    { $max ->
        [2147483648] у него есть по крайней мере { NATURALFIXED($min, 2) }ед. { $reagent }
       *[other]
            { $min ->
                [0] оно имеет не более { NATURALFIXED($max, 2) }ед. { $reagent }
               *[other] оно имеет между { NATURALFIXED($min, 2) }ед. и { NATURALFIXED($max, 2) }ед. { $reagent }
            }
    }
reagent-effect-condition-guidebook-mob-state-condition = моб - { $state }
reagent-effect-condition-guidebook-solution-temperature =
    температура раствора { $max ->
        [2147483648] по меньшей мере { NATURALFIXED($min, 2) }К
       *[other]
            { $min ->
                [0] не более { NATURALFIXED($max, 2) }К
               *[other] между { NATURALFIXED($min, 2) }К и { NATURALFIXED($max, 2) }К
            }
    }
reagent-effect-condition-guidebook-body-temperature =
    температура тела { $max ->
        [2147483648] по меньшей мере { NATURALFIXED($min, 2) }К
       *[other]
            { $min ->
                [0] не более { NATURALFIXED($max, 2) }К
               *[other] между { NATURALFIXED($min, 2) }К и { NATURALFIXED($max, 2) }К
            }
    }
reagent-effect-condition-guidebook-organ-type =
    орган метаболизма { $shouldhave ->
        [true] -
       *[false] - не
    } { INDEFINITE($name) } { $name } орган
reagent-effect-condition-guidebook-has-tag =
    цель { $invert ->
        [true] не имеет
       *[false] имеет
    } тэг { $tag }

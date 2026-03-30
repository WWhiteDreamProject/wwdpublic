reagent-effect-condition-guidebook-total-damage =
    { $max ->
        [2147483648] у него есть по крайней мере [color=#E8A0A0]{ NATURALFIXED($min, 2) }[/color] общего [color=#E8A0A0]урона[/color]
       *[other]
            { $min ->
                [0] оно имеет не более [color=#E8A0A0]{ NATURALFIXED($max, 2) }[/color] общего [color=#E8A0A0]урона[/color]
               *[other] оно имеет между [color=#E8A0A0]{ NATURALFIXED($min, 2) }[/color] и [color=#E8A0A0]{ NATURALFIXED($max, 2) }[/color] общего [color=#E8A0A0]урона[/color]
            }
    }
reagent-effect-condition-guidebook-total-hunger =
    { $max ->
        [2147483648] у цели как минимум [color=#90D890]{ NATURALFIXED($min, 2) }[/color] общего [color=#90D890]голода[/color]
       *[other]
            { $min ->
                [0] у цели не более [color=#90D890]{ NATURALFIXED($max, 2) }[/color] общего [color=#90D890]голода[/color]
               *[other] у цели от [color=#90D890]{ NATURALFIXED($min, 2) }[/color] до [color=#90D890]{ NATURALFIXED($max, 2) }[/color] общего [color=#90D890]голода[/color]
            }
    }

reagent-effect-condition-guidebook-reagent-threshold =
    { $max ->
        [2147483648] у него есть по крайней мере [color=#80C8C8]{ NATURALFIXED($min, 2) }ед.[/color]
       *[other]
            { $min ->
                [0] оно имеет не более [color=#80C8C8]{ NATURALFIXED($max, 2) }ед.[/color]
               *[other] оно имеет между [color=#80C8C8]{ NATURALFIXED($min, 2) }ед.[/color] и [color=#80C8C8]{ NATURALFIXED($max, 2) }ед.[/color]
            }
    } реагента { $isThisReagent ->
        [true] [color=#80C8C8]«{ $reagent }» (текущий реагент)[/color]
       *[false] [color=#80C8C8]«{ $reagent }»[/color]
    }
reagent-effect-condition-guidebook-mob-state-condition = пациент в состоянии [color=#CCA0CC]«{ $state }»[/color]
reagent-effect-condition-guidebook-job-condition = должность пациента — [color=#D0B890]{ $job }[/color]
reagent-effect-condition-guidebook-solution-temperature =
    {"[color=#E8C080]температура раствора[/color]"} { $max ->
        [2147483648] по меньшей мере [color=#E8C080]{ NATURALFIXED($min, 2) }К[/color]
       *[other]
            { $min ->
                [0] не более [color=#E8C080]{ NATURALFIXED($max, 2) }К[/color]
               *[other] между [color=#E8C080]{ NATURALFIXED($min, 2) }К[/color] и [color=#E8C080]{ NATURALFIXED($max, 2) }К[/color]
            }
    }
reagent-effect-condition-guidebook-body-temperature =
    {"[color=#D0B8A0]температура тела[/color]"} { $max ->
        [2147483648] по меньшей мере [color=#D0B8A0]{ NATURALFIXED($min, 2) }К[/color]
       *[other]
            { $min ->
                [0] не более [color=#D0B8A0]{ NATURALFIXED($max, 2) }К[/color]
               *[other] между [color=#D0B8A0]{ NATURALFIXED($min, 2) }К[/color] и [color=#D0B8A0]{ NATURALFIXED($max, 2) }К[/color]
            }
    }
reagent-effect-condition-guidebook-organ-type =
    орган метаболизма цели { $shouldhave ->
        [true] {""}
       *[false] [color=#E7234A]не[/color]{" "}
    }[color=#D0A8B0]{ $name }[/color]
reagent-effect-condition-guidebook-has-tag =
    цель { $invert ->
        [true] не имеет
       *[false] имеет
    } тэг [color=#A0B8D0]«{ $tag }»[/color]
reagent-effect-condition-guidebook-blood-reagent-threshold =
    { $max ->
        [2147483648] в [color=#D0A0A0]крови[/color] как минимум [color=#80C8C8]{ NATURALFIXED($min, 2) }ед.[/color]
       *[other]
            { $min ->
                [0] в [color=#D0A0A0]крови[/color] не более [color=#80C8C8]{ NATURALFIXED($max, 2) }ед.[/color]
               *[other] в [color=#D0A0A0]крови[/color] от [color=#80C8C8]{ NATURALFIXED($min, 2) }ед.[/color] до [color=#80C8C8]{ NATURALFIXED($max, 2) }ед.[/color]
            }
    } реагента { $isThisReagent ->
        [true] [color=#80C8C8]«{ $reagent }» (текущий реагент)[/color]
       *[false] [color=#80C8C8]«{ $reagent }»[/color]
    }

reagent-effect-condition-guidebook-this-reagent = текущий реагент

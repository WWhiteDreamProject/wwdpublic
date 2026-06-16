character-requirement-desc = Требования:

## Job
character-job-requirement = Вы{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должны быть одной из этих ролей: {$jobs}

character-department-requirement = Вы{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должны быть в одном из этих отделов: {$departments}


character-antagonist-requirement = Вы{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должны быть антагонистом

character-mindshield-requirement = У вас{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должна стоять защина разума (МайндШилд)

character-timer-department-insufficient = Вам необходимо наиграть еще [color=yellow]{TOSTRING($time, "0")}[/color] минут в [color={$departmentColor}]{$department}[/color]
character-timer-department-too-high = Вам необходимо иметь на [color=yellow]{TOSTRING($time, "0")}[/color] меньше минут в [color={$departmentColor}]{$department}[/color]

character-timer-overall-insufficient = Вам необходимо наиграть еще [color=yellow]{TOSTRING($time, "0")}[/color] минут
character-timer-overall-too-high = Вам необходимо иметь на [color=yellow]{TOSTRING($time, "0")}[/color] меньше минут в игре

character-timer-role-insufficient = Вам необходимо наиграть еще [color=yellow]{TOSTRING($time, "0")}[/color] минут на роли [color={$departmentColor}]{$job}[/color]
character-timer-role-too-high = Вам необходимо иметь на [color=yellow]{TOSTRING($time, "0")}[/color] меньше минут на роли [color={$departmentColor}]{$job}[/color]


## Logic
character-logic-and-requirement-listprefix = {""}
    {$indent}[color=gray]&[/color]{" "}
character-logic-and-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны соответствовать [color=red]всем[/color] [color=gray]этим[/color] условиям: {$options}

character-logic-or-requirement-listprefix = {""}
    {$indent}[color=white]O[/color]{" "}
character-logic-or-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны соответствовать [color=red]хотя бы одному[/color] из [color=white]этих[/color] условий: {$options}

character-logic-xor-requirement-listprefix = {""}
    {$indent}[color=white]X[/color]{" "}
character-logic-xor-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны соответствовать [color=red]только одному[/color] из [color=white]этих[/color] условий: {$options}
## Profile
character-age-requirement-range = Вы{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должны быть в возрасте от [color=yellow]{$min}[/color] до [color=yellow]{$max}[/color] лет

character-age-requirement-minimum-only = Вы{$inverted ->
    [true]{""}
    *[other]{" "}[color=red]не[/color]
} должны быть моложе [color=yellow]{$min}[/color] лет

character-age-requirement-maximum-only = Вы{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должны быть старше [color=yellow]{$max}[/color] лет

character-backpack-type-requirement = Вы{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} a [color=brown]{$type}[/color] as your bag

character-clothing-preference-requirement = Вы{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
}
character-gender-requirement = Ваши местоимения{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должны быть: [color=white]{$gender}[/color]

character-sex-requirement = У вас{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должен быть пол: [color=white]{$sex ->
    [None] Бесполый
    *[other] {$sex}
}[/color]
character-species-requirement = Ваша раса{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должна быть: {$species}


character-height-requirement = Ваш рост{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должен быть {$min ->
    [-2147483648]{$max ->
        [2147483648]{""}
        *[other] меньше чем [color={$color}]{$max}[/color]см
    }
    *[other]{$max ->
        [2147483648] больше чем [color={$color}]{$min}[/color]см
        *[other] между [color={$color}]{$min}[/color] и [color={$color}]{$max}[/color]см
    }
}


character-width-requirement = Ваша ширина{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должна быть {$min ->
    [-2147483648]{$max ->
        [2147483648]{""}
        *[other] меньше чем [color={$color}]{$max}[/color]см
    }
    *[other]{$max ->
        [2147483648] больше чем [color={$color}]{$min}[/color]см
        *[other] между [color={$color}]{$min}[/color] и [color={$color}]{$max}[/color]см
    }
}


character-weight-requirement = Вы{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должны весить {$min ->
    [-2147483648]{$max ->
        [2147483648]{""}
        *[other] меньше чем [color={$color}]{$max}[/color]кг
    }
    *[other]{$max ->
        [2147483648] больше чем [color={$color}]{$min}[/color]кг
        *[other] между [color={$color}]{$min}[/color] и [color={$color}]{$max}[/color]кг
    }
}



character-trait-requirement = У вас{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должны быть следующие перки: {$traits}


character-loadout-requirement = У вас{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должно быть следующее снаряжение: {$loadouts}




character-item-group-requirement = Вы{$inverted ->
    [true] должны иметь {$max} или больше
    *[other] должны иметь {$max} или меньше
} предметов из группы: [color=white]{$group}[/color]


## Whitelist
character-whitelist-requirement = Вы{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должны быть в вайтлисте

## CVar

character-cvar-requirement =
    Для параметра [color={$color}]{$cvar}[/color]{$inverted ->
    [true]{" "}[color=red]не[/color]
    *[other]{""}
} должно быть установлено значение [color={$color}]{$value}[/color].

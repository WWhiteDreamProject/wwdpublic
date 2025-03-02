## Job
character-job-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны быть одной из этих ролей: {$jobs}

character-department-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны быть в одном из этих отделов: {$departments}

character-timer-department-insufficient = Вам необходимо наиграть еще [color=yellow]{TOSTRING($time, "0")}[/color] минут в [color={$departmentColor}]{$department}[/color] отделе
character-timer-department-too-high = Вам необходимо иметь на [color=yellow]{TOSTRING($time, "0")}[/color] меньше минут в [color={$departmentColor}]{$department}[/color] отделе

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
} должны подходить под [color=red]все[/color] [color=gray]условия[/color]: {$options}

character-logic-or-requirement-listprefix = {""}
    {$indent}[color=white]O[/color]{" "}
character-logic-or-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны подходить под [color=red]хотя бы одно[/color] [color=white]условие[/color]: {$options}

character-logic-xor-requirement-listprefix = {""}
    {$indent}[color=white]X[/color]{" "}
character-logic-xor-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны подходить под [color=red]только одно[/color] [color=white]условие[/color]: {$options}


## Profile
character-age-requirement-range = Вам{$inverted ->
    [true]{" "}не
    *[other]{""}
} должно быть больше [color=yellow]{$min}[/color] и меньше [color=yellow]{$max}[/color] лет

character-age-requirement-minimum-only = Вам{$inverted ->
    [true]{" "}не
    *[other]{""}
} должно быть меньше [color=yellow]{$min}[/color] лет

character-age-requirement-maximum-only = Вам{$inverted ->
    [true]{""}
    *[other]{" "}не
} должно быть больше [color=yellow]{$max}[/color] лет

character-backpack-type-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны использовать [color=brown]{$type}[/color] в качестве вашего рюкзака

character-clothing-preference-requirement = Вы {$inverted ->
    [true]{" "}не
    *[other]{""}
} должны носить [color=white]{$type}[/color]

character-gender-requirement = Ваши местоимения{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны быть [color=white]{$gender}[/color]

character-sex-requirement = Вы {$inverted ->
    [true]{" "}не
    *[other]{""}
} должны быть [color=white]{$sex ->
    [None] бесполым
    *[other] {$sex}
}[/color]

character-species-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должно быть {$species}

character-height-requirement = Ваш рост{$inverted ->
    [true]{" "}не
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
    [true]{" "}не
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
    [true]{" "}не
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
    [true]{" "}не
    *[other]{""}
} должны быть следующие перки: {$traits}

character-loadout-requirement = У вас{$inverted ->
    [true]{" "}не
    *[other]{""}
} должно быть следующее снаряжение: {$loadouts}


character-item-group-requirement = Вы{$inverted ->
    [true] должны иметь {$max} или больше
    *[other] должны иметь {$max} или меньше
} предметов из группы [color=white]{$group}[/color]

## Whitelist
character-whitelist-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны быть в вайтлисте

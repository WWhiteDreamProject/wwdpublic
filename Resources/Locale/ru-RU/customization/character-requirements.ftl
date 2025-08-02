character-requirement-desc = Requirements:

## Job
character-job-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны быть одной из этих ролей: {$jobs}

character-department-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны быть в одном из этих отделов: {$departments}


character-antagonist-requirement = You must{$inverted ->
    [true]{" "}not
    *[other]{""}
} be an antagonist

character-mindshield-requirement = You must{$inverted ->
    [true]{" "}not
    *[other]{""}
} be mindshielded

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
} fit [color=red]all[/color] of [color=gray]these[/color]: {$options}

character-logic-or-requirement-listprefix = {""}
    {$indent}[color=white]O[/color]{" "}
character-logic-or-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
}
character-logic-xor-requirement-listprefix = {""}
    {$indent}[color=white]X[/color]{" "}
character-logic-xor-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
}
## Profile
character-age-requirement-range = Вам{$inverted ->
    [true]{" "}не
    *[other]{""}
} be within [color=yellow]{$min}[/color] and [color=yellow]{$max}[/color] years old

character-age-requirement-minimum-only = Вам{$inverted ->
    [true]{" "}не
    *[other]{""}
} be at least [color=yellow]{$min}[/color] years old

character-age-requirement-maximum-only = Вам{$inverted ->
    [true]{""}
    *[other]{" "}не
} be older than [color=yellow]{$max}[/color] years old

character-backpack-type-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} a [color=brown]{$type}[/color] as your bag

character-clothing-preference-requirement = Вы {$inverted ->
    [true]{" "}не
    *[other]{""}
}
character-gender-requirement = Ваши местоимения{$inverted ->
    [true]{" "}не
    *[other]{""}
} the pronouns [color=white]{$gender}[/color]

character-sex-requirement = Вы {$inverted ->
    [true]{" "}не
    *[other]{""}
} be [color=white]{$sex ->
    [None] unsexed
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
} items from the group [color=white]{$group}[/color]


## Whitelist
character-whitelist-requirement = Вы{$inverted ->
    [true]{" "}не
    *[other]{""}
} должны быть в вайтлисте

## CVar

character-cvar-requirement =
    The server must{$inverted ->
    [true]{" "}not
    *[other]{""}
} have [color={$color}]{$cvar}[/color] set to [color={$color}]{$value}[/color].

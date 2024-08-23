# Job
character-job-requirement = Вы должны {$inverted ->
    [true] не быть
    *[other] быть
} одной из этих профессий: {$jobs}
character-department-requirement = Вы должны {$inverted ->
    [true] не быть
    *[other] быть
} в одном из этих отделов: {$departments}

character-timer-department-insufficient = Вам требуется [color=yellow]{TOSTRING($time, "0")}[/color] больше минут [color={$departmentColor}]{$department}[/color] времени в отделе
character-timer-department-too-high = Вам требуется [color=yellow]{TOSTRING($time, "0")}[/color] меньше минут in [color={$departmentColor}]{$department}[/color] отделе
character-timer-overall-insufficient = Вам требуется [color=yellow]{TOSTRING($time, "0")}[/color] больше минут игрового времени
character-timer-overall-too-high = Вам требуется [color=yellow]{TOSTRING($time, "0")}[/color] меньше минут игрового времени
character-timer-role-insufficient = Вам требуется [color=yellow]{TOSTRING($time, "0")}[/color] more minutes с [color={$departmentColor}]{$job}[/color]
character-timer-role-too-high = Вам требуется[color=yellow] {TOSTRING($time, "0")}[/color] меньше минут с [color={$departmentColor}]{$job}[/color]


# Profile
character-age-requirement = Вы должны {$inverted ->
    [true] не быть
    *[other] быть
} быть в пределах [color=yellow]{$min}[/color] и [color=yellow]{$max}[/color] лет
character-species-requirement = Вы должны {$inverted ->
    [true] не быть
    *[other] быть
} {$species}
character-trait-requirement = Вы должны {$inverted ->
    [true] не иметь
    *[other] иметь
} одну из этих черт: {$traits}
character-loadout-requirement = Вы должны {$inverted ->
    [true] не иметь
    *[other] иметь
} один из этих лоадаутов: {$loadouts}
character-backpack-type-requirement = Вы должны {$inverted ->
    [true] не использовать
    *[other] использовать
} [color=brown]{$type}[/color] в качестве сумки
character-clothing-preference-requirement = Вы должны {$inverted ->
    [true] не носить
    *[other] носить
} [color=white]{$type}[/color]


# Whitelist
character-whitelist-requirement = Вы должны {$inverted ->
    [true] не быть
    *[other] быть
} в вайт-листе

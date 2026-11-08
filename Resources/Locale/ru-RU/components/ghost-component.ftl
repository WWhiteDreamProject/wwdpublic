# Examine text
comp-ghost-examine-time-minutes =
    Умер{ GENDER($ent) ->
   *[male] {""}
    [female] {"а"}
    [epicene] {"и"}
    [neuter] {"о"}
    } [color=yellow]{ $minutes } {RU-PLURAL($minutes, "минуту", "минуты", "минут")} назад.[/color]
comp-ghost-examine-time-seconds =
    Умер{ GENDER($ent) ->
   *[male] {""}
    [female] {"а"}
    [epicene] {"и"}
    [neuter] {"о"}
    } [color=yellow]{ $seconds } {RU-PLURAL($seconds, "секунду", "секунды", "секунд")} назад.[/color]

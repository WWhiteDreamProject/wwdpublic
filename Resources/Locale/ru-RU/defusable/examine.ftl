defusable-examine-defused = {CAPITALIZE($name)} [color=lime]обезврежена[/color].
defusable-examine-live = {CAPITALIZE($name)} [color=red]тикает[/color] и до взрыва осталось [color=red]{$time}[/color] { $time ->
        [one] секунда
        [few] секунды
        *[other] секунд
    }.

defusable-examine-live-display-off = {CAPITALIZE($name)} [color=red]тикает[/color], но таймер, по всей видимости, отключен.
defusable-examine-inactive = {CAPITALIZE($name)} [color=lime]неактивна[/color], но все еще может быть запущена.
defusable-examine-bolts = Болты {$down ->
    [true] [color=red]опущены[/color]
    *[false] [color=green]подняты[/color]
}.

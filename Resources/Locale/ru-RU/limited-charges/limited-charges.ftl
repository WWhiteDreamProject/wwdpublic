limited-charges-charges-remaining =
    { $charges ->
        [one] Оно имеет [color=fuchsia]{ $charges }[/color] оставшийся заряд.
       *[other] Оно имеет [color=fuchsia]{ $charges }[/color] оставшихся зарядов.
    }

limited-charges-max-charges = Оно на [color=green]максимуме[/color] зарядов.
limited-charges-recharging =
    { $seconds ->
        [one] Осталась [color=yellow]{ $seconds }[/color] секунда до получения заряда.
        [few] Осталось [color=yellow]{ $seconds }[/color] секунды до получения заряда.
       *[other] Осталось [color=yellow]{ $seconds }[/color] секунд до получения заряда.
    }

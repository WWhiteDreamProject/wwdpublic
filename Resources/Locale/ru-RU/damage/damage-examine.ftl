# Damage examines
damage-examinable-verb-text = Повреждения
damage-examinable-verb-message = Изучить показатели урона.
damage-hitscan = лучём
damage-projectile = как снаряд
damage-melee = в ближнем бою
damage-melee-heavy = при размахе
damage-throw = при броске
damage-examine = Наносит следующие повреждения:
damage-examine-type = Наносит следующие повреждения ({ $type }):
damage-value =
    - [color=red]{ $amount }[/color] единиц [color=yellow]{ $type ->
        [Asphyxiation] удушающего
        [Bloodloss] обескровливающего
        [Blunt] тупого
        [Cellular] клеточного
        [Slash] режущего
        [Piercing] проникающего
        [Heat] теплового
        [Cold] холодного
        [Shock] электрического
        [Poison] ядовитого
        [Radiation] радиационного
        [Caustic] кислотного
        [Structural] структурного
        [Holy] святого
        *[other] неизвестного
    }[/color] урона.
damage-stamina-cost = [color=cyan]{CAPITALIZE($type)}[/color] тратит ровно [color=orange]{$cost}[/color] [color=yellow]выносливости[/color].
damage-examine-embeddable-harmful = С метким броском [color=cyan]впивается[/color] в цель и продолжает ранить её с течением времени.
damage-examine-embeddable = При попадании [color=cyan]впивается[/color], но не причиняет вреда.

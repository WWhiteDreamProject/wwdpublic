# Damage examines
damage-examinable-verb-text = Повреждения
damage-examinable-verb-message = Изучить показатели урона.
damage-hitscan = хитскан
damage-projectile = снаряд
damage-melee = ближний бой
damage-melee-heavy = размах
damage-throw = бросок
damage-examine = Наносит следующие повреждения:
damage-examine-type = Наносит следующие повреждения ({ $type }):
damage-stamina-cost = [color=cyan]{CAPITALIZE($type)}[/color] тратит [color=orange]{$cost}[/color] [color=yellow]выносливости[/color].
damage-value =
    - [color=red]{ $amount }[/color] единиц [color=yellow]{ $type ->
       *[other] неизвестного
        [Blunt] тупого
        [Slash] рубящего
        [Piercing] проникающего
        [Heat] теплового
        [Radiation] радиационного
        [Caustic] кислотного
        [Structural] структурного
    }[/color] урона.

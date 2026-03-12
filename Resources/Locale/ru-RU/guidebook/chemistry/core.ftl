guidebook-reagent-effect-description =
    {$chance ->
        [1] { $effect }
        *[other] Имеет [color=#C8B8A0]{ NATURALPERCENT($chance, 2) }[/color] шанс на { $effect }
    }{ $conditionCount ->
        [0] .
        *[other] , когда { $conditions }.
    }

guidebook-reagent-name = [bold][color={ $color }]{ CAPITALIZE($name) }[/color][/bold]
guidebook-reagent-recipes-header = Рецепт
guidebook-reagent-recipes-reagent-display = [bold]{ $reagent }[/bold] \[{ $ratio }\]
guidebook-reagent-sources-header = Источники
guidebook-reagent-sources-ent-wrapper = [bold]{$name}[/bold] \[1\]
guidebook-reagent-sources-gas-wrapper = [bold]{$name} [color=gray](газ)[/color][/bold] \[1\]
guidebook-reagent-effects-header = Эффекты
guidebook-reagent-effects-metabolism-group-rate = [bold]{ $group }[/bold] [color=gray]({ $rate } единиц в секунду)[/color]
guidebook-reagent-plant-metabolisms-header = Метаболизм растений
guidebook-reagent-plant-metabolisms-rate = [bold]Метаболизм растений[/bold] [color=gray](1 унция каждые 3 секунды в качестве базовой)[/color]
guidebook-reagent-physical-description = [italic]Кажется {$description}.[/italic]
guidebook-reagent-recipes-mix-info = {$minTemp ->
    [0] {$hasMax ->
            [true] {CAPITALIZE($verb)} ниже [color=#80C0E8]{NATURALFIXED($maxTemp, 2)}K[/color]
            *[false] {CAPITALIZE($verb)}
        }
    *[other] {CAPITALIZE($verb)} {$hasMax ->
            [true] между [color=#E8C080]{NATURALFIXED($minTemp, 2)}K[/color] и [color=#80C0E8]{NATURALFIXED($maxTemp, 2)}K[/color]
            *[false] выше [color=#E8C080]{NATURALFIXED($minTemp, 2)}K[/color]
        }
}


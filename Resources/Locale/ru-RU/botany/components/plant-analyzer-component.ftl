plant-analyzer-component-no-seed = растение не найдено

plant-analyzer-component-health = Здоровье:
plant-analyzer-component-age = Возраст:
plant-analyzer-component-water = Вода:
plant-analyzer-component-nutrition = Питание:
plant-analyzer-component-toxins = Токсины:
plant-analyzer-component-pests = Вредители:
plant-analyzer-component-weeds = Сорняки:

plant-analyzer-component-alive = [color=green]ЖИВ[color]
plant-analyzer-component-dead = [color=red]МЁРТВ[color]
plant-analyzer-component-unviable = [color=red]НЕЖИЗНЕСПОСОБНЫЙ[color]
plant-analyzer-component-mutating = [color=#00ff5f]МУТИРУЕТ[color]
plant-analyzer-component-kudzu = [color=red]КУДЗУ[color]

plant-analyzer-soil = Есть {$count ->
    [one]некоторый
    *[other]некоторые
} not been absorbed.
plant-analyzer-soil-empty = Этот {$holder} не имеет невпитанных веществ.

plant-analyzer-component-environemt = Этот [color=green]{$seedName}[/color] нуждается в атмосфере с давлением [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color], температурой [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color] и уровнем света [color=white]{$lightLevel} ± {$lightTolerance}[/color].
plant-analyzer-component-environemt-void = Этот [color=green]{$seedName}[/color] должен выращиваться [bolditalic]в вакууме космоса[/bolditalic] при уровне света [color=white]{$lightLevel} ± {$lightTolerance}[/color].
plant-analyzer-component-environemt-gas = Этот [color=green]{$seedName}[/color] нуждается в атмосфере, которая содержит [bold]{$gases}[/bold], имеет давление [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color], температуру [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color] и уровень света [color=white]{$lightLevel} ± {$lightTolerance}[/color].

plant-analyzer-produce-plural = {MAKEPLURAL($thing)}
plant-analyzer-output = {$yield ->
    [0]{$gasCount ->
        [0]Кажется, единственное, что он делает, это потребляет воду и питательные вещества.
        *[other]Кажется, единственное, что он делает, это перерабатывает воду и питательные вещества в [bold]{$gases}[/bold].
    }
    *[other]У него есть [color=lightgreen]{$yield} {$potency}[/color]{$seedless ->
        [true]{" "}но [color=red]без семян[/color]
        *[false]{$nothing}
    }{" "}{$yield ->
        [one]цветок
        *[other]цветы
    }{" "}этот{$gasCount ->
        [0]{$nothing}
        *[other]{$yield ->
            [one]{" "}излучает
            *[other]{" "}излучать
        }{" "}[bold]{$gases}[/bold] и
    }{" "}которые превратятся в{$yield ->
        [one]{" "}{INDEFINITE($firstProduce)} [color=#a4885c]{$produce}[/color]
        *[other]{" "}[color=#a4885c]{$producePlural}[/color]
    }.{$chemCount ->
        [0]{$nothing}
        *[other]{" "}Имеются следы [color=white]{$chemicals}[/color] в корне.
    }
}


plant-analyzer-potency-tiny = крошечная
plant-analyzer-potency-small = маленькая
plant-analyzer-potency-below-average = ниже среднего
plant-analyzer-potency-average = средний
plant-analyzer-potency-above-average = выше среднего
plant-analyzer-potency-large = довольно большой
plant-analyzer-potency-huge = огромный
plant-analyzer-potency-gigantic = гигантский
plant-analyzer-potency-ludicrous = смехотворно большой
plant-analyzer-potency-immeasurable = неизмеримо большой

plant-analyzer-print = Распечатать
plant-analyzer-printout-missing = N/A
plant-analyzer-printout = [color=#9FED58][head=2]Отчёт Анализатора Растений[/head][/color]{$nl
    }──────────────────────────────{$nl
    }[bullet/] Вид: {$seedName}{$nl
    }{$indent}[bullet/] Жизнееспособный: {$viable ->
        [no][color=red]Нет[/color]
        [yes][color=green]Да[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }{$nl
    }{$indent}[bullet/] Выносливость: {$endurance}{$nl
    }{$indent}[bullet/] Продолжительность жизни: {$lifespan}{$nl
    }{$indent}[bullet/] Продукт: [color=#a4885c]{$produce}[/color]{$nl
    }{$indent}[bullet/] Кудзу: {$kudzu ->
        [no][color=green]Нет[/color]
        [yes][color=red]Да[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }{$nl
    }[bullet/] Профиль роста:{$nl
    }{$indent}[bullet/] Вода: [color=cyan]{$water}[/color]{$nl
    }{$indent}[bullet/] Питание: [color=orange]{$nutrients}[/color]{$nl
    }{$indent}[bullet/] Токсины: [color=yellowgreen]{$toxins}[/color]{$nl
    }{$indent}[bullet/] Вредители: [color=magenta]{$pests}[/color]{$nl
    }{$indent}[bullet/] Сорняки: [color=red]{$weeds}[/color]{$nl
    }[bullet/] Профиль окружения:{$nl
    }{$indent}[bullet/] Состав: [bold]{$gasesIn}[/bold]{$nl
    }{$indent}[bullet/] Давление: [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color]{$nl
    }{$indent}[bullet/] Температура: [color=lightsalmon]{$temp}°k ± {$tempTolerance}°k[/color]{$nl
    }{$indent}[bullet/] Свет: [color=gray][bold]{$lightLevel} ± {$lightTolerance}[/bold][/color]{$nl
    }[bullet/] Цветы: {$yield ->
        [-1]{LOC("plant-analyzer-printout-missing")}
        [0][color=red]0[/color]
        *[other][color=lightgreen]{$yield} {$potency}[/color]
    }{$nl
    }[bullet/] Семена: {$seeds ->
        [no][color=red]Нет[/color]
        [yes][color=green]Да[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }{$nl
    }[bullet/] Химикаты: [color=gray][bold]{$chemicals}[/bold][/color]{$nl
    }[bullet/] Выбросы: [bold]{$gasesOut}[/bold]

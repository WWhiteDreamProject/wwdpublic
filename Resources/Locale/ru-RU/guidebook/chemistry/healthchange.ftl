health-change-display =
    { $deltasign ->
        [-1] [color=green]{NATURALFIXED($amount, 2)}ед.[/color]
        *[1] [color=red]{NATURALFIXED($amount, 2)}ед.[/color]
    } { $id ->
    [Blunt] [color=#DF3E3E]Тупого урона[/color]
    [Slash] [color=#DF3E3E]Рубящего урона[/color]
    [Piercing] [color=#DF3E3E]Проникающего урона[/color]
    [Heat] [color=#FFD700]Теплового урона[/color]
    [Cold] [color=#3c66f1]Холодного урона[/color]
    [Shock] [color=#FFD700]Электрического урона[/color]
    [Caustic] [color=#47e24f]Кислотного урона[/color]
    [Poison] [color=#A080FF]Ядов[/color]
    [Radiation] [color=#FF9505]Радиации[/color]
    [Asphyxiation] [color=#7DD0FF]Удушения[/color]
    [Bloodloss] [color=#862626]Кровопотери[/color]
    [Cellular] [color=#accc42]Клеточного урона[/color]
    [Structural] [color=#C0C0C0]Структурного урона[/color]
    [Holy] [color=#FFFFFF]Святого урона[/color]
    [Brute] [color=#DF3E3E]Механического урона[/color]
    [Burn] [color=#FFD700]Ожогов[/color]
    [Toxin] [color=#A080FF]Токсинов[/color]
    [Airloss] [color=#7DD0FF]Удушья[/color]
    [Genetic] [color=#accc42]Генетического урона[/color]
    [Metaphysical] [color=#FFFFFF]Метафизического урона[/color]
   *[other] [color=#E65CE6]{ $kind }[/color]
}


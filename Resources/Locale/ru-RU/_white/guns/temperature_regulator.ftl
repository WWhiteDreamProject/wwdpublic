gun-regulator-examine-safety = {$enabled ->
    [true] Ограничения конструкции не позволяют вести огонь при температуре выше [color=yellow]{ $limit } °C[/color].
    *[false] Оружие может вести стрельбу при любой температуре.
}

gun-regulator-examine-safety-toggleable = Оружие оснащено предохранителем, предотвращающим стрельбу при температуре выше [color=yellow]{ $limit } °C[/color]. Предохранитель {$enabled ->
    [true]   [color=yellow]включён[/color].
    *[false] [color=yellow]выключен[/color].
}
gun-regulator-examine-lamp = {$lampstatus ->
    *[0] Регуляторная лампа [color=yellow]отсутствует[/color].
    [1]  Регуляторная лампа [color=yellow]повреждена и требует замены[/color].
    [2]  Регуляторная лампа [color=yellow]установлена и исправна.[/color]
}


gun-regulator-lamp-examine-temperature-range = Эта лампа обеспечит бесперебойную работу при температурах ниже [color=green]{ $safetemp } °C[/color] и гарантированно выйдет из строя после одного выстрела при температуре от [color=red]{ $unsafetemp } °C[/color] и выше.
gun-regulator-lamp-examine-intact = {$intact ->
    [true]   Лампа [color=yellow]исправна[/color].
    *[false] Лампа [color=yellow]сломана[/color].
}

verb-categories-safety = Предохранитель
fireselector-100up-verb = +100°C
fireselector-10up-verb = +10°C
fireselector-toggle-verb = вкл/выкл
fireselector-10down-verb = -10°C
fireselector-100down-verb = -100°C

gun-regulator-temperature-limit-exceeded-popup = Оружие перегрелось!
gun-regulator-lamp-missing-popup = Регуляторная лампа отсутствует!
gun-regulator-lamp-broken-popup = Регуляторная лампа сломана!



ent-BasicRegulatorLamp = регуляторная лампа
    .desc = Внешняя регулирующая лампа, которую можно быстро установить или заменить во время боя. Некоторое лазерное оружие полагается на эти лампы при регулировании лазерного потока.

ent-BoxBasicRegulatorLamp = коробка регуляторных ламп
    .desc = Ангел-хранитель некомпетентных сотрудников Службы Безопасности по всему сектору. Содержит 6 вторых шансов для кадетов.

ent-BoxBasicRegulatorLampBig = большая коробка регуляторных ламп
    .desc = Ангел-хранитель некомпетентных сотрудников Службы Безопасности по всему сектору. Содержит аж 12 вторых шансов для кадетов.

ent-AdvancedRegulatorLamp = продвинутая регуляторная лампа
    .desc = продвинутая регулирующая лампа, которую можно быстро установить или заменить во время боя, гораздо более выносливая нежели обычная версия.

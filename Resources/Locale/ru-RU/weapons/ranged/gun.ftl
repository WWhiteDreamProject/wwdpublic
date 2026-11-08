
gun-selected-mode-examine = Выбран режим огня [color={ $color }]{ $mode }[/color].
gun-fire-rate-examine = Скорострельность [color={ $color }]{ $fireRate }[/color] выстрелов в минуту.
gun-burst-fire-rate-examine = Скорострельность очередью составляет [color={$color}]{ $fireRate }[/color] выстрелов в минуту.
gun-burst-fire-burst-count =
    { $burstcount ->
        [2] Можно установить стрельбу очередями по [color={$color}]{ $burstcount }[/color] выстрела.
        [3] Можно установить стрельбу очередями по [color={$color}]{ $burstcount }[/color] выстрела.
        [4] Можно установить стрельбу очередями по [color={$color}]{ $burstcount }[/color] выстрела.
        *[other] Можно установить стрельбу очередями по [color={$color}]{ $burstcount }[/color] выстрелов.
    }
gun-damage-modifier-examine = Выстрелы этим наносят [color={$color}]x{$damage}[/color] урона.
gun-selector-verb = Изменить на { $mode }
gun-selected-mode = Выбран { $mode }
gun-disabled = Вы не можете использовать оружие!
gun-clumsy = Оружие взрывается вам в лицо!
gun-set-fire-mode = Установлен режим {$mode}

# SelectiveFire
gun-magazine-whitelist-fail = Это не влезет в пистолет!

# SelectiveFire
gun-SemiAuto = полуавто
gun-Burst = очередь
gun-FullAuto = авто

# BallisticAmmoProvider
# BallisticAmmoProvider
gun-ballistic-cycle = Перезарядка
gun-ballistic-cycled = Перезаряжено
gun-ballistic-cycled-empty = Разряжено
gun-ballistic-transfer-invalid = { CAPITALIZE($ammoEntity) } нельзя поместить в { $targetEntity }!
gun-ballistic-transfer-empty = В { CAPITALIZE($entity) } пусто.
gun-ballistic-transfer-target-full = { CAPITALIZE($entity) } уже полностью заряжен.

# CartridgeAmmo
# CartridgeAmmo
gun-cartridge-spent = Он [color=red]израсходован[/color].
gun-cartridge-unspent = Он [color=lime]не израсходован[/color].

# BatteryAmmoProvider
# BatteryAmmoProvider
gun-battery-examine = Заряда хватит на [color={ $color }]{ $count }[/color] выстрелов.

# CartridgeAmmoProvider
# CartridgeAmmoProvider
gun-chamber-bolt-ammo = Затвор не закрыт
gun-chamber-bolt = Затвор [color={$color}]{$bolt}[/color].
gun-chamber-bolt-closed = Затвор закрыт
gun-chamber-bolt-opened = Затвор открыт
gun-chamber-bolt-close = Закрыть завтор
gun-chamber-bolt-open = Открыть затвор
gun-chamber-bolt-closed-state = открыт
gun-chamber-bolt-open-state = закрыт
gun-chamber-rack = Разрядить

# MagazineAmmoProvider
# MagazineAmmoProvider
gun-magazine-examine = Осталось [color={ $color }]{ $count }[/color] выстрелов.

# RevolverAmmoProvider
# RevolverAmmoProvider
gun-revolver-empty = Разрядить револьвер
gun-revolver-full = Револьвер полностью заряжен
gun-revolver-insert = Заряжен
gun-revolver-spin = Вращать барабан
gun-revolver-spun = Барабан вращается
gun-speedloader-empty = Спидлоадер пуст

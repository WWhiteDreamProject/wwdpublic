# Chat window radio wrap (prefix and postfix)
chat-radio-message-wrap = [color={$color}]{$channel} [font size=11][color={$languageColor}][bold]{$language}[/bold][/color][/font][bold]{$name}[/bold] {$verb}, [font="{$fontType}" size={$fontSize}][color={$messageColor}]"{$message}"[/color][/font][/color]
chat-radio-message-wrap-bold = [color={$color}]{$channel} [font size=11][color={$languageColor}][bold]{$language}[/bold][/color][/font][bold]{$name}[/bold] {$verb}, [font="{$fontType}" size={$fontSize}][color={$messageColor}][bold]"{$message}"[/bold][/color][/font][/color]
# WD edit end
examine-headset-default-channel =
    Канал, использующийся этой гарнитурой по умолчанию - [color={ $color }]{ $channel ->
        [Syndicate] Синдикат
        [Supply] Снабжение
        [Command] Командование
        [CentCom] ЦентКом
        [Common] Общий
        [Engineering] Инженерный
        [Science] Научный
        [Medical] Медицинский
        [Security] Безопасность
        [Service] Сервисный
       *[other] _
    }[/color].
chat-radio-common = Общий
chat-radio-centcom = ЦентКом
chat-radio-command = Командование
chat-radio-engineering = Инженерный
chat-radio-medical = Медицинский
chat-radio-science = Научный
chat-radio-security = Безопасность
chat-radio-service = Сервисный
chat-radio-supply = Снабжение
chat-radio-syndicate = Синдикат
chat-radio-freelance = Наемный
# not headset but whatever
chat-radio-handheld = Портативный
chat-radio-binary = Бинарный

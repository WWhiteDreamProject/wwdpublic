# ban
cmd-ban-desc = Банит кого-либо
cmd-ban-help = Usage: <name or user ID> <reason> [duration in minutes, leave out or 0 for permanent ban] [use True for global ban, otherwise False]
cmd-ban-player = Не удалось найти игрока с таким именем.
cmd-ban-invalid-minutes = {$minutes} — это недопустимое количество минут!
cmd-ban-invalid-severity = «{$severity}» не является допустимой степенью серьезности!
cmd-ban-invalid-arguments = Недопустимое количество аргументов
cmd-ban-hint = <name/user ID>
cmd-ban-hint-reason = <reason>
cmd-ban-hint-duration = [duration]
cmd-ban-hint-severity = [severity]

cmd-ban-hint-duration-1 = Навсегда
cmd-ban-hint-duration-2 = 1 день
cmd-ban-hint-duration-3 = 3 дня
cmd-ban-hint-duration-4 = 1 неделя
cmd-ban-hint-duration-5 = 2 недели
cmd-ban-hint-duration-6 = 1 месяц
# listbans
# ban panel
cmd-banpanel-desc = Открывает панель банов
cmd-banpanel-help = Использование: banpanel [name or user guid]
cmd-banpanel-server = Это не может быть использовано из консоли сервера
cmd-banpanel-player-err = Указанный игрок не может быть найден

# listbans
cmd-banlist-desc = Список активных банов пользователя.
cmd-banlist-help = Использование: banlist <name or user ID>
cmd-banlist-empty = Нет активных банов у пользователя { $user }
cmd-banlistF-hint = <name/user ID>
cmd-ban_exemption_update-desc = Установите исключение для определенного типа запрета для игрока.
cmd-ban_exemption_update-help =
    Использование: ban_exemption_update <игрок> <флаг> [<флаг> [...]]
    Укажите несколько флагов, чтобы предоставить игроку несколько флагов освобождения от бана.
    Чтобы удалить все исключения, запустите эту команду и укажите "None" в качестве единственного флага.
cmd-ban_exemption_update-nargs = Ожидалось по крайней мере 2 аргумента
cmd-ban_exemption_update-locate = Не удается найти игрока '{ $player }'.
cmd-ban_exemption_update-invalid-flag = Недопустимый флаг '{ $flag }'.
cmd-ban_exemption_update-success = Обновлены флаги исключения из запрета для '{ $player }' ({ $uid }).
cmd-ban_exemption_update-arg-player = <игрок>
cmd-ban_exemption_update-arg-flag = <флаг>
cmd-ban_exemption_get-desc = Показать исключения из бана для определенного игрока.
cmd-ban_exemption_get-help = Использование: ban_exemption_get <игрок>
cmd-ban_exemption_get-nargs = Ожидается 1 аргумент
cmd-ban_exemption_get-none = Пользователь не освобождается от каких-либо запретов.
cmd-ban_exemption_get-show = Пользователь освобожден от следующих флагов запрета: { $flags }.
cmd-ban_exemption_get-arg-player = <игрок>

# Kick on ban
# Ban panel
ban-panel-title = Панель бана
ban-panel-player = Игрок
ban-panel-ip = IP
ban-panel-hwid = HWID
ban-panel-reason = Причина
ban-panel-last-conn = Использовать IP-адрес и HWID из последнего подключения?
ban-panel-submit = Бан
ban-panel-confirm = Вы уверены?
ban-panel-tabs-basic = Основная информация
ban-panel-tabs-reason = Причина
ban-panel-tabs-players = Список игроков
ban-panel-tabs-role = Информация о джоббане
ban-panel-no-data = Вы должны указать либо пользователя, IP-адрес, либо HWID для бана
ban-panel-invalid-ip = Не удалось разобрать IP-адрес. Пожалуйста, попробуйте снова
ban-panel-select = Выберите тип
ban-panel-server = Серверный бан
ban-panel-role = Джоббан
ban-panel-minutes = Минуты
ban-panel-hours = Часы
ban-panel-days = Дни
ban-panel-weeks = Недели
ban-panel-months = Месяца
ban-panel-years = Года
ban-panel-permanent = Пермач
ban-panel-ip-hwid-tooltip = Оставьте это поле пустым и установите флажок ниже, чтобы использовать данные последнего подключения
ban-panel-severity = Строгость:
ban-panel-erase = Удалить сообщения игрока в чате и его персонажа из раунда с корнем

# Ban string
server-ban-string = {$admin} забанил игрока [{$name}, {$ip}, {$hwid}] со строгостью «{$severity}». Срок действия бана истекает {$expires} и его причина: «{$reason}»
server-ban-string-no-pii = {$admin} забанил игрока {$name} со строгостью «{$severity}». Срок действия бана истекает {$expires}. Причина: «{$reason}»
server-ban-string-never = **НИКОГДА**, хонк!

# Kick on ban
ban-kick-reason = Вы были забанены

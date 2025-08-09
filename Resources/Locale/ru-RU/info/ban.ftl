# ban
cmd-ban-desc = Банит кого-либо
cmd-ban-help = Usage: <name or user ID> <reason> [duration in minutes, leave out or 0 for permanent ban] [use True for global ban, otherwise False]
cmd-ban-player = Не удалось найти игрока с таким именем.
cmd-ban-invalid-minutes = {$minutes} is not a valid amount of minutes!
cmd-ban-invalid-severity = {$severity} is not a valid severity!
cmd-ban-invalid-arguments = Invalid amount of arguments
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
cmd-banpanel-desc = Opens the ban panel
cmd-banpanel-help = Usage: banpanel [name or user guid]
cmd-banpanel-server = This can not be used from the server console
cmd-banpanel-player-err = The specified player could not be found

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
ban-panel-title = Banning panel
ban-panel-player = Player
ban-panel-ip = IP
ban-panel-hwid = HWID
ban-panel-reason = Reason
ban-panel-last-conn = Use IP and HWID from last connection?
ban-panel-submit = Ban
ban-panel-confirm = Are you sure?
ban-panel-tabs-basic = Basic info
ban-panel-tabs-reason = Reason
ban-panel-tabs-players = Player List
ban-panel-tabs-role = Role ban info
ban-panel-no-data = You must provide either a user, IP or HWID to ban
ban-panel-invalid-ip = The IP address could not be parsed. Please try again
ban-panel-select = Select type
ban-panel-server = Server ban
ban-panel-role = Role ban
ban-panel-minutes = Minutes
ban-panel-hours = Hours
ban-panel-days = Days
ban-panel-weeks = Weeks
ban-panel-months = Months
ban-panel-years = Years
ban-panel-permanent = Permanent
ban-panel-ip-hwid-tooltip = Leave empty and check the checkbox below to use last connection's details
ban-panel-severity = Severity:
ban-panel-erase = Erase chat messages and player from round

# Ban string
server-ban-string = {$admin} created a {$severity} severity server ban that expires {$expires} for [{$name}, {$ip}, {$hwid}], with reason: {$reason}
server-ban-string-no-pii = {$admin} created a {$severity} severity server ban that expires {$expires} for {$name} with reason: {$reason}
server-ban-string-never = never

# Kick on ban
ban-kick-reason = Вы были забанены

# ban
cmd-ban-desc = Банит кого-либо
cmd-ban-help = Usage: <name or user ID> <reason> [duration in minutes, leave out or 0 for permanent ban] [use True for global ban, otherwise False]
cmd-ban-player = Не удалось найти игрока с таким именем.
cmd-ban-self = Вы не можете забанить себя!
cmd-ban-hint = <name/user ID>
cmd-ban-hint-reason = <reason>
cmd-ban-hint-duration = [duration]
cmd-ban-hint-duration-1 = Навсегда
cmd-ban-hint-duration-2 = 1 день
cmd-ban-hint-duration-3 = 3 дня
cmd-ban-hint-duration-4 = 1 неделя
cmd-ban-hint-duration-5 = 2 недели
cmd-ban-hint-duration-6 = 1 месяц
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

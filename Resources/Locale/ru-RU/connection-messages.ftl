cmd-whitelistadd-desc = Добавить игрока с указанным юзернеймом в вайтлист.
cmd-whitelistadd-help = Использование: whitelistadd <username или User ID>
cmd-whitelistadd-existing = { $username } уже в вайтлисте!
cmd-whitelistadd-added = { $username } добавлен в вайтлист
cmd-whitelistadd-not-found = Пользователь '{ $username }' не найден
cmd-whitelistadd-arg-player = [player]

cmd-whitelistremove-desc = Удалить игрока с указанным юзернеймом из вайтлиста.
cmd-whitelistremove-help = Использование: whitelistremove <username или User ID>
cmd-whitelistremove-existing = { $username } не в вайтлисте!
cmd-whitelistremove-removed = Пользователь { $username } удалён из вайтлиста
cmd-whitelistremove-not-found = Пользователь '{ $username }' не найден
cmd-whitelistremove-arg-player = [player]

command-kicknonwhitelisted-description = Кикнуть с сервера всех пользователей не из вайтлиста.
command-kicknonwhitelisted-help = Использование: kicknonwhitelisted

ban-banned-permanent = Этот бан можно только обжаловать.
ban-banned-permanent-appeal = Этот бан можно только обжаловать. Для этого посетите наш форум: { $link }
ban-expires = Вы получили бан на { $duration } минут, и он истечёт { $time } по UTC (для москосвкого времени добавьте 3 часа).
ban-banned-1 = Вам, или другому пользователю этого компьютера или соединения, запрещено здесь играть.
ban-banned-2 = Причина бана: "{ $reason }"
ban-banned-3 = Попытки обойти этот бан, такие как создание новой учетной записи, будут фиксироваться.

soft-player-cap-full = Сервер заполнен!
panic-bunker-account-denied = Этот сервер находится в режиме "Бункер". В данный момент новые подключения не принимаются. Повторите попытку позже
panic-bunker-account-denied-reason = Этот сервер находится в режиме "Бункер", и вам было отказано в доступе. Причина: "{ $reason }"
panic-bunker-account-reason-account = Ваш аккаунт должен быть старше { $minutes } минут
panic-bunker-account-reason-overall =
    Необходимо минимальное отыгранное время — { $hours } { $hours ->
        [one] час
        [few] часа
       *[other] часов
    }.

whitelist-playtime = У вас недостаточно игрового времени для подключения к этому серверу. Вам необходимо как минимум {$hours} минут игрового времени для подключения к этому серверу.
whitelist-player-count = В данный момент сервер не принимает игроков. Пожалуйста, попробуйте позже.
whitelist-notes = У вас слишком много административных замечаний для подключения к этому серверу. Вы можете проверить свои замечания, набрав /adminremarks в чате.
whitelist-manual = Вы не добавлены в белый список этого сервера.
whitelist-blacklisted = Вы заблокированы на этом сервере.
whitelist-always-deny = Вам не разрешено подключаться к этому серверу.
whitelist-fail-prefix = Нет в белом списке: {$msg}
whitelist-misconfigured = Сервер некорректно настроен и не принимает игроков. Пожалуйста, свяжитесь с владельцем сервера и попробуйте позже.

cmd-blacklistadd-desc = Добавляет игрока с указанным именем пользователя в черный список сервера.
cmd-blacklistadd-help = Использование: blacklistadd <username или User ID>
cmd-blacklistadd-existing = {$username} уже находится в черном списке!
cmd-blacklistadd-added = {$username} добавлен в черный список
cmd-blacklistadd-not-found = Не удалось найти '{$username}'
cmd-blacklistadd-arg-player = [игрок]

cmd-blacklistremove-desc = Удаляет игрока с указанным именем пользователя из черного списка сервера.
cmd-blacklistremove-help = Использование: blacklistremove <username или User ID>
cmd-blacklistremove-existing = {$username} не находится в черном списке!
cmd-blacklistremove-removed = {$username} удален из черного списка
cmd-blacklistremove-not-found = Не удалось найти '{$username}'
cmd-blacklistremove-arg-player = [игрок]

baby-jail-account-denied = Этот сервер - сервер для новичков, предназначенный для новых игроков и тех, кто хочет им помочь. Новые подключения слишком старых или не внесенных в белый список аккаунтов не принимаются. Загляните на другие серверы и посмотрите все, что может предложить Space Station 14. Веселитесь!
baby-jail-account-denied-reason = Этот сервер - сервер для новичков, предназначенный для новых игроков и тех, кто хочет им помочь. Новые подключения слишком старых или не внесенных в белый список аккаунтов не принимаются. Загляните на другие серверы и посмотрите все, что может предложить Space Station 14. Веселитесь! Причина: "{ $reason }"
baby-jail-account-reason-account = Ваш аккаунт Space Station 14 слишком старый. Он должен быть моложе { $hours } минут
baby-jail-account-reason-overall =
    Наигранное Вами время на сервере должно быть больше { $hours } { $hours ->
        [one] минуты
       *[other] минут
    }.

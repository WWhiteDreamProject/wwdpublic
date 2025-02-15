## Strings for the "grant_connect_bypass" command.

cmd-grant_connect_bypass-desc = Временно разрешить пользователю игнорировать обычные проверки подключения.
cmd-grant_connect_bypass-help = Использование: grant_connect_bypass <пользователь> [продолжительность в минутах]
    Временно предоставляет пользователю возможность игнорировать обычные ограничения на подключение.
    Обход применяется только к этому игровому серверу и истекает через (по умолчанию) 1 час.
    Они смогут присоединиться независимо от белого списка, бункера паники или ограничения количества игроков.

cmd-grant_connect_bypass-arg-user = <пользователь>
cmd-grant_connect_bypass-arg-duration = [продолжительность в минутах]

cmd-grant_connect_bypass-invalid-args = Ожидалось 1 или 2 аргумента
cmd-grant_connect_bypass-unknown-user = Невозможно найти пользователя '{$user}'
cmd-grant_connect_bypass-invalid-duration = Неверная продолжительность '{$duration}'

cmd-grant_connect_bypass-success = Успешно добавлен обход для пользователя '{$user}'
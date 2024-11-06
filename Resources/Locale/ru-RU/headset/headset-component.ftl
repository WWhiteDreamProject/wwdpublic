# Chat window radio wrap (prefix and postfix)
chat-radio-message-wrap = [color={ $color }]{ $channel } { $name } говорит, "{ $message }"[/color]
headset-encryption-key-successfully-installed = Вы вставляете ключ в гарнитуру.
headset-encryption-key-slots-already-full = Здесь нет места для другого ключа.
headset-encryption-keys-all-extracted = Вы вытаскиваете ключи шифрования из гарнитуры!
headset-encryption-keys-no-keys = У этой гарнитуры нет ключей шифрования!
headset-encryption-keys-are-locked = Слоты для ключей гарнитуры заблокированы, вы не можете добавлять или удалять какие-либо ключи.
examine-encryption-key-channels-prefix = Он обеспечивает эти частоты для гарнитуры:
examine-radio-frequency = Он настроен на трансляцию на частоте { $frequency }.
examine-headset-channels-prefix = На небольшом экране гарнитуры отображаются следующие доступные частоты:
examine-headset-channel = [color={ $color }]{ $keys } для { $id } ({ $freq })[/color]
examine-headset-no-keys = Похоже, это сломано. Нет ключей шифрования.
examine-default-channel = Используй { $prefix } для стандартного ([color={ $color }]{ $channel }[/color]).
# not headset but whatever
chat-radio-handheld = Портативный
examine-headset-chat-prefix = Используй { $prefix } для частоты своего отдела.
examine-headset-default-channel =
    Это указывает на то, что канал по умолчанию этой гарнитуры - [color={ $color }]{ $channel ->
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
examine-encryption-key-default-channel = Похоже, что [color={ $color }]{ $channel }[/color] - это канал по умолчанию.
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
chat-radio-binary = Бинарный
chat-radio-freelance = Наемный

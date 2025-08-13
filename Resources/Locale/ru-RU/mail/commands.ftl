# Mailto
command-mailto-description = Помещает посылку в очередь на доставку указанному получателю. Пример использования: `mailto 1234 5678 false false`. Содержимое целевого контейнера будет упаковано в почтовую посылку.
command-mailto-help = Использование: {$command} <UID получателя> <UID контейнера> [хрупкая: true/false] [приоритетная: true/false] [крупногабаритная: true/false, необязательно]
command-mailto-no-mailreceiver = Указанный получатель не содержит компонент {$requiredComponent}.
command-mailto-no-blankmail = Прототип {$blankMail} не существует. Это серьёзная ошибка. Свяжитесь с программистом.
command-mailto-bogus-mail = У {$blankMail} отсутствует компонент {$requiredMailComponent}. Это серьёзная ошибка. Свяжитесь с программистом.
command-mailto-invalid-container = Указанный контейнер не содержит контейнер {$requiredContainer}.
command-mailto-unable-to-receive = Указанный получатель не может принимать почту. Возможно, у него нет идентификатора.
command-mailto-no-teleporter-found = Не удалось сопоставить получателя с каким-либо почтовым телепортом станции. Возможно, он находится вне станции.
command-mailto-success = Успешно! Посылка будет телепортирована через {$timeToTeleport} сек.


# Mailnow
# Mailnow
command-mailnow = Мгновенно активирует все почтовые телепорты для немедленной отправки посылок. Лимит непринятых посылок не нарушается.
command-mailnow-help = Использование: {$command}
command-mailnow-success = Готово! Все телепорты вскоре совершат новую отправку.

# Mailtestbulk
# Mailtestbulk
command-mailtestbulk = Отправляет по одной посылке каждого типа на указанный почтовый телепорт. Автоматически вызывает `mailnow`.
command-mailtestbulk-help = Использование: {$command} <ID телепорта>
command-mailtestbulk-success = Готово! Все почтовые телепорты вскоре начнут новую отправку.

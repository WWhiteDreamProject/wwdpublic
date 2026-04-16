command-list-langs-desc = Перечислите языки, на которых ваша текущая сущность может говорить в данный момент.
command-list-langs-help = Использование: {$command}

command-saylang-desc = Отправьте сообщение на определенном языке. Чтобы выбрать язык, вы можете использовать либо название языка, либо его позицию в списке языков.
command-saylang-help = Использование: {$command} <language id> <message>. Пример: {$command} TauCetiBasic "Hello World!". Пример: {$command} 1 "Hello World!"

command-language-select-desc = Выберите язык, на котором в настоящее время говорит ваше подразделение. Вы можете использовать либо название языка, либо его позицию в списке языков.
command-language-select-help = Использование: {$command} <language id>. Пример: {$command} 1. Пример: {$command} TauCetiBasic

command-language-spoken = Говорит на:
command-language-understood = Понимает:
command-language-current-entry = {$id}. {$language} — {$name} (текущий)
command-language-entry = {$id}. {$language} — {$name}

command-language-invalid-number = Номер языка должен быть между 0 и {$total}. В качестве альтернативы используйте название языка.
command-language-invalid-language = Язык «{$id}» не существует или вы не можете на нём говорить.

# toolshed

# toolshed

command-description-language-add = Добавляет новый язык к указанному объекту. Два последних аргумента указывают, должен ли он быть разговорным/понятным. Пример: 'self language:add "Canilunzt" true true'
command-description-language-rm = Удаляет язык у указанного объекта. Работает аналогично language:add. Пример: 'self language:rm "TauCetiBasic" true true'.
command-description-language-lsspoken = Выводит список всех языков, на которых может говорить объект. Пример: 'self language:lsspoken'
command-description-language-lsunderstood = Выводит список всех языков, которые объект может понимать. Пример: 'self language:lssunderstood'

command-description-translator-addlang = Добавляет новый целевой язык к указанному объекту-переводчику.  См. language:add для подробностей.
command-description-translator-rmlang = Удаляет целевой язык у указанного объекта-переводчика. См. language:rm для подробностей.
command-description-translator-addrequired = Добавляет новый необходимый язык к указанному объекту-переводчику. Пример: 'ent 1234 translator:addrequired "TauCetiBasic"'
command-description-translator-rmrequired = Удаляет необходимый язык у указанного объекта-переводчика. Пример: 'ent 1234 translator:rmrequired "TauCetiBasic"'
command-description-translator-lsspoken = Выводит список всех разговорных языков для указанного объекта-переводчика. Пример: 'ent 1234 translator:lsspoken'
command-description-translator-lsunderstood = Выводит список всех понятных языков для указанного объекта-переводчика. Пример: 'ent 1234 translator:lsunderstood'
command-description-translator-lsrequired = Выводит список всех необходимых языков для указанного объекта-переводчика. Пример: 'ent 1234 translator:lsrequired'

command-language-error-this-will-not-work = Это не сработает.
command-language-error-not-a-translator = Объект {$entity} не является переводчиком.

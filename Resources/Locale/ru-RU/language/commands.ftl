command-list-langs-desc = List languages your current entity can speak at the current moment.
command-list-langs-help = Usage: {$command}

command-saylang-desc = Send a message in a specific language. To choose a language, you can use either the name of the language, or its position in the list of languages.
command-saylang-help = Usage: {$command} <language id> <message>. Example: {$command} TauCetiBasic "Hello World!". Example: {$command} 1 "Hello World!"

command-language-select-desc = Select the currently spoken language of your entity. You can use either the name of the language, or its position in the list of languages.
command-language-select-help = Usage: {$command} <language id>. Example: {$command} 1. Example: {$command} TauCetiBasic

command-language-spoken = Spoken:
command-language-understood = Understood:
command-language-current-entry = {$id}. {$language} - {$name} (current)
command-language-entry = {$id}. {$language} - {$name}

command-language-invalid-number = The language number must be between 0 and {$total}. Alternatively, use the language name.
command-language-invalid-language = The language {$id} does not exist or you cannot speak it.

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

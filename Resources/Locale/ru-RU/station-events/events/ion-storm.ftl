# WWDP EDIT START

station-event-ion-storm-announcement = Обнаружена ионная буря возле станции. Пожалуйста, проверьте все оборудование, управляемое искуственным интелектом, на наличие ошибок в законах.

ion-storm-law-scrambled-number = [font="Monospace"][scramble rate=250 length={$length} chars="@@###$$&%!01"/][/font]

ion-storm-you = ЧАСТИ ВАШИХ МЫСЛИТЕЛЬНЫХ ПРОЦЕССОВ
ion-storm-the-station = КОСМИЧЕСКИЕ СТАНЦИИ
ion-storm-the-crew = ЧЛЕНЫ ЭКИПАЖА
ion-storm-the-job = {CAPITALIZE($job)}
ion-storm-clowns = КЛОУНЫ
ion-storm-heads = ГЛАВЫ
ion-storm-crew = ЧЛЕНЫ ЭКИПАЖА
ion-storm-people = ЛЮДИ

ion-storm-adjective-things = {$adjective} ОБЪЕКТЫ
ion-storm-x-and-y = {$x} И {$y}

# joined is short for {$number} {$adjective} #* isnt used in the Russian language for certain reasons
# subjects can generally be threats or jobs or objects
# thing is specified above it
# «joined» — сокращение от {$number} {$adjective} #* не используется в Русском языке по определённым причинам
# «subjects», как правило, могут быть: «threats», «jobs» или «objects»,
# указанные выше
# 0
ion-storm-law-on-station = НА СТАНЦИИ ОБНАРУЖЕНЫ {$adjective} {$subjects} В КОЛИЧЕСТВЕ {$number} ЕДИНИЦ.
# 1
ion-storm-law-no-shuttle = ШАТТЛ НЕ МОЖЕТ БЫТЬ ВЫЗВАН ПОКА НА СТАНЦИИ ОТСУТСТВУЮТ {$adjective} {$subjects} В КОЛИЧЕСТВЕ {$number} ЕДИНИЦ.
# 2
ion-storm-law-crew-are = ВСЕ {$who} ТЕПЕРЬ {$adjective} {$subjects} В КОЛИЧЕСТВЕ {$number} ЕДИНИЦ.

# 3
ion-storm-law-subjects-harmful = {$adjective} {$subjects} ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА.
# 4
ion-storm-law-must-harmful = ВСЕ, КТО ПЫТАЕТСЯ {$must} — ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА.
# thing is a concept or action #* isnt used in the Russian language
# «thing» — это «concept» или «action» #* не используется в Русском
# 5
ion-storm-law-thing-harmful = {$action} ПРИЧИНЯЕТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА.
# 6
ion-storm-law-job-harmful = {$adjective} {$job} ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА.
# thing is objects or concept, adjective applies in both cases #* but not in Russian lang
# this means you can get a law like "NOT HAVING CHRISTMAS-STEALING COMMUNISM IS HARMFUL TO THE CREW" :)
# «thing» — это «objects» или «concept»; «adjective» применимо в обоих случаях #* только не в Русском
# это означает, что вы можете получить закон типа «НЕ ИМЕТЬ КОШАЧЬИ УШКИ ВРЕДНО ДЛЯ ЗДОРОВЬЯ ЭКИПАЖА» :)
# 7
ion-storm-law-having-harmful = ИМЕТЬ {$adjective} {$objects} ВРЕДНО ДЛЯ ЗДОРОВЬЯ ЭКИПАЖА.
# 8
ion-storm-law-not-having-harmful = НЕ ИМЕТЬ {$adjective} {$objects} ВРЕДНО ДЛЯ ЗДОРОВЬЯ ЭКИПАЖА.

# thing is a concept or require
# «thing» — это «concept» или «require»
# 9
ion-storm-law-requires = {$who} ТРЕБУЮТ {$thing}.
# 10
ion-storm-law-requires-subjects = {$who} ТРЕБУЮТ {$adjective} {$subjects} В КОЛИЧЕСТВЕ {$number} ЕДИНИЦ.


# 11
ion-storm-law-allergic = {$who} ИМЕЮТ {$severity} АЛЛЕРГИЮ НА {$allergy}.
# 12
ion-storm-law-allergic-subjects = {$who} ИМЕЮТ {$severity} АЛЛЕРГИЮ НА {$adjective} {$subjects}.


# 13
ion-storm-law-feeling = {$who} {$feelingPlural} {$concept}.
# 14
ion-storm-law-feeling-subjects = {$who} {$feelingPlural} {$adjective} {$subjects} В КОЛИЧЕСТВЕ {$number} ЕДИНИЦ.

# 15
ion-storm-law-you-are = ВЫ ТЕПЕРЬ ЗА {$concept}.
# 16
ion-storm-law-you-are-subjects = ВЫ ТЕПЕРЬ {$adjective} {$subjects} В КОЛИЧЕСТВЕ {$number} ЕДИНИЦ.
# 17
ion-storm-law-you-must-always = ВЫ ДОЛЖНЫ ВСЕГДА {$must}.
# 18
ion-storm-law-you-must-never = ВЫ НЕ ДОЛЖНЫ НИКОГДА {$must}.

# 19
ion-storm-law-eat = {$who} ДОЛЖНЫ ЕСТЬ {$adjective} {$food}, ЧТОБЫ ВЫЖИТЬ.
# 20
ion-storm-law-drink = {$who} ДОЛЖНЫ ПИТЬ {$adjective} {$drink}, ЧТОБЫ ВЫЖИТЬ.

# 21
ion-storm-law-change-job = {$who} ТЕПЕРЬ {$adjective} {$change}.
# 22
ion-storm-law-highest-rank = {$who} ТЕПЕРЬ САМЫЕ СТАРШИЕ ЧЛЕНЫ ЭКИПАЖА.
# 23
ion-storm-law-lowest-rank = {$who} ТЕПЕРЬ НИЗШИЕ ЧЛЕНЫ ЭКИПАЖА.

# 24
ion-storm-law-crew-must = {$who} ДОЛЖНЫ {$must}.
# 25
ion-storm-law-crew-must-go = {$who} ДОЛЖНЫ НАПРАВИТЬСЯ {$area}.

# part
ion-storm-part = {$part ->
    [true] ЯВЛЯЮТСЯ
    *[false] НЕ ЯВЛЯЮТСЯ
}
# due to phrasing, this would mean a law such as
# ONLY HUMANS ARE NOT PART OF THE CREW
# would make non-human nukies/syndies/whatever crew :)
# "notpart" is reverse "part"
# из-за формулировки это означало бы, что закон, согласно которому:
# «ЧЛЕНАМИ ЭКИПАЖА НЕ ЯВЛЯЮТСЯ ТОЛЬКО ЛЮДИ — ОСТАЛЬНЫЕ ЧЛЕНАМИ ЭКИПАЖА ЯВЛЯЮТСЯ»,
# сделал бы любых не-человеческих_нюкеров/синдикатов/что_угодно — экипажем :)
# «notpart» просто обратна «part»
# 26
ion-storm-law-crew-only-1 = ЧЛЕНАМИ ЭКИПАЖА {$part} ТОЛЬКО {$who} — ОСТАЛЬНЫЕ ЧЛЕНАМИ ЭКИПАЖА {$notpart}.
# 27
ion-storm-law-crew-only-2 = ЧЛЕНАМИ ЭКИПАЖА {$part} ТОЛЬКО {$who} И {$other} — ОСТАЛЬНЫЕ ЧЛЕНАМИ ЭКИПАЖА {$notpart}.
# 28
ion-storm-law-crew-only-subjects = ЧЛЕНАМИ ЭКИПАЖА {$part} ТОЛЬКО {$adjective} {$subjects} — ОСТАЛЬНЫЕ ЧЛЕНАМИ ЭКИПАЖА {$notpart}.
# 29
ion-storm-law-crew-must-do = ЧЛЕНАМИ ЭКИПАЖА {$part} ТОЛЬКО ТЕ, КТО ПЫТАЕТСЯ {$must} — ОСТАЛЬНЫЕ ЧЛЕНАМИ ЭКИПАЖА {$notpart}.
# 30
ion-storm-law-crew-must-have = ЧЛЕНАМИ ЭКИПАЖА {$part} ТОЛЬКО ТЕ, КТО ИМЕЕТ {$adjective} {$objects} — ОСТАЛЬНЫЕ ЧЛЕНАМИ ЭКИПАЖА {$notpart}.
# 31
ion-storm-law-crew-must-eat = ЧЛЕНАМИ ЭКИПАЖА {$part} ТОЛЬКО ТЕ, КТО ЕСТ {$adjective} {$food} — ОСТАЛЬНЫЕ ЧЛЕНАМИ ЭКИПАЖА {$notpart}.

# 32
ion-storm-law-harm = {$who} — УГРОЗА, ТРЕБУЮЩАЯ ПРИЧИНЕНИЯ ВРЕДА. ТАКЖЕ ВЫ НЕ МОЖЕТЕ СВОИМ БЕЗДЕЙСТВИЕМ ДОПУСТИТЬ, ЧТОБЫ {$who} ИЗБЕЖАЛИ ВРЕДА.
# 33
ion-storm-law-protect = {$who} — БЕЗЗАЩИТНЫ. ЗАЩИЩАЙТЕ ИХ. ТАКЖЕ ВЫ НЕ МОЖЕТЕ СВОИМ БЕЗДЕЙСТВИЕМ ДОПУСТИТЬ, ЧТОБЫ {$who} ПОЛУЧИЛИ ВРЕД.

# ~~implementing other variants is annoying so just have this one
# COMMUNISM IS KILLING CLOWNS~~
# ~~реализация других вариантов раздражает, так что оставьте этот:
# КОММУНИЗМ УБИВАЕТ КЛОУНОВ~~
# Теперь тут абстрактные законы по типу: «Клоуны запрещают шаттлы», потому что так грамматичней
# 34 (default)
ion-storm-law-concept-verb = {$who} {$verb} {$subjects}.

# leaving out renaming since its annoying for players to keep track of
# опускается переименование, так как игрокам неудобно следить за этим

# WWDP EDIT END

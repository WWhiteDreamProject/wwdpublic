ent-SpawnPointGhostBlob = точка появления блоба
    .suffix = DEBUG, точка появления призрака
    .desc = { ent-MarkerBase.desc }
ent-MobBlobPod = спора блоба
    .desc = Обычный боец блоба.
ent-MobBlobBlobbernaut = блоббернаут
    .desc = Элитный боец блоба.
ent-BaseBlob = базовый блоб
    .desc = { "" }
ent-NormalBlobTile = обычная плитка блоба
    .desc = Обычная часть блоба, необходимая для строительства более продвинутых плиток.
ent-CoreBlobTile = ядро блоба
    .desc = Самый важный орган блоба. Уничтожив ядро, инфекция прекратится.
ent-FactoryBlobTile = фабрика блоба
    .desc = Со временем производит споры блоба и блоббернаутов.
ent-ResourceBlobTile = ресурсная плитка блоба
    .desc = Производит ресурсы для блоба.
ent-NodeBlobTile = узловой блоб
    .desc = Мини-версия ядра, которая позволяет размещать специальные плитки блоба вокруг себя.
ent-StrongBlobTile = прочная плитка блоба
    .desc = Укреплённая версия обычной плитки. Не пропускает воздух и защищает от механических повреждений.
ent-ReflectiveBlobTile = отражающая плитка блоба
    .desc = Отражает лазеры, но не так хорошо защищает от механических повреждений.
objective-issuer-blob = Блоб


ghost-role-information-blobbernaut-name = Блоббернаут
ghost-role-information-blobbernaut-description = Вы - Блоббернаут. Вы должны защищать ядро блоба.

ghost-role-information-blob-name = Блоб
ghost-role-information-blob-description = Вы - инфекция Блоба. Поглотите станцию.

roles-antag-blob-name = Блоб
roles-antag-blob-objective = Достигните критической массы.

guide-entry-blob = Blob

# Popups
blob-target-normal-blob-invalid = Неправильный тип блоба, выберите обычный блоб.
blob-target-factory-blob-invalid = Неправильный тип блоба, выберите фабрику блоба.
blob-target-node-blob-invalid = Неправильный тип блоба, выберите узловой блоб.
blob-target-close-to-resource = Слишком близко к другому ресурсному блобу.
blob-target-nearby-not-node = Поблизости нет узлового или ресурсного блоба.
blob-target-close-to-node = Слишком близко к другому узловому блобу.
blob-target-already-produce-blobbernaut = Эта фабрика уже произвела блоббернаута.
blob-cant-split = Вы не можете разделить ядро блоба.
blob-not-have-nodes = У вас нет узлов.
blob-not-enough-resources = Недостаточно ресурсов.
blob-help = Только Бог может помочь вам.
blob-swap-chem = В разработке.
blob-mob-attack-blob = Вы не можете атаковать блоба.
blob-get-resource = +{ $point }
blob-spent-resource = -{ $point }
blobberaut-not-on-blob-tile = Вы умираете, находясь не на плитках блоба.
carrier-blob-alert = У вас осталось { $second } секунд до трансформации.
blob-core-under-attack = Ваше ядро атаковано!

blob-mob-zombify-second-start = { $pod } начинает превращать вас в зомби.
blob-mob-zombify-third-start = { $pod } начинает превращать { $target } в зомби.

blob-mob-zombify-second-end = { $pod } превращает вас в зомби.
blob-mob-zombify-third-end = { $pod } превращает { $target } в зомби.

blobberaut-factory-destroy = Ваша фабрика была уничтожена!
blob-target-already-connected = уже соединено


# UI
blob-chem-swap-ui-window-name = Смена химикатов
blob-chem-reactivespines-info = Реактивные шипы
                                Наносит 25 механического урона.
blob-chem-blazingoil-info = Пылающее масло
                            Наносит 15 ожогового урона и поджигает цели.
                            Делает вас уязвимым к воде.
blob-chem-regenerativemateria-info = Регенеративная материя
                                    Наносит 6 механического урона и 15 токсического урона.
                                    Ядро блоба восстанавливает здоровье в 10 раз быстрее и генерирует 1 дополнительный ресурс.
blob-chem-explosivelattice-info = Взрывчатая решётка
                                    Наносит 5 ожогового урона и взрывает цель, нанося 10 механического урона.
                                    Споры взрываются при смерти.
                                    Вы становитесь невосприимчивым к взрывам.
                                    Вы получаете на 50% больше урона от огня и электрического шока.
blob-chem-electromagneticweb-info = Электромагнитная паутина
                                    Наносит 20 ожогового урона, имеет 20% шанс вызвать ЭМИ-импульс при атаке.
                                    Плитки блоба вызывают ЭМИ-импульс при уничтожении.
                                    Вы получаете на 25% больше механического и теплового урона.

blob-alert-out-off-station = Блоб был удален, так как он был обнаружен за пределами станции!

# Announcment
blob-alert-recall-shuttle = Шаттл эвакуации не может быть отправлен, пока на станции присутствует биологическая угроза 5 уровня.
blob-alert-detect = Подтверждена вспышка биологической угрозы 5 уровня на борту станции. Весь персонал должен сдержать вспышку.
blob-alert-critical = Уровень биологической угрозы критический, коды аутентификации ядерной боеголовки отправлены на станцию. Центральное командование приказывает оставшемуся персоналу активировать механизм самоуничтожения.
blob-alert-critical-NoNukeCode = Уровень биологической угрозы критический. Центральное командование приказывает оставшемуся персоналу искать убежище и ожидать спасения.

# Actions
blob-create-factory-action-name = Создать фабрику блоба (40)
blob-create-factory-action-desc = Превращает выбранный обычный блоб в фабрику, которая может производить различных миньонов блоба, если рядом есть узел или ядро.
blob-create-storage-action-name = Создать хранилище блоба (50)
blob-create-storage-action-desc = Превращает выбранный обычный блоб в хранилище, что увеличивает максимальное количество ресурсов, которое может иметь блоб.
blob-create-turret-action-name = Создать турель блоба (75)
blob-create-turret-action-desc = Превращает выбранный обычный блоб в турель, которая стреляет по врагам маленькими спорами, потребляя очки.
blob-create-resource-action-name = Разместить ресурсный блоб (60)
blob-create-resource-action-desc = Превращает выбранный обычный блоб в ресурсный блоб, который генерирует ресурсы, если размещён рядом с ядром или узлом.
blob-create-node-action-name = Разместить узловой блоб (50)
blob-create-node-action-desc = Превращает выбранный обычный блоб в узловой блоб.
                                Узловой блоб активирует эффекты фабрик и ресурсных блобов, лечит другие блобы и медленно расширяется, разрушая стены и создавая обычные блобы.
blob-produce-blobbernaut-action-name = Создать блоббернаута (60)
blob-produce-blobbernaut-action-desc = Создаёт блоббернаута на выбранной фабрике. Каждая фабрика может сделать это только один раз. Блоббернаут будет получать урон вне плиток блоба и восстанавливаться рядом с узлами.
blob-split-core-action-name = Разделить ядро (400)
blob-split-core-action-desc = Вы можете сделать это только один раз. Превращает выбранный узел в независимое ядро, которое будет действовать самостоятельно.
blob-swap-core-action-name = Переместить ядро (200)
blob-swap-core-action-desc = Меняет местами расположение вашего ядра и выбранного узла.
blob-teleport-to-core-action-name = Прыжок к ядру (0)
blob-teleport-to-core-action-desc = Телепортирует вас к вашему ядру блоба.
blob-teleport-to-node-action-name = Прыжок к узлу (0)
blob-teleport-to-node-action-desc = Телепортирует вас к случайному узлу блоба.
blob-help-action-name = Помощь
blob-help-action-desc = Получить основную информацию об игре за блоба.
blob-swap-chem-action-name = Сменить химикаты (70)
blob-swap-chem-action-desc = Позволяет вам сменить текущий химикат.
blob-carrier-transform-to-blob-action-name = Превратиться в блоба
blob-carrier-transform-to-blob-action-desc = Мгновенно уничтожает ваше тело и создаёт ядро блоба. Убедитесь, что вы стоите на напольной плитке, иначе вы просто исчезнете.
blob-downgrade-action-name = Понизить уровень блоба (0)
blob-downgrade-action-desc = Превращает выбранную плитку обратно в обычный блоб, чтобы установить другие типы клеток.

# Ghost role
blob-carrier-role-name = Носитель блоба
blob-carrier-role-desc = Существо, заражённое блобом.
blob-carrier-role-rules = Вы антагонист. У вас есть 10 минут до превращения в блоба.
                        Используйте это время, чтобы найти безопасное место на станции. Имейте в виду, что вы будете очень слабым сразу после трансформации.
blob-carrier-role-greeting = Вы - носитель Блоба. Найдите укромное место на станции и превратитесь в Блоба. Превратите станцию в массу, а её обитателей - в своих слуг. Мы все - Блобы.

# Verbs
blob-pod-verb-zombify = Зомбировать
blob-verb-upgrade-to-strong = Улучшить до прочной плитки
blob-verb-upgrade-to-reflective = Улучшить до отражающей плитки
blob-verb-remove-blob-tile = Удалить плитку блоба

# Alerts
blob-resource-alert-name = Ресурсы ядра
blob-resource-alert-desc = Ваши ресурсы, производимые ядром и ресурсными блобами. Используйте их для расширения и создания специальных блобов.
blob-health-alert-name = Здоровье ядра
blob-health-alert-desc = Здоровье вашего ядра. Вы умрёте, если оно достигнет нуля.

# Greeting
blob-role-greeting =
    Вы блоб - паразитическое космическое существо, способное уничтожить целые станции.
        Ваша цель - выжить и вырасти как можно больше.
        Вы почти неуязвимы для физических повреждений, но тепло все ещё может навредить вам.
        Используйте Alt+ЛКМ для улучшения обычных плиток блоба до прочных, а прочных до отражающих.
        Обязательно размещайте ресурсные блобы для генерации ресурсов.
        Имейте в виду, что ресурсные блобы и фабрики будут работать только рядом с узловыми блобами или ядрами.
blob-zombie-greeting = Вы были заражены и воскрешены спорой блоба. Теперь вы должны помочь блобу захватить станцию.

# End round
blob-round-end-result =
    { $blobCount ->
        [one] Была одна инфекция блоба.
        *[other] Было {$blobCount} блобов.
    }

blob-user-was-a-blob = [color=gray]{$user}[/color] был блобом.
blob-user-was-a-blob-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) был блобом.
blob-was-a-blob-named = [color=White]{$name}[/color] был блобом.

blob-objective-percentage = Он захватил [color=White]{ $progress }%[/color] для победы.
blob-end-victory = [color=Red]Блоб(ы) успешно поглотил(и) станцию![/color]
blob-end-fail = [color=Green]Блоб(ы) не смог(ли) поглотить станцию.[/color]
blob-end-fail-progress = Все блобы захватили [color=Yellow]{ $progress }%[/color] для победы.

preset-blob-objective-issuer-blob = [color=#33cc00]Блоб[/color]

blob-user-was-a-blob-with-objectives = [color=gray]{$user}[/color] был блобом:
blob-user-was-a-blob-with-objectives-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) был блобом:
blob-was-a-blob-with-objectives-named = [color=White]{$name}[/color] был блобом:

# Objectivies
objective-condition-blob-capture-title = Захватить станцию
objective-condition-blob-capture-description = Ваша единственная цель - захватить всю станцию. Вам нужно иметь как минимум {$count} плиток блоба.
objective-condition-success = { $condition } | [color={ $markupColor }]Успех![/color]
objective-condition-fail = { $condition } | [color={ $markupColor }]Провал![/color] ({ $progress }%)

# Radio names
speak-vv-blob = Блоб
speak-vv-blob-core = Ядро блоба

# Language
language-Blob-name = Блоб
chat-language-Blob-name = Блоб
language-Blob-description = Блиб боб! Блоб блоб!

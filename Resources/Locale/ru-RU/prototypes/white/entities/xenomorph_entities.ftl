ent-AlienEggGrowing = инопланетное яйцо
    .desc = Большое инопланетное яйцо, наверное будет хорошей идеей обнять его.
    .suffix = Растёт
ent-AlienEgg = { ent-AlienEggGrowing }
    .desc = { ent-AlienEggGrowing.desc }
ent-AlienEggHatching = { ent-AlienEggGrowing }
    .desc = { ent-AlienEggGrowing.desc }
    .suffix = Вылупляется
ent-AlienEggOpened = { ent-AlienEggGrowing }
    .desc = { ent-AlienEggGrowing.desc }
    .suffix = Открытое, Лицехват
ent-AlienEggOpenedDecoration = { ent-AlienEggGrowing }
    .desc = { ent-AlienEggGrowing.desc }
    .suffix = Открытое, Декорация

ent-AlienEggSpawner = спавнер яйца ксеноморфа
ent-AlienEggSpawnerRandom = { ent-AlienEggSpawner }

ent-MobAlienLarva = грудолом
    .desc = Маленький и безобидный инопланетянин.
    .suffix = Первая стадия
ent-MobAlienLarvaInside = { ent-MobAlienLarva }
    .desc = { ent-MobAlienLarva.desc }
    .suffix = Внутри носителя
ent-MobAlienLarvaGrowStageTwo = { ent-MobAlienLarva }
    .desc = { ent-MobAlienLarva.desc }
    .suffix = Вторая стадия
ent-MobAlienLarvaGrowStageThree = { ent-MobAlienLarva }
    .desc = { ent-MobAlienLarva.desc }
    .suffix = Третья стадия
ent-SpawnPointGhostAlienLarva = спавнер роли призрак
    .desc = { ent-MarkerBase.desc }
    .suffix = Грудолом

ent-XenomorphEmbryon = эмбрион
    .desc = Мёртвая инопланетная штучка.
ent-XenomorphEmbryonDark = { ent-XenomorphEmbryon }
    .desc = { ent-XenomorphEmbryon.desc }

ent-Facehugger = лицехват
    .desc = На конце хвоста есть что-то вроде трубки.
ent-FacehuggerInactive = { ent-Facehugger }
    .desc = { ent-Facehugger.desc }
    .suffix = Мёртвый
ent-FacehuggerLamarr = Ламар
    .desc = На конце хвоста есть что-то вроде трубки, но не похоже, что он может им пользоваться.
    .suffix = Лицехват

ent-GlassBoxLamarrFilled = стеклянный короб
    .desc = Прочная витрина для дорогостоящего экспоната.
    .suffix = Ламар, Заполненный

ent-MobXenomorphDrone = ксеноморф дрон
    .desc = Существо с черной гладкой оболочкой, длинными конечностями, острыми зубами и хищным, вытянутым черепом. У него отсутствуют глаза, а на месте рта ярко выраженные челюсти.
ent-MobXenomorphSentinel = ксеноморф плевальщик
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobXenomorphHunter = ксеноморф охотник
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobXenomorphPraetorian = ксеноморф преторианец
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobXenomorphQueen = ксеноморф королева
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobXenomorphMaid = ксеноморф горничная
    .desc = { ent-MobXenomorphDrone.desc }

ent-MobXenomorphDroneDummy = { ent-MobXenomorphDrone }
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobXenomorphSentinelDummy = { ent-MobXenomorphSentinel }
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobXenomorphHunterDummy = { ent-MobXenomorphHunter }
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobXenomorphPraetorianDummy = { ent-MobXenomorphPraetorian }
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobXenomorphQueenDummy = { ent-MobXenomorphQueen }
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobXenomorphMaidDummy = { ent-MobXenomorphMaid }
    .desc = { ent-MobXenomorphDrone.desc }
ent-MobAlienLarvaGrowStageThreeDummy = { ent-MobAlienLarvaGrowStageThree }
    .desc = { ent-MobAlienLarvaGrowStageThree.desc }

ent-SpawnMobXenomorphDrone = спавнер ксеноморф дрон
    .desc = { ent-MarkerBase.desc }
ent-SpawnMobXenomorphSentinel = спавнер ксеноморф плевальщик
    .desc = { ent-MarkerBase.desc }
ent-SpawnMobXenomorphHunter = спавнер ксеноморф охотник
    .desc = { ent-MarkerBase.desc }
ent-SpawnMobXenomorphPraetorian = спавнер ксеноморф преторианец
    .desc = { ent-MarkerBase.desc }
ent-SpawnMobXenomorphQueen = спавнер ксеноморф королева
    .desc = { ent-MarkerBase.desc }
ent-SpawnMobXenomorphMaid = спавнер ксеноморф горничная
    .desc = { ent-MarkerBase.desc }
ent-SpawnMobAlienLarva = спавнер грудолом
    .desc = { ent-MarkerBase.desc }
ent-SpawnMobFacehugger = спавнер лицехват
    .desc = { ent-MarkerBase.desc }
ent-SpawnMobLamarr = спавнер Ламар
    .desc = { ent-MarkerBase.desc }

station-event-xenomorph-infestation-announcement = Обнаружены неопознанные признаки жизни на борту станции. Обеспечьте безопасность внешних доступов, включая скубберы и вентиляцию.

hud-chatbox-select-channel-XenoHivemind = Разум роя

ghost-role-mob-alien-name = Ксеноморф
ghost-role-mob-alien-description = Станьте ксеноморфом, развивайтесь и сделайте свой улей великим.
ghost-role-information-alien-larva-name = Грудолом
ghost-role-information-alien-larva-description = Станьте безобидной личинкой, развивайтесь и сделайте свой улей великим.
ghost-role-information-alien-larva-inside-name = Грудолом (Вырывающаяся)
ghost-role-information-alien-larva-inside-description = Станьте инопланетной личинкой, которая вот-вот вырвется наружу из своего носителя.

alien-null-greeting = Вы - кто нахуй? Если вы видите это сообщение, значит произошла какая-то ошибка, обратитесь в канал bug-report!
alien-facehugger-greeting = Вы - лицехват!? Сядьте на лицо будущему носителю и распостраните инфекцию внутрь его организма.
alien-larva-greeting = Вы - грудолом. Помогите своему улью расширяться. Если вы первый, или последний в своём улье - найдите безопасное место, для нового улья и эволюционируйте в ДРОНА, иначе рой не сможет продолжить своё существование. Вы можете связаться со своим ульем, добавив английскую "а" в начале своего сообщения.
alien-hunter-greeting = Вы - охотник. Найдите лицехватов и бросайте их в носителей. Пожирайте людей и доставляйте их в свой улей. Вы можете связаться со своим ульем, добавив английскую "а" в начале своего сообщения.
alien-drone-greeting = Вы - дрон. Постройте свой улей, эволюционируйте в преторианца, если вы будете первым. Вы можете связаться со своим ульем, добавив английскую "а" в начале своего сообщения.
alien-sentinel-greeting = Вы - плевальщик. Защищайте свой улей, сражайтесь с незваными гостями. Вы можете связаться со своим ульем, добавив английскую "а" в начале своего сообщения.
alien-praetorian-greeting = Вы - преторианец. Защитите свою королеву. Помогите построить новые ульи. Станьте королевой, если ее еще нет. Вы можете связаться со своим ульем, добавив английскую "а" в начале своего сообщения.
alien-queen-greeting = Вы - королева ксеноморфов. Управляйте своим ульем, откладывайте яйца. ОСТАВАЙТЕСЬ В ЖИВЫХ. Вы можете связаться со своим ульем, добавив английскую "а" в начале своего сообщения.
alien-maid-greeting = Вы - ксено-горничная. Будьте похотливыми. Выполняй самую важную работу в своем улье - соблазняйте космонавтиков. Вы можете связаться со своим ульем, добавив английскую "а" в начале своего сообщения.

alerts-plasma-name = Плазма
alerts-plasma-desc = Ваше тело синтезирует плазму, которую вы можете использовать для своих способностей.

ent-CorrosiveAcid = кислота
    .desc = Яркая зелёная кислота.
ent-CorrosiveAcidOverlay = { ent-CorrosiveAcid }
    .desc = { ent-CorrosiveAcid.desc }

ent-OrganXenomorphAcidGland = кислотная гланда
    .desc = Вставляется в рот, позволяет носителю плеваться плазмой.
ent-OrganXenomorphEggsac = яйцеклад
    .desc = Вставляется в пах, позволяет носителю откладывать яйца.
ent-OrganXenomorphHivenode = разум роя
    .desc = Вставляется в голову, позволяет носителю стать единым с роем.
ent-OrganXenomorphPlasmaVesselTiny = крошечный плазменный мешочек
    .desc = Вставляется в грудь, позволяет носителю хранить и вырабатывать плазму.
ent-OrganXenomorphPlasmaVesselSmall = маленький плазменный мешочек
    .desc = { ent-OrganXenomorphPlasmaVesselTiny.desc }
ent-OrganXenomorphPlasmaVesselMedium = средний плазменный мешочек
    .desc = { ent-OrganXenomorphPlasmaVesselTiny.desc }
ent-OrganXenomorphPlasmaVesselLarge = большой плазменный мешочек
    .desc = { ent-OrganXenomorphPlasmaVesselTiny.desc }
ent-OrganXenomorphBrain = мозг пришельца
    .desc = Вы слишком недоразвиты, чтобы понять его положение.
ent-TorsoXenomorph = торс ксеноморфа
    .desc = Сигма, крипер.

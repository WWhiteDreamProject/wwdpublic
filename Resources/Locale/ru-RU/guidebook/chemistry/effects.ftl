-create-3rd-person =
    { $chance ->
        [1] Создаёт
       *[other] создание
    }
-cause-3rd-person =
    { $chance ->
        [1] Вызывает
       *[other] вызов
    }
-satiate-3rd-person =
    { $chance ->
        [1] Насыщает
       *[other] насыщение
    }
reagent-effect-guidebook-create-entity-reaction-effect =
    { -create-3rd-person(chance: $chance) } { $amount ->
        [1] {""}
       *[other] { $amount }
    } энтити «{ INDEFINITE($entname) }»
reagent-effect-guidebook-explosion-reaction-effect = { -cause-3rd-person(chance: $chance) } [color=#E8A0A0]взрыв[/color]
reagent-effect-guidebook-emp-reaction-effect = { -cause-3rd-person(chance: $chance) } [color=#F0E070]электромагнитный импульс[/color]
reagent-effect-guidebook-foam-area-reaction-effect = { -create-3rd-person(chance: $chance) } большое количество [color=#A8C0D8]пены[/color]
reagent-effect-guidebook-smoke-area-reaction-effect = { -create-3rd-person(chance: $chance) } большое количество [color=#A0B8C8]дыма[/color]
reagent-effect-guidebook-satiate-thirst =
    { -satiate-3rd-person(chance: $chance) } { $relative ->
        [1] жажду [color=#90D8D0]средне[/color]
       *[other] жажду { $relative ->
            [few] [color=#90D8D0]в { NATURALFIXED($relative, 3) } раза[/color]
           *[other] [color=#90D8D0]в { NATURALFIXED($relative, 3) } раз[/color]
        } от среднего значения
    }
reagent-effect-guidebook-satiate-hunger =
    { -satiate-3rd-person(chance: $chance) } { $relative ->
        [1] [color=#90D890]голод[/color] средне
       *[other] [color=#90D890]голод[/color] на { NATURALFIXED($relative, 3) }x от среднего
    }
reagent-effect-guidebook-health-change =
    { $chance ->
        [1]
            { $healsordeals ->
                [heals] Лечит
                [deals] Наносит
               *[both] Изменяет здоровье на
            }
       *[other]
            { $healsordeals ->
                [heals] лечение
                [deals] нанесение
               *[both] изменение здоровья на
            }
    } { $changes }
reagent-effect-guidebook-status-effect =
    { $type ->
        [add]
        { -cause-3rd-person(chance: $chance) } [color=#CCA0CC]{ LOC($key) }[/color] как минимум на { NATURALFIXED($time, 3) } { $time ->
            [one] секунду
            [few] секунды
           *[other] секунд
        } с накоплением
       *[set] { -cause-3rd-person(chance: $chance) } [color=#CCA0CC]{ LOC($key) }[/color] как минимум на { NATURALFIXED($time, 3) } { $time ->
            [one] секунду
            [few] секунды
           *[other] секунд
        } без накопления
        [remove]
            { $chance ->
                [1]
                    Сокращает [color=#CCA0CC]{ LOC($key) }[/color] на { NATURALFIXED($time, 3) } { $time ->
                        [one] секунду
                        [few] секунды
                       *[other] секунд
                    }
               *[other]
                    сокращение эффекта [color=#CCA0CC]«{ LOC($key) }»[/color] на { NATURALFIXED($time, 3) } { $time ->
                    [one] секунды
                   *[other] секунд
                    }
            }


    }
reagent-effect-guidebook-activate-artifact =
    { $chance ->
        [1] Пытается
       *[other] попытку
    } активировать [color=#D0B890]артефакт[/color]
reagent-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Устанавливает [color=#E8C080]температуру раствора[/color]
       *[other] установление [color=#E8C080]температуры раствора[/color]
    } ровно { NATURALFIXED($temperature, 2) }К
reagent-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Нагревает
               *[-1] Охлаждает
            } [color=#E8C080]раствор[/color]
       *[other]
            { $deltasign ->
                [1] нагрев
               *[-1] охлаждение
            } [color=#E8C080]раствора[/color]
    } до тех пор, пока температура не станет { $deltasign ->
        [1] меньше { NATURALFIXED($maxtemp, 2) }К
       *[-1] больше { NATURALFIXED($mintemp, 2) }К
    }
reagent-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавление
               *[-1] удаление
            }
    } { NATURALFIXED($amount, 2) }ед. реагента [color=#80C8C8]«{ $reagent }»[/color] { $deltasign ->
        [1] в раствор
       *[-1] из раствора
    }
reagent-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавление
               *[-1] удаление
            }
    } { NATURALFIXED($amount, 2) }ед. реагентов группы [color=#80C8C8]{ $group }[/color] { $deltasign ->
        [1] в раствор
       *[-1] из раствора
    }
reagent-effect-guidebook-adjust-temperature =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Убирает
            }
       *[other]
            { $deltasign ->
                [1] добавление
               *[-1] извлечение
            }
    } { POWERJOULES($amount) } [color=#E8C080]тепла[/color] { $deltasign ->
        [1] телу
       *[-1] из тела
    }
reagent-effect-guidebook-chem-cause-disease =
    { -cause-3rd-person(chance: $chance) } { $chance ->
        [1] [color=#C8C890]болезнь[/color]
       *[other] [color=#C8C890]болезни[/color]
    } { $diseases }
reagent-effect-guidebook-chem-cause-random-disease =
    { -cause-3rd-person(chance: $chance) } { $chance ->
        [1] [color=#C8C890]болезнь[/color]
       *[other] [color=#C8C890]болезни[/color]
    } { $diseases }
reagent-effect-guidebook-jittering =
    { -cause-3rd-person(chance: $chance) } { $chance ->
        [1] [color=#D8C890]дрожь[/color]
       *[other] [color=#D8C890]дрожи[/color]
    }
reagent-effect-guidebook-chem-clean-bloodstream =
    { $chance ->
        [1] Очищает [color=#80C8C8]кровоток[/color]
       *[other] очищение [color=#80C8C8]кровотока[/color]
    } от других химикатов
reagent-effect-guidebook-cure-disease =
    { $chance ->
        [1] Исцеляет [color=#90D8A0]болезнь[/color]
       *[other] исцеление от [color=#90D8A0]болезни[/color]
    }
reagent-effect-guidebook-cure-eye-damage =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Лечит
               *[-1] Наносит
            } [color=#90C8E0]повреждения глаз[/color]
       *[other]
            { $deltasign ->
                [1] исцеление
               *[-1] нанесение
            } [color=#90C8E0]повреждений глаз[/color]
    }
reagent-effect-guidebook-chem-vomit =
    { $chance ->
        [1] Вызывает [color=#D0C890]рвоту[/color]
       *[other] вызов [color=#D0C890]рвоты[/color]
    }
reagent-effect-guidebook-create-gas =
    { -create-3rd-person(chance: $chance) } { $moles } { $chance ->
        [1]
        { $moles ->
            [one] моль
            [few] моли
           *[other] молей
        }
       *[other]
        { $moles ->
            [one] моли
           *[other] молей
        }
    } газа [color=#B0C8A0]«{ $gas }»[/color]
reagent-effect-guidebook-drunk =
    { -cause-3rd-person(chance: $chance) } { $chance ->
        [1] [color=#D0C0A0]опьянение[/color]
       *[other] [color=#D0C0A0]опьянения[/color]
    }
reagent-effect-guidebook-electrocute =
    { $chance ->
        [1] Обездвиживает
       *[other] обездвиживание
    } [color=#F0E070]током[/color] в течении { NATURALFIXED($time, 3) } { $time ->
        [one] секунды
       *[other] секунд
    }
reagent-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Тушит [color=#80C0E8]огонь[/color]
       *[other] тушение [color=#80C0E8]огня[/color]
    }
reagent-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Увеличивает [color=#E8B070]воспламеняемость[/color]
       *[other] увеличение [color=#E8B070]воспламеняемости[/color]
    }
reagent-effect-guidebook-ignite =
    { $chance ->
        [1] Поджигает
       *[other] поджог
    } [color=#E8A870]употребившего[/color]
reagent-effect-guidebook-make-sentient =
    { $chance ->
        [1] Вызывает признаки
       *[other] вызов признаков
    } [color=#CCA0CC]разумности[/color] у употребившего
reagent-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Превращает
       *[other] превращение
    } употребившего в [color=#B8A8D0]«{ $entityname }»[/color]
reagent-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Вызывает
               *[-1] Уменьшает
            } [color=#D0A0A0]кровотечение[/color]
       *[other]
            { $deltasign ->
                [1] вызов
               *[-1] уменьшение
            } [color=#D0A0A0]кровотечения[/color]
    }
reagent-effect-guidebook-modify-blood-level =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Увеличивает
               *[-1] Уменьшает
            } [color=#D0A0A0]уровень крови[/color]
       *[other]
            { $deltasign ->
                [1] увеличение
               *[-1] уменьшение
            } [color=#D0A0A0]уровня крови[/color]
    }
reagent-effect-guidebook-paralyze =
    { $chance ->
        [1] Парализует
       *[other] парализует
    } [color=#C8A0B8]употребившего[/color] как минимум на { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
       *[other] секунд
    }
reagent-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] Умножает [color=#A0C8B8]скорость передвижения[/color]
       *[other] умножение [color=#A0C8B8]скорости передвижения[/color]
    } в { NATURALFIXED($walkspeed, 3) } { $walkspeed ->
        [few] раза
       *[other] раз
    } как минимум на { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
       *[other] секунд
    }
reagent-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Временно сдерживает [color=#B8B0D0]нарколепсию[/color]
       *[other] временное сдерживание [color=#B8B0D0]нарколепсии[/color]
    }
reagent-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Смывает [color=#D8D0B0]кремовый пирог[/color]
       *[other] смытие [color=#D8D0B0]кремового пирога[/color]
    } с лица
reagent-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Исцеляет
       *[other] исцеление
    } употребившего от [color=#90C890]зомби-инфекции[/color]
reagent-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Заражает
       *[other] заражение
    } употребившего [color=#90C890]зомби-инфекцией[/color]
reagent-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Исцеляет
       *[other] исцеление
    } употребившего от [color=#90C890]зомби-инфекции[/color] и даёт иммунитет к будущим заражениям

reagent-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Исцеляет
        *[other] исцеление
    } {NATURALFIXED($time, 3)} { $time ->
        [one] секунду
        [few] секунды
       *[other] секунд
    } [color=#A0C8A0]гниения[/color]

reagent-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Изменяет [color=#A0D0A0]{$attribute}[/color]
       *[other] изменение аттрибута [color=#A0D0A0]«{$attribute}»[/color]
    } на [color={$colorName}]{$amount}[/color]

reagent-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Омолаживает [color=#A4D0A0]растение[/color]
       *[other] омолаживание [color=#A4D0A0]растения[/color]
    } в зависимости от его возраста и времени роста

reagent-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Изменяет нежизнеспособное
       *[other] изменение нежизнеспособного
    } из-за мутации [color=#A8D0A0]растение[/color] на жизнеспособное

reagent-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Увеличивает [color=#A0D0A4]продолжительность жизни[/color]
       *[other] увеличение [color=#A0D0A4]продолжительности жизни[/color]
    } и/или [color=#A0D0A8]базовое здоровье[/color] растения с вероятностью в 10% для каждой.

reagent-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Увеличивает [color=#A0D4A0]потенцию растения[/color]
       *[other] увеличение [color=#A0D4A0]потенции растения[/color]
    } на {$increase} вплоть до максимума в {$limit}. Приводит к потере растением семян, когда потенция достигает {$seedlesstreshold}. Попытка добавить потенцию свыше {$limit} может привести к снижению урожайности с вероятностью в 10%.

reagent-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Восстанавливает [color=#A0D8A0]семена[/color]
       *[other] восстанавление [color=#A0D8A0]семян[/color]
    } растения.

reagent-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Удаляет [color=#A0D8A0]семена[/color] растения.
       *[other] удаление [color=#A0D8A0]семян[/color] растений.
    }

reagent-effect-guidebook-missing =
    { $chance ->
        [1] Вызывает{" "}
       *[other] {""}
    }[color=#C0C0C0]неизвестный эффект[/color], так как никто ещё не описал этот эффект

reagent-effect-guidebook-change-glimmer-reaction-effect =
    { $chance ->
        [1] Изменяет уровень
       *[other] изменение уровня
    } [color=#C0B0E0]мерцания[/color] на {$count} единиц

reagent-effect-guidebook-chem-remove-psionic =
    { $chance ->
        [1] Убирает [color=#B8A0D8]псионические силы[/color]
       *[other] потерю [color=#B8A0D8]псионических сил[/color]
    }

reagent-effect-guidebook-chem-reroll-psionic =
    { $chance ->
        [1] Даёт шанс получить одну [color=#B8A4D8]псионическую силу[/color]
       *[other] получение одной [color=#B8A4D8]псионической силы[/color]
    }

reagent-effect-guidebook-chem-restorereroll-psionic =
    { $chance ->
        [1] Восстанавливает способность
       *[other] восстанавление способности
    } получать пользу от [color=#B8A8D8]раскрывающих разум[/color] реагентов

reagent-effect-guidebook-add-moodlet =
    Изменяет [color=#D0A8C0]настроение[/color] на {$amount} в течении { $timeout ->
        [0] неограниченного времени
       *[other] {$timeout} { $timeout ->
            [one] секунды
           *[other] секунд
        }
    }

reagent-effect-guidebook-remove-moodlet =
    Убирает модификатор [color=#D0A8C0]настроения[/color] «{$name}».

reagent-effect-guidebook-purge-moodlets = Убирает все активные непостоянные модификаторы [color=#D0A8C0]настроения[/color].

reagent-effect-guidebook-purify-evil = Очищает от [color=#FFFFFF]тёмных сил[/color]

reagent-effect-guidebook-stamina-change =
    { $chance ->
        [1] { $deltasign ->
                [-1] Восстанавливает
               *[1] Истощает
            }
       *[other] { $deltasign ->
                    [-1] восстанавление
                   *[1] истощение
                 }
    } {$amount} [color=#C8C0A0]выносливости[/color]

# Shadowling

reagent-effect-guidebook-blind-non-sling =
    { $chance ->
        [1] Ослепляет
       *[other] ослепление
    } [color=#8090A8]не-тенелингов[/color]

reagent-effect-guidebook-heal-sling =
    { $chance ->
        [1] Лечит
       *[other] лечение
    } [color=#8090A8]тенелингов и их рабов[/color]

reagent-effect-guidebook-add-to-chemicals =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавление
               *[-1] удаление
            }
    } {NATURALFIXED($amount, 2)}ед. реагента [color=#80C8C8]«{ $reagent }»[/color] { $deltasign ->
        [1] в раствор
       *[-1] из раствора
    }

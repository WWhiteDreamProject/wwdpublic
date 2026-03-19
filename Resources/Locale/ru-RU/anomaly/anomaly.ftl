anomaly-component-contact-damage = Аномалия сдирает с вас кожу!

anomaly-vessel-component-anomaly-assigned = Аномалия присвоена сосуду.
anomaly-vessel-component-not-assigned = Этому сосуду не присвоена ни одна аномалия. Попробуйте использовать на нём сканер.
anomaly-vessel-component-assigned = Этому сосуду уже присвоена аномалия.

# WWDP EDIT START
anomaly-vessel-component-upgrade-output = точечный выход
# WWDP EDIT END

anomaly-particles-delta = Дельта-частицы
anomaly-particles-epsilon = Эпсилон-частицы
anomaly-particles-zeta = Зета-частицы
anomaly-particles-omega = Омега-частицы
# WWDP EDIT START
anomaly-particles-sigma = Сигма-частицы
# WWDP EDIT END

anomaly-scanner-component-scan-complete = Сканирование завершено!

# WWDP EDIT START
anomaly-scanner-scan-copied = Данные сканирования аномалий скопированы!
# WWDP EDIT END

anomaly-scanner-ui-title = сканер аномалий
anomaly-scanner-no-anomaly = Нет просканированной аномалии.
anomaly-scanner-severity-percentage = Текущая опасность: [color=gray]{ $percent }[/color]
# WWDP EDIT START
anomaly-scanner-severity-percentage-unknown = Текущая опасность: [color=red]ОШИБКА[/color]
# WWDP EDIT END
anomaly-scanner-stability-low = Текущее состояние аномалии: [color=gold]Распад[/color]
anomaly-scanner-stability-medium = Текущее состояние аномалии: [color=forestgreen]Стабильное[/color]
anomaly-scanner-stability-high = Текущее состояние аномалии: [color=crimson]Рост[/color]
# WWDP EDIT START
anomaly-scanner-stability-unknown = Текущее состояние аномалии: [color=red]ОШИБКА[/color]
# WWDP EDIT END
anomaly-scanner-point-output = Пассивная генерация очков: [color=gray]{ $point }[/color]
# WWDP EDIT START
anomaly-scanner-point-output-unknown = Пассивная генерация очков: [color=red]ОШИБКА[/color]
# WWDP EDIT END
anomaly-scanner-particle-readout = Анализ реакции на частицы:
anomaly-scanner-particle-danger = - [color=crimson]Опасный тип:[/color] { $type }
anomaly-scanner-particle-unstable = - [color=plum]Нестабильный тип:[/color] { $type }
anomaly-scanner-particle-containment = - [color=goldenrod]Сдерживающий тип:[/color] { $type }
# WWDP EDIT START
anomaly-scanner-particle-transformation = - [color=#6b75fa]Трансформирующий тип:[/color] { $type }
anomaly-scanner-particle-danger-unknown = - [color=crimson]Опасный тип:[/color] [color=red]ОШИБКА[/color]
anomaly-scanner-particle-unstable-unknown = - [color=plum]Нестабильный тип:[/color] [color=red]ОШИБКА[/color]
anomaly-scanner-particle-containment-unknown = - [color=goldenrod]Сдерживающий тип:[/color] [color=red]ОШИБКА[/color]
anomaly-scanner-particle-transformation-unknown = - [color=#6b75fa]Трансформирующий тип:[/color] [color=red]ОШИБКА[/color]
# WWDP EDIT END
anomaly-scanner-pulse-timer = Время до следующего импульса: [color=gray]{ $time }[/color]

anomaly-gorilla-core-slot-name = Ядро аномалии
anomaly-gorilla-charge-none = Внутри нет [bold]ядра аномалии[/bold].
anomaly-gorilla-charge-limit = Осталось [color={$count ->
        [3]green
        [2]yellow
        [1]orange
        [0]red
        *[other]purple
    }]{$count} {$count ->
        [one]заряд
        [few]заряда
        *[other]зарядов
    }[/color].

anomaly-gorilla-charge-infinite = В нем [color=gold]неограниченое количество зарядов[/color]. [italic]Пока что...[/italic]

anomaly-sync-connected = Аномалия успешно привязана
anomaly-sync-disconnected = Соединение с аномалией было потеряно!
anomaly-sync-no-anomaly = Отсутствует аномалия в пределах диапазона.
anomaly-sync-examine-connected = Он [color=darkgreen]присоединён[/color] к аномалии.
anomaly-sync-examine-not-connected = Он [color=darkred]не присоединён[/color] к аномалии.
anomaly-sync-connect-verb-text = Присоединить аномалию
anomaly-sync-connect-verb-message = Присоединить близлежащую аномалию к {$machine}.

anomaly-generator-ui-title = Генератор Аномалий
anomaly-generator-fuel-display = Топливо:
anomaly-generator-cooldown = Перезарядка: [color=gray]{ $time }[/color]
anomaly-generator-no-cooldown = Перезарядка: [color=gray]Завершена[/color]
anomaly-generator-yes-fire = Статус: [color=forestgreen]Готов[/color]
anomaly-generator-no-fire = Статус: [color=crimson]Не готов[/color]
anomaly-generator-generate = Создать Аномалию
anomaly-generator-charges =
    { $charges ->
        [one] { $charges } заряд
       *[other] { $charges } заряды
    }

anomaly-generator-announcement = Была сгенерирована аномалия!

anomaly-command-pulse = Пульсирует аномалию
anomaly-command-supercritical = Доводит аномалию до суперкритического состояния

# Flavor text on the footer
# Flavor text on the footer
anomaly-generator-flavor-left = Аномалия может возникнуть внутри пользователя.
anomaly-generator-flavor-right = v1.1
# WWDP EDIT START
anomaly-behavior-unknown = [color=red]ОШИБКА. Не может быть прочтено.[/color]

anomaly-behavior-title = анализ отклонений в поведении:
anomaly-behavior-point =[color=gold]Аномалия приносит { $mod }% очков[/color]

anomaly-behavior-safe = [color=forestgreen]Аномалия чрезвычайно стабильна. Пульсации крайне редки.[/color]
anomaly-behavior-slow = [color=forestgreen]Частота пульсаций значительно снизилась.[/color]
anomaly-behavior-light = [color=forestgreen]Мощность пульсации значительно снижается.[/color]
anomaly-behavior-balanced = Отклонений в поведении не обнаружено.
anomaly-behavior-delayed-force = Частота пульсаций значительно снижается, но их мощность увеличивается.
anomaly-behavior-rapid = Частота пульсации намного выше, но ее сила ослаблена.
anomaly-behavior-reflect = Было обнаружено защитное покрытие.
anomaly-behavior-nonsensivity = Была обнаружена слабая реакция на частицы.
anomaly-behavior-sensivity = Была обнаружена усиленная реакция на частицы.
anomaly-behavior-secret = Обнаружены помехи. Некоторые данные не могут быть считаны
anomaly-behavior-inconstancy = [color=crimson]Обнаружено непостоянство. Типы частиц могут меняться со временем.[/color]
anomaly-behavior-fast = [color=crimson]Частота пульсаций сильно увеличивается.[/color]
anomaly-behavior-strenght = [color=crimson]Мощность пульсации значительно увеличивается.[/color]
anomaly-behavior-moving = [color=crimson]Была обнаружена нестабильность координат.[/color]
# WWDP EDIT END

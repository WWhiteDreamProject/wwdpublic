event-item-dispenser-out-of-stock = Предметы кончились!
event-item-dispenser-limit-reached = Лимит достигнут!

event-item-dispenser-examine-infinite = Внутри {$remaining ->
    [1] остался
    *[other] осталось
} [color=yellow]{$remaining}[/color] {$plural} (из [color=yellow]{$limit}[/color]).

event-item-dispenser-examine-infinite-autodispose = Я могу взять ещё [color=yellow]{$remaining}[/color] {$plural} (из [color=yellow]{$limit}[/color]), прежде чем самый старый предмет пропадёт.
event-item-dispenser-examine-infinite-autodispose-manualdispose = Я могу взять ещё [color=yellow]{$remaining}[/color] {$plural} (из [color=yellow]{$limit}[/color]), прежде чем самый старый предмет пропадёт. Если что, я могу их вернуть, чтобы получить новые.
event-item-dispenser-examine-infinite-manualdispose= Я могу взять ещё [color=yellow]{$remaining}[/color] {$plural} (из [color=yellow]{$limit}[/color]), прежде чем мне придётся их вернуть, чтобы взять новые.

event-item-dispenser-examine-infinite-single = {$remaining ->
    [1] Внутри только [color=yellow]один[/color] предмет.
    *[other] Внутри [color=yellow]пусто[/color].
}

event-item-dispenser-examine-infinite-autodispose-single = {$remaining ->
    [1] Я могу здесь взять только [color=yellow]один[/color] предмет. Если я возьму второй, первый пропадёт.
    *[other] Если я возьму здесь [color=yellow]новый[/color] предмет, старый пропадёт.
}

event-item-dispenser-examine-infinite-autodispose-manualdispose-single = {$remaining ->
    [1] Я могу здесь взять только [color=yellow]один[/color] предмет. Если что, я смогу его вернуть, чтобы получить новый.
    *[other] Если я возьму здесь [color=yellow]новый[/color] предмет, старый пропадёт. А ещё я могу вернуть здесь старый предмет, чтобы получить новый.
}

event-item-dispenser-examine-infinite-manualdispose-single = {$remaining ->
    [1] Я могу здесь взять только [color=yellow]один[/color] предмет. Если что, я смогу его вернуть.
    *[other] Внутри [color=yellow]пусто[/color], но я могу вернуть здесь старый предмет, чтобы получить новый.
}



event-item-dispenser-examine-finite = Внутри {$remaining ->
    [1] остался
    *[other] осталось
} [color=yellow]{$remaining}[/color] {$plural} (из [color=yellow]{$limit}[/color]).

event-item-dispenser-examine-finite-manualdispose = Внутри {$remaining ->
    [1] остался
    *[other] осталось
} [color=yellow]{$remaining}[/color] {$plural} (из [color=yellow]{$limit}[/color]). Если что, я могу их вернуть, чтобы получить новые.

event-item-dispenser-examine-finite-single = {$remaining ->
    [1] Внутри только [color=yellow]один[/color] предмет.
    *[other] Внутри [color=yellow]пусто[/color].
}

event-item-dispenser-examine-finite-manualdispose-single = {$remaining ->
    [1] Внутри только [color=yellow]один[/color] предмет. редмет. Если что, я смогу его вернуть, чтобы получить новый..
    *[other] Внутри [color=yellow]пусто[/color], но я могу вернуть здесь старый предмет, чтобы получить новый.
}



eventitemdispenser-configwindow-title = Erectin' a dispenser
eventitemdispenser-configwindow-dispensingprototype = Прототип предмета
eventitemdispenser-configwindow-autodispose = Автоматически удалять излишек?
eventitemdispenser-configwindow-canmanuallydispose = Можно ли вручную удалить предмет?
eventitemdispenser-configwindow-infinite = Бесконечно восполняемый запас
eventitemdispenser-configwindow-limit = Лимит предметов
eventitemdispenser-configwindow-replacedisposeditems = Заменять автоматически удалённые предметы?
eventitemdispenser-configwindow-disposedreplacement = Прототип замены
eventitemdispenser-configwindow-autocleanup = Автоматическая чистка при удалении раздатчика
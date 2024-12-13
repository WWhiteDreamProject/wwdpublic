-event-item-dispenser-out-of-stock = Предметы кончились!
-event-item-dispenser-limit-reached = Лимит достигнут!
-event-item-dispenser-unlimited = [color=yellow]сколько угодно[/color] предметов
-event-item-dispenser-infinite-count = ещё [color=yellow]{$r}[/color] {$p} (из [color=yellow]{$l}[/color])


event-item-dispenser-examine-infinite = Внутри {$remaining ->
    [true] остался
    *[other] осталось
} [color=yellow]{$remaining}[/color] {$plural} (из [color=yellow]{$limit}[/color]).

event-item-dispenser-examine-infinite-autodispose = Я могу взять {$noLimit -> 
    [true]   { -event-item-dispenser-unlimited }!
    *[other] { -event-item-dispenser-infinite-count(r: $remaining, p: $plural, l:$limit) }, прежде чем самый старый предмет пропадёт.
}

event-item-dispenser-examine-infinite-autodispose-manualdispose = Я могу взять {$noLimit -> 
    [true]   { -event-item-dispenser-unlimited }! Если что, я могу вернуть их здесь.
    *[other] { -event-item-dispenser-infinite-count(r: $remaining, p: $plural, l:$limit) }, прежде чем самый старый предмет пропадёт. Если что, я могу вернуть их здесь, чтобы получить новые.
}

event-item-dispenser-examine-infinite-manualdispose= Я могу взять {$noLimit ->
    [true]   { -event-item-dispenser-unlimited }! Если что, я могу вернуть их здесь.
    *[other] { -event-item-dispenser-infinite-count(r: $remaining, p: $plural, l:$limit) }, прежде чем мне придётся их вернуть, чтобы взять новые.
}

event-item-dispenser-examine-infinite-single = {$remaining ->
    [1] Внутри только [color=yellow]один[/color] предмет.
    *[0] Внутри [color=yellow]пусто[/color].
}

event-item-dispenser-examine-infinite-autodispose-single = {$remaining ->
    [1] Я могу здесь взять только [color=yellow]один[/color] предмет. Если я возьму второй, первый пропадёт.
    *[0] Если я возьму здесь [color=yellow]новый[/color] предмет, старый пропадёт.
}

event-item-dispenser-examine-infinite-autodispose-manualdispose-single = {$remaining ->
    [1] Я могу здесь взять только [color=yellow]один[/color] предмет. Если что, я смогу его вернуть, чтобы получить новый.
    *[0] Если я возьму здесь [color=yellow]новый[/color] предмет, старый пропадёт. А ещё я могу вернуть здесь старый предмет, чтобы получить новый.
}

event-item-dispenser-examine-infinite-manualdispose-single = {$remaining ->
    [1] Я могу здесь взять только [color=yellow]один[/color] предмет. Если что, я смогу его вернуть.
    *[0] Внутри [color=yellow]пусто[/color], но я могу вернуть здесь старый предмет, чтобы получить новый.
}

-event-item-dispenser-finite-count = [color=yellow]{$r}[/color] {$p} (из [color=yellow]{$l}[/color])
-evd-remainplural = {$remaining ->
    [1] остался
    *[other] осталось
}

event-item-dispenser-examine-finite = Внутри {-evd-remainplural} {$noLimit ->
    [true]   { -event-item-dispenser-unlimited }!
    *[other] { -event-item-dispenser-finite-count(r:$remaining, p:$plural, l:$limit) }.
}

event-item-dispenser-examine-finite-manualdispose = Внутри {-evd-remainplural} {$noLimit ->
    [true]   { -event-item-dispenser-unlimited }! Если что, я могу их вернуть.
    *[other] { -event-item-dispenser-finite-count(r:$remaining, p:$plural, l:$limit) } Если что, я могу их вернуть, чтобы получить новые.
}

event-item-dispenser-examine-finite-single = {$remaining ->
    [1] Внутри только [color=yellow]один[/color] предмет.
    *[other] Внутри [color=yellow]пусто[/color].
}

event-item-dispenser-examine-finite-manualdispose-single = {$remaining ->
    [1] Внутри только [color=yellow]один[/color] предмет. Если что, я смогу его вернуть, чтобы получить новый.
    *[other] Внутри [color=yellow]пусто[/color], но я могу вернуть здесь старый предмет, чтобы получить новый.
}



eventitemdispenser-configwindow-dispensingprototype-tooltip = Прототип предмета, который будет выдаваться игроку при клике. Когда вы в aghost: кликните предметом по раздатчику, чтобы скопировать его прототип сюда.
eventitemdispenser-configwindow-autodispose-tooltip = При попытке взять предмет сверх установленного лимита, удаляет самый старый предмет. Не имеет эффекта на раздатчиках с конечным запасом!
eventitemdispenser-configwindow-infinite-tooltip = Раздатчик с конечным запасом отслеживает, сколько предметов взял игрок и не даёт взять сверх лимита. Бесконечный раздатчик в своём подсчёте не учитывает удалённые предметы.
eventitemdispenser-configwindow-limit-tooltip = Максимальное количество предметов, которое может взять каждый игрок из этого раздатчика. Ноль отключает лимит, будьте осторожны!
eventitemdispenser-configwindow-canmanuallydispose-tooltip = Если да, то при клике на раздатчик выданным предметом удаляет его, "возвращая" в раздатчик. Свойства предмета (напр. батарейка егана) не переносятся на следующий выдаваемый предмет.
eventitemdispenser-configwindow-replacedisposeditems-tooltip = При автоматическом удалении лишних предметов заменять их на данный прототип. Не имеет эффекта на раздатчиках без автоудаления излишка!
eventitemdispenser-configwindow-disposedreplacement-tooltip = Прототип предмета, на который будет заменён удаляемый лишний предмет. Рекомендуется указать либо прототип какого-нибудь мусора, либо прототип эффекта. [color=yellow]Не имеет эффекта на раздатчиках без автоудаления излишка![/color]
eventitemdispenser-configwindow-autocleanup-tooltip = Если да, то все предметы, выданные этим раздатчиком, будут удалены вместе с этим раздатчиком. (На самом деле вместе с компонентом EventItemDispenser раздатчика)

eventitemdispenser-configwindow-title = Erectin' a dispenser
eventitemdispenser-configwindow-dispensingprototype = Прототип предмета
eventitemdispenser-configwindow-autodispose = Автоматически удалять излишек?
eventitemdispenser-configwindow-canmanuallydispose = Можно ли вручную удалить предмет?
eventitemdispenser-configwindow-infinite = Бесконечно восполняемый запас
eventitemdispenser-configwindow-limit = Лимит предметов
eventitemdispenser-configwindow-replacedisposeditems = Заменять автоматически удалённые предметы?
eventitemdispenser-configwindow-disposedreplacement = Прототип замены
eventitemdispenser-configwindow-autocleanup = Автоматическая чистка при удалении раздатчика

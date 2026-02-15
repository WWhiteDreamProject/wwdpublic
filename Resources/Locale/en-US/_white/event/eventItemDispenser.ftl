# credit where it's due, it would be much more painful if not for coderabbit generating
# a somewhat passable translation. I just have to edit it so it makes sense.

# I also did not look into how to use fluent for a proper english localization,
# so it will most likely end up looking weird. Oh well.

event-item-dispenser-out-of-stock = Items are out of stock!
event-item-dispenser-limit-reached = Limit reached!
-event-item-dispenser-unlimited = [color=yellow]as many as i want[/color] here

# For some reason, after the {-item-plural()} Fluent fails to insert {$l}, despite it being passed,
# but successfully inserts {$limit}, even though it is "global" and should fail the same way it fails to insert {$remaining}.
-event-item-dispenser-infinite-count = [color=yellow]{ $r }[/color] more { -item-plural(count:$r) } here (out of [color=yellow]{ $limit }[/color])
-item-plural = {$count ->
    [one] item
    *[many] items
}
-item-plural-remain = {$count ->
    [one] remains
    *[many] remain
}

-item-plural-isare = {$count ->
    [one] is
    *[many] are
}

event-item-dispenser-item-name = Dispenses [color=violet]{ $itemName }[/color].

event-item-dispenser-examine-infinite = I can take {$noLimit ->
    [true]   { -event-item-dispenser-unlimited }!
    *[other] { -event-item-dispenser-infinite-count(r: $remaining, l: $limit) }.
}

event-item-dispenser-examine-infinite-autodispose = {$noLimit ->
    [true]   I can take { -event-item-dispenser-unlimited }!
    *[other] I can take { -event-item-dispenser-infinite-count(r: $remaining, l: $limit) }, after which the oldest item will be removed.
}

event-item-dispenser-examine-infinite-autodispose-manualdispose = {$noLimit ->
    [true]   I can take { -event-item-dispenser-unlimited }! If I need to, I can return them here.
    *[other] I can take { -event-item-dispenser-infinite-count(r: $remaining, l: $limit) }, after which the oldest item will be removed. If I need to, I can return them here and get new ones.
}

event-item-dispenser-examine-infinite-manualdispose = {$noLimit ->
    [true]   I can take { -event-item-dispenser-unlimited }! If I need to, I can return them here.
    *[other] I can take { -event-item-dispenser-infinite-count(r: $remaining, l: $limit) }, after which I must return them here if I need to get new ones.
}

event-item-dispenser-examine-infinite-single = {$remaining ->
    [1] There's only [color=yellow]one[/color] item inside.
    *[0] It's [color=yellow]empty[/color].
}

event-item-dispenser-examine-infinite-autodispose-single = {$remaining ->
    [1] I can only take [color=yellow]one[/color] item here. If I take a second one, the first one will disappear.
    *[0] If I take a [color=yellow]new[/color] item here, the old one will disappear.
}

event-item-dispenser-examine-infinite-autodispose-manualdispose-single = {$remaining ->
    [1] I can only take [color=yellow]one[/color] item here. If I need to, I can return it and get a new one.
    *[0] If I take a [color=yellow]new[/color] item here, the old one will disappear. I can also return the old item here.
}

event-item-dispenser-examine-infinite-manualdispose-single = {$remaining ->
    [1] I can only take [color=yellow]one[/color] item here. If I need to, I can return it and get a new one.
    *[0] It's [color=yellow]empty[/color], but I can return the old item here if i want a new one.
}


# For some reason, after the {-item-plural()} fluent fails to insert {$l}, despite it being passed, but successfully inserts {$limit}, even though
# it is "global" and should fail the same way it fails to insert {$remaining}.
-event-item-dispenser-finite-count = [color=yellow]{$r}[/color] { -item-plural(count: $remaining) } remaining. (out of [color=yellow]{$limit}[/color])


event-item-dispenser-examine-finite = {$noLimit ->
    [true]   I can take { -event-item-dispenser-unlimited }!
    *[false] There { -item-plural-isare(count: $remaining) } { -event-item-dispenser-finite-count(r: $remaining, l: $limit) }
}

event-item-dispenser-examine-finite-manualdispose = There { -item-plural-remain(count: $remaining) } {$noLimit ->
    [true]   I can take { -event-item-dispenser-unlimited }! If I need to, I can return them.
    *[false] There { -item-plural-isare(count: $remaining) } { -event-item-dispenser-finite-count(r: $remaining, l: $limit) }. If I need to, I can return them here to get new ones.
}

event-item-dispenser-examine-finite-single = {$remaining ->
    [1] There's only [color=yellow]one[/color] item inside.
    *[0] It's [color=yellow]empty[/color].
}

event-item-dispenser-examine-finite-manualdispose-single = {$remaining ->
    [1] There's only [color=yellow]one[/color] item inside. If I need to, I can return it and get a new one.
    *[0] It's [color=yellow]empty[/color] inside, but I can return the old item here to get a new one.
}

eventitemdispenser-configwindow-dispensingprototype-tooltip = The item prototype to be dispensed to the player on click. When you're in aghost: click on the dispenser with an item to copy its prototype here.
eventitemdispenser-configwindow-autodispose-tooltip = If the limit is exceeded, automatically removes the oldest item. Has no effect on dispensers with a finite stock!
eventitemdispenser-configwindow-infinite-tooltip = A finite-stock dispenser tracks how many items a player has taken and doesn’t allow taking above that limit. An infinite dispenser doesn’t account for removed items in its count.
eventitemdispenser-configwindow-limit-tooltip = The maximum number of items each player can take from this dispenser. Zero disables the limit, be cautious!
eventitemdispenser-configwindow-canmanuallydispose-tooltip = If enabled, clicking on the dispenser with a dispensed item deletes it, effectively “returning” it to the dispenser. The item’s properties (e.g. a gun’s battery) do not carry over to the next dispensed item.
eventitemdispenser-configwindow-replacedisposeditems-tooltip = When automatically removing excess items, replace them with this prototype. Has no effect on dispensers without auto-removal!
eventitemdispenser-configwindow-disposedreplacement-tooltip = The prototype used to replace items automatically removed. It's recommended to use either a “trash” prototype or an effect prototype. Has no effect on dispensers without auto-removal!
eventitemdispenser-configwindow-autocleanup-tooltip = If enabled, all items dispensed by this dispenser will be removed together with the dispenser. (Technically when the EventItemDispenser component is removed)

eventitemdispenser-configwindow-title = Erectin' a dispenser
eventitemdispenser-configwindow-dispensingprototype = Item prototype
eventitemdispenser-configwindow-autodispose = Automatically remove excess?
eventitemdispenser-configwindow-canmanuallydispose = Allow manual removal of items?
eventitemdispenser-configwindow-infinite = Infinite restock
eventitemdispenser-configwindow-limit = Item limit
eventitemdispenser-configwindow-replacedisposeditems = Replace automatically removed items?
eventitemdispenser-configwindow-disposedreplacement = Replacement prototype
eventitemdispenser-configwindow-autocleanup = Automatic cleanup when the dispenser is removed
gun-chamber-examine = - It has a [color={$color}]{$cartridge}[/color] in the chamber.
gun-chamber-examine-empty = - The chamber is [color={$color}]empty[/color].

gun-inserted-magazine-examine = - It is loaded with a [color={$color}]{$magazine}[/color].

gun-ammocount-examine = It has [color={$color}]{$count}[/color] shots remaining.

gun-racked-examine = - It is [color={$color}]racked[/color].
gun-racked-examine-not = - It is [color={$color}]not racked[/color].

gun-revolver-examine =
    { $count ->
        [0] - It is [color={$color}]empty[/color].
        [1] - It is loaded with [color={$color}]{ $count }[/color] cartridge.
        *[other] - It is loaded with [color={$color}]{ $count }[/color] cartridges.
    }

gun-ballistic-extract = Extract
gun-ballistic-full = Full!
gun-ballistic-empty = Empty!

ammo-top-round-examine = It has a [color={$color}]{ $round }[/color] loaded on top.

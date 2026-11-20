-humanoid-markings-selection = { $selectable ->
    [0] You have no markings remaining.
    [one] You can select one more marking.
   *[other] You can select { $selectable } more markings.
}

humanoid-markings-search = Search
humanoid-markings-limits = { $required ->
    [true] { $count ->
        [-1] Select at least one marking.
        [0] You cannot select any markings, but somehow, you have to? This is a bug.
        [one] Select one marking.
       *[other] Select at least one marking and up to {$count} markings. { -humanoid-markings-selection(selectable: $selectable) }
    }
   *[false] { $count ->
        [-1] Select any number of markings.
        [0] You cannot select any markings.
        [one] Select up to one marking.
       *[other] Select up to {$count} markings. { -humanoid-markings-selection(selectable: $selectable) }
    }
}
humanoid-markings-reorder = Reorder markings
humanoid-markings-modifier-respect-limits = Respect limits
humanoid-markings-modifier-respect-group-sex = Respect group & sex restrictions

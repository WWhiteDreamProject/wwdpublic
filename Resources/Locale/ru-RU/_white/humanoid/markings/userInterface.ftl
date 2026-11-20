-humanoid-markings-selection = { $selectable ->
    [0] Больше нельзя выбрать ни одной черты.
    [one] Можно выбрать ещё { $selectable } черту.
    [few] Можно выбрать ещё { $selectable } черты.
   *[other] Можно выбрать ещё { $selectable } черт.
}

humanoid-markings-search = Поиск
humanoid-markings-limits = { $required ->
    [true] { $count ->
        [-1] Выберите хотя бы одну черту.
        [0] Выбрать черты нельзя, но почему-то это обязательно? Это баг.
        [1] Выберите одну черту.
       *[other] Выберите от одной до {$count} черт. { -humanoid-markings-selection(selectable: $selectable) }
    }
   *[false] { $count ->
        [-1] Можно выбрать любое количество черт.
        [0] Черты выбрать нельзя.
        [1] Можно выбрать не более одной черты.
       *[other] Можно выбрать до {$count} черт. { -humanoid-markings-selection(selectable: $selectable) }
    }
}
humanoid-markings-reorder = Изменить порядок черт
humanoid-markings-modifier-respect-limits = Соблюдать ограничения
humanoid-markings-modifier-respect-group-sex = Соблюдать ограничения группы и пола

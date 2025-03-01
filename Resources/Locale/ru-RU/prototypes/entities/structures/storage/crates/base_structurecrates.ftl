ent-CrateGeneric = ящик
    .desc = Большой контейнер для предметов.
    .suffix = { "" }

ent-CrateBaseWeldable = { ent-CrateGeneric }
    .desc = { ent-CrateGeneric.desc }
    .suffix = { "" }

ent-CrateBaseSecure = { ent-CrateBaseWeldable }
    .desc = { ent-CrateBaseWeldable.desc }
    .suffix = Защищённый

ent-CrateBaseSecureReinforced = { ent-CrateBaseSecure }
    .desc = { ent-CrateBaseSecure.desc }
    .suffix = Защищённый, Усиленный

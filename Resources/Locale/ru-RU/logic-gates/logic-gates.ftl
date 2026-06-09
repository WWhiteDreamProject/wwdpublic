logic-gate-examine = В данным момент принадлежит шлюзу { INDEFINITE($gate) } { $gate }.
logic-gate-cycle = Переключено на шлюз { INDEFINITE($gate) } { $gate }
power-sensor-examine = В данный момент проверяет { $output ->
    [true] выходное
    *[false] входное
} напряжение батареи.
power-sensor-voltage-examine = Проверяет { $voltage } электросеть.

power-sensor-switch = Переключено на проверку { $output ->
    [true] выходого
    *[false] входного
} напряжения батареи.
power-sensor-voltage-switch = Сеть переключена на { $voltage ->
    [HV] высоковольтную
    [MV] средневольтную
    [LV] низковольтную
   *[other] НЕ ОПРЕДЕЛЕНО
    }!

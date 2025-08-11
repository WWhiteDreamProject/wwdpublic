logic-gate-examine = В данным момент принадлежит шлюзу { INDEFINITE($gate) } { $gate }.
logic-gate-cycle = Переключено на шлюз { INDEFINITE($gate) } { $gate }
power-sensor-examine = It is currently checking the network's {$output ->
    [true] output
    *[false] input
} battery.
power-sensor-voltage-examine = It is checking the {$voltage} power network.

power-sensor-switch = Switched to checking the network's {$output ->
    [true] output
    *[false] input
} battery.
power-sensor-voltage-switch = Switched network to {$voltage}!

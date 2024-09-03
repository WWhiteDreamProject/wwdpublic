cmd-atvrange-desc = Устанавливает диапазон отладки атмосферы (как два числа с плавающей точкой, начало [красный] и конец [синий])
cmd-atvrange-help = Использование: {$command} <начало> <конец>
cmd-atvrange-error-start = Неверное значение плавающей точки START
cmd-atvrange-error-end = Неверное значение плавающей точки END
cmd-atvrange-error-zero = Масштаб не может быть равен нулю, так как это приведёт к делению на ноль в AtmosDebugOverlay.

cmd-atvmode-desc = Устанавливает режим отладки атмосферы. Это автоматически сбросит масштаб.
cmd-atvmode-help = Использование: {$command} <Общее количество молекул/Количество молекул газа/Температура> [<ID газа (для Количества молекул газа)>]
cmd-atvmode-error-invalid = Неверный режим
cmd-atvmode-error-target-gas = Необходимо указать целевой газ для этого режима.
cmd-atvmode-error-out-of-range = ID газа невозможно разобрать или находится вне диапазона.
cmd-atvmode-error-info = Дополнительная информация для этого режима не требуется.

cmd-atvcbm-desc = Меняет цвет с красного/зеленого/синего на оттенки серого
cmd-atvcbm-help = Использование: {$command} <true/false>
cmd-atvcbm-error = Неверный флаг

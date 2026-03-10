## ConsoleCompiler (Техно-Принтер) Локализация — RU-RU

# Сущности
ent-NCConsoleCompiler = Консоль-Компилятор
    .desc = Высокотехнологичный терминал данных, преобразующий цифровые коды в физические чертежи и рецепты. Загрузите сырые данные и вставьте расшифрованный мастер-диск для печати.

# Сырые данные
ent-NCRawDataT1 = Сырые данные (Тир 1)
    .desc = Низкокачественный осколок данных. При оцифровке даёт 100 очков данных.

ent-NCRawDataT2 = Сырые данные (Тир 2)
    .desc = Среднекачественный осколок данных. При оцифровке даёт 300 очков данных.

ent-NCRawDataT3 = Сырые данные (Тир 3)
    .desc = Высококачественный осколок данных. При оцифровке даёт 500 очков данных.

# Сгоревшая болванка
ent-NCRawDataBurned = Сгоревшая болванка
    .desc = Использованный и выгоревший диск с данными. Абсолютно бесполезен.

# Интерфейс
console-compiler-window-title = MILITECH // COMPILER_OS v1.4
console-compiler-window-data-bank-header = [ DATA BANK // УТИЛИЗАЦИЯ ]
console-compiler-window-input-label = > INPUT:
console-compiler-window-input-empty = EMPTY_SLOT
console-compiler-window-digitize-button = [ ОЦИФРОВАТЬ ДАННЫЕ ]
console-compiler-window-digitize-hint-1 = * СЖИГАЕТ НОСИТЕЛЬ
console-compiler-window-digitize-hint-2 = * КОНВЕРТИРУЕТ В ДАННЫЕ
console-compiler-window-memory-label = СВОБОДНАЯ ПАМЯТЬ:
console-compiler-window-memory-value = { $value } ДАННЫХ
console-compiler-window-eject-button = [ ИЗВЛЕЧЬ ]

console-compiler-window-compiler-header = [ COMPILER // ПЕЧАТЬ ]
console-compiler-window-loaded-label = > LOADED:
console-compiler-window-matrix-not-found = NO_DATA_MATRIX
console-compiler-window-matrix-status = СТАТУС МАТРИЦЫ: { $status }
console-compiler-window-matrix-status-ready = [РАСШИФРОВАНА]
console-compiler-window-matrix-status-missing = [ОТСУТСТВУЕТ]
console-compiler-window-uses-label = ОСТАТОК ЦЕЛОСТНОСТИ:
console-compiler-window-uses-value = [ { $count ->
    [0] —
    *[other] { $count } ИСПОЛЬЗОВАНИЯ
} ]

console-compiler-window-print-blueprint = [ ПЕЧАТЬ: ЧЕРТЕЖ СБОРКИ ]
console-compiler-window-print-recipe = [ ПЕЧАТЬ: РЕЦЕПТ ДЕТАЛИ ]
console-compiler-window-cost-label = ЦЕНА: { $cost } ДАННЫХ | РАСХОД: { $uses } ЗАРЯД
console-compiler-window-eject-master-button = [ ИЗВЛЕЧЬ ДИСК ]

console-compiler-window-status-ready = >> СИСТЕМА: ГОТОВА К РАБОТЕ. ВЕРСИЯ ЯДРА 1.4.0
console-compiler-window-status-printing = >> СИСТЕМА: ИДЕТ ПРОЦЕСС КОМПИЛЯЦИИ...

# Попапы (System)
console-compiler-popup-digitized = +{ $amount } данных оцифровано
console-compiler-popup-no-data = Недостаточно данных!
console-compiler-popup-printed = ✔ { $type } напечатан!
console-compiler-popup-printed-blueprint = Чертёж
console-compiler-popup-printed-recipe = Рецепт
console-compiler-popup-exhausted = Мастер-диск исчерпан! Болванка сгорела.

## ConsoleCompiler Localization — EN-US

# Entities
ent-NCConsoleCompiler = Console Compiler
    .desc = A high-tech data terminal that converts digital codes into physical blueprints and recipes. Load raw data and insert a decrypted master disk to begin printing.

# Raw Data
ent-NCRawDataT1 = Raw Data (Tier 1)
    .desc = A low-quality data shard. Provides 100 data points when digitized.

ent-NCRawDataT2 = Raw Data (Tier 2)
    .desc = A medium-quality data shard. Provides 300 data points when digitized.

ent-NCRawDataT3 = Raw Data (Tier 3)
    .desc = A high-quality data shard. Provides 500 data points when digitized.

# Burned Disk
ent-NCRawDataBurned = Burned Disk
    .desc = A used and burnt-out data disk. Completely useless.

# UI
console-compiler-window-title = MILITECH // COMPILER_OS v1.4
console-compiler-window-data-bank-header = [ DATA BANK // RECYCLING ]
console-compiler-window-input-label = > INPUT:
console-compiler-window-input-empty = EMPTY_SLOT
console-compiler-window-digitize-button = [ DIGITIZE DATA ]
console-compiler-window-digitize-hint-1 = * CONSUMES MEDIA
console-compiler-window-digitize-hint-2 = * CONVERTS TO MB
console-compiler-window-memory-label = FREE MEMORY:
console-compiler-window-memory-value = { $value } MB
console-compiler-window-eject-button = [ EJECT ]

console-compiler-window-compiler-header = [ COMPILER // PRINTING ]
console-compiler-window-loaded-label = > LOADED:
console-compiler-window-matrix-not-found = NO_DATA_MATRIX
console-compiler-window-matrix-status = MATRIX STATUS: { $status }
console-compiler-window-matrix-status-ready = [DECRYPTED]
console-compiler-window-matrix-status-missing = [MISSING]
console-compiler-window-uses-label = INTEGRITY REMAINING:
console-compiler-window-uses-value = [ { $count ->
    [0] —
    *[other] { $count } USES
} ]

console-compiler-window-print-blueprint = [ PRINT: ASSEMBLY BLUEPRINT ]
console-compiler-window-print-recipe = [ PRINT: PART RECIPE ]
console-compiler-window-cost-label = COST: { $cost } MB | DRAIN: { $uses } USES
console-compiler-window-eject-master-button = [ EJECT DISK ]

console-compiler-window-status-ready = >> SYSTEM: READY FOR OPERATION. CORE VERSION 1.4.0
console-compiler-window-status-printing = >> SYSTEM: COMPILATION IN PROGRESS...

# Popups (System)
console-compiler-popup-digitized = +{ $amount } data digitized
console-compiler-popup-no-data = Not enough data!
console-compiler-popup-printed = ✔ { $type } printed!
console-compiler-popup-printed-blueprint = Blueprint
console-compiler-popup-printed-recipe = Recipe
console-compiler-popup-exhausted = ☠ Master disk exhausted! Media burned.

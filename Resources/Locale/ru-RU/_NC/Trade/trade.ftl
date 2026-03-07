ent-NCPrizeTicket = талон каравана
   .desc = Талон, используемый для обмена при помощи специального "торгового автомата". Позволяет заполучить довольно мощное оружие, если конечно хватит талончиков.
ent-NCPrizeTicket1 = { ent-NCPrizeTicket }
   .suffix = 1
   .desc = { ent-NCPrizeTicket.desc }
ent-NCPrizeTicket10  = { ent-NCPrizeTicket }
   .suffix = 10
   .desc = { ent-NCPrizeTicket.desc }
ent-NCPrizeTicket30  = { ent-NCPrizeTicket }
   .suffix = 30
   .desc = { ent-NCPrizeTicket.desc }
ent-NCPrizeTicket60  = { ent-NCPrizeTicket }
   .suffix = 60
   .desc = { ent-NCPrizeTicket.desc }
nc-store-window-title = Торговый Терминал
nc-store-select-category = Выберите категорию
nc-store-search-placeholder = Поиск товаров...
nc-store-footer-balance = Баланс:
nc-store-tab-buy = Покупка
nc-store-tab-sell = Продажа
nc-store-tab-contracts = Контракты
nc-store-cat-ready-short = Готово
nc-store-cat-crate-short = В ящике
nc-store-cat-ready-full = Готово к продаже
nc-store-cat-crate-full = Готово к продаже (в ящике)
nc-store-category-fallback = Разное
nc-store-mass-sell-button = Продать содержимое ящика
nc-store-mass-sell-tooltip = Опция для быстрой продажи всего содержимого.
    Условия:
    • Ящик должен быть закрыт
    • Вы должны тянуть ящик за собой
nc-store-mass-sell-tooltip-with-reward = { nc-store-mass-sell-tooltip }

    Оценочная стоимость: { $reward }
nc-store-only-mass-sell = Этот товар можно продать только оптом через закрытый ящик.
nc-store-show-more = Показать ещё ({ $count })
nc-store-prompt-select-category = Пожалуйста, выберите категорию слева.
nc-store-empty-search = По вашему запросу ничего не найдено.
nc-store-empty-category-search = В этой категории нет товаров, соответствующих запросу.
nc-store-search-results-buy = Результаты поиска (Покупка): { $count }
nc-store-search-results-sell = Результаты поиска (Продажа): { $count }
nc-store-no-stock = Нет в наличии
nc-store-buying-finished = Лимит исчерпан
nc-store-remaining = Остаток: { $count }
nc-store-will-buy = Требуется: { $count }
nc-store-owned = У вас есть: { $count }
nc-store-no-access = Ошибка доступа
nc-store-contracts-empty = Активных контрактов пока нет. Проверьте позже.
nc-store-difficulty-easy = Лёгкий
nc-store-difficulty-medium = Средний
nc-store-difficulty-hard = Сложный
nc-store-contract-title = Контракт ({ $difficulty })
nc-store-contract-badge-single = Разовый
nc-store-contract-badge-single-tooltip =
    Этот контракт доступен для выполнения только один раз за смену.
    После завершения он исчезнет из списка.
nc-store-contract-goals-header = Цели заказа:
nc-store-contract-reward-header = Награда:
nc-store-contract-items-header = Предметы:
nc-store-contract-action-claim = Завершить контракт
nc-store-contract-action-claim-progress = Внести часть ({ $progress }/{ $required })
nc-store-contract-action-can-claim = Готово к сдаче
nc-store-contract-action-not-done = Не выполнено
nc-store-contract-claim-tooltip-single = Завершить разовый контракт и получить полную награду.
nc-store-contract-claim-tooltip-repeatable = Сдать текущий прогресс по контракту и получить награду.
nc-store-contract-claim-tooltip-not-done = Условия контракта ещё не выполнены. Недостаточно предметов.
nc-store-contract-completed = Контракт успешно выполнен!
nc-store-contract-goal-line = { $item }: { $count } шт.
nc-store-contract-progress-line = Прогресс выполнения: { $progress } из { $required }
nc-store-currency-format = { $amount } { $currency }
nc-store-contract-title-pretty = Контракт: { $difficulty } — { $goal }
nc-store-contract-title-pretty-nogoal = Контракт: { $difficulty }

nc-store-contract-desc-default = Выполните требования контракта и заберите награду.
nc-store-contract-desc-generated = Требуется: { $goals }

nc-store-contract-goal-inline = { $item } ×{ $count }

nc-store-unknown-item = ???

nc-store-proto-tooltip-name-only = { $name }
nc-store-proto-tooltip = { $name }
    { $desc }

nc-store-contract-reward-none = Награда не указана
nc-store-contract-reward-item-line = { $item } ×{ $count }

nc-store-contract-badge-completed = ВЫПОЛНЕНО
nc-store-contract-badge-completed-tooltip = Контракт выполнен — можно забрать награду.

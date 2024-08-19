# WWDP

<p style='text-align: center;'><img alt="WWDP logo" src="https://raw.githubusercontent.com/WWhiteDreamProject/wwdpublic/master/Resources/Textures/_White/Logo/WWDPDarkSplashIcon.png" width="512px" /></p>

---

WWDP - форк Einstein Engines, представляющего из себя хард-форк  [Space Station 14](https://github.com/space-wizards/space-station-14), построенный на идеалах и дизайнерском вдохновении семейства серверов BayStation 12 от Space Station 13 с упором на модульный код, который каждый может использовать для создания RP-сервера своей мечты.

WWDP - один из основных серверов русского коммьюнити, который выступает за идеалы свободы отыгрыша, свободы слова и настоящей классической атмосферы Space Station 13 - хаос, веселье, возможности.

Space Station 14 - это ремейк SS13, который работает на собственном движке  [Robust Toolbox](https://github.com/space-wizards/Robust-Toolbox), собственном игровом движке, написанном на C#.

Поскольку это хард-форк, любой код, взятый из другого апстрима, не может быть напрямую замержен сюда, а должен быть перенесен.
Весь код, представленный в этом репозитории, может быть изменен по желанию кодербаса Белой Мечты.

## Ссылки

[Steam](https://store.steampowered.com/app/2585480/Space_Station_Multiverse/) | [Клиент без Steam](https://spacestationmultiverse.com/downloads/) | [Основной репозиторий](https://github.com/Simple-Station/Einstein-Engines)

## Контрибуция

В процессе разработки, ожидайте новостей!

## Сборка

Следуйте [гайду от Space Wizards](https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html) по настройке рабочей среды, но учитывайте, что наши репозитории отличаются и некоторые вещи могут отличаться.
Мы предлагаем несколько скриптов, показанных ниже, чтобы облегчить работу.

### Необходимые зависимости

> - Git
> - .NET SDK 8.0.100


### Windows

> 1. Склонируйте данный репозиторий
> 2. Запустите `git submodule update --init --recursive` в командной строке, чтобы скачать движок игры
> 3. Запускайте `Scripts/bat/buildAllDebug.bat` после любых изменений в коде проекта
> 4. Запустите `Scripts/bat/runQuickAll.bat`, чтобы запустить клиент и сервер
> 5. Подключитесь к локальному серверу и играйте

### Linux

> 1. Склонируйте данный репозиторий.
> 2. Запустите `git submodule update --init --recursive` в командной строке, чтобы скачать движок игры
> 3. Запускайте `Scripts/sh/buildAllDebug.sh` после любых изменений в коде проекта
> 4. Запустите `Scripts/sh/runQuickAll.sh`, чтобы запустить клиент и сервер
> 5. Подключитесь к локальному серверу и играйте

### MacOS

> Предположительно, также, как и на Линуксе.

## Лицензия

Содержимое, добавленное в этот репозиторий после коммита 87c70a89a67d0521a56388e6b1c3f2cb947943e4 (`17 February 2024 23:00:00 UTC`), распространяется по лицензии GNU Affero General Public License версии 3.0, если не указано иное.
См. [LICENSE-AGPLv3](./LICENSE-AGPLv3.txt).

Содержимое, добавленное в этот репозиторий до коммита 87c70a89a67d0521a56388e6b1c3f2cb947943e4 (`17 February 2024 23:00:00 UTC`) распространяется по лицензии MIT, если не указано иное.
См. [LICENSE-MIT](./LICENSE-MIT.txt).

Большинство ресурсов лицензировано под [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/), если не указано иное. Лицензия и авторские права на ресурсах указаны в файле метаданных.
[Example](./Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

Обратите внимание, что некоторые активы лицензированы под некоммерческой [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) или аналогичной некоммерческой лицензией и должны быть удалены, если вы хотите использовать этот проект в коммерческих целях.

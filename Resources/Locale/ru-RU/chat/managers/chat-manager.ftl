### UI

chat-manager-max-message-length = Ваше сообщение превышает лимит в { $maxMessageLength } символов
chat-manager-ooc-chat-enabled-message = OOC чат был включен.
chat-manager-ooc-chat-disabled-message = OOC чат был отключен.
chat-manager-looc-chat-enabled-message = LOOC чат был включен.
chat-manager-looc-chat-disabled-message = LOOC чат был отключен.
chat-manager-dead-looc-chat-enabled-message = Мёртвые игроки теперь могут говорить в LOOC.
chat-manager-dead-looc-chat-disabled-message = Мёртвые игроки больше не могут говорить в LOOC.
chat-manager-crit-looc-chat-enabled-message = Игроки в критическом состоянии теперь могут использовать LOOC.
chat-manager-crit-looc-chat-disabled-message = Игроки в критическом состоянии теперь не могут использовать LOOC.
chat-manager-admin-ooc-chat-enabled-message = Админ OOC чат был включен.
chat-manager-admin-ooc-chat-disabled-message = Админ OOC чат был выключен.

chat-manager-max-message-length-exceeded-message = Ваше сообщение превышает лимит в { $limit } символов
chat-manager-no-headset-on-message = У вас нет гарнитуры!
chat-manager-no-radio-key = Не указан ключ канала!
chat-manager-no-such-channel =  Нет канала с ключем '{$key}'!
chat-manager-whisper-headset-on-message = Вы не можете шептать в радио!

chat-manager-server-wrap-message = СЕРВЕР: { $message }
chat-manager-sender-announcement-wrap-message = [font size=14][bold]Объявление {$sender}:[/font][font size=12]
                                                {$message}[/bold][/font]

chat-manager-entity-say-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] [italic]{$verb}[/italic], [font={$fontType} size={$fontSize}]"[BubbleContent]{$message}[/BubbleContent]"[/font]
chat-manager-entity-say-bold-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]"[BubbleContent][bold]{$message}[/bold][/BubbleContent]"[/font]

chat-manager-entity-whisper-wrap-message = [font size=11][italic][BubbleHeader]{$entityName}[/BubbleHeader] шепчет,"[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]
chat-manager-entity-whisper-unknown-wrap-message = [font size=11][italic][BubbleHeader]Некто[/BubbleHeader] шепчет, "[BubbleContent]{$message}[/BubbleContent]"[/italic][/font]

chat-manager-entity-me-wrap-message = { $entityName } { $message }

chat-manager-entity-looc-wrap-message = LOOC: [bold]{$entityName}:[/bold] {$message}
chat-manager-send-ooc-wrap-message = OOC: [bold]{$playerName}{$rep}:[/bold] {$message}
chat-manager-send-ooc-patron-wrap-message = OOC: [bold][color={$patronColor}]{$playerName}[/color]{$rep}:[/bold] {$message}

chat-manager-send-dead-chat-wrap-message = {$deadChannelName}: [bold][BubbleHeader]{$playerName}[/BubbleHeader]:[/bold] [BubbleContent]{$message}[/BubbleContent]
chat-manager-send-admin-dead-chat-wrap-message = {$adminChannelName}: [bold]([BubbleHeader]{$userName}[/BubbleHeader]):[/bold] [BubbleContent]{$message}[/BubbleContent]
chat-manager-send-admin-chat-wrap-message = {$adminChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-announcement-wrap-message = [bold]{$adminChannelName}: {$message}[/bold]

chat-manager-send-hook-ooc-wrap-message = OOC: [bold](D){$senderName}:[/bold] {$message}

chat-manager-dead-channel-name = МЁРТВЫЕ
chat-manager-admin-channel-name = АДМИН
chat-manager-admin-discord-channel-name = Д-АДМИН

chat-manager-rate-limited = Вы отправляете сообщения слишком часто!
chat-manager-rate-limit-admin-announcement = Игрок { $player } превысил рейт-лимит чата.

## Speech verbs for chat

chat-speech-verb-suffix-exclamation = !
chat-speech-verb-suffix-exclamation-strong = !!
chat-speech-verb-suffix-question = ?
chat-speech-verb-suffix-stutter = -
chat-speech-verb-suffix-mumble = ..

chat-speech-verb-default = говорит
chat-speech-verb-exclamation = восклицает
chat-speech-verb-exclamation-strong = кричит
chat-speech-verb-question = спрашивает
chat-speech-verb-stutter = заикается
chat-speech-verb-mumble = бормочет

chat-speech-verb-insect-1 = стрекочет
chat-speech-verb-insect-2 = чирикает
chat-speech-verb-insect-3 = щелкает

chat-speech-verb-winged-1 = трепещет
chat-speech-verb-winged-2 = жужжит
chat-speech-verb-winged-3 = похлопывает

chat-speech-verb-slime-1 = бормочет
chat-speech-verb-slime-2 = булькает
chat-speech-verb-slime-3 = хлюпает

chat-speech-verb-plant-1 = шуршит
chat-speech-verb-plant-2 = скрипит
chat-speech-verb-plant-3 = трещит

chat-speech-verb-robotic-1 = утверждает
chat-speech-verb-robotic-2 = бипает

chat-speech-verb-reptilian-1 = шипит
chat-speech-verb-reptilian-2 = фыркает
chat-speech-verb-reptilian-3 = пыхтит

chat-speech-verb-skeleton-1 = гремит
chat-speech-verb-skeleton-2 = щелкает
chat-speech-verb-skeleton-3 = скрежет

chat-speech-verb-canine-1 = гавкает
chat-speech-verb-canine-2 = лает
chat-speech-verb-canine-3 = воет

chat-speech-verb-small-mob-1 = пищит

chat-speech-verb-large-mob-1 = рычит
chat-speech-verb-large-mob-2 = урчит

chat-speech-verb-monkey-1 = кричит
chat-speech-verb-monkey-2 = визжит

chat-speech-verb-cluwne-1 = хихикает
chat-speech-verb-cluwne-2 = гогочет
chat-speech-verb-cluwne-3 = смеется

chat-speech-verb-ghost-1 = жалуется
chat-speech-verb-ghost-2 = дышит
chat-speech-verb-ghost-3 = мычит
chat-speech-verb-ghost-4 = бормочет

chat-speech-verb-electricity-1 = потрескивает
chat-speech-verb-electricity-2 = гудит
chat-speech-verb-electricity-3 = скрипит

chat-manager-cooldown-warn-message_channel = Вы сможете писать { $inChat } через: { $remainingTime } сек.
chat-manager-cooldown-warn-message = Вы сможете писать через { $remainingTime } сек.
chat-manager-antispam-warn-message = Вы сможете повторить сообщение через { $remainingTime } сек.

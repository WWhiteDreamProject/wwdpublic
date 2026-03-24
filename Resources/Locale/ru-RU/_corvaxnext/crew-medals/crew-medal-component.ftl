# interaction
# WWDP EDIT DOWN
comp-crew-medal-inspection-text = Награждён(а) {$recipient} за то, что {$reason}.
comp-crew-medal-award-text = {$recipient} получает медаль «{$medal}».

# round end screen
# round end screen
# WWDP EDIT START
comp-crew-medal-round-end-result = {$count ->
    [one] Была вручена одна медаль:
   *[other] Было вручено {$count} {RU-PLURAL($count, "медаль", "медали", "медалей")}:
}
# WWDP EDIT END

# WWDP EDIT START
comp-crew-medal-round-end-list =
    - [color=white]{$recipient}[/color] получил(а) [color=white]{$medal}[/color] за то, что
    {"  "}{$reason}
# WWDP EDIT END

# UI
# UI
crew-medal-ui-header = Настройки медали
crew-medal-ui-reason = Причина награждения:
crew-medal-ui-character-limit = {$number}/{$max}
crew-medal-ui-info = После вручения изменить причину будет невозможно.
crew-medal-ui-save = Сохранить


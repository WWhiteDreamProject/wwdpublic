custom-ghost-fail-exclusive-ghost = This ghost is ckey-locked.
custom-ghost-fail-server-insufficient-playtime = Play on the server for {$requiredHours} {$requiredHours ->
  *[one] hour
  [other] hours
}. { -playtime(pH: $playtimeHours, pM: $playtimeMinutes) }

custom-ghost-fail-job-insufficient-playtime = Play as a {$job} {$requiredHours} {$requiredHours ->
  *[one] hour
  [other] hours
}. { -playtime(pH: $playtimeHours, pM: $playtimeMinutes) }

custom-ghost-fail-department-insufficient-playtime = Play as a member of {$department} for {$requiredHours} {$requiredHours ->
  *[one] hour
  [other] hours
}. { -playtime(pH: $playtimeHours, pM: $playtimeMinutes) }

custom-ghosts-window-title = Custom ghost menu
custom-ghosts-window-show-all-checkbox = Show all
custom-ghosts-window-show-all-checkbox-tooltip = Shows ghosts that are now unlocked yet. Hover over locked entry to see it's unlock requirements.
custom-ghost-window-tooltip-to-unlock = To unlock this ghost you need:

-playtime = Your current playtime is {$pH} {$pH ->
  *[one] hour
  [other] hours
} {$pM} {$pM ->
  *[one] minute
  [other] minutes
}
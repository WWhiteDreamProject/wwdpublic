interaction-LookAt-name = Смотреть на
interaction-LookAt-description = Взглянуть в пустоту и ощутить её взгляд на себе.
interaction-LookAt-success-self-popup = Вы смотрите на {$target}.
interaction-LookAt-success-target-popup = Вы чувствуете, что  {$user} смотрит на вас...
interaction-LookAt-success-others-popup = {$user} смотрит на {$target}.

interaction-Hug-name = Обнять
interaction-Hug-description = Объятия в день избавляют от непостижимых психологических ужасов.
interaction-Hug-success-self-popup = Вы обнимаете {$target}.
interaction-Hug-success-target-popup = {$user} обнимает вас.
interaction-Hug-success-others-popup = {$user} обнимает {$target}.

interaction-Pet-name = Погладить
interaction-Pet-description = Погладьте своего коллегу по работе, чтобы облегчить его стресс.
interaction-Pet-success-self-popup = Вы погладили {$target} по {POSS-ADJ($target)} голове.
interaction-Pet-success-target-popup = {$user} погладил вас по {POSS-ADJ($target)} голове.
interaction-Pet-success-others-popup = {$user} поглаживает {$target}.

interaction-KnockOn-name = Постучать
interaction-KnockOn-description = Постучите по цели, чтобы привлечь внимание.
interaction-KnockOn-success-self-popup = Вы стучите по {$target}.
interaction-KnockOn-success-target-popup = {$user} стучит по вам.
interaction-KnockOn-success-others-popup = {$user} стучит по {$target}.

interaction-Rattle-name = Греметь
interaction-Rattle-success-self-popup = Вы гремите {$target}.
interaction-Rattle-success-target-popup = {$user} гремит вами.
interaction-Rattle-success-others-popup = {$user} гремит {$target}.

# The below includes conditionals for if the user is holding an item
interaction-WaveAt-name = Помахать рукой
interaction-WaveAt-description = Помахать рукой в сторону цели. Если вы держите в руках предмет, вы помашете им.
interaction-WaveAt-success-self-popup = Вы помахали {$hasUsed ->
    [false] {$target}.
    *[true] {$used} {$target}.
}
interaction-WaveAt-success-target-popup = {$user} машет {$hasUsed ->
    [false] вам.
    *[true] {POSS-PRONOUN($user)} {$used} вам.
}
interaction-WaveAt-success-others-popup = {$user} машет {$hasUsed ->
    [false] {$target}.
    *[true] {POSS-PRONOUN($user)} {$used} {$target}.
}

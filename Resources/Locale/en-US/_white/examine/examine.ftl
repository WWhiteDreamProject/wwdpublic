# Poggers examine system

examine-name = It's [bold]{$name}[/bold]!
examine-can-see = Looking at {OBJECT($ent)}, you can see:
examine-can-see-nothing = {CAPITALIZE(SUBJECT($ent))} is completely naked!

id-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} belt.
head-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} head.
eyes-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} eyes.
mask-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} face.
neck-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} neck.
ears-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} ears.
jumpsuit-examine = - [bold]{$item}[/bold] {SUBJECT($ent)} is wearing.
outer-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} body.
suitstorage-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} shoulder.
back-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} back.
gloves-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} hands.
belt-examine = - [bold]{$item}[/bold] {SUBJECT($ent)} is wearing.
shoes-examine = - [bold]{$item}[/bold] on {POSS-ADJ($ent)} feet.

id-card-examine-full = - {CAPITALIZE(POSS-ADJ($wearer))} ID: [bold]{$nameAndJob}[/bold].

# Selfaware version

examine-name-selfaware = It's you, [bold]{$name}[/bold]!
examine-can-see-selfaware = Looking at yourself, you can see:
examine-can-see-nothing-selfaware = You are completely naked!

id-examine-selfaware = - [bold]{$item}[/bold] on your belt.
head-examine-selfaware = - [bold]{$item}[/bold] on your head.
eyes-examine-selfaware = - [bold]{$item}[/bold] on your eyes.
mask-examine-selfaware = - [bold]{$item}[/bold] on your face.
neck-examine-selfaware = - [bold]{$item}[/bold] on your neck.
ears-examine-selfaware = - [bold]{$item}[/bold] on your ears.
jumpsuit-examine-selfaware = - [bold]{$item}[/bold] you are wearing.
outer-examine-selfaware = - [bold]{$item}[/bold] on your body.
suitstorage-examine-selfaware = - [bold]{$item}[/bold] on your shoulder.
back-examine-selfaware = - [bold]{$item}[/bold] on your back.
gloves-examine-selfaware = - [bold]{$item}[/bold] on your hands.
belt-examine-selfaware = - [bold]{$item}[/bold] you are wearing.
shoes-examine-selfaware = - [bold]{$item}[/bold] on your feet.

# Selfaware examine

comp-hands-examine-empty-selfaware = You are not holding anything.
comp-hands-examine-selfaware = You are holding { $items }.

humanoid-appearance-component-examine-selfaware = You are { INDEFINITE($age) } { $age } { $species }.

# Description examine wrapper

examine-entity-description-wrapper = [font size=11][italic][color=SlateGray]{ $description }[/color][/italic][/font]

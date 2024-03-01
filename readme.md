A code library/plugin for Totally Accurate Battlegrounds Dedicated Server to make modding easier. 

Adds the ability to define new chat commands, create custom settings files, and adds functions for altering players in ways that wasn't present in the unmodified server.

<h1>default chat commands list</h1>

most of the commands require a PermLevel higher than 0, which is the default for players. you need to add players using their EPIC ID in the permlist file.

These commands are used in-game, not in the console!

<br>
<br>

Gets the permission status of the player

Perm Level: 1

`/perm-get <player>`

<br>

Starts the countdown timer

Perm Level: 1

`/start [time]`

<br>

Gets the ID of a player with the given name.

Perm Level: 1

`/id <name>`

<br>

Gets the NAME of a player with the given byte playerindex.

Perm Level: 1

`/name [id]`

<br>

Gets the epic id of a player with the given name or index

Perm Level: 1

`/epic <name>`

<br>

Changes or queries a player's team

Perm Level: 2

`/team <get|set> <player> [index](if setting)`

<br>

Brings the command user to the specified player

Perm Level: 2

`/goto <player>`

<br>

Brings a player to the command user

Perm Level: 2

`/bring <player>`

<br>

lists different things in the console.

Perm Level: 2

`/list <teams|players|playerrefs|all>`

<br>

Sends the first player to the second player

Perm Level: 2

`/send <player> <player>`

<br>

gives the user an item with an optional amount

Perm Level: 2

`/give [id] [amount(optional)]`

<br>

gives the target an item with an optional amount

Perm Level: 2

`/gift <player> [id] [amount(optional)]`

<br>

SETS the permission status of the player!

Perm Level: 4

`/perm-set <player>`



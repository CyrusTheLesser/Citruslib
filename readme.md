A code library/plugin for Totally Accurate Battlegrounds Dedicated Server to make modding easier. 

Adds the ability to define new chat commands, create custom settings files, and adds functions for altering players in ways that wasn't present in the unmodified server.

<h1>installation</h1>

[Install Bepinex.](https://docs.bepinex.dev/articles/user_guide/installation/index.html) Then, put the latest release of citruslib in bepinex's plugin folder. launch the game once after this to get most of the config files to appear.

<h1>Custom Chat Commands</h1>
Allows modders to create custom chat commands that players can execute in game.

most of the commands require a PermLevel higher than 0, which is the default for players. you need to add players using their EPIC ID in the permlist file. 
A player can find their EPIC ID EITHER:
In the player log file, located in LocalLow/Landfall/ect/ect
In a server's Guestbook file after connecting to a server with this plugin installed

Some default commands are listed below. A Command List is generated in the server file detailing ALL of the commands.
<details>


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
</details>

<h1>Custom Loot Tables</h1>

Makes Customizing loot tables easier. When first launching a server with citlib, the mod creates a folder with copies of the default loot tables as JSON. You can add, remove, and otherwise edit the files to change

<h1>Custom Settings</h1>

Some default extra settings are included that mostly pertain to citlib or debugging. you can open the settings file to see the setting's descriptions there. Modders can also create their own settings files or add settings to the ExtraSettings file.

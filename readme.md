A code library for Totally Accurate Battlegrounds Dedicated Server to make modding easier. 

Adds the ability to define new chat commands, create custom settings files, and adds functions for altering players in ways that wasn't present in the unmodified server.

<h1>default chat commands list</h1>
most of the commands require a PermLevel higher than 0, which is the default for players. you need to add players using their EPIC ID in the permlist file


Gets the permission status of the player

Admin: True, Perm Level: 1

/perm-get <player>



Starts the countdown timer

Admin: True, Perm Level: 1

/start [time]



Gets the ID of a player with the given name.

Admin: True, Perm Level: 1

/id <name>



Gets the NAME of a player with the given byte playerindex.

Admin: True, Perm Level: 1

/name [id]



Gets the epic id of a player with the given name or index

Admin: True, Perm Level: 1

/epic <name>



Changes or queries a player's team

Admin: True, Perm Level: 2

/team <get|set> <player> [index](if setting)



Brings the command user to the specified player

Admin: True, Perm Level: 2

/goto <player>



Brings a player to the command user

Admin: True, Perm Level: 2

/bring <player>



lists different things in the console.

Admin: True, Perm Level: 2

/list <teams|players|playerrefs|all>



Sends the first player to the second player

Admin: True, Perm Level: 2

/send <player> <player>



gives the user an item with an optional amount

Admin: True, Perm Level: 2

/give [id] [amount(optional)]



gives the user an item with an optional amount

Admin: True, Perm Level: 2

/gift <player> [id] [amount(optional)]



SETS the permission status of the player!

Admin: True, Perm Level: 4

/perm-set <player>



SETS the admin status of the player!

Admin: True, Perm Level: 4

/admin <player>


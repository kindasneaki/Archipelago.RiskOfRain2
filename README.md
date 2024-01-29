# Archipelago.RiskOfRain2 | ![Discord Shield](https://discordapp.com/api/guilds/731205301247803413/widget.png?style=shield)

## To be used with [Archipelago](https://archipelago.gg)

This mod adds support to Risk of Rain 2 for playing as an Archipelago client. For more information on Archipelago head over to https://archipelago.gg or join our Discord.

Multiplayer compatible. Be sure to scale up your YAML settings if you play in multiplayer. All players require the mod in multiplayer.

![In Game Example](https://raw.githubusercontent.com/kindasneaki/Archipelago.RiskOfRain2/main/docs/img/Example.png)
## Gameplay 

### Classic mode

Classic mode is the original way to play Archipelago in Risk of Rain 2.

The Risk of Rain 2 players send checks by causing items to spawn in-game. Currently, this includes opening chests, defeating bosses, using scrappers and 3D printers, opening lunar pods, and accessing terminals. 
An item check is only sent out after a certain number of items are picked up. This count is configurable in the player's YAML.

### Explore mode

Explore mode is an alternative way to play Archipelago in Risk of Rain 2.

The Risk of Rain 2 player sends checks from doing different actions such as opening chests, beating shrines, defeating scavengers, finding radio scanners, and finding newt altars.
These locations divided evenly across the different stages and can only be completed in their respective stages.

Environments will be in the item pool, and you will need to unlock them to progress towards your goal.


The rules for locations are as follows:
- Chest-like interactables will send a check when a certain number of chests are opened. Multishops are not chest, 
but lunar pods and void cradles are. Artifact of sacrifice will treat item drops like opening chests.
- Shrines will send a check when a certain number of shrines are beat. Rules for beating shrines are:
    - Altar of Gold is beat on purchase.
    - Shrine of Blood is beat on interact. Sending shrine as a check denies gold.
    - Shrine of Chance is beat on when rewarded. Sending shrine as a check denies the item.
    - Shrine of Combat is beat on interact.
    - Shrine of Order is beat on purchase.
    - Shrine of the Mountain is beat on defeating the challenge of the Mountain. Sending the shrine as a check denies the bonus item.
    - Shrine of the Woods is beat on the second and third purchases.
- Scavenger bags send checks when opened but do not grant items. Looping to the same environment will let you reopen the scavenger bag.
- Radio Scanners can be found on every stage and send a check. Radio Scanners are guaranteed to spawn.
- Newt Altars send checks when found but do not grant a Blue Portal (Refunds 1 lunar coin). Newts Altars grant portals when the checks are depleted.

Explore mode also attempts to help against being stuck in RNG hell; the teleporter will favor environments that have more checks remaining.


### Achieving Victory or Defeat

Achieving victory is defined as beating Mithrix, defeating the Twisted Scavenger, or beating the Voidling (DLC enabled). (This is both true for Classic and Explore modes.)

Alternatively you can change the Final Stage Death is Win setting to enable Obliteration ending as well as if you
die in the Commencement map (the moon)/ The Planetarium (DLC enabled)/ Hidden Realm: A Momemt, Whole (limbo). 

Due to the nature of roguelike games, you can possibly die and lose your place completely. This is mitigated partly by the free grants of `Dio's Best Friend`
but it is still possible to lose. If you do lose, you can reconnect to the Archipelago server and start a new run. The server will send you the items you have
earned thus far, giving you a small boost to the start of your run.

## YAML Settings

To create a YAML please goto [Archipelago RoR2 Settings](https://archipelago.gg/games/Risk%20of%20Rain%202/player-settings)

## Connecting to an Archipelago Server

I assume you already have an Archipelago server running. Instructions on how to run a server are available at [Setup Guide](https://archipelago.gg/tutorial/Archipelago/setup/en#hosting-an-archipelago-server).

Fill in the relevant info and click `Connect To AP` to connect to the server

Keep password blank if there is no password on the server.

![In Lobby UI Example](https://raw.githubusercontent.com/kindasneaki/Archipelago.RiskOfRain2/main/docs/img/lobby.png)

Once connected it will print in chat that you have successfully connected!

## Changelog

**1.3.2**
    * Support for Progressive Stage items
    * Messaging when you can't progress any further

**1.3.1**
* **Bug Fixes:**
    * Fix Obliterating yourself 

**1.3.0**

* **Added Support for multiplayer in explore mode!**
* Players will receive different items in multiplayer instead of all the same.
* Added new fillers items.
* Added new traps items.
* The Objective for explore mode now shows the environment you are in.
* Increased item notification speed to 2 sec for AP Items.
* Doubled interactables spawn rate.
* New console command to show the current environments recieved. (`archipelago_show_unlocked_stages`)
* New console command to turn on/off final stage death. (`archipelago_final_stage_death true/false`)
* New console command to highlight satellites to make them easier to see. (`archipelago_highlight_satellite true/false`)
* Look up checks when entering a map instead of when starting a run.
* Toggle Connect To AP button to Disconnect.
* Stage items will be required to enter then next set of stages in explore mode.

* **Bug Fixes:**
    * Passwords are apparently a thing.
    * Fix some lag issues with Artifact of Sacrifice.
    * When receiving a deathlink on transitioning to a new stage it would cause it to stop working.
    * Map mods will no longer break but the environments will be blocked in explore mode.

**1.2.5**

* Add Beads of Fealty for real this time
* Updated Multiclient.Net to 5.0.6


**1.2.4**

* Guaranteed newt alter spawn on each stage. Note: that 2 can spawn now but you can only use 1 per map
* Changed the weights of each location to be 5 per instead of 1 per to give a higher chance to get a map with more checks.
* Added in more messages.
* Updated Multiclient.Net to 5.0.5
* Add Beads of Fealty
* Bug Fixes:
    * You can no longer enter The Planetarium without the stage unlocked.
    * Id's for Void Locus and The Planetarium were backwards and are fixed.


**1.2.3**

* An incomplete feature made it in that wasn't supposed to.


**1.2.2**

* Bug Fix:
    * Rolling classic yaml when data package hasn't been updated would result in the wrong starting id.

**1.2.1**

* Bug Fixes:
    * Dying with finalStageDeath: True on would complete the game instead of check to see if you were in specific locations.
    * finalStageDeath was always true on AP version 0.3.8 and older.
    * Using deathlink Command to turn on/off wouldn't work on AP version 0.3.8 and older.

**1.2.0**

* Created Explore mode!
* Added Void Items (with DLC enabled)
* Added DeathLink support (Will send a death link for any player connected, and all players will die if receiving a death link while playing co-op)
* Added Environments as items
* Added Release/Collect prompt on goal completion
* Added ability to hide connect fields
* Update MultiClient.Net to 4.2.2
* Change Default URI to archipelago.gg
* Added Disconnect console command (`archipelago_disconnect`)
* Added Deathlink console command (`archipelago_deathlink true/false`)
* Changed Connect console command to (`archipelago_connect <url> <port> <slot> [password]`)
* The chat for items sent/received are more colorful


* Bug fixes:
  * Reduce chat lag on collects
  * Fixed a disconnect bug where it was being called twice.

**1.1.5**
* Fixed Fields resetting to default after dying.

**1.1.4**
* Fixed Collect Bug.
* Skip collected checks, so you don't just send nothing.
* Now connect to AP through lobby instead of with ready button.
  * Chat enabled for single player
* Added Color to Players in chat for better readability.
* Multi Client 4.0 support.
* Original mod location https://thunderstore.io/package/ArchipelagoMW/Archipelago/

**1.1.3**
* Fixed connection issues.
* Update client protocol version. 
    * Now only works on Archipelago server version 0.3.4 or higher.

**1.1.2**
* SOTV Ending now counts as an acceptable ending.
* Added YAML toggle for 'Death on the final stage counts as a win'.

**1.1.1**
* Update plugin version so it appears properly in the logs.

**1.1.0**
* Update to support Survivors of the Void DLC and updated R2API.
* Fix Archipelago PrintJSON packets.

**1.0.2**

* Update supported Archipelago version to function on current AP source.

**1.0.1**

* Fix chat box getting stuck on enabled sometimes.
* Stop lunar coins, elite drops, artifacts, and artifact keys from counting towards location checks.
    * Enables going to Bulwark's Ambry while you have location checks left.
* Names not appearing in multiplayer fixed.
* Fix lunar equipment grants not previously working.

**1.0 (First Stable Release)**

* Release of all changes from 0.1.5 up to 0.1.7.
* This version purely denotes a release, no new features or fixes were made.

**0.1.7 (Internal Version)**

* Allow for obliteration or fealty endings to work as AP session completion events. You don't _have_ to go to commencement anymore.
* Fix bug with objective display being wrong after game re-make.
* Fix bug with location check progress bar doubling on clients that are not the host.
* Fix bug with location check progress bar not working after reconnect. todo
* Chat messages from players who are not host now send to the multiworld correctly. (But under the name set in the YAML as it's only one slot for the whole RoR session)
* Remove location check progress bar from UI when all checks are complete.

**0.1.6 (Internal Version)**

* UI code refactor. Not visible to users, but code is slightly cleaner.
* Add `archipelago` console command. Syntax: `archipelago <url> <port> <slot> [password]`
* Reconnect logic is greatly improved. Now attempts to reconnect every 5 seconds for 5 tries. If it fails entirely, you can use the archipelago command.
* Your existing equipment drops at your feet when you are granted one from the server. The new one swaps into the slot.
* Add objective tracker for total number of checks remaining.

**0.1.5 (Internal Version)**

* Chat messages go out to the multiworld now.
* Smoke effect now appears when an item drop is turned into a location check as a visual indicator of sending out a check.
* Remove `total_items` YAML option as it doesn't work as intended.
* Other formatting tweaks to README.
* Add HUD for location check progress. Now appears as a bar under your health bar. When it fills up all the way it will reset and you will send out a check.

**0.1.4**

* Update `Newtonsoft.Json.dll` to the correct version, this fixes the client failing to connect to the server.

**0.1.3**

* Set InLobbyConfig as hard dependency.
* Update README to reflect that all players require the mod at the moment.
* Add `total_items` YAML option to README.
* Add `enable_lunar` YAML option to README.

**0.1.2**

* Add R2API as a dependency.

**0.1.1**

* Fix victory condition sending for commencement.
* Remove splash+intro cutscene skip (was for debugging purposes).

**0.1.0**

* Initial version.

## Known Issues

* Splitscreen support is unlikely at the moment. It might work, it might not.

## To-do/Ideas

* Cache and load data package from file system.
* Further randomization in some way. Mob spawns, elite types, variance api, boss types, mob families, mobs with items, etc.
* More item/reward types: warbanner drops, drones
* Funny/joke item types: launching you into the air, switch left and right click
* Trap item types: spawn bosses, drop bombs on the stage

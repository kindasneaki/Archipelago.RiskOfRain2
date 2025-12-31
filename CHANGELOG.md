**1.5.3**
* Update gamelib/mmhook/ror2bepinexpack
* Add more guards to deathlink to prevent attempt to prevent crashes.
*  **Bug Fixes:**
    * New Variant stages have a default weight of 0, causing them to never be accessable if locations are complete
    * 
**1.5.2**
*  **Bug Fixes:**
    * Fixed items not counting as checks when related to pickupDropletController

**1.5.1**
* Update for Allooyed Collective Patch

**1.5.0**
* **Seekers Of The Storm DLC support added.**
    * New Stages for Archipelago Explore Mode:
        * Shattered Abodes added
        * Helminth Hatchery added 
        * **Alternate paths:** (Colossus Portal)
            * Reformed Altar
            * Treeborn Colony
            * Golden Dieback (Variant stage for Treeborn Colony)
            * Prime Meridian
        * **Variant Stages added:**
            * Disturbed Impact added (Variant stage for Shattered Abodes)
            * Viscious Falls (Variant stage for Verdant Falls)
    * **New Ending**:
        * Rebirth (Requires beating False Son)
* **New Seer Portal System added.**
    * Spawns seer portals on the teleporter to allow for easier access to stages you have unlocked.
* **Updated Multiclient.net to 6.6.1**

**1.4.9**
* **Bug Fixes:**
    * Always use CachedName instead of baseStageName for the stage name in explore mode.

**1.4.8**
* **Bug Fixes:**
    * Fixed item weights not being applied correctly in explore mode.

**1.4.7**
* **Bug Fixes:**
    * After one of the more recent patches, the game now preloads the stage, so now we need to manually pick the next stage before that preload happens.
    * The wrong sceneIndex was being used for chests/shrines UI display in explore mode.
    * Preloading progressive/stage items to correctly display which ones are available in the next stage.

**1.4.6**
* Extra null checking to attempt to fix an issue.

**1.4.5**
* Update Multiclient.net to 6.5.0

**1.4.4**
* **Bug Fix:**
    * When loading to many items, it would sometimes skip the environments from loading.

**1.4.3**
* Temporarily make green portals act like blue portals until new stages can get added into the pool.

**1.4.2**
* Update Multiclient.net to 6.3.1
* Update Target Framework to netstandard 2.1
* Update Game Libraries


**1.4.1**
* Updated Multiclient.net to 6.1.1

**1.4.0**
* New Stage, Verdant Falls added

**1.3.7**
* **Bug Fixes:**
    * Update broke chest checks.

**1.3.6**
* Added in slot info configuration
* Added in highlight satellite configuration
* **Bug Fixes:**
    * Progressive amount was not resetting

**1.3.5**
* **Bug Fixes:**
    * Victory messaging happens twice in explore mode now..

**1.3.4**
* **Bug Fixes:**
    * You couldn't see the goal in classic

**1.3.3**
* Reconnecting feature added.
* New console command to reconnect(`archipelago_reconnect)
* **Bug Fixes:**
    * When the socket receives an error it will attempt to reconnect

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

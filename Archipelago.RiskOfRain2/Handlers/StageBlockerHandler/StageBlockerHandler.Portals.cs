using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class StageBlockerHandler
    {
        /**
         * Unalign the teleporter when Commencement is not unlocked.
         */
        private void Active_OnEnter(On.EntityStates.LunarTeleporter.Active.orig_OnEnter orig, EntityStates.LunarTeleporter.Active self)
        {
            if (CheckBlocked("moon2"))
            {
                ChatMessage.SendColored("Just not feeling it right now.", new Color(0x5d, 0xd5, 0xe2));
                self.outer.SetNextState(new EntityStates.LunarTeleporter.ActiveToIdle());
                return;
            }
            orig(self);
        }

        /**
         * Block interaction with the Void Fields portal if the environment is not unlocked.
         */
        private void Interactor_PerformInteraction(On.RoR2.Interactor.orig_PerformInteraction orig, Interactor self, GameObject interactableObject)
        {
            // I settled on hooking this method because I tried all other alternatives I could think of first.
            // I attempted using all of the following with little or no success:
            // - PortalSpawner_AttemptSpawnPortalServer: failed to block voidstage from spawning on teleporter
            // - PortalSpawner_Start: failed to block voidstage portal from spawning on teleporter
            // - GenericInteraction_RoR2_IInteractable_GetInteractability: broke all interactables
            // - GenericInteraction_RoR2_IInteractable_OnInteractionBegin: didn't seem to be called when using void portals

            // Blocking the use of void portals here is preferred over SceneExitController_SetState.
            // This is because it's more user friendly to let the user know they cannot travel to the void
            //  rather than redirect them to the next stage without warning.

            if (NetworkServer.active && interactableObject)
            {
                // TODO how much does this affect performance?
                foreach (IInteractable comp in interactableObject.GetComponents<IInteractable>())
                {
                    GenericInteraction gi = comp as GenericInteraction;
                    if (gi)
                    {
                        Log.LogDebug($"Checking interaction with context token {gi.contextToken} and name of {gi.gameObject.name}");
                        switch (gi.contextToken)
                        {
                            case "PORTAL_ARENA_CONTEXT":
                                if (CheckBlocked("arena"))
                                {
                                    ChatMessage.SendColored("The void rejects you.", new Color(0x88, 0x02, 0xd6));
                                    gi.SetInteractabilityConditionsNotMet();
                                }
                                else gi.SetInteractabilityAvailable();
                                break;
                            case "PORTAL_VOID_CONTEXT":
                                if (CheckBlocked("voidstage"))
                                {
                                    ChatMessage.SendColored("The void rejects you.", new Color(0x88, 0x02, 0xd6));
                                    gi.SetInteractabilityConditionsNotMet();
                                }
                                else gi.SetInteractabilityAvailable();
                                break;
                            case "PORTAL_GOLDSHORES_CONTEXT":
                                if (CheckBlocked("goldshores"))
                                {
                                    // prevents goldshores from being used from the halcyon shrine if not unlocked
                                    ChatMessage.SendColored("The gold portal was missing the key to enter but stayed to taunt you.", Color.yellow);
                                    gi.SetInteractabilityConditionsNotMet();
                                }
                                else gi.SetInteractabilityAvailable();
                                break;
                            case "ACCESSCODES_PORTAL_CONTEXT":
                                if (CheckBlocked("solutionalhaunt") && gi.gameObject.name == "HardwareProgPortal_Haunt(Clone)" && SceneCatalog.mostRecentSceneDef.cachedName == "conduitcanyon")
                                {
                                    ChatMessage.SendColored("The solutional haunt portal failed to decypher the access code but stayed to taunt you.", Color.yellow);
                                    gi.SetInteractabilityConditionsNotMet();
                                } else if (CheckBlocked("conduitcanyon") && (gi.gameObject.name == "HardwareProgPortal(Clone)" || gi.gameObject.name == "HardwareProgPortal_Haunt(Clone)"))
                                {
                                    ChatMessage.SendColored("The conduit portal failed to decypher the access code but stayed to taunt you.", Color.yellow);
                                    gi.SetInteractabilityConditionsNotMet();
                                }
                                else gi.SetInteractabilityAvailable();
                                break;
                            case "PORTAL_EYEPORTAL_CONTEXT":
                                if (CheckBlocked("computationalexchange"))
                                {
                                    ChatMessage.SendColored("That is one big eye isn't it. Too bad it won't let you in!", Color.yellow);
                                }
                                break;
                            case "PORTAL_SOLUSWEB_CONTEXT":
                                if (CheckBlocked("solusweb"))
                                {
                                    ChatMessage.SendColored("You are right there at the entrance but you are missing the most imporant part!!", Color.yellow);
                                    ChatMessage.Send($"<color=#FF00FF>Come back when you have</color> <color=#dda0dd>Neural Sanctum</color>");
                                    gi.SetInteractabilityConditionsNotMet();
                                }
                                else gi.SetInteractabilityAvailable();
                                break;
                                // not blocking voidraid:
                                // NOTE: Planetarium has two entrances, one in Void Locus and one in Commencement
                                // Since this currently seems like an edge case where the player would truely decide to do both
                                //  if the player gets the Planetarium portal from Void Locus, they can travel there.
                                // Only the glass frog interaction in Commencement will be blocked.
                                // This also prevents the player from becoming stuck.

                                // Arguably the other portals could be handled here as well,
                                // however it seems more user friendly to just not spawn the portal at all rather
                                // than spawn the portal and make it unable to be interacted with.
                        }
                    }
                }
            }
            orig(self, interactableObject);
        }

        /**
         * Block players from petting the frog and refund them if the Planetarium is not unlocked.
         */
        private void FrogController_Pet(On.RoR2.FrogController.orig_Pet orig, FrogController self, Interactor interactor)
        {
            // We block usage of the frog out of quality of life.
            // It would feel unfail to use 10 coins just to not spawn a portal or spawn a portal the user cannot use.
            // By adding coins back to the users inventory, it shows that the transaction cannot go through.
            // Adding a message also makes this even more clear.

            if (CheckBlocked("voidraid"))
            {
                Log.LogDebug("Blocking petting the frog for planetarium.");
                // Only host can refund the coin and having the host send the message prevents duplicate messages.
                if (NetworkServer.active)
                {
                    Log.LogDebug("blocking planetarium as host.");
                    // refund the lunar coin if the player who payed the coin is this client's player
                    //interactor.GetComponent<NetworkUser>().AwardLunarCoins(1); // (only the server actually executes the contents of this method) // TODO give coin only to one person
                    foreach (NetworkUser local in NetworkUser.readOnlyLocalPlayersList)
                    {
                        Log.LogDebug("Refunding coins...");
                        local.AwardLunarCoins(1);
                        // TODO This does in fact give more coins back in multiplayer since every player would get a coin.
                        // I don't have a solution for this right now : ^)
                    }

                    ChatMessage.SendColored("The frog does not want to be pet.", Color.white);
                }
                return;
            }
            orig(self, interactor);
        }

        /**
         * Prevent the dialer from changing states if the Bulwark's Ambry is not unlocked.
         */
        private bool PortalDialerController_PerformActionServer(On.RoR2.PortalDialerController.orig_PerformActionServer orig, PortalDialerController self, byte[] sequence)
        {
            Log.LogDebug("PortalDialerController_PerformActionServer called.");
            if (CheckBlocked("artifactworld"))
            {
                // give a message so the user is aware the portal dialer interaction is blocked
                ChatMessage.SendColored($"The code will never work without Hidden Realm: Bulwark's Ambry.", Color.white);
                return false;
            }
            return orig(self, sequence);
        }

        /**
         * Block going to A Monument, Whole if the environment is not unlocked.
         */
        private void TransitionToNextStage_FixedUpdate(On.EntityStates.Interactables.MSObelisk.TransitionToNextStage.orig_FixedUpdate orig, EntityStates.Interactables.MSObelisk.TransitionToNextStage self)
        {
            // If the player decides to commit to Obliterating,
            //  they transition state should simply end the game normally
            //  (since the player should not be allowed into limbo).
            if (CheckBlocked("limbo"))
            {
                // run normal obliterate ending
                Run.instance.BeginGameOver(RoR2Content.GameEndings.ObliterationEnding);
                self.outer.SetNextState(new EntityStates.Idle());
            }
            orig(self);
        }

        /**
         * Give a warning before attempting to Obliterate while A Monument, Whole is still blocked.
         */
        private void ReadyToEndGame_OnEnter(On.EntityStates.Interactables.MSObelisk.ReadyToEndGame.orig_OnEnter orig, EntityStates.Interactables.MSObelisk.ReadyToEndGame self)
        {
            // Giving this warning is important for fairness.
            // This is because if the player decides to still Obliterate,
            //  we are just going to forcefully end the run.

            // Check if this is the server running this OnEnter, since mutliplayer clients could run this.
            // This is used to prevent duplicate messages being sent in multiplayer.
            if (NetworkServer.active && CheckBlocked("limbo"))
            {
                for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                {
                    if (CharacterMaster.readOnlyInstancesList[i].inventory.GetItemCountEffective(RoR2Content.Items.LunarTrinket) > 0)
                    {
                        ChatMessage.SendColored("Despite having Beads, you are not yet ready...", new Color(0x5d, 0xd5, 0xe2));
                        break;
                    }
                }
            }
            orig(self);
        }

        /**
         * Block shop interation with Bazaar Seers for environments that are blocked.
         */
        private void SeerStationController_SetTargetScene(On.RoR2.SeerStationController.orig_SetTargetScene orig, SeerStationController self, SceneDef sceneDef)
        {
            // For the seers, we will not change their behavior for how they pick environments.
            // This behaviour could be changed but would require changing logic in the middle of SetUpSeerStations() which would take IL Hooks.
            // This has the consequence that seers can pick environments that are blocked.
            // In that case, we can just block the seer be able to be interacted with.
            // We also should hide the destination of the Seer since the it will not be reenabled when the player obtains the environment.

            string sceneName = sceneDef.cachedName;
            if (CheckBlocked(sceneName))
            {
                self.GetComponent<PurchaseInteraction>().SetAvailable(false);
                Log.LogDebug($"Bazaar Seer attempted to pick scene {sceneName}; blocked.");
                return;
            } else
            {
                Log.LogDebug($"Bazaar Seer picked scene {sceneName}");
            }
            orig(self, sceneDef);
        }

        private void SceneDirector_PlaceTeleporter(On.RoR2.SceneDirector.orig_PlaceTeleporter orig, SceneDirector self)
        {
            orig(self);
            seerPortal = null;
            seerPortal = new SeerPortal();
            seerPortal.Initialize();
        }

        /**
         * Block portals for blocked environments that would be spawned by the finishing teleporter event.
         */
        private void TeleporterInteraction_AttemptToSpawnAllEligiblePortals1(On.RoR2.TeleporterInteraction.orig_AttemptToSpawnAllEligiblePortals orig, TeleporterInteraction self)
        {
            // If the player unlocks the environments while they have orbs, they can still recieved the portals.
            // But as soon as the teleporter finishes, we will not give them the portals.
            // There could be a more friendly alternative but this should be fine.

            // the portals spawned by the teleporter event are for:
            // Hidden Realm: Bazaar Between Time
            // Hidden Realm: Gilded Coast
            // Hidden Realm: A Moment, Fractured
            GetAvailableStages();
            if (CheckBlocked("bazaar"))
            {
                if (self.shouldAttemptToSpawnShopPortal)
                {
                    Log.LogDebug("Blue / bazaar portal blocked.");
                    ChatMessage.Send("The blue portal was too shy to come out!");
                }
                self.shouldAttemptToSpawnShopPortal = false;
            }
            if (CheckBlocked("goldshores"))
            {
                if (self.shouldAttemptToSpawnGoldshoresPortal)
                {
                    Log.LogDebug("Gold / goldshores portal blocked.");
                    ChatMessage.Send("The gold portal was missing the key to enter and disappeared!");
                }
                self.shouldAttemptToSpawnGoldshoresPortal = false;
            }
            if (CheckBlocked("mysteryspace"))
            {
                if (self.shouldAttemptToSpawnMSPortal)
                {
                    Log.LogDebug("Celestial / mysteryspace portal blocked.");
                    ChatMessage.Send("The celestial portal decided you aren't ready!");
                }
                self.shouldAttemptToSpawnMSPortal = false;
            }
            if (CheckBlocked("conduitcanyon"))
            {
                if (self.shouldAttemptToSpawnHiddenRealmsPortal)
                {
                    Log.LogDebug("Conduit / conduitcanyon portal blocked.");
                    ChatMessage.Send("The conduit portal failed to decypher the access code!");
                }
                self.shouldAttemptToSpawnHiddenRealmsPortal = false;
            }
            orig(self);
        }

        private void PortalSpawner_Start(On.RoR2.PortalSpawner.orig_Start orig, PortalSpawner self)
        {

            if (self.bannedEventFlag == "FalseSonBossComplete")
            {
                self.bannedEventFlag = ""; // this prevents the colossus portal from being blocked after false son has been defeated
            }
            orig(self);
        }
    }
}

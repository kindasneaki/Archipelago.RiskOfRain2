using R2API.Utils;
using RoR2;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class LocationHandler
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Shrine like objects

        // All shrines behave differently and there is no inheritance to a common shrine object
        // Therefore all shrine types will have to be handled differently.

        /// <summary>
        /// Call on beating a shrine. This accounts for the step in shrine uses and submits locations.
        /// </summary>
        /// <returns>Returns true if a location was submitted.</returns>
        private bool shrineBeat()
        {
            bool locationavailable = 0 < checkAvailable(LocationTypes.shrine);

            // only count when checks are avaiable OR when counting does not roll over
            if (locationavailable || 0 != (shrinesUsed + 1) % shrineUseStep)
            {
                shrinesUsed++;
                Log.LogDebug("shrine counted as towards the locations");
                updateBar(LocationTypes.shrine);
            }
            else
            {
                Log.LogDebug("shrine not counted as towards the locations");
            }

            // only send checks when rolling over
            if (locationavailable && 0 == shrinesUsed % shrineUseStep) return sendNextAvailable(LocationTypes.shrine);
            return false;
        }

        /// <summary>
        /// Determines whether the next shrineBeat() call will return true without calling it.
        /// </summary>
        /// <returns>Returns true if shrineBeat() would submit a location.</returns>
        private bool shrineWillBeLocation()
        {
            return (0 == (shrinesUsed + 1) % shrineUseStep) && (0 < checkAvailable(LocationTypes.shrine));
        }

        /// <summary>
        /// Beats the gold portal shrine when attempting to grant the portal entry.
        /// </summary>
        private void PortalStatueBehavior_GrantPortalEntry_Gold(On.RoR2.PortalStatueBehavior.orig_GrantPortalEntry orig, PortalStatueBehavior self)
        {
            orig(self);
            // using the gold shrine beats it; it already costs enough to use the shrine, so taking the portal away is just crule
            if (self.portalType == PortalStatueBehavior.PortalType.Goldshores) shrineBeat();
        }


        /// <summary>
        /// Using the blood shrine beats the shrine.
        /// </summary>
        private void ShrineBloodBehavior_AddShrineStack(On.RoR2.ShrineBloodBehavior.orig_AddShrineStack orig, ShrineBloodBehavior self, Interactor interactor)
        {
            Log.LogDebug("ShrineBloodBehavior_AddShrineStack"); // XXX remove after gold blocking is verified to not perma-block gold
            orig(self, interactor); // XXX somehow block the message about giving money
            // we call beat shrine after setting bloodshrineblockgold to false to let money be collected in case shrineBeat() causes an exception
            shrineBeat(); // using the blood shrine beats it
        }

        /// <summary>
        /// Blood shrine blocks the money that it will give if the shrine was used as a location.
        /// </summary>
        private void CharacterMaster_GiveMoney_uint(On.RoR2.CharacterMaster.orig_GiveMoney_uint orig, CharacterMaster self, uint amount)
        {
            if (!bloodshrineblockgold) orig(self, amount);
            else Log.LogDebug($"CharacterMaster_GiveMoney: Gold blocked because blood shrine."); // XXX
        }

        /// <summary>
        /// Beat the chance shrine when a successful purchase happens.
        /// </summary>
        private void ShrineChanceBehavior_AddShrineStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior self, Interactor activator)
        {
            Log.LogDebug("ShrineChanceBehavior_AddShrineStack"); // XXX remove after item blocking is verified to not perma-block items
            chanceshrineblockitem = shrineWillBeLocation();
            Log.LogDebug($"Intend to block item: {chanceshrineblockitem}"); // XXX
            chanceshrinebeat = false; // set the value to false, if it is set to true we know an item dropped because of the shrine
            orig(self, activator);
            Log.LogDebug($"Item drop detected: {chanceshrinebeat}"); // XXX
            chanceshrineblockitem = false;
            if (chanceshrinebeat) shrineBeat();
        }

        private void PickupDropletController_CreatePickupDroplet_ChanceShrine(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig, GenericPickupController.CreatePickupInfo pickupInfo, UnityEngine.Vector3 position, UnityEngine.Vector3 velocity)
        {
            // when an item dropplet is made, we will consider the shrine beat
            chanceshrinebeat = true;
            // Note, this will set the value to true even when the item is not from a shrine.
            // This is why the value needs to be set to false when the shrine intends to actually use the value and observe it.

            // check if the item being dropped is being asked to not drop
            if (chanceshrineblockitem)
            {
                Log.LogDebug($"chance shrine item {pickupInfo._pickupState} was used to satisfy a location and thus is consumed");
                return;
            }
            orig(pickupInfo, position, velocity);
        }

        /// <summary>
        /// Using the shcange shrine beats it.
        /// </summary>
        private void ShrineCombatBehavior_AddShrineStack(On.RoR2.ShrineCombatBehavior.orig_AddShrineStack orig, ShrineCombatBehavior self, Interactor interactor)
        {
            orig(self, interactor);
            // TODO maybe combat shrine shouldn't be an instant reward
            shrineBeat(); // using the combat shrine beats it
        }

        /// <summary>
        /// Using the order shrine beats it
        /// </summary>
        private void ShrineRestackBehavior_AddShrineStack(On.RoR2.ShrineRestackBehavior.orig_AddShrineStack orig, ShrineRestackBehavior self, Interactor interactor)
        {
            orig(self, interactor);
            shrineBeat(); // using the order shrine beats it
        }

        /// <summary>
        /// When the boss group is attempting to drop bonus rewards, the mountain shrines which granted the bonus are beat.
        /// </summary>
        private void BossGroup_DropRewards(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self)
        {
            Log.LogDebug($"bonusRewardCount initially: {self.bonusRewardCount}");
            for (int n = self.bonusRewardCount; n > 0; n--)
            {
                Log.LogDebug("bonusRewardCount means a mountain shrine was beat");
                // the only way to raise the bonusRewardCount of a boss is via a mountain shrine

                // beat the mountain shrine per mountain activated when the teleporter finishes
                if (shrineBeat()) self.bonusRewardCount--;
                // each location sent should mean one less bonus
            }
            Log.LogDebug($"bonusRewardCount adjusted: {self.bonusRewardCount}");

            orig(self);
        }

        /// <summary>
        /// Purchasing the each of the last two upgrades of the woods shrine beats the shrine.
        /// </summary>
        private void ShrineHealingBehavior_AddShrineStack(On.RoR2.ShrineHealingBehavior.orig_AddShrineStack orig, ShrineHealingBehavior self, Interactor activator)
        {
            orig(self, activator);
            // the last two purchases of woods shine are checks
            if (self.purchaseCount > self.maxPurchaseCount - 2)
            {
                shrineBeat();
                return;
            }

            if (currentlocations.TryGetValue(sceneIndex, out var locationsinenvironment))
            {
                Log.LogDebug($"amount of shrine locations left {locationsinenvironment[LocationTypes.shrine]}");
                if (locationsinenvironment[1] == 0) return;
            }
            if (self.purchaseCount == 1) ChatMessage.Send("Hmm thats weird, maybe try again");
        }

        private void ShrineColossusAccessBehavior_OnInteraction(On.RoR2.ShrineColossusAccessBehavior.orig_OnInteraction orig, ShrineColossusAccessBehavior self, Interactor interactor)
        {
            orig(self, interactor);
            shrineBeat();
        }

        /// <summary>
        /// Interacting with colossus shrine beats it.
        /// </summary>

        private void ShrineCombatTroopBehavior_AddShrineStack(On.RoR2.ShrineCombatTroopBehavior.orig_AddShrineStack orig, ShrineCombatTroopBehavior self, Interactor interactor)
        {
            orig(self, interactor);
            shrineBeat(); // using the combat shrine beats it
        }




        /// <summary>
        /// Interacting with the combat troop shrine (conduit canyon shrine) beats it.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}

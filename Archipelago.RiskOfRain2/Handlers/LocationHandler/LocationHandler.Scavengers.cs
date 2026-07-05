using RoR2;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class LocationHandler
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Scavenger

        // Scavengers will be counted by the number of bags opened.

        private void Opening_OnEnter(On.EntityStates.ScavBackpack.Opening.orig_OnEnter orig, EntityStates.ScavBackpack.Opening self)
        {
            orig(self);
            scavbackpackHash = self.chestBehavior.GetHashCode();
            scavbackpackWasLocation = sendNextAvailable(LocationTypes.scavenger);
        }

        private void ChestBehavior_ItemDrop_Scavenger(On.RoR2.ChestBehavior.orig_ItemDrop orig, ChestBehavior self)
        {
            // All chest like objects drop 1 item, this includes scavenger backpacks which just call this method several times.
            // Therefore we need to manually make sure the call here is from the backpack.
            if(NetworkServer.active && self.currentPickup != UniquePickup.none && scavbackpackHash == self.GetHashCode())
            {
                // TODO make an option to block scavenger backpacks from dropping items
                scavbackpackblockitem = scavbackpackWasLocation;
            }

            orig(self); // the original will end up calling PickupDropletController_CreatePickupDroplet as well as other things
            scavbackpackblockitem = false;
        }

        private void PickupDropletController_CreatePickupDroplet_Scavenger(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig, GenericPickupController.CreatePickupInfo pickupInfo, UnityEngine.Vector3 position, UnityEngine.Vector3 velocity)
        {
            // check if the item being dropped is being asked to not drop
            if (scavbackpackblockitem)
            {
                Log.LogDebug($"scavenger backpack was used as a location so this item will be consumed");
                return;
            }
            orig(pickupInfo, position, velocity);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}

using RoR2;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    partial class LocationHandler
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Newt Altars

        private void PortalStatueBehavior_GrantPortalEntry_Blue(On.RoR2.PortalStatueBehavior.orig_GrantPortalEntry orig, PortalStatueBehavior self)
        {
            orig(self);
            if (self.portalType != PortalStatueBehavior.PortalType.Shop)
            {
                return;
            } // the below code is only applied to blue portal, ie an altar was used


            Log.LogDebug("intercepted blue portal ie altar used; attempt to send check");
            if (false == sendNextAvailable(LocationTypes.newt_altar))
            {
                Log.LogDebug("no check performed; granting blue portal");
                // orig(self);
                return;
            }
            else Log.LogDebug("check performed; denying blue portal");

            // refund the lunar coin if the player who payed the coin is this client's player
            //interactor.GetComponent<NetworkUser>().AwardLunarCoins(1); // (only the server actually executes the contents of this method) // TODO give coin only to one person
            foreach (NetworkUser local in NetworkUser.readOnlyLocalPlayersList)
            {
                Log.LogDebug("Refunding coins...");
                local.AwardLunarCoins(1);
                // TODO This does in fact give more coins back in multiplayer since every player would get a coin.
                // I don't have a solution for this right now : ^)
            }

            // don't block the other newts, more than one newt in a stage is rare and if also rewards knowing where newts can spawn when you can find and get to two

            // don't run the original as we do not want to spawn the portal
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}

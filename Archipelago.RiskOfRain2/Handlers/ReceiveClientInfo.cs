using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using KinematicCharacterController;
using RoR2;
using UnityEngine;

namespace Archipelago.RiskOfRain2.Handlers
{
    class ReceiveClientInfo : IHandler
    {

        public static ArchipelagoLocationCheckProgressBarUI itemCheckBar;
        public static ArchipelagoLocationCheckProgressBarUI shrineCheckBar;

        public void Hook()
        {
            ArchipelagoTeleportClient.OnArchipelagoTeleportClient += ArchipelagoTeleportClient_OnArchipelagoTeleportClient;
            ArchipelagoClientLunarCoin.OnArchipelagoClientLunarCoin += ArchipelagoClientLunarCoin_OnArchipelagoClientLunarCoin;
            SyncLocationCheckProgress.OnLocationSynced += itemCheckBar.UpdateCheckProgress;
            SyncShrineCheckProgress.OnShrineSynced += shrineCheckBar.UpdateCheckProgress;
        }


        public void UnHook()
        {
            ArchipelagoTeleportClient.OnArchipelagoTeleportClient -= ArchipelagoTeleportClient_OnArchipelagoTeleportClient;
            ArchipelagoClientLunarCoin.OnArchipelagoClientLunarCoin -= ArchipelagoClientLunarCoin_OnArchipelagoClientLunarCoin;
            SyncLocationCheckProgress.OnLocationSynced -= itemCheckBar.UpdateCheckProgress;
            SyncShrineCheckProgress.OnShrineSynced -= shrineCheckBar.UpdateCheckProgress;

        }

        private void ArchipelagoTeleportClient_OnArchipelagoTeleportClient()
        {
            foreach (NetworkUser local in NetworkUser.readOnlyLocalPlayersList)
            {
                if (local)
                {
                    SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
                    spawnCard = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscBarrel1");

                    Xoroshiro128Plus xoroshiro128PlusRadioScanner = new Xoroshiro128Plus(RoR2Application.rng);
                    if (DirectorCore.instance != null)
                    {
                        var card = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(spawnCard, new DirectorPlacementRule
                        {
                            placementMode = DirectorPlacementRule.PlacementMode.Random
                        }, xoroshiro128PlusRadioScanner));
                        var position = card.transform.position;
                        var directorPlacement = new DirectorPlacementRule
                        {
                            placementMode = DirectorPlacementRule.PlacementMode.Random,
                            minDistance = 5f,
                            maxDistance = 20f,
                        };
                        Log.LogDebug($"directorPlacemnet {directorPlacement.targetPosition} card position {position + new Vector3(0, 10, 0)}");
                        var body = local.master.GetBody();
                        body.GetComponentInChildren<KinematicCharacterMotor>().SetPosition(position + new Vector3(0, 10, 0));
                    }
                }
            }
        }

        private void ArchipelagoClientLunarCoin_OnArchipelagoClientLunarCoin()
        {
            foreach (NetworkUser local in NetworkUser.readOnlyLocalPlayersList)
            {
                if (local)
                {
                    Log.LogDebug("Refunding coins...");
                    local.AwardLunarCoins(1);
                    Chat.AddPickupMessage(local.master.GetBody(), "Lunar Coin", Color.blue, 1);
                }
            }
        }
    }
}

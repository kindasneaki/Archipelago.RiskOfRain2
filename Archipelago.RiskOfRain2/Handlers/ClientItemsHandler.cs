using Archipelago.RiskOfRain2.Net;
using Archipelago.RiskOfRain2.UI;
using KinematicCharacterController;
using RoR2;
using UnityEngine;

namespace Archipelago.RiskOfRain2.Handlers
{
    class ClientItemsHandler : IHandler
    {

        public static ArchipelagoLocationCheckProgressBarUI itemCheckBar;
        public static ArchipelagoLocationCheckProgressBarUI shrineCheckBar;

        public ClientItemsHandler()
        {
            itemCheckBar = new ArchipelagoLocationCheckProgressBarUI(new Vector2(-40, 0), Vector2.zero, "Item Check Progress:");
            shrineCheckBar = new ArchipelagoLocationCheckProgressBarUI(new Vector2(0, 170), new Vector2(50, -50), "Shrine Check Progress:");
        }
        public void Hook()
        {
            Log.LogDebug("Client Items Started");
            SyncLocationCheckProgress.OnLocationSynced += itemCheckBar.UpdateCheckProgress;
            ArchipelagoStartExplore.OnArchipelagoStartExplore += ArchipelagoStartExplore_OnArchipelagoStartExplore;
            ArchipelagoStartClassic.OnArchipelagoStartClassic += ArchipelagoStartClassic_OnArchipelagoStartClassic;
            ArchipelagoTeleportClient.OnArchipelagoTeleportClient += ArchipelagoTeleportClient_OnArchipelagoTeleportClient;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
        }

        public void UnHook()
        {
            ArchipelagoTeleportClient.OnArchipelagoTeleportClient -= ArchipelagoTeleportClient_OnArchipelagoTeleportClient;
            if (itemCheckBar != null)
            {
                itemCheckBar.Dispose();
            }
            if (shrineCheckBar != null)
            {
                shrineCheckBar.Dispose();
            }
            ArchipelagoStartExplore.OnArchipelagoStartExplore -= ArchipelagoStartExplore_OnArchipelagoStartExplore;
            Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;


        }

        private void ArchipelagoStartClassic_OnArchipelagoStartClassic()
        {
            Log.LogDebug("Client Classic Started");
            if (shrineCheckBar != null)
            {
                shrineCheckBar.Dispose();
            }
        }

        private void ArchipelagoStartExplore_OnArchipelagoStartExplore()
        {
            Log.LogDebug("Client Explore Started");
            SyncShrineCheckProgress.OnShrineSynced += shrineCheckBar.UpdateCheckProgress;


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
                        Log.LogDebug($"teleport position {position + new Vector3(0, 10, 0)}");
                        var body = local.master.GetBody();
                        body.GetComponentInChildren<KinematicCharacterMotor>().SetPosition(position + new Vector3(0, 10, 0));
                        card.SetActive(false);
                    }
                }
            }
        }

        private void Run_onRunDestroyGlobal(Run obj)
        {
            UnHook();
        }
    }
}

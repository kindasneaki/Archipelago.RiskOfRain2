using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    internal class SeerPortal
    {
        private GameObject portalPrefab;

        public void Initialize()
        {
            portalPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/bazaar/SeerStation.prefab").WaitForCompletion();
        }
        public void CreatePortal(List<SceneDef> sceneDef, float radius = 10f)
        {
            if (portalPrefab != null)
            {
                var teleporterMesh = UnityEngine.GameObject.Find("TeleporterBaseMesh");
                GameObject teleporterGameObject = teleporterMesh.transform.parent.gameObject;
                if (teleporterMesh == null)
                {
                    Log.LogWarning("TeleporterBaseMesh not found!");
                    return;
                }
                if (teleporterGameObject == null)
                {
                    Log.LogWarning("Teleporter not found!");
                    return;
                }
                var center = teleporterMesh.transform.position;

                for (int i = 0; i < sceneDef.Count; i++)
                {
                    SceneIndex scene = sceneDef[i].sceneDefIndex;
                    Log.LogDebug($"Creating seer portal for stage {sceneDef[i].cachedName}. Portal #{i}");

                    // Calculate position in a circle around the teleporter
                    float angle = i * Mathf.PI * 2f / sceneDef.Count;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    Vector3 spawnPos = center + offset;

                    // Instantiate and configure the portal
                    GameObject portal = GameObject.Instantiate(portalPrefab);
                    SeerStationController seerStation = portal.GetComponent<SeerStationController>();
                    PurchaseInteraction purchaseInteraction = portal.GetComponent<PurchaseInteraction>();

                    portal.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
                    portal.transform.position = spawnPos;
                    portal.transform.localScale = Vector3.one;

                    seerStation.PreStartClient();
                    seerStation.GetNetworkChannel();
                    seerStation.SetTargetScene(sceneDef[i]);
                    seerStation.explicitTargetSceneExitController = teleporterGameObject.GetComponent<SceneExitController>();

                    purchaseInteraction.Networkcost = 0;
                    purchaseInteraction.cost = 0;
                    purchaseInteraction.contextToken = sceneDef[i].cachedName;
                }
            }
        }

    }
}

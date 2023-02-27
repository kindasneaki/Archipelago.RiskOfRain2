using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    internal class SeerPortal : NetworkBehaviour
    {
        private GameObject portalPrefab;
        private PlayerCharacterMasterController[] PlayerCharCMasters { get => PlayerCharacterMasterController.instances.ToArray(); }

        private void Start()
        {
            Initialize();
            Log.LogDebug("Seer Portal Start");
        }

        private void Initialize()
        {
            portalPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/bazaar/SeerStation.prefab").WaitForCompletion();
            CreatePortal();
        }
        public void CreatePortal()
        {
            if (portalPrefab != null)
            {
                var position = PlayerCharCMasters[0].body.footPosition;
                var portal = GameObject.Instantiate(portalPrefab);
                portal.transform.localPosition = position;
                portal.transform.localScale = Vector3.one;
                portal.GetComponent<SeerStationController>().GetNetworkChannel();
            }

    }
}
}

using System;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class ArchipelagoTeleportClient : INetMessage
    {
        public static event Action OnArchipelagoTeleportClient;

        public void Deserialize(NetworkReader reader)
        {

        }

        public void OnReceived()
        {
            if (OnArchipelagoTeleportClient != null)
            {
                OnArchipelagoTeleportClient();
            }
        }

        public void Serialize(NetworkWriter writer)
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;
using Archipelago.RiskOfRain2.UI;

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

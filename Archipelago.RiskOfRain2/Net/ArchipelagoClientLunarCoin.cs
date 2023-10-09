using System;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class ArchipelagoClientLunarCoin : INetMessage
    {
        public static event Action OnArchipelagoClientLunarCoin;

        public void Deserialize(NetworkReader reader)
        {

        }

        public void OnReceived()
        {
            if (OnArchipelagoClientLunarCoin != null)
            {
                OnArchipelagoClientLunarCoin();
            }
        }

        public void Serialize(NetworkWriter writer)
        {

        }
    }
}

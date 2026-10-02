using System;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;
using Archipelago.RiskOfRain2.UI;

namespace Archipelago.RiskOfRain2.Net
{
    public class ArchipelagoStartMessage : INetMessage
    {
        public static event Action OnArchipelagoSessionStart;

        public void Deserialize(NetworkReader reader)
        {
            
        }

        public void OnReceived()
        {
            ArchipelagoTotalChecksObjectiveController.AddObjective();
            if (OnArchipelagoSessionStart != null)
            {
                OnArchipelagoSessionStart();
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            
        }
    }
}

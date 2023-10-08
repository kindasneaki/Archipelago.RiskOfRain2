using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class AllChecksCompleteInStage : INetMessage
    {
        public static event Action OnAllChecksCompleteInStage;

        public void Deserialize(NetworkReader reader)
        {
            
        }

        public void OnReceived()
        {
            ArchipelagoLocationsInEnvironmentController.RemoveObjective();
            if (OnAllChecksCompleteInStage != null)
            {
                OnAllChecksCompleteInStage();
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            
        }
    }
}

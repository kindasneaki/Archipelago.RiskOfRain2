using System;
using System.Collections.Generic;
using System.Text;
using Archipelago.RiskOfRain2.UI;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class NextStageObjectives : INetMessage
    {
        public static event Action OnNextStageObjectives;

        public void Deserialize(NetworkReader reader)
        {

        }

        public void OnReceived()
        {
            ArchipelagoLocationsInEnvironmentController.AddObjective();
            if (OnNextStageObjectives != null)
            {
                OnNextStageObjectives();
            }
        }

        public void Serialize(NetworkWriter writer)
        {

        }
    }
}
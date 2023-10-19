using System;
using System.Collections.Generic;
using System.Text;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;
using Archipelago.RiskOfRain2.UI;
using UnityEngine;

namespace Archipelago.RiskOfRain2.Net
{
    public class ArchipelagoStartClassic : INetMessage
    {
        public static event Action OnArchipelagoStartClassic;

        public void Deserialize(NetworkReader reader)
        {

        }

        public void OnReceived()
        {
            if (OnArchipelagoStartClassic != null)
            {
                OnArchipelagoStartClassic();
            }
        }

        public void Serialize(NetworkWriter writer)
        {

        }
    }
}

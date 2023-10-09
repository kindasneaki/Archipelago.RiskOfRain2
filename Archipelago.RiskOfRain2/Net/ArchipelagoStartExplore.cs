using System;
using System.Collections.Generic;
using System.Text;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;
using Archipelago.RiskOfRain2.UI;
using UnityEngine;

namespace Archipelago.RiskOfRain2.Net
{
    public class ArchipelagoStartExplore : INetMessage
    {
        public static event Action OnArchipelagoStartExplore;

        public void Deserialize(NetworkReader reader)
        {

        }

        public void OnReceived()
        {
            if (OnArchipelagoStartExplore != null)
            {
                OnArchipelagoStartExplore();
            }
        }

        public void Serialize(NetworkWriter writer)
        {

        }
    }
}

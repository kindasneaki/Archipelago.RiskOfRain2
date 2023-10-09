using System;
using System.Collections.Generic;
using System.Text;
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Net
{
    public class SyncShrineCheckProgress : INetMessage
    {
        public delegate void ShrineCheckSyncHandler(int count, int step);
        public static event ShrineCheckSyncHandler OnShrineSynced;

        int itemPickupCount;
        int itemPickupStep;

        public SyncShrineCheckProgress()
        {

        }

        public SyncShrineCheckProgress(int shrineCount, int shrinePickupStep)
        {
            itemPickupCount = shrineCount;
            itemPickupStep = shrinePickupStep;
        }

        public void Deserialize(NetworkReader reader)
        {
            itemPickupStep = reader.ReadInt32();
            itemPickupCount = reader.ReadInt32();
        }

        public void OnReceived()
        {
            if (OnShrineSynced != null)
            {
                OnShrineSynced(itemPickupCount, itemPickupStep);
            }
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(itemPickupStep);
            writer.Write(itemPickupCount);
        }
    }
}

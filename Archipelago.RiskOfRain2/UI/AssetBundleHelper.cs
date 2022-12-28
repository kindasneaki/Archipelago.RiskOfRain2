using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Archipelago.RiskOfRain2.UI
{
    internal class AssetBundleHelper
    {
        public static AssetBundle localAssetBundle { get; private set; }
        internal static void LoadBundle()
        {
            localAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ArchipelagoPlugin.Instance.Info.Location), "connectbundle"));
            if (localAssetBundle == null)
            {
                Debug.LogError("Failed to load AssetBundle!");
                return;
            }
        }
        internal static GameObject LoadPrefab(string name)
        {
            return localAssetBundle?.LoadAsset<GameObject>(name);
        }
    }
}

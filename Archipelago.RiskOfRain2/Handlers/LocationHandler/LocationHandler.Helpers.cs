
namespace Archipelago.RiskOfRain2.Handlers
{
    partial class LocationHandler
    {
        public int? GetSceneTotalLocations(string sceneName)
        {
            CatchUpSceneLocations(sceneName);
            int index = GetSceneIndex(sceneName);
            Log.LogDebug($"GetSceneTotalLocations: {sceneName} -> {index} -> total {currentlocations[index].total()}");
            if (currentlocations[index] == null )
            {
                return null;
            }
            return currentlocations[index].total();
        }
    }
}

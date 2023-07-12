using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using R2API;
using R2API.Utils;
using RoR2;

namespace Archipelago.RiskOfRain2.Handlers;

public class ScavengerSpawnBoosterHandler : IHandler
{
    private readonly LocationHandler _locationHandler;

    internal ScavengerSpawnBoosterHandler(LocationHandler locationHandler)
    {
        _locationHandler = locationHandler;
    }

    public void Hook()
    {
        DirectorAPI.MonsterActions += OnMonsterActions;
    }

    private void OnMonsterActions(DccsPool dccsPool, List<DirectorAPI.DirectorCardHolder> directorCardHolders,
        DirectorAPI.StageInfo stageInfo)
    {
        var stageInt = GetStageInt(stageInfo.stage);
        var info = _locationHandler.GetLocationsForEnvironment(stageInt)!;
        var scavengersRemaining = info[LocationHandler.LocationTypes.scavenger];
        Log.LogInfo($"OnMonsterActions, scavengers remaining : {scavengersRemaining}");
        if (scavengersRemaining > 0 && info.total() == scavengersRemaining)
        {
            var scavengerCardHolder = directorCardHolders
                .Single(s => s.IsMonster
                             && s.Card is { spawnCard: CharacterSpawnCard csc }
                             && csc.name.ToLower() == DirectorAPI.Helpers.MonsterNames.Scavenger.ToLower()
                );
            
            Log.LogInfo($"Card found : {scavengerCardHolder}");
            Log.LogInfo($"Original cost : {scavengerCardHolder.Card.spawnCard.directorCreditCost}");
            scavengerCardHolder.Card.spawnCard.directorCreditCost = 500;
            //todo restore cost when exiting game or changing level, this edit is permanent
            Log.LogInfo($"New cost : {scavengerCardHolder.Card.cost}");
        }
    }

    private int GetStageInt(DirectorAPI.Stage stageInfoStage)
    {
        return stageInfoStage switch
        {
            DirectorAPI.Stage.AphelianSanctuary => LocationHandler.ancientloft,
            DirectorAPI.Stage.DistantRoost => LocationHandler.blackbeach,
            DirectorAPI.Stage.AbyssalDepths => LocationHandler.dampcavesimple,
            DirectorAPI.Stage.WetlandAspect => LocationHandler.foggyswamp,
            DirectorAPI.Stage.RallypointDelta => LocationHandler.frozenwall,
            DirectorAPI.Stage.TitanicPlains => LocationHandler.golemplains,
            DirectorAPI.Stage.AbandonedAqueduct => LocationHandler.goolake,
            DirectorAPI.Stage.SunderedGrove => LocationHandler.rootjungle,
            DirectorAPI.Stage.SirensCall => LocationHandler.shipgraveyard,
            DirectorAPI.Stage.SkyMeadow => LocationHandler.skymeadow,
            DirectorAPI.Stage.SiphonedForest => LocationHandler.snowyforest,
            DirectorAPI.Stage.SulfurPools => LocationHandler.sulfurpools,
            DirectorAPI.Stage.ScorchedAcres => LocationHandler.wispgraveyard,
            _ => throw new ArgumentOutOfRangeException(nameof(stageInfoStage), stageInfoStage, null)
        };
    }

    public void UnHook()
    {
        DirectorAPI.MonsterActions += OnMonsterActions;
    }
}
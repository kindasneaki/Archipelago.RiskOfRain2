using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.RiskOfRain2;
using Archipelago.RiskOfRain2.Extensions;
using R2API.Utils;
using RoR2;
using RoR2.Artifacts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Archipelago.RiskOfRain2.Handlers
{
    internal class DeathLinkHandler : IHandler
    {
        private readonly DeathLinkService deathLink;
        private Thread thread;
        // TODO perhaps a more robust system to prevent cyclical deaths is probably necessary
        private bool recievedDeath = false; // used to prevent cyclical deaths
        private bool deathLinkActive = false;

        public DeathLinkHandler(DeathLinkService deathLink)
        {
            Log.LogDebug($"DeathLink handler constructor.");
            this.deathLink = deathLink;
        }
        public void Hook()
        {
            if (!deathLinkActive)
            {
                deathLink.OnDeathLinkReceived += DeathLink_OnDeathLinkReceived;
                On.RoR2.CharacterMaster.OnBodyDeath += CharacterMaster_OnBodyDeath;
                deathLinkActive = true;
            }
        }

        public void UnHook()
        {
            deathLink.OnDeathLinkReceived -= DeathLink_OnDeathLinkReceived;
            On.RoR2.CharacterMaster.OnBodyDeath -= CharacterMaster_OnBodyDeath;
            deathLinkActive = false;
        }

        private void CharacterMaster_OnBodyDeath(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody body)
        {
            // TODO for multiplayer, this needs to make sure the dying player belongs to this client as to prevent redundant deathlink signals
            if (PlayerCharacterMasterController.instances.Select(x => x.master).Contains(self))
            {
                //Single player does not send a Display name so we will use the slot name here.
                string playerName = "";
                if (self.playerCharacterMasterController.GetDisplayName() == "")
                {
                    playerName = ArchipelagoClient.connectedPlayerName;
                }
                else
                {
                    playerName = self.playerCharacterMasterController.GetDisplayName();
                }
                Log.LogDebug($"Player OnBodyDeath of {playerName}.");
                if (!recievedDeath) // if this client just recieved a death, don't send it cyclically
                {
                    recievedDeath = true;
                    DeathLink dl = new DeathLink(playerName, $"The planet rejected {playerName}"); // TODO send the cause of death
                    Log.LogDebug($"Deathlink sending. Source: {dl.Source} Cause: {dl.Cause} Timestamp: {dl.Timestamp}");
                    try
                    {
                        deathLink.SendDeathLink(dl);
                    }
                    // In some cases: the game will end, the archipelago socket will close, and then the player will die.
                    // The above attempt to send the deathlink will fail because the socket closed, causing the gameover screen to not show up.
                    // The simplest solution is to just give up rather than attempt to remedy the situation.
                    // From testing, it does seemed that all deaths would send deathlink appropriately.
                    catch (Archipelago.MultiClient.Net.Exceptions.ArchipelagoSocketClosedException)
                    {
                        Log.LogDebug("Deathlink failed to send because socket was closed.");
                    }
                }
                if(thread == null)
                {
                    thread = new Thread(() => Prevent_Deathlink_Thread());
                    thread.Start();
                }
                else
                {
                    if (!thread.IsAlive)
                    {
                        thread = new Thread(() => Prevent_Deathlink_Thread());
                        thread.Start();
                    }
                }
                
                
            }

            orig(self, body);
        }
        private void Prevent_Deathlink_Thread()
        {
            Thread.Sleep(10000);
            Log.LogDebug("It has been 10 seconds you can now die again!");
            recievedDeath = false;
        }

        private void DeathLink_OnDeathLinkReceived(DeathLink deathLink)
        {
            Log.LogDebug($"Deathlink received. Source: {deathLink.Source} Cause: {deathLink.Cause} Timestamp: {deathLink.Timestamp}");
            if (recievedDeath) // if this client just sent a death, don't recieve it cyclically
            {
                return;
            }
            recievedDeath = true;
            classicDeathLink(deathLink);
        }

        private void classicDeathLink(DeathLink dl)
        {
            Log.LogDebug("Running classic DeathLink");
            System.Collections.ObjectModel.ReadOnlyCollection<PlayerCharacterMasterController> players = PlayerCharacterMasterController.instances;
            ChatMessage.SendColored($"{dl.Source} died...", Color.red);
            // TODO it does not make sense for multiplayer to kill all players, each players client should suicide independently if deathlink is enabled
            foreach (PlayerCharacterMasterController player in players)
            {
                ChatMessage.SendColored($"...and so does {player.GetDisplayName()}.", Color.red);
                Log.LogDebug($"Selected player {player.GetDisplayName()} to die. NetID: {player.netId}");
                player.master.GetBody().healthComponent.Suicide(damageType: DamageType.VoidDeath);
            }
        }
    }
}

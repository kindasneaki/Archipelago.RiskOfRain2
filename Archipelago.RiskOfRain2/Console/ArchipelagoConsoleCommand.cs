using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;
using R2API.Utils;
using RoR2;

namespace Archipelago.RiskOfRain2.Console
{
    public static class ArchipelagoConsoleCommand
    {
        public delegate void ArchipelagoConsoleCommandHandler(string url, int port, string slot, string password);
        public delegate void OnArchipelagoDisconnectCommandHandler();
        public delegate void OnArchipelagoReconnectCommandHandler();
        public delegate void OnArchipelagoDeathLinkCommandHandler(bool link);
        public delegate void OnArchipelagoHighlightSatelliteCommandHandler(bool highlight);
        public delegate void OnArchipelagoFinalStageDeathCommandHandler(bool finalstage);
        public delegate void OnArchipelagoShowUnlockedStagesCommandHandler();

        public static event ArchipelagoConsoleCommandHandler OnArchipelagoCommandCalled;
        public static event OnArchipelagoDisconnectCommandHandler OnArchipelagoDisconnectCommandCalled;
        public static event OnArchipelagoDisconnectCommandHandler OnArchipelagoReconnectCommandCalled;
        public static event OnArchipelagoDeathLinkCommandHandler OnArchipelagoDeathLinkCommandCalled;
        public static event OnArchipelagoHighlightSatelliteCommandHandler OnArchipelagoHighlightSatelliteCommandCalled;
        public static event OnArchipelagoFinalStageDeathCommandHandler OnArchipelagoFinalStageDeathCommandCalled;
        public static event OnArchipelagoShowUnlockedStagesCommandHandler OnArchipelagoShowUnlockedStagesCommandCalled;

        [ConCommand(
    commandName = "archipelago_connect",
    flags = ConVarFlags.SenderMustBeServer,
    helpText = "Connects to Archipelago. Syntax: archipelago_connect <url> <port> <slot> [password]")]
        private static void ArchipelagoConCommand(ConCommandArgs args)
        {
            if (args.Count < 3 || args.Count > 4)
            {
                ChatMessage.Send("Invalid command. Correct Syntax: archipelago_connect <url> <port> <slot> [password]");
                return;
            }
            args.CheckArgumentCount(3);

            var url = args.GetArgString(0);
            var port = args.GetArgInt(1);
            var slot = args.GetArgString(2);
            string password = string.Empty;

            if (args.Count == 4)
            {
                password = args.GetArgString(3);
            }

            OnArchipelagoCommandCalled(url, port, slot, password);
        }

        [ConCommand(commandName = "archipelago_disconnect", flags = ConVarFlags.SenderMustBeServer, helpText = "Disconnects from Archipelago.")]
        private static void ArchipelagoDisconnect(ConCommandArgs args)
        {
            OnArchipelagoDisconnectCommandCalled();
        }
        
        [ConCommand(commandName = "archipelago_reconnect", flags = ConVarFlags.SenderMustBeServer, helpText = "Attemps to reconnect to Archipelago.")]
        private static void ArchipelagoReconnect(ConCommandArgs args)
        {
            OnArchipelagoReconnectCommandCalled();
        }

        [ConCommand(commandName = "archipelago_show_unlocked_stages", flags = ConVarFlags.SenderMustBeServer, helpText = "Shows the current stages unlocked")]
        private static void ArchipelagoShowUnlockedStages(ConCommandArgs args)
        {
            OnArchipelagoShowUnlockedStagesCommandCalled();
        }

        [ConCommand(commandName = "archipelago_deathlink", flags = ConVarFlags.SenderMustBeServer, helpText = "Change deathlink. Syntax archipelago_deathlink <true/false>.")]

        private static void ArchipelagoDeathlink(ConCommandArgs args)
        {
            if (args.Count > 1)
            {
                ChatMessage.Send("Only accepts one arguement!");
            }
            else if(args.GetArgString(0) == "true" || args.GetArgString(0) == "false")
            {
                bool link = Convert.ToBoolean(args.GetArgString(0));
                OnArchipelagoDeathLinkCommandCalled(link);
                ChatMessage.Send($"Deathlink is now set to {link}");
            }
            else
            {
                ChatMessage.Send("Invalid argument. Correct Syntax: archipelago_deathlink true/false");
            }
        }

        [ConCommand(commandName = "archipelago_final_stage_death", flags = ConVarFlags.SenderMustBeServer, helpText = "Change final stage death. Syntax archipelago_deathlink <true/false>.")]

        private static void ArchipelagoFinalStageDeath(ConCommandArgs args)
        {
            if (args.Count > 1)
            {
                ChatMessage.Send("Only accepts one arguement!");
            }
            else if (args.GetArgString(0) == "true" || args.GetArgString(0) == "false")
            {
                bool finalstage = Convert.ToBoolean(args.GetArgString(0));
                OnArchipelagoFinalStageDeathCommandCalled(finalstage);
                ChatMessage.Send($"FinalStageDeath is now set to {finalstage}");

            }
            else
            {
                ChatMessage.Send("Invalid argument. Correct Syntax: archipelago_final_stage_deat true/false");
            }
        }

        [ConCommand(commandName = "archipelago_highlight_satellite", flags =ConVarFlags.SenderMustBeServer, helpText = "Change to highlight the radar satellite <true/false>.")]
        private static void ArchipelagoHighlightSatellite(ConCommandArgs args)
        {
            if (args.Count > 1)
            {
                ChatMessage.Send("Only accepts one arguement!");
            }
            else if (args.GetArgString(0) == "true" || args.GetArgString(0) == "false")
            {
                bool highlight = Convert.ToBoolean(args.GetArgString(0));
                OnArchipelagoHighlightSatelliteCommandCalled(highlight);
                var radar = UnityEngine.GameObject.Find("RadarTower(Clone)");
                if (radar != null)
                {
                    radar.GetComponent<Highlight>().isOn = Convert.ToBoolean(args.GetArgString(0));
                }
                ChatMessage.Send($"Satellite Highlight is now set to {Convert.ToBoolean(args.GetArgString(0))}");
            }
            else
            {
                ChatMessage.Send("Invalid argument. Correct Syntax: archipelago_highlight_satellitek true/false");
            }
        }
    }
}

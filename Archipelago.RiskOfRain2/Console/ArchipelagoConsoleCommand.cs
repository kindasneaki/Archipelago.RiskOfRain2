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
        public delegate void OnArchipelagoDeathLinkCommandHandler(bool link);
        public delegate void OnArchipelagoHighlightSatelliteCommandHandler(bool highlight);
        public static event ArchipelagoConsoleCommandHandler OnArchipelagoCommandCalled;
        public static event OnArchipelagoDisconnectCommandHandler OnArchipelagoDisconnectCommandCalled;
        public static event OnArchipelagoDeathLinkCommandHandler OnArchipelagoDeathLinkCommandCalled;
        public static event OnArchipelagoHighlightSatelliteCommandHandler OnArchipelagoHighlightSatelliteCommandCalled;

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
            }
            else
            {
                ChatMessage.Send("Invalid argument. Correct Syntax: archipelago_deathlink true/false");
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

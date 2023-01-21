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
        public static event ArchipelagoConsoleCommandHandler OnArchipelagoCommandCalled;
        public static event OnArchipelagoDisconnectCommandHandler OnArchipelagoDisconnectCommandCalled;

        [ConCommand(
    commandName = "archipelago_connect",
    flags = ConVarFlags.SenderMustBeServer,
    helpText = "Connects to Archipelago. Syntax: archipelago <url> <port> <slot> [password]")]
        private static void ArchipelagoConCommand(ConCommandArgs args)
        {
            if (args.Count < 3 || args.Count > 4)
            {
                ChatMessage.Send("Invalid command. Correct Syntax: archipelago <url> <port> <slot> [password]");
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
    }
}

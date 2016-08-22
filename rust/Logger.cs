using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Logger", "Wulf/lukespragg", "1.1.1", ResourceId = 670)]
    [Description("Configurable logging of chat, commands, and more")]

    class Logger : RustPlugin
    {
        // Do NOT edit this file, instead edit Logger.json in oxide/config and Logger.en.json in the oxide/lang directory,
        // or create a new language file for another language using the 'en' file as a default.

        #region Initialization

        void Init()
        {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            LoadDefaultConfig();
            LoadDefaultMessages();

            if (!logChat) Unsubscribe("OnPlayerChat");
            if (!logCommands) Unsubscribe("OnServerCommand");
            if (!logConnections) Unsubscribe("OnPlayerInit");
            if (!logRespawns) Unsubscribe("OnPlayerRespawned");
        }

        #endregion

        #region Configuration

        List<object> exclusions;
        bool logChat;
        bool logCommands;
        bool logConnections;
        bool logRespawns;
        bool logToConsole;

        protected override void LoadDefaultConfig()
        {
            Config["Exclusions"] = exclusions = GetConfig("Exclusions", new List<object>
            {
                "/help", "/version", "chat.say", "craft.add", "craft.canceltask", "global.kill", "global.respawn",
                "global.respawn_sleepingbag", "global.status", "global.wakeup", "inventory.endloot"
            });
            Config["LogChat"] = logChat = GetConfig("LogChat", false);
            Config["LogCommands"] = logCommands = GetConfig("LogCommands", true);
            Config["LogConnections"] = logConnections = GetConfig("LogConnections", true);
            Config["LogRespawns"] = logRespawns = GetConfig("LogRespawns", false);
            Config["LogToConsole"] = logToConsole = GetConfig("LogToConsole", true);
            SaveConfig();
        }

        #endregion

        #region Localization

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChatCommand"] = "{0} ({1}) ran chat command: {2}",
                ["Connected"] = "{0} ({1}) connected from {2}",
                ["ConsoleCommand"] = "{0} ({1}) ran console command: {2} {3}",
                ["Respawned"] = "{0} ({1}) respawned at {2}"
            }, this);
        }

        #endregion

        #region Logging

        void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (player == null) return;

            var args = arg.GetString(0, "text");
            LogToFile("chat", $"{player.displayName} ({player.userID}): {args}");
        }

        void OnPlayerConnected(Network.Message packet)
        {
            var con = packet.connection;
            LogToFile("connections", Lang("Connected", null, con.username, con.userid, IpAddress(con.ipaddress)));
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            LogToFile("respawns", Lang("Respawned", null, player.displayName, player.userID, player.transform.position));
        }

        void OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.connection == null) return;

            var command = arg.cmd.namefull;
            var args = arg.GetString(0, "text");

            if (args.StartsWith("/") && !exclusions.Contains(args))
                LogToFile("commands", Lang("ChatCommand", null, arg.connection.username, arg.connection.userid, args));
            if (command != "chat.say" && !exclusions.Contains(command))
                LogToFile("commands", Lang("ConsoleCommand", null, arg.connection.username, arg.connection.userid, command, arg.ArgsStr));
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        static string IpAddress(string ip) => Regex.Replace(ip, @":{1}[0-9]{1}\d*", "");

        void LogToFile(string fileName, string text)
        {
            var dateTime = DateTime.Now.ToString("yyyy-MM-dd");
            ConVar.Server.Log($"oxide/logs/{Title.ToLower()}-{fileName}_{dateTime}.txt", text);
            if (logToConsole) Puts(text);
        }

        #endregion
    }
}
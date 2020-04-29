using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Inspirobot
{
    class Configuration
    {
        public string Token { get; set; } = "INSERT_TOKEN_HERE";
        public ulong ChannelId { get; set; } = 0;
        public ulong OwnerId { get; set; } = 0;
        public string CommandPrefix { get; set; } = "?";
        public Dictionary<ulong, ulong[]> UserToStickyRoles { get; set; } = new Dictionary<ulong, ulong[]>();
    }

    class ConfigParser
    {
        public static Configuration LoadConfig(string filePath)
        {
            //Check that file exists
            if (!File.Exists(filePath))
            {
                GenerateNewConfigFileAndExit(filePath);
            }

            string jsonText = File.ReadAllText(filePath);
            Configuration config = null;
            try
            {

                config = JsonConvert.DeserializeObject<Configuration>(jsonText);
            }
            catch (JsonException)
            {
                GenerateNewConfigFileAndExit(filePath);
            }

            //Save the config to fix any issues
            SaveConfig(filePath, config);

            return config;
        }

        private static void GenerateNewConfigFileAndExit(string filePath)
        {
            string jsonText = JsonConvert.SerializeObject(new Configuration());
            File.WriteAllText(filePath, jsonText);
            Environment.Exit(1);
        }

        public static void SaveConfig(string filePath, Configuration config)
        {
            string jsonText = JsonConvert.SerializeObject(config);
            File.WriteAllText(filePath, jsonText);
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using GeoPlayClientSDK.Internal.Models;
using Newtonsoft.Json;

namespace GeoPlayClientSDK.Internal.Core
{
    internal class ConfigLoader
    {
        private const string ConfigFilePath = "Assets/geoplay-config.json";

        public static bool ConfigFileExists()
        {
            return FindConfigFile() != null;
        }

        public async Task<GeoPlayConfig> LoadConfigAsync()
        {
            try
            {
                var configPath = FindConfigFile();
                if (string.IsNullOrEmpty(configPath))
                {
                    Console.WriteLine($"[SDK] {ConfigFilePath} not found");
                    return null;
                }

                Console.WriteLine($"[SDK] Loading config from: {configPath}");

                var json = await File.ReadAllTextAsync(configPath);
                json = json.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty).Trim();
                var configRoot = JsonConvert.DeserializeObject<GeoPlayConfigRoot>(json);

                if (configRoot == null)
                {
                    Console.WriteLine("[SDK] Failed to parse config file");
                    return null;
                }

                var config = configRoot.geoplay_config;

                if (config == null)
                {
                    Console.WriteLine("[SDK] Failed to parse config file");
                    return null;
                }

                ValidateConfig(config);
                Console.WriteLine("[SDK] Configuration loaded successfully");
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SDK] Failed to load configuration: {ex.Message}");
                return null;
            }
        }

        private static string FindConfigFile()
        {
            return ConfigFilePath;
        }

        private void ValidateConfig(GeoPlayConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.project_id))
                throw new ArgumentException("ProjectId is required");
            if (string.IsNullOrWhiteSpace(config.api_key))
                throw new ArgumentException("ApiKey is required");
            if (string.IsNullOrWhiteSpace(config.base_url))
                throw new ArgumentException("ApiBaseUrl is required");
        }
    }
}

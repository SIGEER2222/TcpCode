using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RxTcpServer
{
    public static class DeviceConfigLoader
    {
        private static readonly string ConfigFilePath = "config.json";
        private static readonly Dictionary<string, DeviceConfig> DeviceMap = new();

        static DeviceConfigLoader()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            if (!File.Exists(ConfigFilePath))
            {
                throw new FileNotFoundException($"设备配置文件未找到: {ConfigFilePath}");
            }

            var json = File.ReadAllText(ConfigFilePath);
            var devices = JsonSerializer.Deserialize<List<DeviceConfig>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            DeviceMap.Clear();
            foreach (var device in devices)
            {
                DeviceMap[device.IP] = device;
            }
        }

        public static DeviceConfig GetDeviceConfig(string ip)
        {
            return DeviceMap.TryGetValue(ip, out var config) ? config : null;
        }
    }
}

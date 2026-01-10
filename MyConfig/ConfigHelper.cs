using HandyControl.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;

namespace MyConfig
{
    public class ConfigHelper : IConfigService
    {
        public object RawJsonConfig => _configJson;
        private string _configPath;
        public Dictionary<string, object> Settings { get; private set; } = new Dictionary<string, object>();
        public JObject _configJson;
        private static readonly object _cfgLock = new();

        public void ConfigInit()
        {
            if (!File.Exists(_configPath))
                throw new FileNotFoundException("找不到配置文件: " + _configPath);

            var json = File.ReadAllText(_configPath);
            Settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            _configJson = JObject.Parse(File.ReadAllText(_configPath));
        }
        public bool SetConfig(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            lock (_cfgLock)
            {
                if (Settings == null || Settings.Count == 0) ConfigInit();

                if (!Settings.ContainsKey(key)) return false;          // 只改不增
                if (Settings[key] == value) return true;               // 无变化不写盘

                Settings[key] = value;

                var json = JsonConvert.SerializeObject(Settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_configPath, json, new System.Text.UTF8Encoding(false));
                return true;
            }
        }
        public void SaveConfig()
        {
            File.WriteAllText(_configPath, _configJson.ToString(Newtonsoft.Json.Formatting.Indented));
        }
        public ConfigHelper(string configPath = "config.json")
        {
            _configPath = configPath;
            ConfigInit();
        }
        public string GetValue(string? key)
        {
            if (key == null)
            {
                return "-";
            }
            if (Settings.TryGetValue(key, out var value))
                return value?.ToString();
            throw new KeyNotFoundException($"配置项中未找到键：{key}");
        }




    }
}

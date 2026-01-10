using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // 可以移除
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyConfig
{
    public class ConfigHelper : IConfigService
    {
        private string _configPath;
        // 1. Settings 是唯一的“真实数据源”
        public Dictionary<string, object> Settings { get; private set; } = new Dictionary<string, object>();
        private static readonly object _cfgLock = new();

        public ConfigHelper(string configPath = "config.json")
        {
            _configPath = configPath;
            ConfigInit();
        }

        public void ConfigInit()
        {
            if (!File.Exists(_configPath))
            {
                // 如果文件不存在，初始化为空或者写入默认值
                Settings = new Dictionary<string, object>();
                return;
            }

            var json = File.ReadAllText(_configPath);
            // 2. 只加载到 Settings 字典
            Settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }

        public bool SetConfig(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            lock (_cfgLock)
            {
                if (Settings == null) ConfigInit();

                // 3. SetConfig 只更新内存，不写文件（提高性能）
                // 如果键存在则更新，如果允许新增也可以直接赋值
                if (Settings.ContainsKey(key))
                {
                    Settings[key] = value;
                    return true;
                }
                // 如果想要支持新增配置项，取消下面的注释：
                // Settings[key] = value; 
                // return true;

                return false;
            }
        }

        public void SaveConfig()
        {
            lock (_cfgLock)
            {
                // 4. SaveConfig 统一负责将内存数据写入硬盘
                // 这样 TextDialog 循环调用 SetConfig 后，最后调一次 SaveConfig 即可正确保存
                var json = JsonConvert.SerializeObject(Settings, Newtonsoft.Json.Formatting.Indented);

                //System.Windows.MessageBox.Show("正在保存到: " + System.IO.Path.GetFullPath(_configPath));
                File.WriteAllText(_configPath, json, new UTF8Encoding(false));
            }
        }

        public string GetValue(string? key)
        {
            if (key == null) return "-";
            if (Settings.TryGetValue(key, out var value))
                return value?.ToString();
            // 这里根据需求决定是抛出异常还是返回默认值
            return string.Empty;
        }

        // 5. 实现 GetConfigItems，从 Settings 字典中读取，确保数据一致性
        public IEnumerable<ConfigItem> GetConfigItems(Func<string, bool> predicate)
        {
            if (Settings == null) ConfigInit();

            // 遍历字典，询问 predicate 是否需要这个 Key
            foreach (var kvp in Settings)
            {
                // predicate(kvp.Key) 执行调用者写的逻辑
                if (predicate(kvp.Key))
                {
                    yield return new ConfigItem
                    {
                        Key = kvp.Key,
                        Value = kvp.Value?.ToString()
                    };
                }
            }
        }

    }
}
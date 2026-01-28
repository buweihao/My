using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq; // 需要引用 System.Linq

namespace MyConfig
{
    public class ConfigHelper : IConfigService
    {
        // 存储所有配置文件的路径列表（有序）
        private List<string> _configPaths = new List<string>();

        // 记录每个 Key 属于哪个文件路径：Key -> FilePath
        private Dictionary<string, string> _keySourceMap = new Dictionary<string, string>();

        // 唯一的“真实数据源”（合并后的视图）
        public Dictionary<string, object> Settings { get; private set; } = new Dictionary<string, object>();

        private static readonly object _cfgLock = new();

        /// <summary>
        /// 构造函数支持传入一个或多个路径
        /// </summary>
        public ConfigHelper(params string[] configPaths)
        {
            if (configPaths == null || configPaths.Length == 0)
            {
                _configPaths.Add("config.json"); // 默认值
            }
            else
            {
                _configPaths.AddRange(configPaths);
            }
            ConfigInit();
        }

        public void ConfigInit()
        {
            Settings.Clear();
            _keySourceMap.Clear();

            lock (_cfgLock)
            {
                // 依次加载所有文件
                foreach (var path in _configPaths)
                {
                    if (!File.Exists(path)) continue; // 文件不存在则跳过，等待 Save 时可能创建

                    try
                    {
                        var json = File.ReadAllText(path);
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                        if (dict != null)
                        {
                            foreach (var kvp in dict)
                            {
                                // 1. 合并到主 Settings (后加载的会覆盖前面的)
                                Settings[kvp.Key] = kvp.Value;

                                // 2. 记录该 Key 的来源文件
                                _keySourceMap[kvp.Key] = path;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 可以在这里记录日志
                        System.Diagnostics.Debug.WriteLine($"加载配置文件失败: {path}, {ex.Message}");
                    }
                }
            }
        }

        public bool SetConfig(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            lock (_cfgLock)
            {
                if (Settings == null) ConfigInit();

                // 更新内存中的值
                Settings[key] = value;

                // 维护来源映射
                if (!_keySourceMap.ContainsKey(key))
                {
                    // 如果是全新的 Key，默认归属到第一个文件（主配置文件）
                    // 或者是你指定的某个特定文件
                    _keySourceMap[key] = _configPaths.FirstOrDefault() ?? "config.json";
                }

                return true;
            }
        }

        public void SaveConfig()
        {
            lock (_cfgLock)
            {
                // 1. 准备分桶：每个文件路径对应一个字典
                var fileDataMap = new Dictionary<string, Dictionary<string, object>>();

                // 确保所有注册的路径都有一个空字典（防止文件被置空或遗漏）
                foreach (var path in _configPaths)
                {
                    fileDataMap[path] = new Dictionary<string, object>();
                }

                // 2. 将 Settings 中的数据分配回各自的文件桶
                foreach (var kvp in Settings)
                {
                    string targetPath;

                    // 查找该 Key 应该存入哪个文件
                    if (_keySourceMap.TryGetValue(kvp.Key, out var sourcePath) && _configPaths.Contains(sourcePath))
                    {
                        targetPath = sourcePath;
                    }
                    else
                    {
                        // 如果找不到来源（异常情况），存入第一个文件
                        targetPath = _configPaths.FirstOrDefault();
                    }

                    if (targetPath != null)
                    {
                        fileDataMap[targetPath][kvp.Key] = kvp.Value;
                    }
                }

                // 3. 遍历分桶，执行物理写入
                foreach (var fileEntry in fileDataMap)
                {
                    var path = fileEntry.Key;
                    var data = fileEntry.Value;

                    try
                    {
                        var json = JsonConvert.SerializeObject(data, Formatting.Indented);

                        // 确保目录存在
                        var dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        File.WriteAllText(path, json, new UTF8Encoding(false));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"保存配置文件失败: {path}, {ex.Message}");
                    }
                }
            }
        }

        public string GetValue(string? key)
        {
            if (key == null) return "-";
            if (Settings.TryGetValue(key, out var value))
                return value?.ToString();
            return string.Empty;
        }

        public IEnumerable<ConfigItem> GetConfigItems(Func<string, bool> predicate)
        {
            if (Settings == null) ConfigInit();
            // 使用 ToList 或数组快照以避免集合修改异常，虽然 Settings 变动通常在主线程
            foreach (var kvp in Settings)
            {
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
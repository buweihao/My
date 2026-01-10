using System;
using System.Collections.Generic;
using System.Text;

namespace MyConfig
{
    public class ConfigItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public interface IConfigService
    {
        string GetValue(string? key);
        bool SetConfig(string key, string value);
        void SaveConfig();


        IEnumerable<ConfigItem> GetConfigItems(Func<string, bool> predicate);
    }
}

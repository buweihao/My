using System;
using System.Collections.Generic;
using System.Text;

namespace MyConfig
{
    public interface IConfigService
    {
        // 获取配置值
        string GetValue(string? key);

        // 设置配置值
        bool SetConfig(string key, string value);

        // 保存配置
        void SaveConfig();

        // 暴露原始数据供 UI 绑定使用 (或者你可以封装更高级的方法)
        Dictionary<string, object> Settings { get; }

        // 获取底层的 JObject，用于 TextDialog 的遍历逻辑
        object RawJsonConfig { get; }
    }
}

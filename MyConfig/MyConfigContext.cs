using System;
using System.Collections.Generic;
using System.Text;

namespace MyConfig
{
    /// <summary>
    /// 提供一键访问的全局上下文
    /// </summary>
    public static class MyConfigContext
    {
        private static IConfigService _service;
        private static IConfigWindowManager _manager;

        // 懒加载单例
        public static IConfigWindowManager DefaultManager
        {
            get
            {
                if (_manager == null)
                {
                    // 默认配置路径
                    _service = new ConfigHelper("config.json");
                    _manager = new ConfigWindowManager(_service);
                }
                return _manager;
            }
        }

        // 允许用户自定义初始化（如果需要更改路径）
        public static void Initialize(string configPath)
        {
            _service = new ConfigHelper(configPath);
            _manager = new ConfigWindowManager(_service);
        }
    }
}

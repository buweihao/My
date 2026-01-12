using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace MyConfig
{
    /// <summary>
    /// 提供一键访问的全局上下文
    /// </summary>
    public static class MyConfigContext
    {
        private static IConfigService _service;
        private static IConfigWindowManager _manager;
        // 1. [新增] 公开 ConfigService 供外部获取值
        public static IConfigService Service
        {
            get
            {
                if (_service == null)
                {
                    // 触发 DefaultManager 的初始化逻辑，确保 _service 被创建
                    var _ = DefaultManager;
                }
                return _service;
            }
        }
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
        // 2. 【核心】提供一个静态方法，供 C# 代码直接调用
        public static void ShowPanel()
        {
            DefaultManager.ShowManagementPanel();
        }

        // 3. 【核心】提供一个静态命令，供 XAML 直接绑定 (如 MenuItem)
        private static ICommand _openUICommand;
        public static ICommand OpenUICommand => _openUICommand ??= new RelayCommand(ShowPanel);
    }
}

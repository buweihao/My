using HandyControl.Controls;
using MyConfig.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Window = System.Windows.Window;

namespace MyConfig
{
    public interface IConfigWindowManager
    {
        void ShowManagementPanel();

        // 注册时传入逻辑
        void RegisterCategory(string displayName, Func<string, bool> filter);

        // 打开配置页时传入逻辑
        void ShowConfigDialog(Func<string, bool> filter);
    }

    public class ConfigItemModel
    {
        public string Name { get; set; }
        public Func<string, bool> Filter { get; set; }
    }

    public class ConfigWindowManager : IConfigWindowManager
    {
        private readonly IConfigService _configService;
        private readonly List<ConfigItemModel> _categories = new();

        // 【关键】必须作为类成员变量，而不是局部变量
        private Window _configWindow;
        private Frame _mainFrame;

        public ConfigWindowManager(IConfigService configService)
        {
            _configService = configService;
        }

        public void RegisterCategory(string displayName, Func<string, bool> filter)
        {
            _categories.Add(new ConfigItemModel
            {
                Name = displayName,
                Filter = filter
            });
        }

        public void ShowManagementPanel()
        {
            // 1. 如果窗口已存在，直接激活，不重复创建
            if (_configWindow != null && _configWindow.IsLoaded)
            {
                _configWindow.Activate();
                // 如果当前不在面板页，跳转回面板
                if (_mainFrame.Content is not ConfigDashboard)
                {
                    _mainFrame.Navigate(new ConfigDashboard(_categories, this));
                }
                return;
            }

            // 2. 初始化 Frame (关键：这里设置隐藏导航条)
            _mainFrame = new Frame
            {
                // 【重点】设置为 Hidden，去掉前进后退按钮，但保留导航能力
                NavigationUIVisibility = NavigationUIVisibility.Hidden,
                Background = null
            };

            // 3. 初始化 Window (赋值给类成员 _configWindow)
            _configWindow = new Window
            {
                Title = "配置管理面板",
                Content = _mainFrame, // 窗口内容必须是 Frame
                Width = 450,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize
            };

            // 监听关闭事件，清理引用
            _configWindow.Closed += (s, e) =>
            {
                _configWindow = null;
                _mainFrame = null;
            };

            // 4. 跳转到首页
            _mainFrame.Navigate(new ConfigDashboard(_categories, this));

            _configWindow.Show();
        }

        public void ShowConfigDialog(Func<string, bool> filter)
        {
            // 如果窗口意外关闭或未打开，先打开它
            if (_configWindow == null || _mainFrame == null)
            {
                ShowManagementPanel();
            }

            // 5. 在同一个窗口内跳转到配置页
            var dialogPage = new TextDialog(filter, _configService);
            _mainFrame.Navigate(dialogPage);
        }
    }
}

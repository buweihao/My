using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyConsole
{
    /// <summary>
    /// ConsoleButton.xaml 的交互逻辑
    /// </summary>
    public partial class ConsoleButton : UserControl
    {
        public ConsoleButton()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 点击时，只负责通知管理器显示窗口
            ConsoleManager.ShowConsole();
        }
    }
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // *** 启动日志拦截系统 ***
            ConsoleManager.Init();
        }
    }
}

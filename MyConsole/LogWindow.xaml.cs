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
using System.Windows.Shapes;

namespace MyConsole
{
    /// <summary>
    /// LogWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LogWindow : Window
    {
        public LogWindow()
        {
            InitializeComponent();
        }

        // 供外部调用的追加日志方法
        public void AppendLog(string message)
        {
            // 必须在 UI 线程操作
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 简单的防内存溢出：超过10000字符截断一半（实际工程建议用队列管理）
                if (TxtLog.Text.Length > 10000)
                {
                    TxtLog.Text = TxtLog.Text.Substring(5000);
                }

                TxtLog.AppendText(message);

                if (ChkAutoScroll.IsChecked == true)
                {
                    TxtLog.ScrollToEnd();
                }
            }));
        }

        // 暂停按钮逻辑
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            AppController.TogglePause();
            BtnPause.Content = AppController.IsPaused ? "▶ 继续运行" : "⏸ 暂停业务逻辑";
            BtnPause.Background = AppController.IsPaused ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.LightPink;

            // 打印一条系统日志
            Console.WriteLine(AppController.IsPaused ? ">>> 系统已暂停 <<<" : ">>> 系统继续运行 <<<");
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
        }

        // *** 关键：拦截关闭事件，改为隐藏 ***
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // 取消真正的关闭
            this.Hide();     // 只是隐藏
        }
    }
}

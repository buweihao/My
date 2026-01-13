using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyConsole
{
    public static class ConsoleManager
    {
        private static LogWindow _logWindow;
        private static ConsoleOutputRedirector _redirector;
        private static bool _isInitialized = false;

        public static void Init()
        {
            if (_isInitialized) return;

            // 1. 初始化重定向器
            _redirector = new ConsoleOutputRedirector(AppController.PauseEvent);

            // 2. 【关键修改】使用静态方法作为中转，而不是直接用 lambda 订阅
            // 这样无论 LogWindow 重建多少次，Redirector 只有一个订阅者
            _redirector.OnLogReceived += StaticLogHandler;

            // 3. 接管控制台
            Console.SetOut(_redirector);
            _isInitialized = true;
        }

        // 静态中转方法：只把消息发给当前活着的窗口
        private static void StaticLogHandler(string msg)
        {
            // 如果窗口存在且已经加载，就写入
            if (_logWindow != null)
            {
                // 这里的 AppendLog 内部已经有了 Dispatcher，所以直接调
                _logWindow.AppendLog(msg);
            }
        }

        public static void ShowConsole()
        {
            if (!_isInitialized) Init();

            // 如果窗口不存在，或者被意外关闭了（IsLoaded 为 false）
            if (_logWindow == null || !_logWindow.IsLoaded)
            {
                _logWindow = new LogWindow();
                // 注意：这里不再需要 += 订阅事件了，因为上面 Init 里的 StaticLogHandler 会自动找到这个新赋值的 _logWindow
            }

            _logWindow.Show();
            _logWindow.Activate();

            if (_logWindow.WindowState == System.Windows.WindowState.Minimized)
            {
                _logWindow.WindowState = System.Windows.WindowState.Normal;
            }
        }
    }
}

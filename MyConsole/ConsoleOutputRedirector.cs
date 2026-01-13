using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Windows;


namespace MyConsole
{

    public class ConsoleOutputRedirector : TextWriter
    {
        private TextWriter _originalConsoleOutput;
        public event Action<string> OnLogReceived;

        // 引用全局的暂停控制器
        private ManualResetEvent _pauseEvent;

        public ConsoleOutputRedirector(ManualResetEvent pauseEvent)
        {
            _originalConsoleOutput = Console.Out;
            _pauseEvent = pauseEvent;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string value)
        {
            // =========================================================
            // 核心修改：模拟控制台的阻塞行为
            // =========================================================

            // 1. 安全检查：绝对不能拦截 UI 线程，否则界面会死锁，按钮按不下去！
            if (Application.Current != null &&
                Thread.CurrentThread.ManagedThreadId != Application.Current.Dispatcher.Thread.ManagedThreadId)
            {
                // 如果处于暂停状态，后台线程会卡死在这一行，直到信号被 Set()
                _pauseEvent.WaitOne();
            }

            // 2. 原样输出到输出窗口（可选）
            _originalConsoleOutput.Write(value);

            // 3. 通知 UI 更新
            OnLogReceived?.Invoke(value);
        }

        public override void WriteLine(string value)
        {
            // 获取当前时间，格式为 10:30
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");

            // 拼接格式：[10:30] 你的日志内容
            // 注意：我们只在 WriteLine 时添加时间戳，这样如果你用 Console.Write 打印进度条（.....）不会被打断
            string finalMessage = $"[{timeStamp}] {value}{Environment.NewLine}";

            // 调用 Write 方法触发事件
            Write(finalMessage);
        }
    }
}

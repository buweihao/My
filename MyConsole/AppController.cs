using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace MyConsole
{


    public static class AppController
    {
        // true 表示有信号（通行），false 表示无信号（暂停）
        public static ManualResetEvent PauseEvent = new ManualResetEvent(true);
        public static bool IsPaused = false;

        public static void TogglePause()
        {
            if (IsPaused)
            {
                PauseEvent.Set(); // 绿灯：放行所有卡在 Write 里的线程
                IsPaused = false;
            }
            else
            {
                PauseEvent.Reset(); // 红灯：后续所有调用 Console.WriteLine 的线程都会停住
                IsPaused = true;
            }
        }
    }
}

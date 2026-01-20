using Serilog;
using Serilog.Events;
using System;
using System.Collections.Concurrent; // 必须引入，用于线程安全字典

namespace MyLog
{
    public static class LogHelper
    {
        // 使用 ConcurrentDictionary 替代 Dictionary，保证多线程安全
        private static readonly ConcurrentDictionary<string, ILogger> _loggers = new();

        public static SerilogLoggerService GetLogger(string name, string filePath)
        {
            // GetOrAdd 是原子操作，无需 lock
            var logger = _loggers.GetOrAdd(name, _ =>
            {
                return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    // Shared: true 允许不同进程或多个实例写入同一个日志文件
                    .WriteTo.File(filePath,
                        rollingInterval: RollingInterval.Day,
                        restrictedToMinimumLevel: LogEventLevel.Debug,
                        shared: true,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                    .CreateLogger();
            });

            return new SerilogLoggerService(logger);
        }

        // AOP 专用 Logger
        public static SerilogLoggerService GetAopLogger()
        {
            // 注意：这里路径建议放在 AppDomain.CurrentDomain.BaseDirectory 下，防止路径问题
            return GetLogger("AOPLogger", "logs/Aop.log");
        }

        public static void DisposeAll()
        {
            foreach (var logger in _loggers.Values)
            {
                (logger as IDisposable)?.Dispose();
            }
            _loggers.Clear();
        }
    }
}
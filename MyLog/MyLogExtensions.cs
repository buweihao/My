using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLog
{
    // 这是一个静态桥梁，用于连接 DI 世界和 Attribute 世界
    public static class AopLogManager
    {
        // Attribute 将从这里获取 Logger
        public static ILoggerService ServiceProvider { get; private set; }

        // 用于在 App 启动后初始化这个静态属性
        public static void SetLogger(ILoggerService logger)
        {
            ServiceProvider = logger;
        }
    }

    public static class MyLogExtensions
    {
        public static IServiceCollection AddMyLogService(this IServiceCollection services)
        {
            // 1. 配置 Serilog
            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/App.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console()
                .CreateLogger();

            // 2. 注册为单例
            services.AddSingleton<Serilog.ILogger>(serilogLogger);
            services.AddSingleton<ILoggerService, SerilogLoggerService>();

            return services;
        }
    }
}

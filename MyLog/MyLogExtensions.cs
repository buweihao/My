using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        // 配置项
        public static MyLogOptions Options { get; set; } = new MyLogOptions();

        // 用于在 App 启动后初始化这个静态属性
        public static void SetLogger(ILoggerService logger)
        {
            ServiceProvider = logger;
        }
    }

    public class MyLogOptions
    {
        /// <summary>
        /// 是否启用默认的 Serilog 注册。如果为 false，调用者需要自己注册 ILoggerService。
        /// </summary>
        public bool EnableDefaultSerilog { get; set; } = true;

        public Action<ILoggerService?, MethodBase, object[]> OnEntry { get; set; }
        public Action<ILoggerService?, MethodBase> OnExit { get; set; }
        public Action<ILoggerService?, MethodBase, object[], Exception> OnException { get; set; }

        public MyLogOptions()
        {
            // 默认实现
            OnEntry = (logger, method, args) => 
                logger?.Debug("--> Entering {MethodName} with args: {@Args}", method.Name, args);
            
            OnExit = (logger, method) => 
                logger?.Debug("<-- Exiting {MethodName}", method.Name);

            OnException = (logger, method, args, ex) => 
                logger?.Error(ex, "!! Exception in {MethodName} with args: {@Args}", method.Name, args);
        }
    }

    public static class MyLogExtensions
    {
        public static IServiceCollection AddMyLogService(this IServiceCollection services, Action<MyLogOptions> configure = null)
        {
            var options = new MyLogOptions();
            configure?.Invoke(options);
            AopLogManager.Options = options;

            // 如果不启用默认 Serilog，直接返回
            if (!options.EnableDefaultSerilog)
            {
                return services;
            }

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

            // 2. 注册为单例 (使用 TryAdd 避免覆盖用户可能的自定义注册，虽然 EnableDefaultSerilog 应该已经控制了)
            services.TryAddSingleton<Serilog.ILogger>(serilogLogger);
            services.TryAddSingleton<ILoggerService, SerilogLoggerService>();

            return services;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyLog
{
    public static class MyLogExtensions
    {
        public static IServiceCollection AddMyLogService(this IServiceCollection services)
        {
            // 1. 尝试注册默认配置（如果用户之前已经注册了 IMyLogConfig，这行会被忽略）
            services.TryAddSingleton<IMyLogConfig, DefaultLogConfig>();

            // 2. 注册 LogService，工厂方法中解析配置
            services.TryAddSingleton<ILoggerService>(sp =>
            {
                var configService = sp.GetRequiredService<IMyLogConfig>();
                var options = configService.Configure();

                var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Is(options.MinimumLevel);

                if (options.EnableFile)
                {
                    loggerConfig.WriteTo.File(options.FilePath,
                        rollingInterval: options.FileRollingInterval,
                        restrictedToMinimumLevel: options.MinimumLevel,
                        shared: true,
                        outputTemplate: options.OutputTemplate);
                }

                if (options.EnableConsole)
                {
                    loggerConfig.WriteTo.Console(restrictedToMinimumLevel: options.MinimumLevel);
                }

                var serilogLogger = loggerConfig.CreateLogger();
                return new SerilogLoggerService(serilogLogger);
            });

            // 兼容性保留 Serilog.ILogger 的直接注册 (可选)
            // services.TryAddSingleton<Serilog.ILogger>(sp => (sp.GetRequiredService<ILoggerService>() as SerilogLoggerService)...); 
            // 鉴于 ILoggerService 包装了 Serilog，如果需要直接用 Serilog，可以自行转换，或者这里也注册一下：
             services.TryAddSingleton<Serilog.ILogger>(sp => 
             {
                 // 这里有点绕，因为 SerilogLoggerService 里面是 private。
                 // 简单起见，我们在 SerilogLoggerService 公开 Logger 或者不强求直接注入 Serilog.ILogger。
                 // 为了保持清洁，暂时只暴露 ILoggerService。
                 return new LoggerConfiguration().CreateLogger(); // 占位，防止崩溃，建议用户只用 ILoggerService
             });

            return services;
        }
    }
}

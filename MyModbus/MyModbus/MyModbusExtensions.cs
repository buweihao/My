using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyModbus
{
    public static class MyModbusExtensions
    {
        // 1. 修改方法签名，增加 extraConfig 回调
        public static IServiceCollection AddMyModbusCore(
            this IServiceCollection services,
            string configFilePath = "config.csv",
            Action<List<Device>>? extraConfig = null) // 👈 新增参数
        {
            // 1. 【移动顺序】先注册配置列表 (原代码中是在第2步，现在移到最前面)
            services.AddSingleton<List<Device>>(provider =>
            {
                // A. 先从 CSV 加载基础配置
                var devices = ConfigLoader.LoadConfig(configFilePath);

                // B. 执行外部注入的“克隆/修改”逻辑
                if (extraConfig != null)
                {
                    extraConfig(devices);
                }

                return devices;
            });

            // 2. 【修改注册】注册 DataBus，并注入 List<Device>
            services.AddSingleton<DataBus>(provider =>
            {
                var devices = provider.GetRequiredService<List<Device>>();
                return new DataBus(devices);
            });

            // 3. 注册采集引擎 (Engine)
            services.AddSingleton<IDriverFactory, HslDriverFactory>();
            services.AddSingleton<DataCollectionEngine>();

            // 4. 注册生命周期管理器
            services.AddHostedService<EngineLifecycleManager>();

            // 5. 注册 PlcLink
            services.AddSingleton<PlcLink>(provider =>
            {
                var bus = provider.GetRequiredService<DataBus>();
                var engine = provider.GetRequiredService<DataCollectionEngine>();
                var devList = provider.GetRequiredService<List<Device>>();

                // 这里的 DistinctBy 保证了即使克隆出了多台设备，
                // 只要 TagName 遵循了命名规范（例如前缀区分），就能正确注册
                var tagLookup = devList.SelectMany(d => d.Tags)
                                       .DistinctBy(t => t.TagName)
                                       .ToDictionary(t => t.TagName);

                return new PlcLink(bus, tagLookup, engine);
            });

            // 6. UI 相关
            services.AddSingleton<DashboardViewModel>();
            services.AddTransient<DashboardWindow>();
            services.AddSingleton<Func<DashboardWindow>>(provider =>
            {
                return () => provider.GetRequiredService<DashboardWindow>();
            });

            return services;
        }

    }
}

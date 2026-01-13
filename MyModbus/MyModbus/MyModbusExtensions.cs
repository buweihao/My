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
        public static IServiceCollection AddMyModbusCore(this IServiceCollection services, string configFilePath = "config.csv")
        {
            // 1. 注册 DataBus
            services.AddSingleton<DataBus>();

            // 2. 注册配置列表
            services.AddSingleton<List<Device>>(provider =>
            {
                return ConfigLoader.LoadConfig(configFilePath);
            });

            // 3. 注册采集引擎 (Engine)
            //当有人要 IDriverFactory 时，给他 HslDriverFactory
            services.AddSingleton<IDriverFactory, HslDriverFactory>();
            services.AddSingleton<DataCollectionEngine>();

            // 4. 注册生命周期管理器 (自动 Start/Stop)
            services.AddHostedService<EngineLifecycleManager>();

            // 5. 注册 PlcLink
            services.AddSingleton<PlcLink>(provider =>
            {
                var bus = provider.GetRequiredService<DataBus>();
                var engine = provider.GetRequiredService<DataCollectionEngine>();
                var devList = provider.GetRequiredService<List<Device>>();

                var tagLookup = devList.SelectMany(d => d.Tags)
                                       .DistinctBy(t => t.TagName)
                                       .ToDictionary(t => t.TagName);

                return new PlcLink(bus, tagLookup, engine);
            });

            // 6. UI 相关注册
            services.AddSingleton<DashboardViewModel>();
            services.AddTransient<DashboardWindow>();

            // ✅ 注册一个工厂方法，让外部 ViewModel 可以轻松创建 DashboardWindow
            // 这样外部就不需要注入 IServiceProvider 了
            services.AddSingleton<Func<DashboardWindow>>(provider =>
            {
                return () => provider.GetRequiredService<DashboardWindow>();
            });

            return services;
        }
    }
}

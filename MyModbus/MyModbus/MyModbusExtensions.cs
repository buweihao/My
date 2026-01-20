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
            // 1. 注册 DataBus
            services.AddSingleton<DataBus>();

            // 2. 注册配置列表 (核心修改处)
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

        // =============================================================
        // ✨ 新增：Device 的深拷贝扩展方法
        // =============================================================
        /// <summary>
        /// 克隆一个设备，并替换其 ID 和 IP
        /// </summary>
        public static Device Clone(this Device template, string newDeviceId, string newIp, int? newPort = null)
        {
            var newDevice = new Device
            {
                DeviceId = newDeviceId,
                IpAddress = newIp,
                Port = newPort ?? template.Port, // 没传就用模板的端口
                Station = template.Station,
                Timeout = template.Timeout,
                IsActive = template.IsActive,
                ByteOrder = template.ByteOrder,
                IsStringReverse = template.IsStringReverse,
                Tags = new List<Tag>() // 创建新列表
            };

            // 深拷贝 Tags，防止引用同一对象
            foreach (var tag in template.Tags)
            {
                newDevice.Tags.Add(new Tag
                {
                    // 核心技巧：自动给 TagName 加上设备前缀，防止重名！
                    // 例如模板叫 "Speed", 新设备叫 "Module_01_Speed"
                    // 如果你的 TagName 已经包含了设备名，可以根据需求调整这里的逻辑
                    TagName = ReplacePrefix(tag.TagName, template.DeviceId, newDeviceId),

                    Description = tag.Description,
                    Address = tag.Address,
                    StartAddress = tag.StartAddress,
                    Length = tag.Length,
                    Area = tag.Area,
                    DataType = tag.DataType,
                    ScanRate = tag.ScanRate,
                    Scale = tag.Scale,
                    Offset = tag.Offset,
                    IsFavorite = tag.IsFavorite
                });
            }

            return newDevice;
        }

        private static string ReplacePrefix(string originalName, string oldPrefix, string newPrefix)
        {
            string bodyName = originalName;

            // 1. 剥离旧前缀 (如果存在)
            // 例如: "Template_Motor_Speed" (旧前缀 "Template") -> "_Motor_Speed"
            if (originalName.StartsWith(oldPrefix))
            {
                bodyName = originalName.Substring(oldPrefix.Length);
            }

            // 2. 清理开头的下划线 (关键步骤！)
            // 防止出现 "01_Unloader__Speed" (双下划线) 这种情况
            if (bodyName.StartsWith("_"))
            {
                bodyName = bodyName.Substring(1);
            }

            // 3. 统一拼接：{新ID}_{纯点位名}
            // 最终结果: "01_Unloader_Motor_Speed"
            return $"{newPrefix}_{bodyName}";
        }
    }
}

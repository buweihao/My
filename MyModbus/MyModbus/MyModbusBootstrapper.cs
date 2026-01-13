using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyModbus
{
    public static class ModbusApp
    {
        /// <summary>
        /// 一键创建并配置 Host
        /// </summary>
        /// <param name="configPath">CSV 配置文件路径</param>
        /// <param name="configureExtra">回调函数，用于注册宿主程序自己的窗口和ViewModel</param>
        public static IHost CreateHost(string configPath = "config.csv", Action<IServiceCollection>? configureExtra = null)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 1. 自动挂载 MyModbus 核心库
                    services.AddMyModbusCore(configPath);

                    // 2. 注册宿主程序自己的服务 (如果有)
                    configureExtra?.Invoke(services);
                })
                .Build();
        }
    }
}

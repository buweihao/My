using System.Configuration;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using MyModbus;
using My.Services;

namespace My
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 1. 定义主机
        public IHost Host { get; private set; } // 改为 public 属性

        public App()
        {
            // ✅ 一行代码搞定初始化
            Host = ModbusApp.CreateHost("Configs/config.csv", services =>
            {
                string jsonConfigPath = "Configs/custom_config.json";
                services.AddSingleton<IConfigService>(provider =>
                {
                    // 确保这里的 ConfigService 是你定义的那个类名
                    // 如果你原来的类叫 ConfigHelper，这里就 new ConfigHelper
                    return new ConfigService(jsonConfigPath);
                });


                services.AddSingleton<IModbusService, ModbusService>();
                // 注册你自己的主窗口和 ViewModel
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            });
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await Host.StartAsync(); // 🚀 引擎会自动在后台启动

            // 显示主窗口
            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await Host.StopAsync(); // 🛑 引擎会自动停止
            base.OnExit(e);
        }



    }


}

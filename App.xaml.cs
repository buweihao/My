using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using My.Services;
using MyModbus;
using SqlSugar;
using System.Configuration;
using System.Data;
using System.Windows;
using MyDatabase;
using MyLog;
// using My.Configs; // Removed
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
            Host = AppBootstrapper.CreateHost(services =>
            {
                //库内容

                string modbusConfigPath = "Configs/config.csv";
                services.AddMyModbusCore(modbusConfigPath); // 假设这是你的扩展方法

                string jsonConfigPath = "Configs/custom_config.json";
                services.AddSingleton<IConfigService>(provider =>
                {
                    return new ConfigService(jsonConfigPath);
                });

                var dbConfig = new ConnectionConfig
                {
                    ConnectionString = "DataSource=IndustrialData.db", // 👈 硬编码路径
                    DbType = SqlSugar.DbType.Sqlite,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute,
                    MoreSettings = new ConnMoreSettings { IsAutoRemoveDataCache = true }
                };
                services.AddMySqlSugarStore(dbConfig
                    , typeof(ProductionData)
                    , typeof(DeviceLog)
                );

                //调用者内容
                services.AddSingleton<IModbusService, ModbusService>();
                
                // 注册业务服务
                services.AddSingleton<IUserCalculationService, UserCalculationService>();

                // 3. 配置 Log
                // 将 IMyLogConfig 映射到已经注册的 IUserCalculationService 实例
                // 这样 MyLog 就会使用 UserCalculationService 中定义的配置
                services.AddSingleton<IMyLogConfig>(sp => sp.GetRequiredService<IUserCalculationService>());
                
                // 注册 MyLog 服务
                services.AddMyLogService();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            });


            var loggerService = Host.Services.GetRequiredService<ILoggerService>();
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

    public static class AppBootstrapper
    {
        /// <summary>
        /// 通用的 Host 创建器
        /// 不再强制依赖 config.csv，也不强制加载 MyModbusCore
        /// </summary>
        /// <param name="configureServices">回调函数，由上层决定注册哪些服务</param>
        public static IHost CreateHost(Action<IServiceCollection> configureServices)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 这里不再写死 services.AddMyModbusCore(...)
                    // 而是完全执行传入的委托，让调用者自己决定
                    configureServices?.Invoke(services);
                })
                .Build();
        }
    }
}

using My.Services;
using MyLog;
using MyModbus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace My
{
    public class MainViewModel
    {
        #region MyModbus
        // 用于界面直接绑定显示 (通用监控),当前端需要直接展示点位控件时才需要传入，一般不需要
        public PlcLink Plc { get; }

        // 用于业务控制 (核心逻辑)
        private readonly IModbusService _machine;

        private readonly Func<DashboardWindow> _dashboardFactory;

        public ICommand StartCommand { get; }
        public ICommand SetSpeedCommand { get; }
        public ICommand OpenDashboardCommand { get; }
        public ICommand RunCalculationCommand { get; }
        #endregion

        #region Log

        private readonly ILoggerService _logger;
        private readonly IUserCalculationService _calculationService;
        #endregion

        // ✅ 构造函数不再包含 DataBus 和 Engine
        public MainViewModel(PlcLink plc, 
            IModbusService machine, 
            Func<DashboardWindow> dashboardFactory, 
            IConfigService configService, 
            ILoggerService logger,
            IUserCalculationService calculationService)
        {
            #region MyModbus

            Plc = plc;
            _machine = machine;
            _calculationService = calculationService;
            _dashboardFactory = dashboardFactory;

            StartCommand = new RelayCommand(_ => _machine.StartMachine());
            OpenDashboardCommand = new RelayCommand(_ => OpenDashboard());
            // 演示业务逻辑调用和日志
            RunCalculationCommand = new RelayCommand(async _ => await RunCalculationDemo());
            // 订阅业务事件，而不是 Modbus 事件
            _machine.SpeedChanged += OnSpeedChanged;
            #endregion

            #region  Log
            _logger = logger;
            StartProduction();


            #endregion


        }
        // 纯 UI 逻辑：打开子窗口
        #region MyModbus
        private void OpenDashboard()
        {
            var win = _dashboardFactory();
            win.Owner = Application.Current.MainWindow; // 只有 ViewModel 层知道"主窗口"这类概念
            win.Show();
        }
        private void OnSpeedChanged(int newSpeed)
        {
            // 处理速度变化的 UI 逻辑
            Console.WriteLine($"当前转速: {newSpeed}");
        }
        #endregion


        #region  Log
        public void StartProduction()
        {
            _logger.Info("Business logic inside StartProduction...");
        }

        private async Task RunCalculationDemo()
        {
            _logger.Info("--- Demo Start ---");
            
            // 1. 同步调用
            var sum = _calculationService.Add(10, 20);
            _logger.Info($"Add Calculation Result: {sum}");

            // 2. 异步调用
            _logger.Info("Starting heavy calculation...");
            var result = await _calculationService.HeavyCalculationAsync(144);
            _logger.Info($"Heavy Calculation Result: {result}");

            // 3. 模拟异常
            try
            {
                _logger.Info("Simulating error...");
                _calculationService.SimulateError();
            }
            catch (Exception ex)
            {
                _logger.Warn("Caught exception in ViewModel: " + ex.Message);
            }

            _logger.Info("--- Demo End ---");
        }
        #endregion

    }
}

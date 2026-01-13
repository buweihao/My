using My.Services;
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
        #endregion



        // ✅ 构造函数不再包含 DataBus 和 Engine
        public MainViewModel(PlcLink plc, IModbusService machine, Func<DashboardWindow> dashboardFactory)
        {
            #region MyModbus

            Plc = plc;
            _machine = machine;
            _dashboardFactory = dashboardFactory;

            StartCommand = new RelayCommand(_ => _machine.StartMachine());
            OpenDashboardCommand = new RelayCommand(_ => OpenDashboard());
            // 订阅业务事件，而不是 Modbus 事件
            _machine.SpeedChanged += OnSpeedChanged;
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
    }
}

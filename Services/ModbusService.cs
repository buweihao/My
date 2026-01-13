using MyModbus;
using System;
using System.Collections.Generic;
using System.Text;

namespace My.Services
{
    //在这个服务里面将驱动语言转换为ViewModel中直接使用到的业务语言
    public interface IModbusService
    {
        // 事件：只暴露业务关心的变化，而不是抛出原始的 TagData
        event Action<int> SpeedChanged;
        event Action<bool> OnError;

        // 动作：业务语义的方法
        void StartMachine();
        void StopMachine();
        void SetSpeed(int rpm);

        // 状态查询
        bool IsRunning { get; }
    }
    public class ModbusService : IModbusService
    {
        private readonly DataCollectionEngine _engine;
        private readonly DataBus _bus;

        public event Action<int> SpeedChanged;
        public event Action<bool> OnError;

        public bool IsRunning { get; private set; }

        // 构造函数注入底层库
        public ModbusService(DataCollectionEngine engine, DataBus bus)
        {
            _engine = engine;
            _bus = bus;
            InitializeSubscriptions();
        }

        private void InitializeSubscriptions()
        {
            // 在这里处理复杂的订阅逻辑，过滤数据，转换类型
            _bus.Subscribe("Motor_Speed", data =>
            {
                if (data.IsQualityGood && data.Value is short speed)
                {
                    // 只有当数据有效时，才通知上层
                    SpeedChanged?.Invoke(speed);
                }
            });

            // 可以在这里处理报警逻辑
            _bus.OnDataChanged += data =>
            {
                if (!data.IsQualityGood) OnError?.Invoke(true);
            };
        }

        public void StartMachine()
        {
            // 封装具体的点位名称，ViewModel 不需要知道是 "System_Start" 还是 "M100"
            _engine.WriteTag("System_Start", true);
            IsRunning = true;
        }

        public void StopMachine()
        {
            _engine.WriteTag("System_Start", false);
            IsRunning = false;
        }

        public void SetSpeed(int rpm)
        {
            if (rpm < 0 || rpm > 3000) return; // 这里可以写保护逻辑
            _engine.WriteTag("Motor_Speed", (short)rpm);
        }
    }
}

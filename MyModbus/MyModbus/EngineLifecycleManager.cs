using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyModbus
{
    // 这是一个后台托管服务，专门负责在程序启动/关闭时操作 Engine
    internal class EngineLifecycleManager : IHostedService
    {
        private readonly DataCollectionEngine _engine;

        public EngineLifecycleManager(DataCollectionEngine engine)
        {
            _engine = engine;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _engine.Start(); // 程序启动 -> 自动开启采集
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _engine.Stop(); // 程序退出 -> 自动停止采集
            return Task.CompletedTask;
        }
    }
}

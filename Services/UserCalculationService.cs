using Microsoft.Extensions.DependencyInjection;
using MyLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace My.Services
{
    public interface IUserCalculationService : IMyLogConfig
    {
        int Add(int a, int b);
        Task<double> HeavyCalculationAsync(double input);
        void SimulateError();
    }

    public class UserCalculationService : IUserCalculationService
    {
        private ILoggerService _logger => _serviceProvider.GetRequiredService<ILoggerService>();
        private readonly IServiceProvider _serviceProvider;

        public UserCalculationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public MyLogOptions Configure()
        {
            return new MyLogOptions
            {
                MinimumLevel = Serilog.Events.LogEventLevel.Verbose, // 演示：针对此服务的配置
                EnableConsole = true,
                EnableFile = true,
                FilePath = "logs/ServiceDefined.log",
                OutputTemplate = "{Timestamp:HH:mm:ss} [Service] {Message:lj}{NewLine}{Exception}"
            };
        }

        public int Add(int a, int b)
        {
            _logger.Debug("Executing Add with parameters: a={A}, b={B}", a, b);
            var result = a + b;
            _logger.Debug("Add result: {Result}", result);
            return result;
        }

        public async Task<double> HeavyCalculationAsync(double input)
        {
            _logger.Info("Starting heavy calculation for input: {Input}", input);
            await Task.Delay(500); // 模拟耗时操作
            var result = Math.Sqrt(input) * 100;
            _logger.Info("Heavy calculation completed. Result: {Result}", result);
            return result;
        }

        public void SimulateError()
        {
            try
            {
                throw new InvalidOperationException("This is a simulated business error.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred in SimulateError");
                throw; // Rethrow to let caller handle if needed
            }
        }
    }
}

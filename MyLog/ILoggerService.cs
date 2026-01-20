using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLog
{
    /// <summary>
    /// 抽象日志接口，支持结构化日志模板
    /// </summary>
    public interface ILoggerService
    {
        // 对应 Serilog 的 Information(string messageTemplate, params object[] propertyValues)
        void Info(string messageTemplate, params object[] propertyValues);

        // 对应 Serilog 的 Debug
        void Debug(string messageTemplate, params object[] propertyValues);

        // 对应 Serilog 的 Warning
        void Warn(string messageTemplate, params object[] propertyValues);

        // 专门用于记录异常，通常异常对象放在第一个参数
        void Error(Exception exception, string messageTemplate, params object[] propertyValues);

        // 不带 Exception 的 Error（虽然少见，但为了兼容性可以保留）
        void Error(string messageTemplate, params object[] propertyValues);
    }

    /// <summary>
    /// Serilog 的具体实现包装
    /// </summary>
    public class SerilogLoggerService : ILoggerService
    {
        private readonly ILogger _logger;

        public SerilogLoggerService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Info(string messageTemplate, params object[] propertyValues)
        {
            _logger.Information(messageTemplate, propertyValues);
        }

        public void Debug(string messageTemplate, params object[] propertyValues)
        {
            _logger.Debug(messageTemplate, propertyValues);
        }

        public void Warn(string messageTemplate, params object[] propertyValues)
        {
            _logger.Warning(messageTemplate, propertyValues);
        }

        public void Error(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            _logger.Error(exception, messageTemplate, propertyValues);
        }

        public void Error(string messageTemplate, params object[] propertyValues)
        {
            _logger.Error(messageTemplate, propertyValues);
        }
    }
}

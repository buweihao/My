using Serilog.Events;
using Serilog;

namespace MyLog
{
    public class MyLogOptions
    {
        public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Debug;
        public bool EnableConsole { get; set; } = true;
        public bool EnableFile { get; set; } = true;
        public string FilePath { get; set; } = "logs/App.log";
        public RollingInterval FileRollingInterval { get; set; } = RollingInterval.Day;
        public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    }

    public interface IMyLogConfig
    {
        MyLogOptions Configure();
    }

    internal class DefaultLogConfig : IMyLogConfig
    {
        public MyLogOptions Configure()
        {
            return new MyLogOptions();
        }
    }
}

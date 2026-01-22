using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyModbus
{
    /// <summary>
    /// 【库核心】统一管理所有点位命名规则
    /// 任何层级都不允许私自拼接字符串，必须调用此类
    /// </summary>
    public static class ModbusKeyHelper
    {
        // 如果想改成分号 ":" 或斜杠 "/"，只改这就行
        public const string Separator = "_";

        /// <summary>
        /// 生成完整唯一的点位Key
        /// 场景：业务层订阅时调用
        /// </summary>
        /// <param name="deviceId">设备ID (如 "1")</param>
        /// <param name="group">分组/位置 (如 "IO", 可空)</param>
        /// <param name="name">具体字段名 (如 "FeedLift")</param>
        public static string Build(string deviceId, string group, string name)
        {
            if (string.IsNullOrEmpty(group))
            {
                return $"{deviceId}{Separator}{name}";
            }
            return $"{deviceId}{Separator}{group}{Separator}{name}";
        }

        /// <summary>
        /// 重定向前缀（用于克隆设备）
        /// 场景：将 "Template_IO_Run" 转换为 "1_IO_Run"
        /// </summary>
        public static string Reparent(string fullTagName, string oldDeviceId, string newDeviceId)
        {
            // 1. 生成旧的前缀
            string oldPrefix = $"{oldDeviceId}{Separator}";

            // 2. 剥离旧前缀，获取 "核心后缀" (比如 "IO_Run")
            string coreName = fullTagName;
            if (fullTagName.StartsWith(oldPrefix))
            {
                coreName = fullTagName.Substring(oldPrefix.Length);
            }

            // 3. 加上新前缀
            return $"{newDeviceId}{Separator}{coreName}";
        }
    }
}

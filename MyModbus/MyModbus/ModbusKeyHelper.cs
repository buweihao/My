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

        /// <summary>
        /// 【新增】构建模组化的设备ID
        /// 场景：在克隆设备时，生成如 "1_UpLoad" 这样的ID
        /// </summary>
        /// <param name="moduleId">模组编号 (如 "1")</param>
        /// <param name="originalDeviceId">原始设备名 (如 "UpLoad")</param>
        public static string BuildDeviceId(string moduleId, string originalDeviceId)
        {
            // 如果原来的名字里已经包含了模组前缀（防止重复添加），可以加个判断
            // 但通常直接拼接即可： 1_UpLoad
            return $"{moduleId}{Separator}{originalDeviceId}";
        }

        /// <summary>
        /// 【新增】生成带模组的三级Key
        /// 场景：直接获取 1号模组-上料机-启动按钮
        /// 结果：1_UpLoad_IO_Start
        /// </summary>
        public static string Build(string moduleId, string deviceId, string group, string name)
        {
            string compositeDevice = BuildDeviceId(moduleId, deviceId);
            return Build(compositeDevice, group, name);
        }
    }
}

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
        /// <summary>
        /// 从完整点位名中提取模组ID
        /// 原理：获取第一个分隔符前的字符串
        /// 示例：1_PLC_Peripheral_Status -> 返回 "1"
        /// </summary>
        public static string GetModuleId(string fullTagName)
        {
            if (string.IsNullOrEmpty(fullTagName)) return string.Empty;

            int idx = fullTagName.IndexOf(Separator);
            if (idx > 0)
            {
                return fullTagName.Substring(0, idx);
            }
            return string.Empty; // 没有分隔符，说明不是模组点位
        }
        /// <summary>
        /// 解析出原始的模板设备名（需要知道模组ID长度）
        /// 示例：("1_PLC_Peripheral", "1") -> 返回 "PLC_Peripheral"
        /// </summary>
        public static string GetOriginalDeviceId(string currentDeviceId, string moduleId)
        {
            string prefix = $"{moduleId}{Separator}";
            if (currentDeviceId.StartsWith(prefix))
            {
                return currentDeviceId.Substring(prefix.Length);
            }
            return currentDeviceId;
        }

        /// <summary>
        /// 【新增】获取兄弟点位名
        /// 场景：已知 "1_PLC_Flipper_Trigger"，想拿 "1_PLC_Flipper_FixtureCode"
        /// 原理：找到最后一个分隔符，保留前缀，替换后缀
        /// </summary>
        /// <param name="currentFullTagName">当前完整点位名</param>
        /// <param name="newSuffix">兄弟点位的CSV原始名 (如 "FixtureCode")</param>
        /// <returns>1_PLC_Flipper_FixtureCode</returns>
        public static string GetSibling(string currentFullTagName, string newSuffix)
        {
            if (string.IsNullOrEmpty(currentFullTagName)) return string.Empty;

            // 找到最后一个 "_" 的位置
            int lastSeparatorIndex = currentFullTagName.LastIndexOf(Separator);

            if (lastSeparatorIndex < 0) return newSuffix; // 防御性代码

            // 截取 "1_PLC_Flipper_"
            string prefix = currentFullTagName.Substring(0, lastSeparatorIndex + 1);

            // 拼接 "1_PLC_Flipper_" + "FixtureCode"
            return prefix + newSuffix;
        }

        /// <summary>
        /// 【新增】从完整点位名提取设备前缀
        /// 示例：1_PLC_Flipper_Trigger -> 1_PLC_Flipper
        /// </summary>
        public static string GetDeviceNameFromTag(string fullTagName)
        {
            if (string.IsNullOrEmpty(fullTagName)) return string.Empty;
            int lastSeparatorIndex = fullTagName.LastIndexOf(Separator);
            if (lastSeparatorIndex < 0) return fullTagName;
            return fullTagName.Substring(0, lastSeparatorIndex);
        }
    }
}

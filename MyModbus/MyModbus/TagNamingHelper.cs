using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicRegionNavigation.Helper
{
    /// <summary>
    /// 基础设施层命名助手：仅负责处理 [设备ID] 与 [点位名] 的拼接规则
    /// 没有任何业务逻辑
    /// </summary>
    public static class ModbusNamingHelper
    {
        // 可以在库级别定义分隔符，保证统一
        public const string Separator = "_";

        /// <summary>
        /// 生成完整的设备点位名
        /// 格式：DeviceID_BaseTagName
        /// </summary>
        public static string Format(string deviceId, string baseTagName)
        {
            if (string.IsNullOrEmpty(deviceId)) return baseTagName;
            return $"{deviceId}{Separator}{baseTagName}";
        }

        /// <summary>
        /// 从带前缀的全名中剥离设备ID，还原基础点位名
        /// 用于克隆逻辑
        /// </summary>
        public static string StripDeviceId(string fullTagName, string deviceId)
        {
            if (string.IsNullOrEmpty(fullTagName) || string.IsNullOrEmpty(deviceId))
                return fullTagName;

            string prefix = $"{deviceId}{Separator}";

            if (fullTagName.StartsWith(prefix))
            {
                return fullTagName.Substring(prefix.Length);
            }

            // 如果不匹配，原样返回，防止破坏数据
            return fullTagName;
        }
    }
}

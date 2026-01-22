using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyModbus
{
    #region  加载
    /// <summary>
    /// 设备模型：代表一个物理连接（如一台 PLC）
    /// </summary>
    public class Device
    {
        // --- 基础属性 ---
        public string DeviceId { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; } = 502;
        public int Station { get; set; } = 1;
        public int Timeout { get; set; } = 1000;
        public bool IsActive { get; set; } = true;

        // 数据字节序配置
        public DataFormat ByteOrder { get; set; } // 假设你有这个枚举，或者具体的 ByteOrder 属性

        // 点位列表
        public List<Tag> Tags { get; set; } = new List<Tag>();
        public bool IsStringReverse { get; set; } = false;

        /// <summary>
        /// 【核心逻辑】克隆当前设备为新模组
        /// 自动处理点位重命名
        /// </summary>
        /// <param name="newDeviceId">新模组ID (如 "1", "2")</param>
        /// <param name="newIp">新IP地址</param>
        /// <param name="newPort">新端口 (可选，不传则沿用)</param>
        /// <returns>全新的 Device 实例</returns>
        public Device CloneAsNew(string newDeviceId, string newIp, int? newPort = null)
        {
            // 1. 复制基础属性
            var newDevice = new Device
            {
                DeviceId = newDeviceId,
                IpAddress = newIp,
                Port = newPort ?? this.Port,
                Station = this.Station,
                Timeout = this.Timeout,
                IsActive = this.IsActive,
                ByteOrder = this.ByteOrder, // 确保所有属性都复制了
                Tags = new List<Tag>()        // 初始化新列表
            };

            // 2. 深拷贝 Tags 并重命名
            if (this.Tags != null)
            {
                foreach (var oldTag in this.Tags)
                {
                    // --- 关键点 ---
                    // 调用 Helper 进行改名，业务逻辑完全不需要介入
                    string newTagName = ModbusKeyHelper.Reparent(oldTag.TagName, this.DeviceId, newDeviceId);
                    var newTag = new Tag
                    {
                        TagName = newTagName,

                        // --- 复制物理寻址核心 ---
                        Address = oldTag.Address,
                        StartAddress = oldTag.StartAddress, // 必须复制解析后的起始地址
                        Length = oldTag.Length,
                        Area = oldTag.Area,                 // 必须复制存储区类型 (Coils/Registers)
                        DataType = oldTag.DataType,

                        // --- 复制基础标识 ---
                        Description = oldTag.Description,

                        // --- 复制采集策略 ---
                        ScanRate = oldTag.ScanRate,

                        // --- 复制数据处理 ---
                        Scale = oldTag.Scale,
                        Offset = oldTag.Offset,

                        // --- 复制用户偏好 ---
                        IsFavorite = oldTag.IsFavorite
                    };
                    newDevice.Tags.Add(newTag);
                }
            }
            return newDevice;
        }
    }
    /// <summary>
    /// 点位模型：最小采集单元
    /// </summary>
    public class Tag
    {
        // --- 基础标识 ---
        public string TagName { get; set; }         // 业务名称 (如 "Motor_Speed")
        public string Description { get; set; }     // 描述

        // --- 物理寻址核心 ---
        public string Address { get; set; }         // 原始地址字符串 (如 "D100", "M10")
        public int StartAddress { get; set; }       // 解析后的数字起始地址 (如 100)
        public int Length { get; set; } = 1;        // 读取长度 (Bool=1, Float=2, String=N)
        public StorageArea Area { get; set; }       // 存储区 (强制区分 Coils/Registers)
        public DataType DataType { get; set; }      // 数据类型

        // --- 采集策略 ---
        public int ScanRate { get; set; } = 1000;   // 扫描周期 (ms)，用于后续分组

        // --- 数据处理 ---
        public float Scale { get; set; } = 1.0f;    // 比例系数 (Raw * Scale + Offset)
        public float Offset { get; set; } = 0.0f;   // 偏移量

        public bool IsFavorite { get; set; } = false;

    }

    #endregion


    #region 编组
    /// <summary>
    /// 地址块：对应一个物理 Modbus 请求包
    /// </summary>
    public class AddressBlock
    {
        public int StartAddress { get; set; }   // 请求起始地址
        public int Length { get; set; }         // 请求长度 (Word数量 或 Coil数量)
        public StorageArea Area { get; set; }   // 存储区

        // 这个 Block 里的原始数据属于哪些 Tag？
        // 采集回来后，解析器会遍历这个列表进行分发
        public List<Tag> Tags { get; set; } = new List<Tag>();
    }

    /// <summary>
    /// 采集任务组：对应一个独立的线程或 Task
    /// </summary>
    public class PollGroup
    {
        public int ScanRate { get; set; }       // 扫描频率 (如 100ms)
        public List<AddressBlock> Blocks { get; set; } = new List<AddressBlock>();
    }

    #endregion

    #region 解析层
    /// <summary>
    /// 解析后的单点数据
    /// </summary>
    public struct TagData
    {
        public string TagName { get; set; }
        public object Value { get; set; }     // 解析后的真实值 (float, bool...)
        public DateTime Timestamp { get; set; }
        public bool IsQualityGood { get; set; } // 通信质量标记
    }



    #endregion
}

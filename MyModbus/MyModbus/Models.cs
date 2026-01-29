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
        /// 【新增】将此设备作为模板，克隆到指定模组
        /// </summary>
        /// <param name="moduleId">模组ID (如 1, 2)</param>
        /// <param name="newIp">新设备的IP地址</param>
        /// <returns>克隆后的新设备</returns>
        public Device CloneToModule(int moduleId, string newIp)
        {
            return CloneToModule(moduleId.ToString(), newIp);
        }

        public Device CloneToModule(string moduleId, string newIp)
        {
            // 1. 严格使用 Helper 生成新 ID，禁止私自拼接字符串！
            // 结果示例: "UpLoad" + "1" -> "1_UpLoad"
            string newDeviceId = ModbusKeyHelper.BuildDeviceId(moduleId, this.DeviceId);

            // 2. 调用底层的克隆逻辑 (复用你现有的 CloneAsNew)
            return this.CloneAsNew(newDeviceId, newIp);
        }


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
            // 1. 复制设备级属性
            var newDevice = new Device
            {
                DeviceId = newDeviceId,
                IpAddress = newIp,
                Port = newPort ?? this.Port,
                Station = this.Station,
                Timeout = this.Timeout,
                IsActive = this.IsActive,
                ByteOrder = this.ByteOrder,
                Tags = new List<Tag>()
            };

            // 2. 遍历并深拷贝每一个 Tag
            if (this.Tags != null)
            {
                foreach (var tag in this.Tags)
                {
                    var newTag = new Tag
                    {
                        // --- 关键：重命名点位名 ---
                        TagName = ModbusKeyHelper.Reparent(tag.TagName, this.DeviceId, newDeviceId),
                        Description = tag.Description,

                        // --- 物理地址与类型复制 ---
                        Address = tag.Address,
                        StartAddress = tag.StartAddress,
                        Length = tag.Length,
                        Area = tag.Area,
                        DataType = tag.DataType,

                        // --- 策略与处理复制 ---
                        ScanRate = tag.ScanRate,
                        Scale = tag.Scale,
                        Offset = tag.Offset,
                        IsFavorite = tag.IsFavorite
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

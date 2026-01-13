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
        public string DeviceId { get; set; }        // 设备唯一标识 (如 "PLC_01")
        public string IpAddress { get; set; }       // IP地址
        public int Port { get; set; } = 502;        // 端口
        public int Station { get; set; } = 1;       // 站号
        public int Timeout { get; set; } = 1000;    // 超时时间
        public bool IsActive { get; set; } = true;  // 启用/禁用开关

        // 这里持有该设备下所有的点位配置
        public List<Tag> Tags { get; set; } = new List<Tag>();

        // 预留给运行时使用的驱动对象（暂不实例化）
        // public IDriver Driver { get; set; } 

        /// <summary>
        /// 数值类型的字节序 (默认 ABCD)
        /// 控制 Int32, Float, Double 等多字节数值
        /// </summary>
        public DataFormat ByteOrder { get; set; } = DataFormat.ABCD;

        /// <summary>
        /// 字符串是否需要字节反转 (即：IsStringReverseByteWord)
        /// 控制 String 类型。True 表示每两个字节互换位置。
        /// </summary>
        public bool IsStringReverse { get; set; } = false;
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

        // --- 运行时数据 (之后步骤会用到) ---
        // public object CurrentValue { get; set; }
        // public DateTime LastUpdateTime { get; set; }
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

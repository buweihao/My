using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyModbus
{
    /// <summary>
    /// 数据类型：决定了后续解析层读取几个字节，以及如何转换
    /// </summary>
    public enum DataType
    {
        Bool,       // 1 Bit
        Byte,       // 8 Bit
        Int16,      // 16 Bit (1 Register)
        UInt16,     // 16 Bit
        Int32,      // 32 Bit (2 Registers)
        UInt32,     // 32 Bit
        Double,     // 【新增】64 Bit (Double) - 占 4 个寄存器
        Float,      // 32 Bit
        String      // Variable Length
    }

    /// <summary>
    /// 存储区域：核心枚举，用于物理隔离 Bool 和 寄存器 请求
    /// </summary>
    public enum StorageArea
    {
        // 对应功能码 01/02，位操作区
        Coils,

        // 对应功能码 03/04，字操作区
        Registers
    }

    /// <summary>
    /// 数据格式 (字节序)，参考 HSL 命名习惯
    /// A=高字节, B=低字节. ABCD=Big-Endian, DCBA=Little-Endian
    /// </summary>
    public enum DataFormat
    {
        ABCD = 0, // 标准 Modbus (Big-Endian): [00, 00, 12, 34] -> 0x00001234
        BADC = 1, // 单字反转: [00, 00, 34, 12]
        CDAB = 2, // 双字反转 (Word Swap): [12, 34, 00, 00] -> 常见于部分 PLC
        DCBA = 3, // 小端模式 (Little-Endian): [34, 12, 00, 00]
    }

}

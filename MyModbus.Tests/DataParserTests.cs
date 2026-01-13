using Xunit;
using MyModbus; // 引用你的核心命名空间
using System.Collections.Generic;
using System;

namespace MyModbus.Tests
{
    public class DataParserTests
    {
        // 1. 测试 CDAB 格式的浮点数解析
        [Fact]
        public void ParseBlock_Should_Parse_Float_With_CDAB_Order_Correctly()
        {
            // Arrange
            // 配置设备为 CDAB 格式 (根据 Core.cs 的逻辑，这是字内字节交换)
            var device = new Device
            {
                ByteOrder = DataFormat.CDAB,
                IsStringReverse = false
            };

            var tag = new Tag
            {
                TagName = "Test_Float_CDAB",
                Address = "200",
                StartAddress = 200,
                DataType = DataType.Float,
                Scale = 1.0f,
                Offset = 0.0f,
                Area = StorageArea.Registers // 必须显式设为 Registers
            };

            var block = new AddressBlock
            {
                StartAddress = 200,
                Length = 2,
                Area = StorageArea.Registers,
                Tags = new List<Tag> { tag }
            };

            // 目标值: 12345.6789f
            float expectedValue = 12345.6789f;
            byte[] littleEndianBytes = BitConverter.GetBytes(expectedValue); // Windows 本机序 [b0, b1, b2, b3]

            // 构造模拟 PLC 发来的 CDAB 数据
            // 你的 Core.cs 逻辑中 CDAB 是: dest[0]=src[1], dest[1]=src[0]... 即 0<->1, 2<->3 交换
            // 所以为了得到正确的 dest (LittleEndian)，我们需要反向构造 src
            byte[] rawBytesCDAB = new byte[4];
            rawBytesCDAB[0] = littleEndianBytes[1];
            rawBytesCDAB[1] = littleEndianBytes[0];
            rawBytesCDAB[2] = littleEndianBytes[3];
            rawBytesCDAB[3] = littleEndianBytes[2];

            // Act
            var result = DataParser.ParseBlock(block, rawBytesCDAB, device);

            // Assert
            Assert.Single(result);
            Assert.Equal(expectedValue, (float)result[0].Value, precision: 4);
        }

        // 2. 测试 Int32 (DInt) 类型解析
        [Fact]
        public void ParseBlock_Should_Parse_Int32_Correctly()
        {
            // Arrange
            var device = new Device { ByteOrder = DataFormat.ABCD }; // 标准 Modbus 大端

            var tag = new Tag
            {
                TagName = "Test_DInt",
                StartAddress = 300,
                DataType = DataType.Int32,
                Scale = 1.0f,
                Offset = 0.0f,
                Area = StorageArea.Registers
            };

            var block = new AddressBlock
            {
                StartAddress = 300,
                Length = 2,
                Area = StorageArea.Registers,
                Tags = new List<Tag> { tag }
            };

            int expectedValue = 123456789;
            byte[] littleEndianBytes = BitConverter.GetBytes(expectedValue);

            // 模拟 ABCD (Big-Endian): 整体翻转
            byte[] rawBytesBigEndian = new byte[4];
            Array.Copy(littleEndianBytes, rawBytesBigEndian, 4);
            Array.Reverse(rawBytesBigEndian);

            // Act
            var result = DataParser.ParseBlock(block, rawBytesBigEndian, device);

            // Assert
            Assert.Single(result);
            // 注意：Core.cs 里 ParseBlock 内部 Int32 也会走线性变换逻辑转为 double 再转回，
            // 如果 Tag.Scale 是 1.0，值应该保持不变。但 TagData.Value 此时可能是 int 或 double，取决于你的 Core.cs 修改版。
            // 这里我们断言它可以转为 int。
            Assert.Equal(expectedValue, Convert.ToInt32(result[0].Value));
        }

        // 3. 测试标准 String 解析
        [Fact]
        public void ParseBlock_Should_Parse_String_Correctly()
        {
            // Arrange
            var device = new Device { IsStringReverse = false };

            var tag = new Tag
            {
                TagName = "Test_String",
                StartAddress = 400,
                DataType = DataType.String,
                // 在你的 Core.cs 中，String 的 Length 代表寄存器数量，byte长度 = Length * 2
                // 设为 5 个寄存器 = 10 字节
                Length = 5,
                Area = StorageArea.Registers
            };

            var block = new AddressBlock
            {
                StartAddress = 400,
                Length = 5,
                Area = StorageArea.Registers,
                Tags = new List<Tag> { tag }
            };

            string expectedStr = "MyModbus";
            // 构造 10 个字节的数组，不足补 0
            byte[] rawBytes = new byte[10];
            byte[] strBytes = System.Text.Encoding.ASCII.GetBytes(expectedStr);
            Array.Copy(strBytes, rawBytes, strBytes.Length);

            // Act
            var result = DataParser.ParseBlock(block, rawBytes, device);

            // Assert
            Assert.Equal(expectedStr, result[0].Value.ToString());
        }

        // 4. 测试带字节翻转的 String 解析 (IsStringReverse = true)
        [Fact]
        public void ParseBlock_Should_Parse_String_With_ByteSwap_Correctly()
        {
            // Arrange
            var device = new Device
            {
                // 开启字符串字节翻转
                IsStringReverse = true
            };

            var tag = new Tag
            {
                TagName = "Test_String_Swap",
                StartAddress = 500,
                DataType = DataType.String,
                Length = 3, // 6 字节
                Area = StorageArea.Registers
            };

            var block = new AddressBlock
            {
                StartAddress = 500,
                Length = 3,
                Area = StorageArea.Registers,
                Tags = new List<Tag> { tag }
            };

            // 预期结果: "ABCDEF"
            // 正常顺序: 41 42 43 44 45 46
            // 模拟 PLC 发来的翻转数据 (每两个字节交换): [42 41] [44 43] [46 45] -> "BADC FE"
            byte[] rawBytesSwapped = new byte[]
            {
        0x42, 0x41, // BA -> AB
        0x44, 0x43, // DC -> CD
        0x46, 0x45  // FE -> EF
            };

            // Act
            var result = DataParser.ParseBlock(block, rawBytesSwapped, device);

            // Assert
            Assert.Equal("ABCDEF", result[0].Value.ToString());
        }
    }
}

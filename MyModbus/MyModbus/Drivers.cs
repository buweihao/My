

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MyModbus
{

    #region  硬件抽象层
    /// <summary>
    /// 驱动层统一返回结果
    /// </summary>
    public class DriverResult<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }

        public static DriverResult<T> Success(T data)
            => new DriverResult<T> { IsSuccess = true, Data = data };
        public static DriverResult<T> Fail(string msg)
            => new DriverResult<T> { IsSuccess = false, Message = msg };
    }

    public interface IDriver : IDisposable
    {
        /// <summary>
        /// 核心读取：读取一个打包好的地址块
        /// </summary>
        DriverResult<byte[]> ReadBlock(AddressBlock block);

        /// <summary>
        /// 核心写入：写入单个 Tag
        /// </summary>
        DriverResult<bool> Write(Tag tag, object value);
    }
    public class HslModbusDriver : IDriver
    {
        // 持有你的底层接口 (需确保 IPlcDriver 已包含 ReadRawBytes 和 新的 Write 重载)
        private readonly IPlcDriver _plc;

        public HslModbusDriver(IPlcDriver plcDriver)
        {
            _plc = plcDriver;
        }

        /// <summary>
        /// 读取实现：将引擎的 Block 映射到底层的 RawBytes
        /// </summary>
        public DriverResult<byte[]> ReadBlock(AddressBlock block)
        {
            // 1. 快速检查连接 (虽然底层Execute有重连，但这里做预判更节省性能)
            //if (!_plc.IsConnected)
            //    return DriverResult<byte[]>.Fail("Device Disconnected");

            try
            {
                // 2. 区分存储区
                bool isCoil = block.Area == StorageArea.Coils;

                // 3. 调用底层的通用字节读取接口
                byte[] rawData = _plc.ReadRawBytes(
                    block.StartAddress.ToString(),
                    (ushort)block.Length,
                    isCoil
                );

                return DriverResult<byte[]>.Success(rawData);
            }
            catch (Exception ex)
            {
                // 底层抛出的异常 (如连接超时) 在这里被捕获并封装
                return DriverResult<byte[]>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 写入实现：根据 Tag 类型智能分发
        /// </summary>
        public DriverResult<bool> Write(Tag tag, object value)
        {
            //if (!_plc.IsConnected)
            //    return DriverResult<bool>.Fail("Device Disconnected");

            try
            {
                bool success = false;
                string address = tag.Address;

                // 1. 处理布尔值 (Coils)
                if (tag.Area == StorageArea.Coils || tag.DataType == DataType.Bool)
                {
                    // 使用 Convert 容错 (例如 UI 传过来的是 0/1 或 "true")
                    bool boolVal = Convert.ToBoolean(value);
                    success = _plc.WriteCoil(address, boolVal);
                }
                // 2. 处理寄存器值 (Registers)
                else
                {
                    // 根据 Tag 定义的数据类型，调用底层对应的强类型写入方法
                    // 这样 HSL 会自动根据 DataFormat 处理大小端，无需上层操心
                    switch (tag.DataType)
                    {
                        case DataType.Int16:
                            success = _plc.Write(address, Convert.ToInt16(value));
                            break;
                        case DataType.UInt16:
                            success = _plc.Write(address, Convert.ToUInt16(value));
                            break;
                        case DataType.Int32:
                            success = _plc.Write(address, Convert.ToInt32(value));
                            break;
                        case DataType.UInt32:
                            // HSL可能没有显式的Write(uint)，通常用int强转或Write(addr, byte[])
                            // 这里假设你扩展了uint或者用int代替
                            success = _plc.Write(address, Convert.ToInt32(value));
                            break;
                        case DataType.Float:
                            success = _plc.Write(address, Convert.ToSingle(value));
                            break;
                        // 字符串暂略，逻辑类似
                        case DataType.String:
                            success = _plc.WriteString(address, value?.ToString());
                            break;
                        default:
                            return DriverResult<bool>.Fail($"Unsupported DataType: {tag.DataType}");
                    }
                }

                return success
                    ? DriverResult<bool>.Success(true)
                    : DriverResult<bool>.Fail("Write Failed (Driver returned false)");
            }
            catch (Exception ex)
            {
                return DriverResult<bool>.Fail($"Write Exception: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // 适配器一般不负责释放底层 Service，生命周期由 Manager 管理
            // 除非你是独占模式
        }
    }
    #endregion
}

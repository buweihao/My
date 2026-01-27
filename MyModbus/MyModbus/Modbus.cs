using HslCommunication;
using HslCommunication.Core;
using HslCommunication.ModBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyModbus
{
    public interface IPlcDriver : IDisposable
    {
        string Name { get; }
        bool IsConnected { get; }

        // --- 读取 ---
        string ReadRegisters(string address, ushort length);
        string ReadDInt(string address, ushort length);
        string ReadCoils(string address, ushort count);
        string ReadString(string address, ushort length, Encoding? encoding = null);
        byte[] ReadRawBytes(string address, ushort length, bool isCoil);

        // --- 写入 (增强版) ---

        // 1. 核心：写入原始字节数组 (用于批量写入寄存器)
        bool Write(string address, byte[] value);

        // 2. 基础类型 (利用 HSL 自动处理 Endian)
        bool Write(string address, short value);
        bool Write(string address, ushort value);
        bool Write(string address, int value);
        bool Write(string address, float value);

        // 3. 线圈与字符串
        bool WriteCoil(string address, bool value);
        bool WriteString(string address, string value);
    }
    public class ModbusManager
    {
        private readonly Dictionary<string, IPlcDriver> _services = new();

        public ModbusManager()
        {
            if (!HslCommunication.Authorization.SetAuthorizationCode("fe49cdb6-b388-4c05-9b66-0e3f1ad3627f"))
            {
                throw new Exception("HSL激活失败");
            }
        }

        public void AddTcp(string name, string ip, int port = 502)
        {
            if (ip == "-" || _services.ContainsKey(name)) return;
            // 实例化时不再触发连接，真正的连接发生在第一次读写时
            var service = new ModbusTcpService(name, ip, port);
            _services[name] = service;
        }

        public IPlcDriver? GetService(string name) => _services.TryGetValue(name, out var s) ? s : null;

        public void CloseAll()
        {
            foreach (var s in _services.Values) s.Dispose();
            _services.Clear();
        }
    }

    public class ModbusTcpService : IPlcDriver
    {
        public string Name { get; }
        public bool IsConnected { get; private set; } = false;

        private readonly ModbusTcpNet _modbus;
        private readonly object _lock = new();

        public ModbusTcpService(
                string name,
                string ip,
                int port = 502,
                // 1. 直接接收枚举，而不是字符串，把解析压力的甩锅给调用方
               HslCommunication.Core.DataFormat dataFormat = HslCommunication.Core.DataFormat.CDAB,
                // 2. 直接接收布尔值
                bool isStringReverseByteWord = false,
                string heartbeatAddress = "4000",
                int connectTimeout = 1000, int receiveTimeout = 1000
                )
        {
            Name = name;

            // 初始化
            _modbus = new ModbusTcpNet(ip, port)
            {
                ConnectTimeOut = 2000
            };

            // 核心配置逻辑
            // 第一步：先应用 DataFormat（宏观字节序）
            _modbus.DataFormat = dataFormat;

            // 第二步：再应用特殊规则（微调）
            // 只有当传入为 true 时才去修改，避免不必要的赋值
            if (isStringReverseByteWord)
            {
                _modbus.ByteTransform.IsStringReverseByteWord = true;
            }
            //Task.Run(async () =>
            //{

            //    while(true)
            //    {
            //        await Task.Delay(1000);

            //        Console.WriteLine(Name+IsConnected);
            //    }

            //});
        }

        #region 核心逻辑 (自动重连机制)
        // 记录上次重连失败的时间
        private DateTime _lastConnectFailTime = DateTime.MinValue;
        // 失败后的冷却时间 (比如 5秒内不再尝试重连，直接报错)
        private readonly int _failRetryDelay = 5000;

        // 所有的读写请求都走这里
        private T Execute<T>(Func<T> action)
        {
            // 定义一个变量来标记是否成功获取了锁
            bool lockTaken = false;

            try
            {
                // 【关键修改】尝试获取锁，超时时间为 0ms (立即返回)
                // 如果无法立即获取锁，说明上一次采集还在进行中，直接跳过
                Monitor.TryEnter(_lock, 200, ref lockTaken);

                if (!lockTaken)
                {
                    // 这里抛出的异常会被上层捕获，代表“本次采集被丢弃”
                    // 这样就不会阻塞调用线程，解决了线程堆积问题
                    throw new Exception($"[{Name}] 通信繁忙(上一帧未完成)，跳过本次采集。");
                }

                // ================= 以下为拿到锁之后的逻辑 =================

                // 1. 【熔断机制】如果处于断线冷却期，直接抛出异常，不进行物理连接尝试
                if (!IsConnected)
                {
                    if ((DateTime.Now - _lastConnectFailTime).TotalMilliseconds < _failRetryDelay)
                    {
                        // 此时直接抛出异常，不耗时
                        throw new Exception($"[{Name}] 设备断线，正在等待重连冷却...");
                    }
                }

                // 2. 检查连接状态 (懒连接 / 自动重连)
                if (!IsConnected)
                {
                    // 尝试连接 (耗时操作，约 1~2秒，由 ConnectTimeOut 决定)
                    var connectRes = _modbus.ConnectServer();
                    IsConnected = connectRes.IsSuccess;

                    if (!IsConnected)
                    {
                        // 连接失败，记录当前时间，开启冷却倒计时
                        _lastConnectFailTime = DateTime.Now;
                        throw new Exception($"[{Name}] 连接失败: {connectRes.Message}");
                    }
                }

                // 3. 执行实际的 Read/Write 操作
                try
                {
                    return action();
                }
                catch (Exception)
                {
                    // 如果在 Read/Write 过程中发生异常（通常是 Socket 断开）
                    // 标记为离线，并关闭连接
                    IsConnected = false;
                    _modbus.ConnectClose();

                    // 注意：这里通常不需要开启冷却。
                    // 逻辑是：读写失败后，我们希望下一次采集时立即尝试重连一次。
                    // 只有当“重连动作”本身失败了，才进入冷却。
                    throw;
                }
            }
            finally
            {
                // 【关键】只有在真正拿到了锁的情况下，才释放锁
                if (lockTaken)
                {
                    Monitor.Exit(_lock);
                }
            }
        }

        // 专门处理 OperateResult 的结果检查
        private T CheckResult<T>(OperateResult<T> result, string opName)
        {
            if (!result.IsSuccess)
            {
                // 如果是网络层面的错误，标记离线，以便下次调用时触发重连
                // HSL 的错误码通常包含网络错误信息，这里简单粗暴处理：只要失败就怀疑连接问题
                // 你也可以判断 result.ErrorCode 来决定是否置为 false
                IsConnected = false;
                _modbus.ConnectClose();

                throw new Exception($"[{Name}] {opName} 失败: {result.Message}");
            }
            return result.Content;
        }

        #endregion

        #region 读写接口实现

        public string ReadRegisters(string address, ushort length)
        {
            return Execute(() =>
            {
                var res = _modbus.ReadUInt16(address, length);
                var data = CheckResult(res, $"ReadReg {address}");
                return string.Join(",", data);
            });
        }

        public string ReadDInt(string address, ushort length)
        {
            return Execute(() =>
            {
                var res = _modbus.ReadUInt32(address, length);
                var data = CheckResult(res, $"ReadDInt {address}");
                return string.Join(",", data);
            });
        }

        public string ReadCoils(string address, ushort count)
        {
            return Execute(() =>
            {
                var res = _modbus.ReadCoil(address, count);
                var data = CheckResult(res, $"ReadCoil {address}");
                return string.Join(",", data.Select(b => b ? "true" : "false"));
            });
        }

        public string ReadString(string address, ushort length, Encoding? encoding = null)
        {
            return Execute(() =>
            {
                var res = _modbus.ReadString(address, length, encoding ?? Encoding.ASCII);
                return CheckResult(res, $"ReadString {address}").TrimEnd('\0');
            });
        }

        /// <summary>
        /// 【新增】通用原始字节读取接口 (适配采集引擎)
        /// </summary>
        /// <param name="address">地址 (如 "100")</param>
        /// <param name="length">长度 (寄存器数量 或 线圈数量)</param>
        /// <param name="isCoil">是否是读线圈</param>
        public byte[] ReadRawBytes(string address, ushort length, bool isCoil)
        {
            return Execute(() =>
            {
                if (isCoil)
                {
                    // 将 bool[] 压缩回 byte[]
                    var res = _modbus.ReadCoil(address, length);
                    var bools = CheckResult(res, $"ReadCoilRaw {address}");
                    return _modbus.ByteTransform.TransByte(bools);
                }
                else
                {
                    // 寄存器读取 (默认返回 Big-Endian 字节流，除非配置了 DataFormat)
                    var res = _modbus.Read(address, length);
                    return CheckResult(res, $"ReadRegRaw {address}");
                }
            });
        }


        // =============================================================
        //  写入部分 (增强版)
        // =============================================================

        /// <summary>
        /// 私有辅助方法：统一处理写入操作的异常捕获和重连逻辑
        /// </summary>
        private bool WriteBase(Func<OperateResult> writeAction)
        {
            try
            {
                return Execute(() =>
                {
                    var res = writeAction();
                    if (!res.IsSuccess)
                    {
                        // 写入失败视为连接问题
                        IsConnected = false;
                        return false; // 或者 throw new Exception(res.Message);
                    }
                    return true;
                });
            }
            catch
            {
                // Execute 内部抛出的断线/锁超时异常在这里被吞掉并返回 false
                // 符合 "Write returns bool" 的设计约定
                return false;
            }
        }

        /// <summary>
        /// 核心：写入原始字节数组
        /// 适用场景：上层已经打包好了数据，或者需要批量写入多个寄存器
        /// </summary>
        public bool Write(string address, byte[] value)
        {
            if (value == null || value.Length == 0) return false;

            // HSL 的 Write(address, byte[]) 默认对应 Modbus FC16 (写多个寄存器)
            // 注意：Modbus 寄存器是双字节，建议 value.Length 为偶数
            return WriteBase(() => _modbus.Write(address, value));
        }

        /// <summary>
        /// 写入 Short (自动处理大小端)
        /// </summary>
        public bool Write(string address, short value)
        {
            return WriteBase(() => _modbus.Write(address, value));
        }

        /// <summary>
        /// 写入 UShort (自动处理大小端)
        /// </summary>
        public bool Write(string address, ushort value)
        {
            return WriteBase(() => _modbus.Write(address, value));
        }

        /// <summary>
        /// 写入 Int (自动处理大小端)
        /// </summary>
        public bool Write(string address, int value)
        {
            return WriteBase(() => _modbus.Write(address, value));
        }

        /// <summary>
        /// 写入 Float (自动处理大小端)
        /// </summary>
        public bool Write(string address, float value)
        {
            return WriteBase(() => _modbus.Write(address, value));
        }

        /// <summary>
        /// 写入线圈 (FC05/FC15)
        /// </summary>
        public bool WriteCoil(string address, bool value)
        {
            return WriteBase(() => _modbus.Write(address, value));
        }

        // 重载：批量写入线圈
        public bool WriteCoil(string address, bool[] values)
        {
            return WriteBase(() => _modbus.Write(address, values));
        }

        /// <summary>
        /// 写入字符串
        /// </summary>
        public bool WriteString(string address, string asciiText)
        {
            return WriteBase(() => _modbus.Write(address, asciiText));
        }

        // 兼容旧接口 (如果接口中必须叫 WriteRegisters)
        public bool WriteRegisters(string address, ushort value) => Write(address, value);

        #endregion

        public void Dispose()
        {
            lock (_lock)
            {
                _modbus?.ConnectClose();
                IsConnected = false;
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyModbus;
using static HslCommunication.Profinet.Knx.KnxCode;


namespace MyModbus
{
    #region  加载
    public class ConfigLoader
    {
        public static List<Device> LoadConfig(string csvFilePath)
        {
            var devices = new List<Device>();

            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"[Config Error] 找不到配置文件: {csvFilePath}");
                return devices;
            }

            try
            {
                var lines = File.ReadAllLines(csvFilePath);
                int lineNumber = 0;

                foreach (var line in lines)
                {
                    lineNumber++;
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//")) continue;
                    if (trimmedLine.StartsWith("DeviceID", StringComparison.OrdinalIgnoreCase)) continue;

                    try
                    {
                        var parts = trimmedLine.Split(',').Select(p => p.Trim()).ToArray();

                        // 基础列数检查 (至少要有 8 列)
                        if (parts.Length < 8)
                        {
                            Console.WriteLine($"[Config Warning] 第 {lineNumber} 行格式错误(列数不足): {line}");
                            continue;
                        }

                        // --- 解析列 ---
                        // 0: DeviceID, 1: IP, 2: Port, 3: TagName, 4: Address, 
                        // 5: DataType, 6: ScanRate, 7: Area, 8: IsFavorite
                        // 9: Length (新增可选列)

                        string devId = parts[0];
                        string ip = parts[1];
                        int port = int.Parse(parts[2]);

                        var device = devices.FirstOrDefault(d => d.DeviceId == devId);
                        if (device == null)
                        {
                            device = new Device
                            {
                                DeviceId = devId,
                                IpAddress = ip,
                                Port = port,
                                ByteOrder = DataFormat.ABCD,
                                IsStringReverse = false
                            };
                            devices.Add(device);
                        }

                        Tag newTag = new Tag
                        {
                            TagName = parts[3],
                            Address = parts[4],
                            StartAddress = int.Parse(parts[4]),
                            DataType = (DataType)Enum.Parse(typeof(DataType), parts[5], true),
                            ScanRate = int.Parse(parts[6]),
                            Area = (StorageArea)Enum.Parse(typeof(StorageArea), parts[7], true),
                            // 解析第 9 列 (IsFavorite)
                            IsFavorite = parts.Length > 8 && (parts[8].Equals("True", StringComparison.OrdinalIgnoreCase) || parts[8] == "1")
                        };

                        // =========================================================
                        // ✨ 核心修改：解析第 10 列 (索引 9) 作为长度
                        // =========================================================
                        int lengthParam = 1; // 默认基准

                        // 1. 如果 CSV 里有第 10 列，且能解析成数字，就用它
                        if (parts.Length > 9 && int.TryParse(parts[9], out int csvLen))
                        {
                            lengthParam = csvLen;
                        }
                        else
                        {
                            // 2. 如果没配长度，给 String 一个保底值 (比如 32)，防止没改 CSV 的旧项目报错
                            if (newTag.DataType == DataType.String)
                            {
                                lengthParam = 32;
                            }
                        }

                        // CalculateLength 会自动处理：
                        // - 如果是 Int16/Float，它会忽略 lengthParam，返回固定的 1 或 2
                        // - 如果是 String，它会使用 lengthParam
                        newTag.Length = CalculateLength(newTag.DataType, lengthParam);

                        device.Tags.Add(newTag);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Config Error] 解析第 {lineNumber} 行失败: {line}. 原因: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config Fatal] 读取文件失败: {ex.Message}");
            }

            return devices;
        }

        private static int CalculateLength(DataType type, int lengthProp)
        {
            switch (type)
            {
                case DataType.Bool:
                case DataType.Int16:
                case DataType.UInt16: return 1;
                case DataType.Int32:
                case DataType.UInt32:
                case DataType.Float: return 2;
                case DataType.Double: return 4;
                case DataType.String: return lengthProp; // 只有 String 使用传入的参数
                default: return 1;
            }
        }
    }
    #endregion


    #region  编组
    public class GroupOptimizer
    {
        // ...常量定义不变...
        private const int MAX_GAP = 10;
        private const int MAX_BATCH_REG = 100;
        private const int MAX_BATCH_BOOL = 800;

        public static Dictionary<int, List<PollGroup>> Optimize(List<Device> devices)
        {
            var result = new Dictionary<int, List<PollGroup>>();

            foreach (var device in devices)
            {
                var rateGroups = device.Tags
                    .GroupBy(t => t.ScanRate)
                    .Select(g => new PollGroup
                    {
                        ScanRate = g.Key,
                        Blocks = CreateBlocks(g.ToList())
                    })
                    .ToList();

                result[device.GetHashCode()] = rateGroups;
            }

            return result;
        }

        private static List<AddressBlock> CreateBlocks(List<Tag> tags)
        {
            var blocks = new List<AddressBlock>();

            // 使用 StorageArea 枚举进行分组
            var areaGroups = tags.GroupBy(t => t.Area);

            foreach (var areaGroup in areaGroups)
            {
                var sortedTags = areaGroup.OrderBy(t => t.StartAddress).ToList();
                if (sortedTags.Count == 0) continue;

                var currentBlock = new AddressBlock
                {
                    StartAddress = sortedTags[0].StartAddress,
                    Length = sortedTags[0].Length,
                    Area = areaGroup.Key
                };
                currentBlock.Tags.Add(sortedTags[0]);

                // 修正点：直接使用 StorageArea 枚举，不再加 Models 前缀
                int maxBatch = (areaGroup.Key == StorageArea.Coils) ? MAX_BATCH_BOOL : MAX_BATCH_REG;

                for (int i = 1; i < sortedTags.Count; i++)
                {
                    var tag = sortedTags[i];
                    int tagEnd = tag.StartAddress + tag.Length;
                    int blockEnd = currentBlock.StartAddress + currentBlock.Length;

                    bool isGapTolerable = (tag.StartAddress - blockEnd) <= MAX_GAP;
                    bool isSizeSafe = (tagEnd - currentBlock.StartAddress) <= maxBatch;

                    if (isGapTolerable && isSizeSafe)
                    {
                        int newLength = Math.Max(currentBlock.Length, tagEnd - currentBlock.StartAddress);
                        currentBlock.Length = newLength;
                        currentBlock.Tags.Add(tag);
                    }
                    else
                    {
                        blocks.Add(currentBlock);

                        currentBlock = new AddressBlock
                        {
                            StartAddress = tag.StartAddress,
                            Length = tag.Length,
                            Area = areaGroup.Key
                        };
                        currentBlock.Tags.Add(tag);
                    }
                }
                blocks.Add(currentBlock);
            }

            return blocks;
        }
    }
    #endregion


    #region  解析
    public class DataBus
    {
        // 1. 线程安全的缓存字典：Key = TagName, Value = TagData
        // ConcurrentDictionary 保证了多线程读写不会冲突
        private readonly ConcurrentDictionary<string, TagData> _cache = new();

        // 2. 数据变更事件 (Pub/Sub 模式)
        // UI 或 数据库存储模块 订阅此事件
        public event Action<TagData> OnDataChanged;
        private readonly ConcurrentDictionary<string, List<Action<TagData>>> _tagSubscriptions = new();
        /// <summary>
        /// 【新功能】精准订阅：只订阅特定名称的点位
        /// </summary>
        public void Subscribe(string tagName, Action<TagData> callback)
        {
            _tagSubscriptions.AddOrUpdate(tagName,
                new List<Action<TagData>> { callback },
                (key, list) => { list.Add(callback); return list; });
        }
        /// <summary>
        /// 【核心重载】批量订阅：返回 值数组 + 整体质量标志
        /// </summary>
        /// <typeparam name="T">期望的返回类型</typeparam>
        /// <param name="tagNames">点位名称列表</param>
        /// <param name="callback">回调函数：(数据数组, 整体质量是否良好)</param>
        public void Subscribe<T>(IEnumerable<string> tagNames, Action<T[], bool> callback)
        {
            if (tagNames == null || callback == null) return;

            // 1. 固化查询列表
            var tags = tagNames.ToArray();

            // 2. 定义统一处理逻辑
            Action<TagData> groupHandler = (triggerData) =>
            {
                T[] results = new T[tags.Length];
                bool isAllGood = true; // 默认为好

                for (int i = 0; i < tags.Length; i++)
                {
                    // 从缓存获取完整 TagData（包含 IsQualityGood）
                    TagData? data = GetTagData(tags[i]);

                    // 检查：如果任意一个点位数据为空或质量为 Bad
                    if (data == null || !data.Value.IsQualityGood)
                    {
                        isAllGood = false;
                        // 注意：这里不要直接 return，即使坏了也要把数组填满（防止空引用），
                        // 只是把 isAllGood 标记为 false 告诉上层。
                    }

                    // 转换数值（复用之前的 ConvertValue 辅助方法）
                    results[i] = ConvertValue<T>(data?.Value);
                }

                // 3. 【关键】将数据和质量标志一起传给订阅者
                callback(results, isAllGood);
            };

            // 3. 逐个订阅
            foreach (var tag in tags)
            {
                Subscribe(tag, groupHandler);
            }
        }
        /// <summary>
        /// 内部辅助：通用类型转换
        /// </summary>
        private T ConvertValue<T>(object value)
        {
            if (value == null) return default(T);

            // 1. 如果类型直接匹配 (最快)
            if (value is T tVal) return tVal;

            try
            {
                // 2. 特殊处理 String
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value.ToString();
                }

                // 3. 通用转换 (处理 float <-> double, int <-> short 等)
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 批量更新数据 (由 Engine 采集线程调用)
        /// </summary>
        public void Update(List<TagData> newDataList)
        {
            if (newDataList == null) return;

            foreach (var newData in newDataList)
            {
                bool hasOldValue = _cache.TryGetValue(newData.TagName, out TagData oldData);

                // 死区与质量变化判断
                if (!hasOldValue ||
                    oldData.IsQualityGood != newData.IsQualityGood ||
                    IsChanged(oldData.Value, newData.Value))
                {
                    _cache[newData.TagName] = newData;

                    // 1. 全局推送
                    OnDataChanged?.Invoke(newData);

                    // 2. 精准推送 (修复后这里就能通过编译了)
                    if (_tagSubscriptions.TryGetValue(newData.TagName, out var callbacks))
                    {
                        foreach (var cb in callbacks)
                        {
                            try
                            {
                                // 现在 cb 是 Action<TagData>，可以直接传入 newData
                                cb.Invoke(newData);
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        public object GetValue(string tagName)
        {
            if (_cache.TryGetValue(tagName, out var data)) return data.Value;
            return null;
        }

        public TagData? GetTagData(string tagName)
        {
            if (_cache.TryGetValue(tagName, out var data)) return data;
            return null;
        }

        private bool IsChanged(object oldVal, object newVal)
        {
            if (oldVal == null || newVal == null) return true;
            if (oldVal is float fOld && newVal is float fNew) return Math.Abs(fNew - fOld) > 0.0001f;
            if (oldVal is double dOld && newVal is double dNew) return Math.Abs(dNew - dOld) > 0.0001d;
            return !oldVal.Equals(newVal);
        }
    }
    public static class DataParser
    {
        /// <summary>
        /// 核心解析方法
        /// </summary>
        /// <param name="block">地址块定义</param>
        /// <param name="rawData">驱动读回来的原始字节流</param>
        /// <param name="device">设备配置 (用于获取字节序参数)</param>
        public static List<TagData> ParseBlock(AddressBlock block, byte[] rawData, Device device)
        {
            var results = new List<TagData>();
            if (rawData == null || rawData.Length == 0) return results;

            foreach (var tag in block.Tags)
            {
                try
                {
                    object parsedValue = null;

                    // --- 场景 A: 线圈 (Bool) ---
                    // 线圈不受字节序影响，只受位位置影响
                    if (tag.Area == StorageArea.Coils)
                    {
                        int bitOffset = tag.StartAddress - block.StartAddress;
                        int byteIndex = bitOffset / 8;
                        int bitInByte = bitOffset % 8;

                        if (byteIndex < rawData.Length)
                        {
                            parsedValue = ((rawData[byteIndex] >> bitInByte) & 1) == 1;
                        }
                    }
                    // --- 场景 B: 寄存器 (Word/DWord/String) ---
                    else
                    {
                        // 1. 切片：从大包中切出该 Tag 对应的小字节数组
                        int byteOffset = (tag.StartAddress - block.StartAddress) * 2;
                        int byteLength = GetByteLength(tag.DataType, tag.Length); // 注意：String 需要用到 tag.Length

                        if (byteOffset + byteLength <= rawData.Length)
                        {
                            byte[] tagBytes = new byte[byteLength];
                            Array.Copy(rawData, byteOffset, tagBytes, 0, byteLength);

                            // =======================================================
                            // 核心修改：根据参数进行字节变换
                            // =======================================================

                            if (tag.DataType == DataType.String)
                            {
                                // 2.1 字符串特殊处理：仅受 IsStringReverse 控制
                                if (device.IsStringReverse)
                                {
                                    // 交换相邻字节：[A, B, C, D] -> [B, A, D, C]
                                    tagBytes = SwapBytesInWord(tagBytes);
                                }
                                parsedValue = Encoding.ASCII.GetString(tagBytes).TrimEnd('\0');
                            }
                            else
                            {
                                // 2.2 数值特殊处理：受 DataFormat (ABCD/CDAB...) 控制
                                // 先将 rawBytes 调整为当前 CPU 能识别的顺序 (通常是 Little-Endian)
                                byte[] targetBytes = TransformBytes(tagBytes, device.ByteOrder);

                                parsedValue = ConvertBytesToValue(targetBytes, tag.DataType);

                                // 3. 线性变换 (Scale/Offset)
                                if (parsedValue is double || parsedValue is float || parsedValue is int || parsedValue is short)
                                {
                                    double rawNum = Convert.ToDouble(parsedValue);
                                    double finalNum = rawNum * tag.Scale + tag.Offset;

                                    // ✅ 修改方案：使用 if-else 明确赋值，阻止编译器进行类型提升
                                    if (tag.DataType == DataType.Float)
                                    {
                                        parsedValue = (float)finalNum;
                                    }
                                    else
                                    {
                                        parsedValue = finalNum;
                                    }
                                }
                            }
                        }
                    }

                    if (parsedValue != null)
                    {
                        results.Add(new TagData
                        {
                            TagName = tag.TagName,
                            Value = parsedValue,
                            Timestamp = DateTime.Now,
                            IsQualityGood = true
                        });
                    }
                }
                catch (Exception)
                {
                    // 忽略单点解析错误
                }
            }
            return results;
        }

        #region 核心字节序处理逻辑

        /// <summary>
        /// 根据 DataFormat 将原始字节流转换为本机可读的字节流 (Little-Endian)
        /// </summary>
        private static byte[] TransformBytes(byte[] src, DataFormat format)
        {
            // 假设本机是 Little-Endian (Windows/Intel CPU 绝大多数情况)
            // 目标：把 src 转换成 DCBA 顺序

            byte[] dest = (byte[])src.Clone();

            // 如果数据长度不够4字节(例如short)，可视作CDAB或ABCD的子集处理
            // 这里主要处理 4字节 (Int32, Float) 的情况
            if (dest.Length >= 4)
            {
                switch (format)
                {
                    case DataFormat.ABCD:
                        // 源: Big-Endian [00, 00, 12, 34]
                        // 目标: Little-Endian [34, 12, 00, 00]
                        Array.Reverse(dest);
                        break;

                    case DataFormat.DCBA:
                        // 源: Little-Endian [34, 12, 00, 00]
                        // 目标: Little-Endian (无需变动)
                        break;

                    case DataFormat.BADC:
                        // 源: [00, 00, 34, 12] (单字内反转)
                        // 1. 先字内交换 -> [00, 00, 12, 34] (ABCD)
                        dest = SwapBytesInWord(dest);
                        // 2. 再整体反转 -> [34, 12, 00, 00] (DCBA)
                        Array.Reverse(dest);
                        break;

                    case DataFormat.CDAB:
                        // 源: [12, 34, 00, 00] (双字反转)
                        // 1. 也是先字内交换 -> [34, 12, 00, 00] (DCBA) ? 不对，CDAB是字序换了
                        // CDAB 的实际逻辑是：Word[0] 和 Word[1] 交换了位置
                        // 简单做法：先当作ABCD反转，得到 [00, 00, 34, 12]，然后再字内反转?

                        // 标准 CDAB 转换算法：
                        // [0] [1] [2] [3]  (Source)
                        // [2] [3] [0] [1]  (Target - BigEndian)
                        // [1] [0] [3] [2]  (Target - LittleEndian) <--- 我们要这个

                        // 最稳妥的通用做法：直接手动重排
                        dest = new byte[4];
                        dest[0] = src[1];
                        dest[1] = src[0];
                        dest[2] = src[3];
                        dest[3] = src[2];
                        break;
                }
            }
            else if (dest.Length == 2)
            {
                // 对于 Short/UShort，只有 BigEndian(ABCD) 和 LittleEndian(DCBA) 之分
                // BADC 和 CDAB 在 2字节下退化为同一种情况
                if (format == DataFormat.ABCD || format == DataFormat.CDAB)
                {
                    Array.Reverse(dest);
                }
            }

            return dest;
        }

        /// <summary>
        /// 字内字节交换 (用于 String 的 IsStringReverse 和 BADC)
        /// [0x01, 0x02, 0x03, 0x04] -> [0x02, 0x01, 0x04, 0x03]
        /// </summary>
        private static byte[] SwapBytesInWord(byte[] src)
        {
            byte[] dest = (byte[])src.Clone();
            for (int i = 0; i < dest.Length - 1; i += 2)
            {
                byte temp = dest[i];
                dest[i] = dest[i + 1];
                dest[i + 1] = temp;
            }
            return dest;
        }

        #endregion

        #region 基础辅助方法

        private static int GetByteLength(DataType type, int lengthProp)
        {
            switch (type)
            {
                case DataType.Int16:
                case DataType.UInt16: return 2;
                case DataType.Int32:
                case DataType.UInt32:
                case DataType.Float: return 4;
                case DataType.Double: return 8;
                case DataType.String: return lengthProp * 2; // String长度通常指字符数/寄存器数，需根据配置约定
                default: return 2;
            }
        }

        private static object ConvertBytesToValue(byte[] data, DataType type)
        {
            // 此时 data 已经是 Little-Endian (Windows本机序)
            switch (type)
            {
                case DataType.Int16: return BitConverter.ToInt16(data, 0);
                case DataType.UInt16: return BitConverter.ToUInt16(data, 0);
                case DataType.Int32: return BitConverter.ToInt32(data, 0);
                case DataType.UInt32: return BitConverter.ToUInt32(data, 0);
                case DataType.Float: return BitConverter.ToSingle(data, 0);
                case DataType.Double: return BitConverter.ToDouble(data, 0);
                default: return 0;
            }
        }

        #endregion
    }

    #endregion

    #region  引擎
    /// <summary>
    /// 写入时的上下文，包含 Tag 定义和对应的驱动，让写数据时能瞬间找到对应的驱动
    /// </summary>
    internal struct WriteContext
    {
        public Tag Tag;
        public IDriver Driver;
    }
    public class DataCollectionEngine
    {
        // --- 核心组件 ---
        private readonly List<Device> _devices;
        private readonly DataBus _dataBus;

        // 驱动字典: Key=DeviceId, Value=Driver
        private readonly Dictionary<string, IDriver> _drivers = new();

        // 写入查找表: Key=TagName
        private readonly Dictionary<string, WriteContext> _writeLookup = new();

        // 线程控制
        private CancellationTokenSource _cts;
        private readonly List<Task> _tasks = new();

        //工厂
        private readonly IDriverFactory _driverFactory; // <--- 新增依赖

        /// <summary>
        /// 构造函数
        /// </summary>
        public DataCollectionEngine(List<Device> devices, DataBus bus, IDriverFactory driverFactory)
        {
            _devices = devices;
            _dataBus = bus;
            _driverFactory = driverFactory; // <--- 保存工厂
            Init();
        }

        /// <summary>
        /// 1. 初始化阶段：创建驱动，建立索引
        /// </summary>
        public void Init()
        {
            foreach (var device in _devices)
            {
                var driver = _driverFactory.CreateDriver(device);
                _drivers[device.DeviceId] = driver;

                // C. 建立写入查找表 (Tag Name -> Driver)
                foreach (var tag in device.Tags)
                {
                    if (!_writeLookup.ContainsKey(tag.TagName))
                    {
                        _writeLookup[tag.TagName] = new WriteContext { Tag = tag, Driver = driver };
                    }
                }
            }
        }

        /// <summary>
        /// 2. 启动采集
        /// </summary>
        public void Start()
        {
            if (_cts != null) return; // 防止重复启动
            _cts = new CancellationTokenSource();

            // A. 调用优化器进行分组 (Step 2)
            // 结果结构: Dictionary<DeviceHash, List<PollGroup>>
            var optimizedMap = GroupOptimizer.Optimize(_devices);

            // B. 为每个设备、每个频率组启动一个独立的 Task
            foreach (var kvp in optimizedMap)
            {
                // 获取对应的 Device 对象 (为了传给 Parser 用配置)
                // 这里有个小技巧：Hash可能碰撞，严谨做法是用 DeviceId 做 Key。
                // 假设我们在 Optimize 改用 DeviceId 做 Key，这里简单处理：
                var device = _devices.FirstOrDefault(d => d.GetHashCode() == kvp.Key);
                if (device == null) continue;

                IDriver driver = _drivers[device.DeviceId];

                foreach (var group in kvp.Value)
                {
                    // 启动采集循环任务
                    var task = Task.Factory.StartNew(
                        () => PollingLoop(device, driver, group, _cts.Token),
                        _cts.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default
                    );
                    _tasks.Add(task);
                }
            }
        }

        /// <summary>
        /// 3. 停止采集
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            // 等待所有任务完成（可选）
            // Task.WaitAll(_tasks.ToArray(), 1000);
            _tasks.Clear();
            _cts = null;

            // 释放驱动资源
            foreach (var driver in _drivers.Values)
            {
                driver.Dispose();
            }
            _drivers.Clear();
        }

        /// <summary>
        /// 核心采集循环 (跑在独立线程中)
        /// </summary>
        private void PollingLoop(Device device, IDriver driver, PollGroup group, CancellationToken token)
        {
            int interval = group.ScanRate;
            var stopwatch = new Stopwatch();

            while (!token.IsCancellationRequested)
            {
                stopwatch.Restart();

                try
                {
                    foreach (var block in group.Blocks)
                    {
                        if (token.IsCancellationRequested) break;

                        // 1. 驱动读取
                        var readResult = driver.ReadBlock(block);

                        if (readResult.IsSuccess)
                        {
                            // === 情况 A: 通信成功 ===
                            // 由 Parser 设置 IsQualityGood = true
                            var parsedData = DataParser.ParseBlock(block, readResult.Data, device);
                            _dataBus.Update(parsedData);
                        }
                        else
                        {
                            // === 情况 B: 通信失败 (核心维护点) ===
                            // 必须主动告诉 DataBus：这些点位坏掉了！

                            var badDataList = new List<TagData>();

                            foreach (var tag in block.Tags)
                            {
                                // 构造一个“坏”数据包
                                badDataList.Add(new TagData
                                {
                                    TagName = tag.TagName,
                                    Value = null,           // 或者保留上一次的值(如果有缓存)，通常设为null或默认值
                                    Timestamp = DateTime.Now,
                                    IsQualityGood = false   // <--- 这里！标记质量为 Bad
                                });
                            }

                            // 推送坏数据，这样 UI 才能变灰或报警
                            _dataBus.Update(badDataList);

                            // 可选：记录日志
                            // Console.WriteLine($"[Error] {device.DeviceId} 读取失败: {readResult.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Polling Loop Fatal Error: {ex.Message}");
                }

                // --- 周期补偿 (关键) ---
                // 如果采集耗时 20ms，间隔 100ms，则只睡 80ms
                // 如果采集耗时 120ms，则不睡 (直接下一轮)
                stopwatch.Stop();
                int elapsed = (int)stopwatch.ElapsedMilliseconds;
                int waitTime = interval - elapsed;

                if (waitTime > 0)
                {
                    // 使用 SpinWait 还是 Sleep 取决于精度要求，100ms级用 Sleep 足够
                    Thread.Sleep(waitTime);
                }
                else
                {
                    // 警告：采集过慢，发生堆积
                    // Console.WriteLine($"[Perf Warning] Loop took {elapsed}ms > {interval}ms");
                }
            }
        }

        /// <summary>
        /// 4. 写入接口 (供 UI 调用)
        /// </summary>
        public bool WriteTag(string tagName, object value)
        {
            if (_writeLookup.TryGetValue(tagName, out var ctx))
            {
                // 找到对应的驱动和 Tag 定义，直接写入
                var result = ctx.Driver.Write(ctx.Tag, value);
                return result.IsSuccess;
            }
            return false; // Tag 不存在
        }
    }

    #endregion


}

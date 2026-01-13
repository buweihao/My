//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using MyModbus;
//using MyModbus.IndustrialGateway.Models;

//namespace MyModbus
//{
//    public static partial class Global
//    {
//        static Global()
//        {
//            PLCInit();
//        }

//        // 对外暴露给 XAML 的唯一入口
//        public static PlcLink Plc { get; private set; }

//        // 1. 准备组件
//        public static DataBus bus = new DataBus();
//        public static DataCollectionEngine engine;
//        public static void PLCInit()
//        {
//            // 2. 加载配置 (Step 1)
//            List<Device> devices = ConfigLoader.LoadConfig("config.csv");

//            // 3. 创建引擎 (Step 5)
//            engine = new DataCollectionEngine(devices, bus);

//            // 4. 初始化 (建立连接对象，但不连接)
//            engine.Init();

//            // 6. 启动引擎 (开始多线程采集)
//            engine.Start();


//            // ==============================================
//            // 4. 【关键步骤】生成扁平化的查找表
//            // ==============================================
//            var tagLookup = new Dictionary<string, Tag>();

//            foreach (var device in devices)
//            {
//                foreach (var tag in device.Tags)
//                {
//                    // 防止 CSV 里有重复名字导致报错
//                    if (!tagLookup.ContainsKey(tag.TagName))
//                    {
//                        tagLookup.Add(tag.TagName, tag);
//                    }
//                }
//            }

//            Plc = new PlcLink(bus, tagLookup);
//        }


//    }
//}

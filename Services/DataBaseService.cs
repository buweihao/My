using MyDatabase;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;

namespace My.Services
{
    public interface IDataBaseService
    {
        // --- 业务模块 A: 生产管理 ---

        /// <summary>
        /// 记录一条生产数据
        /// </summary>
        Task AddProductionRecordAsync(string batchNo, string code, int qty, bool passed);

        /// <summary>
        /// 获取当班产量 (查询)
        /// </summary>
        Task<List<ProductionData>> GetTodayProductionAsync();

        /// <summary>
        /// 修正产量 (更新)
        /// </summary>
        Task UpdateQuantityAsync(int id, int newQuantity);

        // --- 业务模块 B: 日志管理 ---

        /// <summary>
        /// 写入报警/日志
        /// </summary>
        Task LogInfoAsync(string module, string message);
        Task LogErrorAsync(string module, string message);

        /// <summary>
        /// 清理旧日志 (删除)
        /// </summary>
        Task<bool> ClearLogsOlderThanAsync(int days);

        /// <summary>
        /// 分页获取日志
        /// </summary>
        Task<(List<DeviceLog> List, int Total)> GetLogsByPageAsync(int pageIndex, int pageSize);
    }

    public class DataBaseService : IDataBaseService
    {
        // 注入泛型仓储
        private readonly IRepository<ProductionData> _productionRepo;
        private readonly IRepository<DeviceLog> _logRepo;

        public DataBaseService(
            IRepository<ProductionData> productionRepo,
            IRepository<DeviceLog> logRepo)
        {
            _productionRepo = productionRepo;
            _logRepo = logRepo;
        }

        #region 生产数据管理

        public async Task AddProductionRecordAsync(string batchNo, string code, int qty, bool passed)
        {
            var entity = new ProductionData
            {
                BatchNo = batchNo,
                ProductCode = code,
                Quantity = qty,
                IsPassed = passed,
                Timestamp = DateTime.Now
            };
            await _productionRepo.InsertAsync(entity);
        }

        public async Task<List<ProductionData>> GetTodayProductionAsync()
        {
            var today = DateTime.Today;
            // 封装查询逻辑：只查今天的数据
            return (List<ProductionData>)await _productionRepo.GetListAsync(
                x => x.Timestamp >= today && x.Timestamp < today.AddDays(1)
            );
        }

        public async Task UpdateQuantityAsync(int id, int newQuantity)
        {
            // 先查后改（或者直接用 UpdateColumn，取决于仓储支持）
            var entity = await _productionRepo.GetAsync(x => x.Id == id);
            if (entity != null)
            {
                entity.Quantity = newQuantity;
                await _productionRepo.UpdateAsync(entity);
            }
        }

        #endregion

        #region 日志管理

        public async Task LogInfoAsync(string module, string message)
        {
            await _logRepo.InsertAsync(new DeviceLog
            {
                Module = module,
                Level = "Info",
                Message = message,
                CreateTime = DateTime.Now
            });
        }

        public async Task LogErrorAsync(string module, string message)
        {
            await _logRepo.InsertAsync(new DeviceLog
            {
                Module = module,
                Level = "Error",
                Message = message,
                CreateTime = DateTime.Now
            });
        }

        public async Task<bool> ClearLogsOlderThanAsync(int days)
        {
            var limitDate = DateTime.Now.AddDays(-days);
            // 封装删除逻辑：删除 N 天前的数据
            return await _logRepo.DeleteAsync(x => x.CreateTime < limitDate);
        }

        public async Task<(List<DeviceLog> List, int Total)> GetLogsByPageAsync(int pageIndex, int pageSize)
        {
            // 封装分页逻辑：按时间倒序
            return await _logRepo.GetPageListAsync(
                predicate: x => true, // 查询所有
                pageIndex: pageIndex,
                pageSize: pageSize,
                orderByDesc: x => x.CreateTime // 最新日志在最前
            );
        }

        #endregion
    }
    // 生产数据表
    [SugarTable("Production_Data")]
    public class ProductionData
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        public string BatchNo { get; set; } // 批次号
        public string ProductCode { get; set; } // 产品型号
        public int Quantity { get; set; } // 数量
        public bool IsPassed { get; set; } // 是否合格
        public DateTime Timestamp { get; set; } // 生产时间
    }

    // 设备日志表
    [SugarTable("Device_Logs")]
    public class DeviceLog
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        public string Module { get; set; } // 来源模块 (例如: PLC, Vision)
        public string Level { get; set; } // Info, Warning, Error
        public string Message { get; set; }
        public DateTime CreateTime { get; set; }
    }
}

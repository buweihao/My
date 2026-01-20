using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DbType = SqlSugar.DbType;

namespace MyDatabase
{
    // ==========================================
    // 1. 核心接口定义
    // ==========================================

    /// <summary>
    /// SqlSugar 客户端工厂接口 (用于解耦)
    /// </summary>
    public interface ISqlSugarClientFactory
    {
        SqlSugarClient GetClient();
        ConnectionConfig Config { get; }
    }

    /// <summary>
    /// 通用泛型仓储接口
    /// </summary>
    public interface IRepository<TEntity> where TEntity : class, new()
    {
        // --- 写入 ---
        Task<TEntity> InsertAsync(TEntity entity);
        Task<bool> InsertBatchAsync(IEnumerable<TEntity> entities);

        // --- 删除 ---
        Task<bool> DeleteAsync(TEntity entity);
        Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate);
        Task<bool> DeleteAsync(IEnumerable<TEntity> entities);

        // --- 更新 ---
        Task<bool> UpdateAsync(TEntity entity);
        Task<bool> UpdateAsync(IEnumerable<TEntity> entities);

        // --- 查询 ---
        Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate);
        Task<bool> IsAnyAsync(Expression<Func<TEntity, bool>> predicate);
        Task<IEnumerable<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate);
        Task<IEnumerable<TEntity>> GetAllAsync();

        // --- 分页 ---
        Task<(List<TEntity> Data, int TotalCount)> GetPageListAsync(
            Expression<Func<TEntity, bool>> predicate,
            int pageIndex, int pageSize,
            Expression<Func<TEntity, object>>? orderByDesc = null);
    }

    // ==========================================
    // 2. 核心实现类
    // ==========================================

    /// <summary>
    /// 工厂实现：负责持有配置并生产 Client
    /// </summary>
    public class SqlSugarClientFactory : ISqlSugarClientFactory
    {
        private readonly ConnectionConfig _config;

        // 通过构造函数注入配置，配置通常来自 AppSettings
        public SqlSugarClientFactory(ConnectionConfig config)
        {
            _config = config;
        }

        public ConnectionConfig Config => _config;

        public SqlSugarClient GetClient()
        {
            return new SqlSugarClient(_config);
        }
    }

    /// <summary>
    /// 泛型仓储实现
    /// </summary>
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class, new()
    {
        private readonly ISqlSugarClientFactory _factory;

        // DI 构造函数注入
        public Repository(ISqlSugarClientFactory factory)
        {
            _factory = factory;
        }

        #region 写入

        public async Task<TEntity> InsertAsync(TEntity entity)
        {
            using var db = _factory.GetClient();
            return await db.Insertable(entity).ExecuteReturnEntityAsync();
        }

        public async Task<bool> InsertBatchAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null || !entities.Any()) return false;
            using var db = _factory.GetClient();
            // 大数据量插入，SimpleClient/ExecuteCommandAsync 内部已包含事务优化
            return await db.Insertable(entities.ToList()).ExecuteCommandAsync() > 0;
        }

        #endregion

        #region 删除

        public async Task<bool> DeleteAsync(TEntity entity)
        {
            using var db = _factory.GetClient();
            return await db.Deleteable(entity).ExecuteCommandAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> predicate)
        {
            using var db = _factory.GetClient();
            return await db.Deleteable<TEntity>().Where(predicate).ExecuteCommandAsync() > 0;
        }

        public async Task<bool> DeleteAsync(IEnumerable<TEntity> entities)
        {
            using var db = _factory.GetClient();
            return await db.Deleteable(entities.ToList()).ExecuteCommandAsync() > 0;
        }

        #endregion

        #region 更新

        public async Task<bool> UpdateAsync(TEntity entity)
        {
            using var db = _factory.GetClient();
            return await db.Updateable(entity).ExecuteCommandAsync() > 0;
        }

        public async Task<bool> UpdateAsync(IEnumerable<TEntity> entities)
        {
            using var db = _factory.GetClient();
            return await db.Updateable(entities.ToList()).ExecuteCommandAsync() > 0;
        }

        #endregion

        #region 查询

        public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate)
        {
            using var db = _factory.GetClient();
            return await db.Queryable<TEntity>().FirstAsync(predicate);
        }

        public async Task<bool> IsAnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            using var db = _factory.GetClient();
            return await db.Queryable<TEntity>().AnyAsync(predicate);
        }

        public async Task<IEnumerable<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate)
        {
            using var db = _factory.GetClient();
            return await db.Queryable<TEntity>().Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            using var db = _factory.GetClient();
            return await db.Queryable<TEntity>().ToListAsync();
        }

        public async Task<(List<TEntity> Data, int TotalCount)> GetPageListAsync(
            Expression<Func<TEntity, bool>> predicate,
            int pageIndex, int pageSize,
            Expression<Func<TEntity, object>>? orderByDesc = null)
        {
            using var db = _factory.GetClient();
            var query = db.Queryable<TEntity>().Where(predicate);

            if (orderByDesc != null)
                query = query.OrderByDescending(orderByDesc);

            RefAsync<int> total = 0;
            var list = await query.ToPageListAsync(pageIndex, pageSize, total);
            return (list, total.Value);
        }

        #endregion
    }

    // ==========================================
    // 3. 数据库初始化器 (分离关注点)
    // ==========================================

    /// <summary>
    /// 负责数据库的创建、表结构初始化及 WAL 模式设置
    /// </summary>
    public class DbInitializer
    {
        private readonly ISqlSugarClientFactory _factory;

        public DbInitializer(ISqlSugarClientFactory factory)
        {
            _factory = factory;
        }

        public void Initialize(params Type[] entityTypes)
        {
            using var db = _factory.GetClient();

            // 1. 创建库文件
            db.DbMaintenance.CreateDatabase();

            // 2. [关键优化] SQLite 开启 WAL 模式 + Normal 同步
            // 工业现场必备！极大降低 "database is locked" 概率，允许读写并发。
            if (db.CurrentConnectionConfig.DbType == DbType.Sqlite)
            {
                try
                {
                    db.Ado.ExecuteCommand("PRAGMA journal_mode=WAL;");
                    db.Ado.ExecuteCommand("PRAGMA synchronous=NORMAL;");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SQLite WAL 设置警告: {ex.Message}");
                }
            }

            // 3. CodeFirst 自动建表
            if (entityTypes != null && entityTypes.Length > 0)
            {
                // 注意：这里只会创建不存在的表，不会删除已有数据
                db.CodeFirst.InitTables(entityTypes);
            }
        }
    }

    // ==========================================
    // 4. DI 扩展方法 (一行代码集成)
    // ==========================================

    public static class SqlSugarServiceExtensions
    {
        /// <summary>
        /// 注册 SqlSugar 仓储及自动初始化服务
        /// </summary>
        public static IServiceCollection AddMySqlSugarStore(
            this IServiceCollection services,
            ConnectionConfig config,
            params Type[] entityTypes) // 👈 这里直接传入需要建表的实体类型
        {
            // 1. 注册配置
            services.AddSingleton(config);

            // 2. 注册工厂
            services.AddSingleton<ISqlSugarClientFactory, SqlSugarClientFactory>();

            // 3. 注册仓储
            services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));

            // 4. [关键] 注册自动初始化服务
            // 这样你就不需要在 App.xaml.cs 的 OnStartup 里手动调用 Initialize 了
            services.AddHostedService(provider =>
                new DbInitializationService(
                    provider.GetRequiredService<ISqlSugarClientFactory>(),
                    entityTypes
                ));

            return services;
        }
    }

    // ==========================================
    // 新增：自动初始化服务 (IHostedService)
    // ==========================================
    /// <summary>
    /// 这是一个后台服务，随 Host.StartAsync() 自动运行
    /// 负责建库、建表和设置 WAL 模式
    /// </summary>
    public class DbInitializationService : IHostedService
    {
        private readonly ISqlSugarClientFactory _factory;
        private readonly Type[] _entityTypes;

        public DbInitializationService(ISqlSugarClientFactory factory, Type[] entityTypes)
        {
            _factory = factory;
            _entityTypes = entityTypes;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // 这里执行初始化逻辑
            using var db = _factory.GetClient();

            // 1. 建库
            db.DbMaintenance.CreateDatabase();

            // 2. SQLite 工业优化 (WAL)
            if (db.CurrentConnectionConfig.DbType == DbType.Sqlite)
            {
                try
                {
                    db.Ado.ExecuteCommand("PRAGMA journal_mode=WAL;");
                    db.Ado.ExecuteCommand("PRAGMA synchronous=NORMAL;");
                }
                catch { /* 忽略异常 */ }
            }

            // 3. 自动建表
            if (_entityTypes != null && _entityTypes.Length > 0)
            {
                db.CodeFirst.InitTables(_entityTypes);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }


}
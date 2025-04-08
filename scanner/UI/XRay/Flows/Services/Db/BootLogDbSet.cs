using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Db;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.Db
{
    /// <summary>
    /// 开机日志数据表管理
    /// </summary>
    public class BootLogDbSet
    {
        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }

        private DbSet<BootLog> Set
        {
            get { return ContextProvider.BootLogSet; }
        }

        public BootLogDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public void Add(BootLog log)
        {
            Set.Add(log);
            SaveChanges();
        }

        public void SaveChanges()
        {
            Context.SaveChanges();
        }

        /// <summary>
        /// 根据开机时间，查询指定时间范围内的开机日志
        /// </summary>
        /// <param name="startTime">指定起始时间，大于等于</param>
        /// <param name="endTime">指定结束时间，小于</param>
        /// <returns></returns>
        public List<BootLog> GetByBootTime(DateTime startTime, DateTime endTime)
        {
            return Set.Where(log => log.BootTime >= startTime && log.BootTime < endTime).ToList();
        }

        /// <summary>
        /// 异步获取开机时间
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public async Task<IQueryable<BootLog>> GetByTime(DateTime startTime, DateTime endTime)
        {
            return await Task.Run(() =>
            {
                return Set.Where(log => log.BootTime >= startTime && log.BootTime < endTime);
            });
        }
    }
}

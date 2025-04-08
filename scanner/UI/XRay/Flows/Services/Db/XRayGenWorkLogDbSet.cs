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
    /// X射线发生器工作日志数据库管理
    /// </summary>
    public class XRayGenWorkLogDbSet
    {
        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }

        private DbSet<XRayGenWorkLog> Set
        {
            get { return ContextProvider.XRayGenWorkLogSet; }
        }

        public XRayGenWorkLogDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public void Add(XRayGenWorkLog log)
        {
            Set.Add(log);
            SaveChanges();
        }

        public void SaveChanges()
        {
            Context.SaveChanges();
        }

        /// <summary>
        /// 根据起止时间，查询指定时间范围内的射线源工作日志
        /// </summary>
        /// <param name="startTime">指定起始时间，包含起始时间</param>
        /// <param name="endTime">指定结束时间，不包含结束时间</param>
        /// <returns></returns>
        public List<XRayGenWorkLog> GetByTime(DateTime startTime, DateTime endTime)
        {
            return Set.Where(log => log.Date >= startTime && log.Date < endTime).ToList();
        }

        /// <summary>
        /// 异步查询
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public async Task<IQueryable<XRayGenWorkLog>> GetLogs(DateTime startTime, DateTime endTime)
        {
            return await Task.Run(() =>
            {
                return Set.Where(log => log.Date >= startTime && log.Date < endTime);
            });
        }
    }
}

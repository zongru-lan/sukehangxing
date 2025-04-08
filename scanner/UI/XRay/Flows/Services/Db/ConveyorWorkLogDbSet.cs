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
    /// 输送机工作日志数据库管理
    /// </summary>
    public class ConveyorWorkLogDbSet
    {
         public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }

        private DbSet<ConveyorWorkLog> Set
        {
            get { return ContextProvider.ConveyorWorkLogSet; }
        }

        public ConveyorWorkLogDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public void Add(ConveyorWorkLog log)
        {
            Set.Add(log);
            SaveChanges();
        }

        public void SaveChanges()
        {
            Context.SaveChanges();
        }

        /// <summary>
        /// 根据起止时间，查询指定时间范围内的输送机工作日志
        /// </summary>
        /// <param name="startTime">指定起始时间，包含起始时间</param>
        /// <param name="endTime">指定结束时间，不包含结束时间</param>
        /// <returns></returns>
        public List<ConveyorWorkLog> GetByTime(DateTime startTime, DateTime endTime)
        {
            return Set.Where(log => log.Date >= startTime && log.Date < endTime).ToList();
        }
    }
}

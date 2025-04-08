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
    public class TipEventRecordDbSet
    {
        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }

        private DbSet<TipEventRecord> DbSet
        {
            get { return ContextProvider.TipEventRecordsSet; }
        }

        public TipEventRecordDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        /// <summary>
        /// 添加一个新的tip考核记录
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public TipEventRecord Add(TipEventRecord record)
        {
            DbSet.Add(record);
            Context.SaveChanges();
            return record;
        }

        public List<TipEventRecord> SelectAll()
        {
            return DbSet.Select(record => record).ToList();
        }

        /// <summary>
        /// 根据Tip考核记录时间范围，获取特定的记录
        /// </summary>
        /// <param name="startTime">记录时刻的最小值，包含此值</param>
        /// <param name="endTime">记录时刻的最大值，不包含此值</param>
        /// <returns></returns>
        public List<TipEventRecord> Select(DateTime startTime, DateTime endTime)
        {
            return DbSet.Where(record => record.InjectionTime >= startTime && record.InjectionTime < endTime).ToList();
        }
    }
}

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
    /// 工作会话记录数据集
    /// </summary>
    public class WorkSessionDbSet
    {
        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }

        private DbSet<WorkSession> DbSet
        {
            get { return ContextProvider.WorkSessionSet; }
        }

        public WorkSessionDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public void Add(WorkSession session)
        {
            DbSet.Add(session);
            Context.SaveChanges();
        }

        public void Update(WorkSession session)
        {
            Context.SaveChanges();
        }

        /// <summary>
        /// 根据登录时间，选择指定时间范围内的会话记录
        /// </summary>
        /// <param name="startTime">登录起始时间</param>
        /// <param name="endTime">登录结束时间</param>
        /// <returns></returns>
        public List<WorkSession> GetByLoginTime(DateTime startTime, DateTime endTime)
        {
            return DbSet.Where(session => session.LoginTime >= startTime && session.LoginTime <= endTime).ToList();
        }

        /// <summary>
        /// 根据登录时间，选择指定时间范围内的会话记录
        /// </summary>
        /// <param name="startTime">登录起始时间，包含此值</param>
        /// <param name="endTime">登录结束时间，不包含此值</param>
        /// <param name="accountId">指定的账户Id</param>
        /// <returns></returns>
        public List<WorkSession> GetByLoginTime(DateTime startTime, DateTime endTime, string accountId)
        {
            return DbSet.Where(session => session.LoginTime >= startTime && session.LoginTime < endTime && session.AccountId == accountId).ToList();
        }

        public WorkSession GetLast()
        {
            var last = DbSet.ToList();
            return last.Last();
        }
    }
}

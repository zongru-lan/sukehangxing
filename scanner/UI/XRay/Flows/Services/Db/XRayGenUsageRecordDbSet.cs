using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Db;
using UI.XRay.Business.Entities;
using System.Data.Entity.Migrations;

namespace UI.XRay.Flows.Services.Db
{
    /// <summary>
    /// 记录射线源的使用情况，
    /// 在更换射线源的情况下，可以对使用时间和次数做矫正
    /// </summary>
    public class XRayGenUsageRecordDbSet
    {
        private DbSet<XRayGenUsageRecord> XRayGenUsageRecordSet
        {
            get { return ContextProvider.XRayGenUsageRecordSet; }
        }
        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }
        /// <summary>
        /// 构造一个实例，同时将会创建一个新的数据库连接。
        /// </summary>
        public XRayGenUsageRecordDbSet(IScannerDbContextProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            ContextProvider = provider;
        }

        public XRayGenUsageRecordDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public void Add(XRayGenUsageRecord record)
        {
            XRayGenUsageRecordSet.Add(record);
            SaveChanges();
        }

        /// <summary>
        /// 保存此前所有修改
        /// </summary>
        private void SaveChanges()
        {
            Context.SaveChanges();
        }
        public XRayGenUsageRecord TakeLatest()
        {
            var query = XRayGenUsageRecordSet.OrderByDescending(record => record.ChangeTime).Take(1);
            return query.Count() > 0 ? query.ToList()[0] : new XRayGenUsageRecord("",0,0,0,0);
        }

        public void Update(XRayGenUsageRecord record)
        {
            if (record != null)
            {
                XRayGenUsageRecordSet.AddOrUpdate(record);
                SaveChanges();
            }
        }
    }
}

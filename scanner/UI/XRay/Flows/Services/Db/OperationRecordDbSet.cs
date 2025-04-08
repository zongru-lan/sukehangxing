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
    /// 用户的操作记录
    /// </summary>
    public class OperationRecordDbSet
    {
        private DbSet<OperationRecord> OperationRecordSet
        {
            get { return ContextProvider.OperationRecordSet; }
        }

        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }

        /// <summary>
        /// 构造一个实例，同时将会创建一个新的数据库连接。
        /// </summary>
        public OperationRecordDbSet(IScannerDbContextProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            ContextProvider = provider;
        }

        public OperationRecordDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public void Add(OperationRecord record, bool commitImmediately = true)
        {
            OperationRecordSet.Add(record);
            if (commitImmediately)
            {
                SaveChanges();
            }
        }

        public void AddRange(List<OperationRecord> records, bool commitImmediately = true)
        {
            OperationRecordSet.AddRange(records);
            if (commitImmediately)
            {
                SaveChanges();
            }
        }

        /// <summary>
        /// 获取所有操作信息
        /// </summary>
        /// <returns></returns>
        public List<OperationRecord> SelectAll()
        {
            return OperationRecordSet.Select(record => record).ToList();
        }

        public List<OperationRecord> SelectType(OperationUI type)
        {
            return OperationRecordSet.Where(record => record.OperateUI == type).ToList();
        }

        public List<OperationRecord> TakeByConditions(DateTime scanTimeStart, DateTime scanTimeEnd, List<string> accountId)
        {
            var query =
                OperationRecordSet.Where(record => record.OperateTime >= scanTimeStart && record.OperateTime <= scanTimeEnd);

            if (accountId != null && accountId.Count > 0)
            {
                query = query.Where(record => accountId.Contains(record.AccountId));
            }

            return query.OrderByDescending(r => r.OperateTime).ToList();
        }

        /// <summary>
        /// 删除一组图像记录
        /// </summary>
        /// <param name="records"></param>
        /// <param name="commitImmediately"></param>
        public void RemoveRange(IEnumerable<OperationRecord> records, bool commitImmediately = true)
        {
            OperationRecordSet.RemoveRange(records);

            if (commitImmediately)
            {
                SaveChanges();
            }
        }

        /// <summary>
        /// 保存此前所有修改
        /// </summary>
        public void SaveChanges()
        {
            Context.SaveChanges();
        }
    }
}

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
    public class TipPlanandImageDbSet
    {
        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }

        private DbSet<TipPlanandImage> DbSet
        {
            get { return ContextProvider.TipPlanandImageSet; }
        }        

        public TipPlanandImageDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public TipPlanandImageDbSet(IScannerDbContextProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            ContextProvider = provider;
        }

        /// <summary>
        /// 添加一条新的记录，成功后返回被添加的记录
        /// 添加成功后，记录中的 主键 将被自动分配一个值，即数据库自动生成的主键
        /// </summary>
        /// <param name="record"></param>
        /// <returns>返回添加成功的记录，其中的主键将自动被分配一个值</returns>
        public TipPlanandImage Add(TipPlanandImage record, bool commit)
        {
            DbSet.Add(record);

            if (commit)
            {
                SaveChanges();
            }
            return record;
        }

        public void AddorUpdate(TipPlanandImage plan)
        {
            DbSet.AddOrUpdate(plan);
            SaveChanges();
        }

        public void Remove(TipPlanandImage plan)
        {
            if(plan!=null)
            {
                DbSet.Remove(plan);
                SaveChanges();
            }
        }

        public void RemoveRange(List<TipPlanandImage> plans)
        {
            if (plans != null && plans.Count > 0)
            {
                DbSet.RemoveRange(plans);
                SaveChanges();
            }
        }

        /// <summary>
        /// 获取所有账户信息
        /// </summary>
        /// <returns></returns>
        public List<TipPlanandImage> SelectAll()
        {
            return DbSet.Select(plan => plan).ToList();
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

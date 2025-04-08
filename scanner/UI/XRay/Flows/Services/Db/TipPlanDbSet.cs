using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Db;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.Db
{
    /// <summary>
    /// Tip计划的数据库管理
    /// </summary>
    public class TipPlanDbSet
    {
        public IScannerDbContextProvider ContextProvider { get; private set; }

        protected DbContext Context
        {
            get { return ContextProvider.Context; }
        }

        private DbSet<TipPlan> DbSet
        {
            get { return ContextProvider.TipPlanSet; }
        }        

        public TipPlanDbSet()
        {
            ContextProvider = DbContextFactory.GetContextProvider();
        }

        public TipPlanDbSet(IScannerDbContextProvider provider)
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
        public TipPlan Add(TipPlan record, bool commit)
        {
            DbSet.Add(record);

            if (commit)
            {
                SaveChanges();
            }
            return record;
        }

        public void AddorUpdate(TipPlan plan)
        {
            DbSet.AddOrUpdate(plan);
            SaveChanges();
        }

        public void Remove(TipPlan plan)
        {
           
            if(plan!= null)
            {
                if(DbSet.Find(plan.TipPlanId) != null)
                {
                    DbSet.Remove(plan);
                    SaveChanges();
                }
            }
        }

        /// <summary>
        /// 获取所有账户信息
        /// </summary>
        /// <returns></returns>
        public List<TipPlan> SelectAll()
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

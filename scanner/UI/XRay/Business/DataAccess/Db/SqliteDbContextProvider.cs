using System.Data.Entity;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.DataAccess.Db
{
    /// <summary>
    /// 基于Sqlite的数据库上下文
    /// </summary>
    public class SqliteDbContextProvider : DbContext, IScannerDbContextProvider
    {
        /// <summary>
        /// 账户数据集。注意：这里的set访问器必须是public的
        /// </summary>
        public DbSet<Account> AccountSet { get; set; }

        public DbSet<AccountGroup> AccountGroupSet { get; set; }

        public DbSet<ImageRecord> ImageRecordSet { get; set; }

        public DbSet<TipPlan> TipPlanSet { get; set; }

        public DbSet<TipPlanandImage> TipPlanandImageSet { get; set; }

        public DbSet<TipEventRecord> TipEventRecordsSet { get; set; }

        public DbSet<WorkSession> WorkSessionSet { get; set; }

        public DbSet<XRayGenWorkLog> XRayGenWorkLogSet { get; set; }

        public DbSet<ConveyorWorkLog> ConveyorWorkLogSet { get; set; }

        public DbSet<BootLog> BootLogSet { get; set; }

        public DbSet<OperationRecord> OperationRecordSet { get; set; }

        public DbSet<XRayGenUsageRecord> XRayGenUsageRecordSet { get; set; }

        public DbContext Context
        {
            get { return this; }
        }

        public SqliteDbContextProvider()
            : base("XRay.Security.Scanner.DbConnection")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map schemas 
            modelBuilder.Configurations.Add(new AccountMap());
            modelBuilder.Configurations.Add(new AccountGroupMap());
            modelBuilder.Configurations.Add(new ImageRecordMap());
            modelBuilder.Configurations.Add(new WorkSessionMap());
            modelBuilder.Configurations.Add(new BootLogMap());
            modelBuilder.Configurations.Add(new TipPlanMap());
            modelBuilder.Configurations.Add(new TipPlanandImageMap());
            modelBuilder.Configurations.Add(new TipEventRecordMap());
            modelBuilder.Configurations.Add(new XRayGenWorkLogMap());
            modelBuilder.Configurations.Add(new ConveyorWorkLogMap());
            modelBuilder.Configurations.Add(new OperationRecordMap());
            modelBuilder.Configurations.Add(new XRayGenUsageRecordMap());
        }
    }
}

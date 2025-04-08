using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.DataAccess.Db
{
    internal class AccountMap : EntityTypeConfiguration<Account>
    {
        /// <summary>
        /// 构造函数，完成实体关系映射
        /// </summary>
        public AccountMap()
        {
            ToTable("Account");
        }
    }

    internal class AccountGroupMap : EntityTypeConfiguration<AccountGroup>
    {
        public AccountGroupMap()
        {
            ToTable("AccountGroup");
        }
    }

    internal class BootLogMap : EntityTypeConfiguration<BootLog>
    {
        public BootLogMap()
        {
            ToTable("BootLog");
        }
    }

    internal class ImageRecordMap : EntityTypeConfiguration<ImageRecord>
    {
        public ImageRecordMap()
        {
            ToTable("ImageRecord");

            this.Property(record => record.ImageRecordId);
        }
    }

    internal class TipPlanMap : EntityTypeConfiguration<TipPlan>
    {
        /// <summary>
        /// 构造函数，完成实体关系映射
        /// </summary>
        public TipPlanMap()
        {
            ToTable("TipPlan");
        }
    }

    internal class TipPlanandImageMap : EntityTypeConfiguration<TipPlanandImage>
    {
        /// <summary>
        /// 构造函数，完成实体关系映射
        /// </summary>
        public TipPlanandImageMap()
        {
            ToTable("TipPlanandImage");
        }
    }

    internal class TipEventRecordMap : EntityTypeConfiguration<TipEventRecord>
    {
        /// <summary>
        /// 构造函数，完成实体关系映射
        /// </summary>
        public TipEventRecordMap()
        {
            ToTable("TipEventRecord");
        }
    }


    internal class WorkSessionMap : EntityTypeConfiguration<WorkSession>
    {
        /// <summary>
        /// 构造函数，完成实体关系映射
        /// </summary>
        public WorkSessionMap()
        {
            ToTable("WorkSession");
        }
    }

    internal class XRayGenWorkLogMap : EntityTypeConfiguration<XRayGenWorkLog>
    {
        /// <summary>
        /// 构造函数，完成实体关系映射
        /// </summary>
        public XRayGenWorkLogMap()
        {
            ToTable("XRayGenWorkLog");
        }
    }

    internal class ConveyorWorkLogMap : EntityTypeConfiguration<ConveyorWorkLog>
    {
        /// <summary>
        /// 构造函数，完成实体关系映射
        /// </summary>
        public ConveyorWorkLogMap()
        {
            ToTable("ConveyorWorkLog");
        }
    }

    internal class OperationRecordMap :　EntityTypeConfiguration<OperationRecord>
    {
        public OperationRecordMap()
        {
            ToTable("OperationRecord");
        }
    }

    internal class XRayGenUsageRecordMap : EntityTypeConfiguration<XRayGenUsageRecord>
    {
        public XRayGenUsageRecordMap()
        {
            ToTable("XRayGenUsageRecord");
        }
    }
}

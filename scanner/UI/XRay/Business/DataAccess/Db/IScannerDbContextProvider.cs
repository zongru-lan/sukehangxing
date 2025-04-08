using System;
using System.Data.Entity;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.DataAccess.Db
{
    public interface IScannerDbContextProvider : IDisposable
    {
        DbContext Context { get; }

        DbSet<Account> AccountSet { get; }

        DbSet<AccountGroup> AccountGroupSet { get; }

        DbSet<ImageRecord> ImageRecordSet { get; }

        DbSet<TipPlan> TipPlanSet { get; }

        DbSet<TipPlanandImage> TipPlanandImageSet { get; }

        DbSet<TipEventRecord> TipEventRecordsSet { get; }

        DbSet<WorkSession> WorkSessionSet { get; }

        DbSet<XRayGenWorkLog> XRayGenWorkLogSet { get; }

        DbSet<ConveyorWorkLog> ConveyorWorkLogSet { get; }

        DbSet<BootLog> BootLogSet { get; }

        DbSet<OperationRecord> OperationRecordSet { get; }

        DbSet<XRayGenUsageRecord> XRayGenUsageRecordSet { get; }
    }
}

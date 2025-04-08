using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Services
{
    public class OperationRecordService
    {
        //public static OperationRecordService Service { get; private set; }

        //static OperationRecordService()
        //{
        //    Service = new OperationRecordService();
        //}

        public OperationRecordDbSet DbSet { get; set; }

        public OperationRecordService()
        {
            DbSet = new OperationRecordDbSet();
        }

        public void RecordOperation(OperationUI ui,string opobject, OperationCommand cmd, string content)
        {
            AddRecord(new OperationRecord()
            {
                AccountId = PermissionService.Service.CurrentAccount !=null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty,
                OperateUI = ui,
                OperateTime = DateTime.Now,
                OperateObject = opobject,
                OperateCommand = cmd,
                OperateContent = content,
            });
        }

        public void AddRecordRange(List<OperationRecord> records, bool commitImmediately = true)
        {
            if (DbSet!=null)
            {
                DbSet.AddRange(records,commitImmediately);
            }
        }

        public void AddRecord(OperationRecord record, bool commitImmediately = true)
        {            
            if (DbSet != null)
            {
                DbSet.Add(record,commitImmediately);
            }
        }
    }
}

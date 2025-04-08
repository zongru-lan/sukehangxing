using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Flows.Controllers.SummaryEntity;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 输送机工作日志查询控制器
    /// </summary>
    public class ConveyorWorkLogsRetrievalController : DevicePartWorkLogsRetrievalControllerBase
    {
        public List<PeriodWorkingHours> GetStatisticalResults()
        {
            // 根据当前选定的查询周期，查询指定时间范围内的记录
            var dbSet = new ConveyorWorkLogDbSet();
            var targetRecords = dbSet.GetByTime(StartDate, EndDate);

            return RetrieveStatisticalResults(targetRecords);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Flows.Controllers.SummaryEntity;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 登录日志检索控制器
    /// </summary>
    public class LoginLogsRetrievalController : LogsRetrievalControllerBase
    {
        public LoginLogsRetrievalController()
        {
            
        }

        /// <summary>
        /// 根据当前设定的检索条件，获取统计结果
        /// </summary>
        /// <returns></returns>
        public List<WorkSession> GetStatisticalResults(DateTime startTime,DateTime endTime)
        {
            var sessions = GetWorkSessions(startTime, endTime);
            return sessions;
        }

        /// <summary>
        /// 根据当前的年份、查询周期，查询相应的Tip考核记录
        /// </summary>
        /// <returns></returns>
        private List<WorkSession> GetWorkSessions(DateTime startTime, DateTime endTime)
        {
            var query = new WorkSessionDbSet();
            var records = query.GetByLoginTime(startTime, endTime);

            List<WorkSession> results = null;
            if (records != null && records.Count > 0)
            {
                results = new List<WorkSession>(records.Count);
            }

            var accounts = GetTargetAccountIds();
            if (accounts != null && results != null)
            {
                // 针对每个用户，统计其Tip考核结果
                foreach (var account in accounts)
                {
                    if (!string.IsNullOrEmpty(account))
                    {
                        results.AddRange(records.Where(record => record.AccountId == account));
                    }
                }
            }

            return results;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// 用户操作日志检索控制器
    /// </summary>
    public class OperationLogsRetrievalController : LogsRetrievalControllerBase
    {
       
       
        private List<OperationStatisticResult> _currentResult;
      
        /// <summary>
        /// 根据当前设定的检索条件，获取统计结果
        /// </summary>
        /// <returns></returns>
        public List<OperationStatisticResult> GetStatisticalResults()
        {
            var sessions = GetWorkSessions();

            var result = new List<OperationStatisticResult>();

            if (sessions != null)
            {
                var accounts = GetTargetAccountIds();
                if (accounts != null)
                {
                    // 针对每个用户，统计其Tip考核结果
                    foreach (var account in accounts)
                    {
                        if (!string.IsNullOrEmpty(account))
                        {
                            var accountResult = GetStaResultsOfAccount(sessions, account);
                            result.AddRange(accountResult);
                        }
                    }
                }
            }

            // 计算tip 漏检率
            foreach (var e in result)
            {
                if (e.TipInjectionCount > 0)
                {
                    e.MissRate = e.MissTipCount / (double)e.TipInjectionCount * 100;
                }
                else
                {
                    e.MissRate = 0;
                }
            }
            
             
                for(int i= result.Count-1; i>=0; i-- )
                {
                   var temp=result[i];

                  if (temp.BagCount == 0 && temp.TipInjectionCount == 0 && temp.TotolMarkCount == 0 && temp.MissTipCount == 0 && temp.MissRate == 0)
                  {
                    result.Remove(temp);
                  }
                }
            
            _currentResult = result;
            return result;
        }
     

        /// <summary>
        /// 获取针对某一用户的Tip考核统计
        /// </summary>
        /// <param name="records"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
        private List<OperationStatisticResult> GetStaResultsOfAccount(List<WorkSession> records, string accountId)
        {
            // 先根据账户Id，筛选出考核记录
            var targetRecords = records.Where(record => record.AccountId == accountId);

            var accountExam = new List<OperationStatisticResult>(53);
            if (SelectedPeriod == StatisticalPeriod.Dayly)
            {
                var days = DateTime.DaysInMonth(SelectedYear, SelectedMonth);

                for (int i = 1; i <= days; i++)
                {
                    accountExam.Add(new OperationStatisticResult() { AccountId = accountId, Date = new DateTime(SelectedYear, SelectedMonth, i).ToString("yyyy/MM/dd") });
                }

                foreach (var r in targetRecords)
                {
                    var exam = accountExam[r.LoginTime.Day - 1];
                    exam.BagCount += r.ObjectCount;
                    exam.TotolMarkCount += r.TotalMarkCount;
                    exam.MissTipCount += r.MissTipCount;
                    exam.TipInjectionCount += r.TipInjectionCount;
                    //exam.TotolMarkCount = exam.TipInjectionCount - exam.MissTipCount;
                    exam.LogInTimes++;
                }
            }
            else if (SelectedPeriod == StatisticalPeriod.Monthly)
            {
                for (int i = 1; i <= 12; i++)
                {
                    accountExam.Add(new OperationStatisticResult() { AccountId = accountId, Date = new DateTime(SelectedYear, i, i).ToString("yyyy/MM") });
                }

                foreach (var r in targetRecords)
                {
                    var exam = accountExam[r.LoginTime.Month - 1];
                    exam.BagCount += r.ObjectCount;
                    exam.TotolMarkCount += r.TotalMarkCount;
                    exam.MissTipCount += r.MissTipCount;
                    exam.TipInjectionCount += r.TipInjectionCount;
                    //exam.TotolMarkCount = exam.TipInjectionCount - exam.MissTipCount;
                    exam.LogInTimes++;
                }
            }
            else if (SelectedPeriod == StatisticalPeriod.Weekly)
            {
                for (int i = 1; i <= 52; i++)
                {
                    accountExam.Add(new OperationStatisticResult() { AccountId = accountId, Date = string.Format("{0}-{1}", SelectedYear, i) });
                }

                foreach (var r in targetRecords)
                {
                    var weekOfYear = r.LoginTime.WeekOfYear();

                    if (weekOfYear == 53 && accountExam.Count == 52)
                    {
                        accountExam.Add(new OperationStatisticResult() { AccountId = accountId, Date = string.Format("{0}-{1}", SelectedYear, 53) });
                    }

                    var exam = accountExam[weekOfYear - 1];
                    exam.BagCount += r.ObjectCount;
                    exam.TotolMarkCount += r.TotalMarkCount;
                    exam.MissTipCount += r.MissTipCount;
                    exam.TipInjectionCount += r.TipInjectionCount;
                    //exam.TotolMarkCount = exam.TipInjectionCount - exam.MissTipCount;
                    exam.LogInTimes++;
                }
            }
            else
            {
                accountExam.Add(new OperationStatisticResult() { AccountId = accountId, Date = TranslationService.FindTranslation("Season", "Q1") });
                accountExam.Add(new OperationStatisticResult() { AccountId = accountId, Date = TranslationService.FindTranslation("Season", "Q2") });
                accountExam.Add(new OperationStatisticResult() { AccountId = accountId, Date = TranslationService.FindTranslation("Season", "Q3") });
                accountExam.Add(new OperationStatisticResult() { AccountId = accountId, Date = TranslationService.FindTranslation("Season", "Q4") });

                foreach (var r in targetRecords)
                {
                    var s = (r.LoginTime.Month - 1) / 3;
                    var exam = accountExam[s];
                    exam.BagCount += r.ObjectCount;
                    exam.TotolMarkCount += r.TotalMarkCount;
                    exam.MissTipCount += r.MissTipCount;
                    exam.TipInjectionCount += r.TipInjectionCount;
                    //exam.TotolMarkCount = exam.TipInjectionCount - exam.MissTipCount;
                    exam.LogInTimes++;
                }
            }

            return accountExam;
        }

        /// <summary>
        /// 根据当前的年份、查询周期，查询相应的Tip考核记录
        /// </summary>
        /// <returns></returns>
        private List<WorkSession> GetWorkSessions()
        {
            var query = new WorkSessionDbSet();
            return query.GetByLoginTime(StartDate, EndDate);
        }

        public List<OperationRecord> GetOperationRecord(DateTime startDate,DateTime endDate)
        {
            var query = new OperationRecordDbSet();
            return query.TakeByConditions(startDate, endDate, GetTargetAccountIds());
        }

        public void CleanEarlyRecord(DateTime endtime)
        {
            var query = new OperationRecordDbSet();
            var controller = new AccountDbSet();
            var allAccount = controller.SelectAll();
            var allRecords = query.TakeByConditions(new DateTime(2020, 1, 1), endtime, allAccount.Select(a=>a.AccountId).ToList());
            query.RemoveRange(allRecords);
        }
    }
}

using System;
using System.Collections.Generic;
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
    /// 设备部件工作日志获取控制器基类：提供对统计结果的汇总计算方法
    /// </summary>
    public class DevicePartWorkLogsRetrievalControllerBase : LogsRetrievalControllerBase
    {
        /// <summary>
        /// 根据数据库查询的统计设备部件工作时长日志，以及当前选中的统计周期、统计起始时间等，计算统计结果
        /// </summary>
        /// <param name="targetRecords"></param>
        /// <returns></returns>
        protected List<PeriodWorkingHours> RetrieveStatisticalResults(IEnumerable<DevicePartWorkLog> targetRecords)
        {
            // 根据当前选定的查询周期，查询指定时间范围内的记录
            var dbSet = new ConveyorWorkLogDbSet();

            var results = new List<PeriodWorkingHours>();

            // 对记录进行统计
            if (SelectedPeriod == StatisticalPeriod.Dayly)
            {
                var days = DateTime.DaysInMonth(SelectedYear, SelectedMonth);

                for (int i = 1; i <= days; i++)
                {
                    results.Add(new PeriodWorkingHours() { Date = new DateTime(SelectedYear, SelectedMonth, i).ToString("yyyy/MM/dd") });
                }

                foreach (var r in targetRecords)
                {
                    var log = results[r.Date.Day - 1];
                    log.Hours += TimeSpan.FromSeconds(r.Seconds).TotalHours;
                }
            }
            else if (SelectedPeriod == StatisticalPeriod.Hourly)
            {
                for (int i = 0; i <= 23; i++)
                {
                    results.Add(new PeriodWorkingHours() { Date = i.ToString("00") });
                }

                foreach (var r in targetRecords)
                {
                    var log = results[r.Hour];
                    log.Hours += TimeSpan.FromSeconds(r.Seconds).TotalHours;
                }
            }
            else if (SelectedPeriod == StatisticalPeriod.Monthly)
            {
                for (int i = 1; i <= 12; i++)
                {
                    results.Add(new PeriodWorkingHours() { Date = new DateTime(SelectedYear, i, i).ToString("yyyy/MM") });
                }

                foreach (var r in targetRecords)
                {
                    var log = results[r.Date.Month - 1];
                    log.Hours += TimeSpan.FromSeconds(r.Seconds).TotalHours;
                }
            }
            else if (SelectedPeriod == StatisticalPeriod.Weekly)
            {
                for (int i = 1; i <= 52; i++)
                {
                    results.Add(new PeriodWorkingHours() { Date = string.Format("{0}-{1}", SelectedYear, i) });
                }

                foreach (var r in targetRecords)
                {
                    var weekOfYear = r.Date.WeekOfYear();

                    if (weekOfYear == 53 && results.Count == 52)
                    {
                        results.Add(new PeriodWorkingHours() { Date = string.Format("{0}-{1}", SelectedYear, 53) });
                    }

                    var log = results[weekOfYear - 1];
                    log.Hours += TimeSpan.FromSeconds(r.Seconds).TotalHours;
                }
            }
            else
            {
                results.Add(new PeriodWorkingHours() { Date = TranslationService.FindTranslation("Season", "Q1") });
                results.Add(new PeriodWorkingHours() { Date = TranslationService.FindTranslation("Season", "Q2") });
                results.Add(new PeriodWorkingHours() { Date = TranslationService.FindTranslation("Season", "Q3") });
                results.Add(new PeriodWorkingHours() { Date = TranslationService.FindTranslation("Season", "Q4") });

                foreach (var r in targetRecords)
                {
                    var s = (r.Date.Month - 1) / 3;
                    var log = results[s];
                    log.Hours += TimeSpan.FromSeconds(r.Seconds).TotalHours;
                }
            }

            var total = new PeriodWorkingHours()
            {
                Date = TranslationService.FindTranslation("Total"),
                Hours = results.Sum(r => r.Hours)
            };

            results.Add(total);
            return results;
        }
    }
}

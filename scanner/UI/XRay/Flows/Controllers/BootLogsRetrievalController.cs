using System;
using System.Collections.Generic;
using System.Linq;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Flows.Controllers.SummaryEntity;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 开机日志检索控制
    /// </summary>
    public class BootLogsRetrievalController : LogsRetrievalControllerBase
    {
        public List<BootWorkingHours> GetStatisticalResults()
        {
            // 根据当前选定的查询周期，查询指定时间范围内的记录
            var dbSet = new BootLogDbSet();
            var targetRecords = dbSet.GetByBootTime(StartDate, EndDate);

            var imageSet = new ImageRecordDbSet();

            var results = new List<BootWorkingHours>();


            // 对记录进行统计
            // 只显示时间不为零的条目，因此先生成所有日/周/月/季的条目，然后移除工作时间为0的
            if (SelectedPeriod == StatisticalPeriod.Dayly)
            {
                var days = DateTime.DaysInMonth(SelectedYear, SelectedMonth);

                for (int i = 1; i <= days; i++)
                {
                    var date = new DateTime(SelectedYear, SelectedMonth, i);
                    var count = imageSet.CountByConditions(date, date.AddDays(1));
                    results.Add(new BootWorkingHours() { Date = date.ToString("yyyy/MM/dd"), BagCount = count });
                }

                foreach (var r in targetRecords)
                {
                    var log = results[r.BootTime.Day - 1];
                    log.Hours += r.WorkingTimeSpan.TotalHours;
                }
            }
            else if (SelectedPeriod == StatisticalPeriod.Monthly)
            {
                for (int i = 1; i <= 12; i++)
                {
                    var start = new DateTime(SelectedYear, i, 1);
                    var end = new DateTime(SelectedYear, i, 1).AddMonths(1);
                    var count = imageSet.CountByConditions(start, end);
                    results.Add(new BootWorkingHours() { Date = new DateTime(SelectedYear, i, i).ToString("yyyy/MM"), BagCount = count });
                }

                foreach (var r in targetRecords)
                {
                    var log = results[r.BootTime.Month - 1];
                    log.Hours += r.WorkingTimeSpan.TotalHours;
                }
            }
            else if (SelectedPeriod == StatisticalPeriod.Weekly)
            {
                for (int i = 1; i <= 52; i++)
                {
                    var tuple = GetFirstEndDayOfWeek(SelectedYear, i);
                    var count = imageSet.CountByConditions(tuple.Item1, tuple.Item2);
                    results.Add(new BootWorkingHours() { Date = string.Format("{0}-{1}", SelectedYear, i), BagCount = count });
                }

                foreach (var r in targetRecords)
                {
                    var weekOfYear = r.BootTime.WeekOfYearFromFirstDay();

                    if (weekOfYear == 53 && results.Count == 52)
                    {
                        var tuple = GetFirstEndDayOfWeek(SelectedYear, 53);
                        var count = imageSet.CountByConditions(tuple.Item1, tuple.Item2);
                        results.Add(new BootWorkingHours() { Date = string.Format("{0}-{1}", SelectedYear, 53), BagCount = count });
                    }

                    var log = results[weekOfYear - 1];
                    log.Hours += r.WorkingTimeSpan.TotalHours;
                }
            }
            else
            {
                var count = imageSet.CountByConditions(new DateTime(SelectedYear, 1, 1), new DateTime(SelectedYear, 4, 1));
                results.Add(new BootWorkingHours() { Date = TranslationService.FindTranslation("Season", "Q1"), BagCount = count });
                count = imageSet.CountByConditions(new DateTime(SelectedYear, 4, 1), new DateTime(SelectedYear, 7, 1));
                results.Add(new BootWorkingHours() { Date = TranslationService.FindTranslation("Season", "Q2"), BagCount = count });
                count = imageSet.CountByConditions(new DateTime(SelectedYear, 7, 1), new DateTime(SelectedYear, 10, 1));
                results.Add(new BootWorkingHours() { Date = TranslationService.FindTranslation("Season", "Q3"), BagCount = count });
                count = imageSet.CountByConditions(new DateTime(SelectedYear, 10, 1), new DateTime(SelectedYear + 1, 1, 1));
                results.Add(new BootWorkingHours() { Date = TranslationService.FindTranslation("Season", "Q4"), BagCount = count });

                foreach (var r in targetRecords)
                {
                    var s = (r.BootTime.Month - 1) / 3;
                    var log = results[s];
                    log.Hours += r.WorkingTimeSpan.TotalHours;
                }
            }

            // 移除开机时间和包裹数为0的条目
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Hours == 0 && results[i].BagCount == 0)
                {
                    results.Remove(results[i]);
                    i--;
                }
                else
                {
                    results[i].H = sec_to_hms(results[i].Hours * 3600);
                }
            }

            var hours = results.Sum(r => r.Hours);
            var total = new BootWorkingHours()
            {
                Date = TranslationService.FindTranslation("Total"),
                Hours = hours,
                H = sec_to_hms(hours*3600),//YXC
                BagCount = results.Sum(r => r.BagCount)
            };
            results.Add(total);
            return results;
        }

        public string sec_to_hms(double duration)  //yxc 时间格式转换
        {
            int du = Convert.ToInt32(duration);
            int A, B, C;
            A = (du / 3600);
            B = ((du - A * 3600) / 60);
            C = (du - A * 3600 - B * 60);

            //  TimeSpan ts = new TimeSpan(0, 0, Convert.ToInt32(duration));

            string str = "";
            if (A > 0)
            {
                str = String.Format("{0:00}", A) + ":" + String.Format("{0:00}", B) + ":" + String.Format("{0:00}", C);
            }
            if (A == 0 && B > 0)
            {
                str = "00:" + String.Format("{0:00}", B) + ":" + String.Format("{0:00}", C);
            }
            if (A == 0 && B == 0)
            {
                str = "00:00:" + String.Format("{0:00}", C);
            }
            return str;

        }


        Tuple<DateTime, DateTime> GetFirstEndDayOfWeek(int year, int weekNumber)
        {
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("zh-CN");
            culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Monday;
            System.Globalization.Calendar calendar = culture.Calendar;
            DateTime firstOfYear = new DateTime(year, 1, 1, calendar);
            DateTime lastOfYear = new DateTime(year, 12, 31, calendar);
            DateTime targetDay = calendar.AddWeeks(firstOfYear, weekNumber - 1);
            DayOfWeek firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;

            while (targetDay.DayOfWeek != firstDayOfWeek)
            {
                targetDay = targetDay.AddDays(-1);
            }
            var Weakend = targetDay.AddDays(7);
            var rst = DateTime.Compare(firstOfYear, targetDay);
            return Tuple.Create<DateTime, DateTime>(rst > 0 ? firstOfYear : targetDay, DateTime.Compare(lastOfYear, Weakend) > 0 ? Weakend : lastOfYear); ;
        }
    }
}

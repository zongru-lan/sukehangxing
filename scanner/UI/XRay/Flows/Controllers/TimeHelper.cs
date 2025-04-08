using System;
using System.Collections.Generic;
using System.Linq;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Flows.Controllers
{
    public static class TimeHelper
    {
        /// <summary>
        /// 枚举所有的小时，即0-23
        /// </summary>
        /// <returns></returns>
        public static List<int> EnumAllHours()
        {
            var list = new List<int>();
            for (var i = 0; i <= 23; i++)
            {
                list.Add(i);
            }

            return list;
        }

        /// <summary>
        /// 枚举所有的分钟，即0-59
        /// </summary>
        /// <returns></returns>
        public static List<int> EnumAllMinutes()
        {
            var list = new List<int>();
            for (var i = 0; i <= 59; i++)
            {
                list.Add(i);
            }

            return list;
        }

        /// <summary>
        /// 获取2015年以来的所有年份枚举列表
        /// </summary>
        /// <returns></returns>
        public static List<int> EnumAllYears()
        {
            var list = new List<int>();

            var curYear = DateTime.Now.Year;

            for (int i = 2015; i <= curYear; i++)
            {
                list.Add(i);
            }

            return list;
        }

        public static List<int> EnumAllMonths()
        {
            return new List<int>(){1,2,3,4,5,6,7,8,9,10,11,12};
        }

        public static List<ValueStringItem<int>> GetMonthStringList()
        {
            var result = new List<ValueStringItem<int>>();

            result.Add(new ValueStringItem<int>(1, TranslationService.FindTranslation("Month", "January")));
            result.Add(new ValueStringItem<int>(2, TranslationService.FindTranslation("Month", "February")));
            result.Add(new ValueStringItem<int>(3, TranslationService.FindTranslation("Month", "March")));
            result.Add(new ValueStringItem<int>(4, TranslationService.FindTranslation("Month", "April")));
            result.Add(new ValueStringItem<int>(5, TranslationService.FindTranslation("Month", "May")));
            result.Add(new ValueStringItem<int>(6, TranslationService.FindTranslation("Month", "June")));
            result.Add(new ValueStringItem<int>(7, TranslationService.FindTranslation("Month", "July")));
            result.Add(new ValueStringItem<int>(8, TranslationService.FindTranslation("Month", "August")));
            result.Add(new ValueStringItem<int>(9, TranslationService.FindTranslation("Month", "September")));
            result.Add(new ValueStringItem<int>(10, TranslationService.FindTranslation("Month", "October")));
            result.Add(new ValueStringItem<int>(11, TranslationService.FindTranslation("Month", "November")));
            result.Add(new ValueStringItem<int>(12, TranslationService.FindTranslation("Month", "December")));

            return result;
        }

        /// <summary>
        /// 获取指定月份的天的列表
        /// </summary>
        /// <returns></returns>
        public static List<int> GetDaysListOfMonth(int year, int month)
        {
            var daysCount = DateTime.DaysInMonth(year, month);

            var list = new List<int>();
            for (int i = 1; i <= daysCount; i++)
            {
                list.Add(i);
            }

            return list;
        }

        /// <summary>
        /// 获取统计周期的值-字符串列表：按小时、日、周、月、季度、自定义等
        /// </summary>
        /// <returns></returns>
        public static List<ValueStringItem<StatisticalPeriod>> GetStatisticsPeriods()
        {
            return (from StatisticalPeriod p in Enum.GetValues(typeof (StatisticalPeriod))
                select new ValueStringItem<StatisticalPeriod>(p, TranslationService.FindTranslation(p))).ToList();
        }

        /// <summary>
        /// 如果12月31号与下一年的1月1好在同一个星期则算下一年的第一周
        /// </summary>
        /// <param name="dTime"></param>
        /// <returns></returns>
        public static int GetWeekIndex(DateTime dTime)
        {
            try
            {
                int dayOfYear = dTime.DayOfYear;

                //当年第一天
                var firstDay = new DateTime(dTime.Year, 1, 1);

                //确定当年第一天
                int weekdayOfFirstDay = (int)firstDay.DayOfWeek;

                weekdayOfFirstDay = weekdayOfFirstDay == 0 ? 7 : weekdayOfFirstDay;

                //确定星期几
                int index = (int)dTime.DayOfWeek;

                index = index == 0 ? 7 : index;

                //当前周的范围
                DateTime retStartDay = dTime.AddDays(-(index - 1));
                DateTime retEndDay = dTime.AddDays(7 - index);

                //确定当前是第几周
                int weekIndex = (int)Math.Ceiling(((double)dayOfYear + weekdayOfFirstDay - 1) / 7);


                if (retStartDay.Year < retEndDay.Year)
                {
                    weekIndex = 1;
                }

                return weekIndex;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            //string retVal = retStartDay.ToString("yyyy/MM/dd") + "～" + retEndDay.ToString("yyyy/MM/dd");

        }

        #region 获取某一天是一年中的第几各周

        /// <summary>
        /// Offsets to move the day of the year on a week, allowing
        /// for the current year Jan 1st day of week, and the Sun/Mon 
        /// week start difference between ISO 8601 and Microsoft
        /// </summary>
        private static int[] moveByDays = { 6, 7, 8, 9, 10, 4, 5 };

        /// <summary>
        /// Get the Week number of the year
        /// (In the range 1..53)
        /// This conforms to ISO 8601 specification for week number.
        /// </summary>
        /// <param name="date"></param>
        /// <returns>Week of year</returns>
        public static int WeekOfYear(this DateTime date)
        {
            DateTime startOfYear = new DateTime(date.Year, 1, 1);
            DateTime endOfYear = new DateTime(date.Year, 12, 31);
            // ISO 8601 weeks start with Monday 
            // The first week of a year includes the first Thursday 
            // This means that Jan 1st could be in week 51, 52, or 53 of the previous year...
            int numberDays = date.Subtract(startOfYear).Days +
                             moveByDays[(int)startOfYear.DayOfWeek];
            int weekNumber = numberDays / 7;
            switch (weekNumber)
            {
                case 0:
                    // Before start of first week of this year - in last week of previous year
                    weekNumber = WeekOfYear(startOfYear.AddDays(-1));
                    break;
                case 53:
                    // In first week of next year.
                    if (endOfYear.DayOfWeek < DayOfWeek.Thursday)
                    {
                        weekNumber = 1;
                    }
                    break;
            }
            return weekNumber;
        }

        /// <summary>
        /// 一年的第一天所在的周算作第一周
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int WeekOfYearFromFirstDay(this DateTime date)
        {
            DateTime startOfYear = new DateTime(date.Year, 1, 1);
            int daysOfFirstWeek = 7 - ((int)startOfYear.DayOfWeek + 6) % 7;
            int weekNumber = (date.Subtract(startOfYear).Days + 7 - daysOfFirstWeek) / 7 + 1;

            return weekNumber;
        }

        #endregion 获取某一天是一年中的第几各周
    }
}

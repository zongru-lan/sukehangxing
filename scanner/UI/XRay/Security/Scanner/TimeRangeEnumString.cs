using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// 时间范围枚举与其对应的字符串，供ComboBox等绑定使用
    /// </summary>
    public class TimeRangeEnumString
    {
        public TimeRange TimeRange { get; private set; }

        public string Translation { get; private set; }

        public TimeRangeEnumString(TimeRange tr, string str)
        {
            TimeRange = tr;
            Translation = str;
        }

        /// <summary>
        /// 获取所有与时间范围有关的枚举字符串
        /// </summary>
        /// <returns></returns>
        public static List<TimeRangeEnumString> EnumAll()
        {
            var list = new List<TimeRangeEnumString>(6);
            list.Add(new TimeRangeEnumString(TimeRange.LastHour, TranslationService.FindTranslation("TimeRange", "LastHour")));
            list.Add(new TimeRangeEnumString(TimeRange.Today, TranslationService.FindTranslation("TimeRange", "Today")));
            list.Add(new TimeRangeEnumString(TimeRange.Yestoday, TranslationService.FindTranslation("TimeRange", "Yestoday")));
            list.Add(new TimeRangeEnumString(TimeRange.RecentWeek, TranslationService.FindTranslation("TimeRange", "RecentWeek")));
            list.Add(new TimeRangeEnumString(TimeRange.RecentMonth, TranslationService.FindTranslation("TimeRange", "RecentMonth")));
            list.Add(new TimeRangeEnumString(TimeRange.SpecifiedTimeRange, TranslationService.FindTranslation("TimeRange", "SpecifiedTimeRange")));

            return list;
        }
    }
}

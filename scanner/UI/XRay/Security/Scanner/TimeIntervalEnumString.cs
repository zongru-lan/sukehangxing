using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner
{
    public class TimeIntervalEnumString
    {
        public TimeIntervalEnum Interval { get; private set; }
        public string Translation { get; private set; }

        private static double PerSecond = 0.0001; // 每分钟秒数

        private static int PerMinuteSecond = 60; // 每分钟秒数
        
        private static int PerHourSecond = 60 * 60; // 每小时秒数

        private static int HalfHour = 30 * 60;

        public TimeIntervalEnumString(TimeIntervalEnum tr, string str)
        {
            Interval = tr;
            Translation = str;
        }

        /// <summary>
        /// 获取所有与时间范围有关的枚举字符串
        /// </summary>
        /// <returns></returns>
        //public static List<TimeIntervalEnumString> EnumAll()
        //{
        //    var list = new List<TimeIntervalEnumString>(6);
        //    //list.Add(new TimeIntervalEnumString(TimeIntervalEnum.HalfAMinute, TranslationService.FindTranslation("Interval", "HalfAMinute")));
        //    //list.Add(new TimeIntervalEnumString(TimeIntervalEnum.OneMinute, TranslationService.FindTranslation("Interval", "OneMinute")));
        //    list.Add(new TimeIntervalEnumString(TimeIntervalEnum.HalfHour, TranslationService.FindTranslation("Interval", "HalfHour")));
        //    list.Add(new TimeIntervalEnumString(TimeIntervalEnum.OneHour, TranslationService.FindTranslation("Interval", "OneHour")));
        //    list.Add(new TimeIntervalEnumString(TimeIntervalEnum.SesquiHour, TranslationService.FindTranslation("Interval", "SesquiHour")));
        //    list.Add(new TimeIntervalEnumString(TimeIntervalEnum.TwoHours, TranslationService.FindTranslation("Interval", "TwoHours")));
        //    list.Add(new TimeIntervalEnumString(TimeIntervalEnum.TwoAndAHalfHours, TranslationService.FindTranslation("Interval", "TwoAndAHalfHours")));
        //    list.Add(new TimeIntervalEnumString(TimeIntervalEnum.ThreeHours, TranslationService.FindTranslation("Interval", "ThreeHours")));
        //    list.Add(new TimeIntervalEnumString(TimeIntervalEnum.ThreeAndHalfHours, TranslationService.FindTranslation("Interval", "ThreeAndHalfHours")));
        //    list.Add(new TimeIntervalEnumString(TimeIntervalEnum.FourHours, TranslationService.FindTranslation("Interval", "FourHours")));

        //    return list;
        //}

        public static List<TimeIntervalEnumString> EnumAll()
        {
            var list = new List<TimeIntervalEnumString>(6);
            //list.Add(new TimeIntervalEnumString(TimeIntervalEnum.HalfAMinute, TranslationService.FindTranslation("Interval", "HalfAMinute")));
            //list.Add(new TimeIntervalEnumString(TimeIntervalEnum.OneMinute, TranslationService.FindTranslation("Interval", "OneMinute")));
            list.Add(new TimeIntervalEnumString(TimeIntervalEnum.TenMinute, TranslationService.FindTranslation("Interval", "TenMinute")));
            list.Add(new TimeIntervalEnumString(TimeIntervalEnum.TwentyMinute, TranslationService.FindTranslation("Interval", "TwentyMinute")));
            list.Add(new TimeIntervalEnumString(TimeIntervalEnum.HalfHour, TranslationService.FindTranslation("Interval", "HalfHour")));
            list.Add(new TimeIntervalEnumString(TimeIntervalEnum.FortyMinute, TranslationService.FindTranslation("Interval", "FortyMinute")));
            list.Add(new TimeIntervalEnumString(TimeIntervalEnum.FiftyMinute, TranslationService.FindTranslation("Interval", "FiftyMinute")));
            list.Add(new TimeIntervalEnumString(TimeIntervalEnum.OneHour, TranslationService.FindTranslation("Interval", "OneHour")));
            return list;
        }
        //public static double GetEnumToSecond(TimeIntervalEnum intervalEnum)
        //{
        //    double second = 0;
        //    switch (intervalEnum)
        //    {
        //        //case TimeIntervalEnum.HalfAMinute: second = 0.5 * PerMinuteSecond; break;
        //        //case TimeIntervalEnum.OneMinute: second = PerMinuteSecond; break;
        //        case TimeIntervalEnum.HalfHour: second = 0.5 * PerHourSecond; break;
        //        case TimeIntervalEnum.OneHour: second = PerHourSecond; break;
        //        case TimeIntervalEnum.SesquiHour: second = 1.5 * PerHourSecond; break;
        //        case TimeIntervalEnum.TwoHours: second = 2 * PerHourSecond; break;
        //        case TimeIntervalEnum.TwoAndAHalfHours: second = 2.5 * PerHourSecond; break;
        //        case TimeIntervalEnum.ThreeHours: second = 3 * PerHourSecond; break;
        //        case TimeIntervalEnum.ThreeAndHalfHours: second = 3.5 * PerHourSecond; break;
        //        case TimeIntervalEnum.FourHours: second = 4 * PerHourSecond; break;
        //    }
        //    return second;
        //}

        public static double GetEnumToSecond(TimeIntervalEnum intervalEnum)
        {
            double second = 0;
            switch (intervalEnum)
            {
                //case TimeIntervalEnum.HalfAMinute: second = 0.5 * PerMinuteSecond; break;
                //case TimeIntervalEnum.OneMinute: second = PerMinuteSecond; break;
                case TimeIntervalEnum.TenMinute: second = 10 * PerMinuteSecond; break;
                case TimeIntervalEnum.TwentyMinute: second = 20 * PerMinuteSecond; break;
                case TimeIntervalEnum.HalfHour: second = 0.5 * PerHourSecond; break;
                case TimeIntervalEnum.FortyMinute: second = 40 * PerMinuteSecond; break;
                case TimeIntervalEnum.FiftyMinute: second = 50 * PerMinuteSecond; break;
                case TimeIntervalEnum.OneHour: second = PerHourSecond; break;
            }
            return second;
        }
        //public static double GetEnumToSecond2(TimeIntervalEnum intervalEnum)
        //{
        //    double second = 0;
        //    switch (intervalEnum)
        //    {
        //        //case TimeIntervalEnum.HalfAMinute: second = 30 * PerSecond; break;
        //        //case TimeIntervalEnum.OneMinute: second = 60 * PerSecond; break;
        //        case TimeIntervalEnum.HalfHour: second = HalfHour * PerSecond; break;
        //        case TimeIntervalEnum.OneHour: second = (HalfHour * 2) * PerSecond; break;
        //        case TimeIntervalEnum.SesquiHour: second = (HalfHour * 3) * PerSecond; break;
        //        case TimeIntervalEnum.TwoHours: second = (HalfHour * 4) * PerSecond; break;
        //        case TimeIntervalEnum.TwoAndAHalfHours: second = (HalfHour * 5) * PerSecond; break;
        //        case TimeIntervalEnum.ThreeHours: second = (HalfHour * 6) * PerSecond; break;
        //        case TimeIntervalEnum.ThreeAndHalfHours: second = (HalfHour * 7) * PerSecond; break;
        //        case TimeIntervalEnum.FourHours: second = (HalfHour * 8) * PerSecond; break;
        //    }
        //    return second;
        //}
    }
}

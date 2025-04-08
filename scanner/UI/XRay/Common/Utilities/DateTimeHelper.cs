using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Common.Utilities
{
    public static class DateTimeHelper
    {
        [DllImport("Kernel32.dll")]
        public static extern bool SetLocalTime(ref SystemTime time);

        [DllImport("Kernel32.dll")]
        public static extern void GetLocalTime(ref SystemTime time);

        public static bool SetLocalTime(DateTime dateTime)
        {
            var st = new SystemTime();
            st.FromDateTime(dateTime);
            return SetLocalTime(ref st);
        }

        public static bool SetLocalTime(int year, int month, int day, int hour, int minute, int second)
        {
            var st = new SystemTime
            {
                _wYear = (ushort) year,
                _wMonth = (ushort) month,
                _wDay = (ushort) day,
                _wHour = (ushort) hour,
                _wMinute = (ushort) minute,
                _wSecond = (ushort) second,
                _wMilliseconds = 0
            };

            return SetLocalTime(ref st);
        }


        public static DateTime GetDateTime(string dateTimeStr, string defaultTimeStr, int defaultHour)
        {
            DateTime diskSpaceCleanupDateTime;

            if (String.IsNullOrWhiteSpace(dateTimeStr) || !DateTime.TryParse(dateTimeStr, out diskSpaceCleanupDateTime))
            {
                if (String.IsNullOrWhiteSpace(defaultTimeStr) || DateTime.TryParse(defaultTimeStr, out diskSpaceCleanupDateTime))
                {
                    diskSpaceCleanupDateTime = new DateTime();
                    diskSpaceCleanupDateTime = diskSpaceCleanupDateTime.AddHours(defaultHour);
                }
            }
            return diskSpaceCleanupDateTime;
        }

        public static bool TimeHourMinuteBigger(DateTime time1, DateTime time2)
        {
            if (time1.Hour > time2.Hour)
            {
                return true;
            }
            if (time1.Hour == time2.Hour)
            {
                if (time1.Minute > time2.Minute)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SystemTime
    {
        public ushort _wYear;
        public ushort _wMonth;
        public ushort _wDayOfWeek;
        public ushort _wDay;
        public ushort _wHour;
        public ushort _wMinute;
        public ushort _wSecond;
        public ushort _wMilliseconds;

        /// <summary>
        /// 从System.DateTime转换。
        /// </summary>
        /// <param name="time">System.DateTime类型的时间。</param>
        public void FromDateTime(DateTime time)
        {
            _wYear = (ushort)time.Year;
            _wMonth = (ushort)time.Month;
            _wDayOfWeek = (ushort)time.DayOfWeek;
            _wDay = (ushort)time.Day;
            _wHour = (ushort)time.Hour;
            _wMinute = (ushort)time.Minute;
            _wSecond = (ushort)time.Second;
            _wMilliseconds = (ushort)time.Millisecond;
        }
        /// <summary>
        /// 转换为System.DateTime类型。
        /// </summary>
        /// <returns></returns>
        public DateTime ToDateTime()
        {
            return new DateTime(_wYear, _wMonth, _wDay, _wHour, _wMinute, _wSecond, _wMilliseconds);
        }
        /// <summary>
        /// 静态方法。转换为System.DateTime类型。
        /// </summary>
        /// <param name="time">SYSTEMTIME类型的时间。</param>
        /// <returns></returns>
        public static DateTime ToDateTime(SystemTime time)
        {
            return time.ToDateTime();
        }
    }
}

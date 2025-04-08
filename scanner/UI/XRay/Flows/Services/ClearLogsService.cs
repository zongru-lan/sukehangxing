using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 清理早期的日志
    /// </summary>
    public class ClearLogsService
    {
        public static ClearLogsService Service { get; private set; }

        static ClearLogsService()
        {
            Service = new ClearLogsService();
        }
        public ClearLogsService()
        {
            _logsStorePath = ConfigPath.LogsStorePath;
        }

        /// <summary>
        /// 清理X天之前的日志
        /// </summary>
        /// <param name="days">天</param>
        public void ClearLogs(int days)
        {
            Task.Run(() =>
                {
                    if (Directory.Exists(_logsStorePath))
                    {
                        try
                        {
                            string[] dirs = Directory.GetDirectories(_logsStorePath);
                            string[] format = { "yyyyMMdd" };
                            CultureInfo culture = CultureInfo.CreateSpecificCulture("zh-cn");
                            DateTimeStyles style = DateTimeStyles.None;
                            DateTime dirDt = DateTime.Now;

                            var boundaryDateTime = DateTime.Now.AddDays(-days);

                            foreach (var dir in dirs)
                            {
                                int index = dir.LastIndexOf('\\');
                                string date = dir.Substring(index + 1, 8);

                                if (DateTime.TryParseExact(date, format, culture, style, out dirDt))
                                {
                                    if (dirDt < boundaryDateTime)
                                    {
                                        Directory.Delete(dir, true);
                                    }
                                }

                            }
                        }
                        catch (Exception e)
                        {
                            Tracer.TraceError("In ClearLogs: Failed to clear logs: " + e.Message);
                        }
                    }
                });
        }

        private string _logsStorePath;

    }
}

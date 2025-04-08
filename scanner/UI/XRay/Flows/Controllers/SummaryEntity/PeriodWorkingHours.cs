namespace UI.XRay.Flows.Controllers.SummaryEntity
{
    /// <summary>
    /// 表示一段时间内的工作小时数
    /// </summary>
    public class PeriodWorkingHours : StatisticResultBase
    {
        /// <summary>
        /// 以浮点数表示的小时数
        /// </summary>
        public double Hours { get; set; }
    }
}
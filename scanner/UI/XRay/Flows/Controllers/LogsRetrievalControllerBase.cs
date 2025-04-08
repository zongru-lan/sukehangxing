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
    /// 日志检索控制器基类，提供基本的访问功能
    /// </summary>
    public class LogsRetrievalControllerBase
    {
        private AccountRole? _selectedRole;

        /// <summary>
        /// 选中的账户类型
        /// </summary>
        public AccountRole? SelectedRole
        {
            get { return _selectedRole; }
            set
            {
                _selectedRole = value;
                AccountIds = GetAccountsIdByRole(value);
            }
        }

        /// <summary>
        /// 选中的账户Id
        /// </summary>
        public string SelectedAccountId { get; set; }

        /// <summary>
        /// 获取或设置查询的年份
        /// </summary>
        public int SelectedYear { get; set; }

        /// <summary>
        /// 当前选中的月份。在按日查询时使用
        /// </summary>
        public int SelectedMonth { get; set; }

        /// <summary>
        /// 用户自定的起始时间
        /// </summary>
        public DateTime UserDefinedStartTime { get; set; }

        /// <summary>
        /// 用户自定义的结束时间
        /// </summary>
        public DateTime UserDefinedEndTime { get; set; }

        /// <summary>
        /// 当前选中用户角色对应的账户Id列表，其中包括一个空字符串，表示所有选择所有账户
        /// </summary>
        public List<string> AccountIds { get; set; }

        /// <summary>
        /// 获取或设置查询周期
        /// </summary>
        public StatisticalPeriod SelectedPeriod { get; set; }

        public LogsRetrievalControllerBase()
        {
            UserDefinedEndTime = DateTime.Now;
            UserDefinedStartTime = DateTime.Now;
        }

        /// <summary>
        /// 将指定的列表统计结果输出到指定的文件中，如果文件已经存在，则覆盖
        /// </summary>
        /// <typeparam name="T">统计结果类型</typeparam>
        /// <param name="list">要输出的统计列表</param>
        /// <param name="properties">要输出的属性名及其对应的文本翻译，文本翻译做为列表头输出</param>
        /// <param name="fileName">要输出的文件名全路径，如果文件存在，则覆盖</param>
        public void Export<T>(List<T> list, List<KeyValuePair<string, string>> properties, string fileName)
        {
            if (list == null || properties == null || string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var type = typeof(T);
            var pis = type.GetProperties();

            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 500000))
            {
                var writer = new StreamWriter(stream, Encoding.UTF8);

                var builder = new StringBuilder(150);

                // export headers
                foreach (var pair in properties)
                {
                    builder.Append(pair.Value).Append(',');
                }

                builder.Remove(builder.Length - 1, 1);
                writer.WriteLine(builder.ToString());

                builder.Clear();

                // export datas
                foreach (var t in list)
                {
                    foreach (var pair in properties)
                    {
                        var p = type.GetProperty(pair.Key);
                        if (p != null)
                        {
                            var value = p.GetValue(t);
                            
                            builder.Append(value).Append(",");
                        }
                    }

                    // remove last ','
                    builder.Remove(builder.Length - 1, 1);
                    writer.WriteLine(builder.ToString());
                    builder.Clear();
                }

                writer.Close();
                writer.Dispose();
            }
        }

        /// <summary>
        /// 获取2015年以来至今的年份列表
        /// </summary>
        /// <returns></returns>
        public List<int> GetYearsList()
        {
            return TimeHelper.EnumAllYears();
        }

        /// <summary>
        /// 获取月份-字符串列表
        /// </summary>
        /// <returns></returns>
        public List<ValueStringItem<int>> GetMonthsList()
        {
            return TimeHelper.GetMonthStringList();
        }

        /// <summary>
        /// 获取除UserDefined 和 Hourly之外的统计周期列表
        /// </summary>
        /// <returns></returns>
        public List<ValueStringItem<StatisticalPeriod>> GetPeriodList1()
        {
            var list = TimeHelper.GetStatisticsPeriods();
            list.RemoveAll(item => item.Value == StatisticalPeriod.UserDefined);
            list.RemoveAll(item => item.Value == StatisticalPeriod.Hourly);

            return list;
        }

        /// <summary>
        /// 获取除UserDefined 之外的统计周期列表
        /// </summary>
        /// <returns></returns>
        public List<ValueStringItem<StatisticalPeriod>> GetPeriodList3()
        {
            var list = TimeHelper.GetStatisticsPeriods();
            list.RemoveAll(item => item.Value == StatisticalPeriod.UserDefined);

            return list;
        }

        /// <summary>
        /// 获取除统计周期列表：仅包括自定义和按日统计
        /// </summary>
        /// <returns></returns>
        public List<ValueStringItem<StatisticalPeriod>> GetPeriodList2()
        {
            var list = TimeHelper.GetStatisticsPeriods();
            return
                list.Where(item => item.Value == StatisticalPeriod.UserDefined || item.Value == StatisticalPeriod.Dayly)
                    .ToList();
        }

        /// <summary>
        /// 获取指定年中的指定月份的天数列表
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public List<int> GetDaysListOfMonth(int year, int month)
        {
            return TimeHelper.GetDaysListOfMonth(year, month);
        }

        /// <summary>
        /// 获取所有的用户角色的字符串对，包括null，null表示所有角色
        /// </summary>
        /// <returns></returns>
        public List<ValueStringItem<AccountRole?>> GetRoleStringList()
        {
            return AccountHelper.GetAccountRoleStringList();
        }

        /// <summary>
        /// 根据选中的用户角色，获取指定的用户id列表，其中包含一个空字符串，表示选择所有用户
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public List<string> GetAccountsIdByRole(AccountRole? role)
        {
            var accountManager = new AccountDbSet();
            var list = new List<Account>();

            if (role == null)
            {
                list = accountManager.SelectAll();
            }
            else
            {
                switch (role.Value)
                {
                    case AccountRole.Operator:
                        list = accountManager.SelectOperators();
                        break;
                    case AccountRole.Admin:
                        list = accountManager.SelectAdmins();
                        break;
                    case AccountRole.Maintainer:
                        list = accountManager.SelectMaintains();
                        break;
                    case AccountRole.System:
                        list = accountManager.SelectSystems();
                        break;
                }
            }
            
            var result = list.Select(account => account.AccountId).ToList();

            if(LoginAccountManager.Service.CurrentAccount.Role == role)
            {
                result.Clear();
                result.Add(LoginAccountManager.Service.CurrentAccount.AccountId);
            }

            result.Insert(0, "");
            return result;
        }

        /// <summary>
        /// 根据当前选定的统计周期，获取统计的起始时刻
        /// 返回值为指定起始日期的0点0分0秒
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                switch (SelectedPeriod)
                {
                    // 对于按小时统计，返回用户自定义日期的起始日期作为起始时间
                    case StatisticalPeriod.Hourly:
                        return UserDefinedStartTime.Date;

                    // 对于按天统计，需要指定月份
                    case StatisticalPeriod.Dayly:
                        return new DateTime(SelectedYear, SelectedMonth, 1).Date;

                    // 对于按周、月、季统计，从指定年的第一天开始
                    case StatisticalPeriod.Weekly:
                    case StatisticalPeriod.Monthly:
                    case StatisticalPeriod.Quarterly:
                        return new DateTime(SelectedYear, 1, 1).Date;

                    case StatisticalPeriod.UserDefined:
                        return UserDefinedStartTime.Date;
                }

                return new DateTime(SelectedYear, SelectedMonth, 1).Date;
            }
        }

        /// <summary>
        /// 根据当前选定的统计周期，获取统计的结束时刻
        /// 返回值为用户选定日期第二天的0点0分0秒，或者是用户选定年份的第二年的第一天的0点0分0秒
        /// </summary>
        public DateTime EndDate
        {
            get
            {
                switch (SelectedPeriod)
                {
                    // 对于按小时统计，返回用户自定义日期的下一天的0点作为结束时间
                    case StatisticalPeriod.Hourly:
                        return UserDefinedStartTime.AddDays(1).Date;

                    // 对于按天统计，需要指定月份
                    case StatisticalPeriod.Dayly:
                        return new DateTime(SelectedYear, SelectedMonth, DateTime.DaysInMonth(SelectedYear, SelectedMonth)).AddDays(1).Date;

                    // 对于按周、月、季统计，至指定年的最后一天 
                    case StatisticalPeriod.Weekly:
                    case StatisticalPeriod.Monthly:
                    case StatisticalPeriod.Quarterly:
                        return new DateTime(SelectedYear+1, 1, 1);
                    case StatisticalPeriod.UserDefined:
                        return UserDefinedEndTime.AddDays(1).Date;
                }

                return new DateTime(SelectedYear, SelectedMonth, 31, 23, 59, 59);
            }
        }

        /// <summary>
        /// 根据当前选定的用户组、用户Id设置，获取要统计的目标账户id
        /// </summary>
        /// <returns></returns>
        public List<string> GetTargetAccountIds()
        {
            var accountList = new List<string>();

            // 如果选择了某个用户，则仅返回该用户
            if (!string.IsNullOrEmpty(SelectedAccountId))
            {
                accountList.Add(SelectedAccountId);
                return accountList;
            }

            // 如果未选择指定用户，则返回根据当前角色选择的账户列表（可能是所有账户，也可能是指定角色的账户）
            if (!SelectedRole.HasValue)
            {
                AccountIds = LoginAccountManager.Service.GetCurrentAndLessThanId();
            }
            return AccountIds;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Flows.TRSNetwork.Models;
using UI.XRay.Business.Entities;
using XRayNetEntities.Models;
using XRayNetEntities.Tools;


namespace UI.XRay.Flows.TRSNetwork.Tools
{
    public class DBHelper
    {
        static string LoggerName = "DB";

        /// <summary>
        /// 设备登录存储过程
        /// </summary>
        /// <returns></returns>
        public static bool ExecuteDeviceLoginProc()
        {
            string procName = "HW_PROC_DEVICELOGIN";


            SqlParameter[] parm = new SqlParameter[17];
            parm[0] = new SqlParameter("@Return_value", SqlDbType.Int);
            parm[0].Direction = ParameterDirection.ReturnValue;

            parm[1] = new SqlParameter("@DType", SqlDbType.VarChar, 32);
            parm[1].Direction = ParameterDirection.Output;
            parm[2] = new SqlParameter("@DeviceName", SqlDbType.VarChar, 128);
            parm[2].Direction = ParameterDirection.Output;
            parm[3] = new SqlParameter("@DModel", SqlDbType.VarChar, 100);
            parm[3].Direction = ParameterDirection.Output;
            parm[4] = new SqlParameter("@DeviceNO", SqlDbType.VarChar, 100);
            parm[4].Direction = ParameterDirection.Output;
            parm[5] = new SqlParameter("@DeviceID", SqlDbType.Int, 32);
            parm[5].Direction = ParameterDirection.Output;
            parm[6] = new SqlParameter("@Island", SqlDbType.Int, 32);
            parm[6].Direction = ParameterDirection.Output;
            parm[7] = new SqlParameter("@StationNo", SqlDbType.Int, 32);
            parm[7].Direction = ParameterDirection.Output;
            parm[8] = new SqlParameter("@ChannelID", SqlDbType.VarChar, 5);
            parm[8].Direction = ParameterDirection.Output;
            parm[9] = new SqlParameter("@CounterNo", SqlDbType.VarChar, 5);
            parm[9].Direction = ParameterDirection.Output;
            parm[10] = new SqlParameter("@WorkMode", SqlDbType.SmallInt, 32);
            parm[10].Direction = ParameterDirection.Output;
            parm[11] = new SqlParameter("@JudgeScope", SqlDbType.SmallInt, 32);
            parm[11].Direction = ParameterDirection.Output;
            parm[12] = new SqlParameter("@StationType", SqlDbType.SmallInt, 32);
            parm[12].Direction = ParameterDirection.Output;
            parm[13] = new SqlParameter("@PostCode", SqlDbType.VarChar, 20);
            parm[13].Direction = ParameterDirection.Output;
            parm[14] = new SqlParameter("@PostGroup", SqlDbType.VarChar, 20);
            parm[14].Direction = ParameterDirection.Output;
            parm[15] = new SqlParameter("@NeedUserLogin", SqlDbType.SmallInt, 32);
            parm[15].Direction = ParameterDirection.Output;

            parm[16] = new SqlParameter("@DeviceIP", SqlDbType.VarChar, 15);
            parm[16].Direction = ParameterDirection.Input;
            parm[16].Value = Global.Instance.Sys.NetIPAddress;


            var prs = parm.ToDictionary(s => s.ParameterName);
            try
            {

                var line = FreeSqlHelper.ISql.Ado.CommandFluent(procName, prs)
                    .CommandType(CommandType.StoredProcedure)
                    .ExecuteNonQuery();
                var prsArray = prs.Values.ToArray();
                if (false == prsArray[0].Value.ToString().Equals("0"))
                {
                    //登录失败, 数据库里不存在指定IP
                    LogUtil.Failure(string.Format("Device login failed. IP = {0}", Global.Instance.Sys.NetIPAddress), LoggerName);
                    return false;
                }
                var setting = Global.Instance.Sys;
                setting.ChannelID = parm[8].Value.ToString();
                setting.PostCode = parm[13].Value.ToString();
                setting.PostGroup = parm[14].Value.ToString();
                Global.Instance.Save("sys");
                LogUtil.Info(string.Format("{0} DeviceLogin success!", setting.NetIPAddress), LoggerName);
                return true;
            }
            catch (Exception e)
            {
                LogUtil.Exception(e, "", LoggerName);
                return false;
            }
        }

        /// <summary>
        /// 执行设备登出存储过程
        /// </summary>
        /// <param name="deviceIp">设备ip</param>
        /// <returns>执行结果</returns>
        public static bool ExecuteDeviceLogoutProc()
        {
            string procName = "HW_PROC_DEVICEEXIT";
            string deviceIP = Global.Instance.Sys.NetIPAddress;

            SqlParameter[] parm = new SqlParameter[4];
            parm[0] = new SqlParameter("@Return_value", SqlDbType.Int);
            parm[0].Direction = ParameterDirection.ReturnValue;

            parm[1] = new SqlParameter("@DType", SqlDbType.VarChar, 32);
            parm[1].Direction = ParameterDirection.Output;
            parm[2] = new SqlParameter("@DeviceName", SqlDbType.VarChar, 128);
            parm[2].Direction = ParameterDirection.Output;

            parm[3] = new SqlParameter("@DeviceIP", SqlDbType.VarChar, 15);
            parm[3].Direction = ParameterDirection.Input;
            parm[3].Value = deviceIP;

            var prs = parm.ToDictionary(p => p.ParameterName);

            try
            {
                var line = FreeSqlHelper.ISql.Ado.CommandFluent(procName, prs)
                    .CommandType(CommandType.StoredProcedure)
                    .ExecuteNonQuery();

                if (prs.Values.ToArray()[0].Value.ToString().Equals("0"))
                {
                    LogUtil.Info(string.Format("{0} logout success.", deviceIP), LoggerName);
                    return true;
                }
                else
                {
                    LogUtil.Failure(string.Format("{0} logout failed.", deviceIP), LoggerName);
                    return false;
                }
            }
            catch (Exception e)
            {
                LogUtil.Exception(e, "", LoggerName);
                return false;
            }
        }

        /// <summary>
        /// 用户登录存储过程
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        public static int ExecuteUserLoginProc(string username, string password, out Account account)
        {
            account = null;
            string procName = "HW_PROC_USERLOGIN";
            System.Data.SqlClient.SqlParameter[] parm = new System.Data.SqlClient.SqlParameter[8];
            parm[0] = new System.Data.SqlClient.SqlParameter("@UserName", SqlDbType.VarChar, 32);
            parm[0].Direction = ParameterDirection.Output;
            parm[1] = new System.Data.SqlClient.SqlParameter("@PostName", SqlDbType.VarChar, 32);
            parm[1].Direction = ParameterDirection.Output;


            parm[2] = new System.Data.SqlClient.SqlParameter("@UserLogin", SqlDbType.VarChar, 32);
            parm[2].Direction = ParameterDirection.Input;
            parm[2].Value = username;

            parm[3] = new System.Data.SqlClient.SqlParameter("@Userpasswd", SqlDbType.VarChar, 32);
            parm[3].Direction = ParameterDirection.Input;
            parm[3].Value = password;

            parm[4] = new System.Data.SqlClient.SqlParameter("@PostCode", SqlDbType.VarChar, 32);
            parm[4].Direction = ParameterDirection.Input;
            parm[4].Value = Global.Instance.Sys.PostCode;

            parm[5] = new System.Data.SqlClient.SqlParameter("@PostGroup", SqlDbType.VarChar, 32);
            parm[5].Direction = ParameterDirection.Input;
            parm[5].Value = Global.Instance.Sys.PostGroup;

            parm[6] = new System.Data.SqlClient.SqlParameter("@DeviceIP", SqlDbType.VarChar, 32);
            parm[6].Direction = ParameterDirection.Input;
            parm[6].Value = Global.Instance.Sys.NetIPAddress;

            parm[7] = new System.Data.SqlClient.SqlParameter("@Return_value", SqlDbType.Int);
            parm[7].Direction = ParameterDirection.ReturnValue;

            var prs = parm.ToDictionary(p => p.ParameterName);
            try
            {
                var line = FreeSqlHelper.ISql.Ado.CommandFluent(procName, prs)
                    .CommandType(CommandType.StoredProcedure)
                    .ExecuteNonQuery();

                var ret = prs.Values.ToArray();

                int rtnValue = (int)ret[7].Value;
                if (rtnValue == 0)
                {
                    account = new Account();
                    account.AccountId = username;
                    account.Name = ret[0].Value.ToString();
                    account.IsActive = true;
                    account.IsEnable = true;
                    account.Password = password;                    
                    // 功能权限等级
                    PermissionList Permission = new PermissionList();
                    Permission.CanTraining = true;
                    Permission.CanChangeImageSettings = false;
                    Permission.CanManageDisk = false;
                    Permission.CanManageLog = false;
                    account.PermissionValue = GetPermissionValue(Permission);

                    // 权限等级
                    account.Role = AccountRole.Operator;
                    LogUtil.Info(string.Format("[TRS]...UserLogin success! Id: {0}, UserName: {1} ", account.AccountId, account.Name), LoggerName);
                    return 0;
                }
                else
                {
                    LogUtil.Failure(string.Format("{0} user login failed. Return value is {1}", username, rtnValue), LoggerName);
                    return rtnValue;
                }

            }
            catch (Exception e)
            {
                LogUtil.Exception(e, "", LoggerName);
                return -4;
            }
        }

        private static int GetPermissionValue(PermissionList pl)
        {
            int temp = 0;
            if (pl.CanTraining)
            {
                temp += 0x01;
            }
            if (pl.CanChangeImageSettings)
            {
                temp += 0x02;
            }
            if (pl.CanManageDisk)
            {
                temp += 0x04;
            }
            if (pl.CanManageLog)
            {
                temp += 0x08;
            }
            return temp;
        }

        /// <summary>
        /// 人员登出
        /// </summary>
        /// <param name="deviceIp">设备ip</param>
        /// <param name="user">用户</param>
        /// <param name="code">设备编号</param>
        /// <param name="group">设备区域</param>
        /// <returns>是否成功</returns>
        public static bool ExecuteUserLogoutProc(string user)
        {
            string procName = "HW_PROC_USEREXIT";

            System.Data.SqlClient.SqlParameter[] parm = new System.Data.SqlClient.SqlParameter[5];
            parm[0] = new System.Data.SqlClient.SqlParameter("@UserLogin", SqlDbType.VarChar, 32);
            parm[0].Direction = ParameterDirection.Input;
            parm[0].Value = user;

            parm[1] = new System.Data.SqlClient.SqlParameter("@PostCode", SqlDbType.VarChar, 32);
            parm[1].Direction = ParameterDirection.Input;
            parm[1].Value = Global.Instance.Sys.PostCode;

            parm[2] = new System.Data.SqlClient.SqlParameter("@PostGroup", SqlDbType.VarChar, 32);
            parm[2].Direction = ParameterDirection.Input;
            parm[2].Value = Global.Instance.Sys.PostGroup;

            parm[3] = new System.Data.SqlClient.SqlParameter("@DeviceIP", SqlDbType.VarChar, 32);
            parm[3].Direction = ParameterDirection.Input;
            parm[3].Value = Global.Instance.Sys.NetIPAddress;

            parm[4] = new System.Data.SqlClient.SqlParameter("@Return_value", SqlDbType.Int);
            parm[4].Direction = ParameterDirection.ReturnValue;
            var prs = parm.ToDictionary(p => p.ParameterName);
            try
            {
                var line = FreeSqlHelper.ISql.Ado.CommandFluent(procName, prs)
                    .CommandType(CommandType.StoredProcedure)
                    .ExecuteNonQuery();
                var ret = prs.Values.ToArray();

                int rtnValue = (int)ret[4].Value;

                if (rtnValue == 0)
                {
                    LogUtil.Info(string.Format("{0} user logout success.", user), LoggerName);
                    return true;
                }
                else
                {
                    LogUtil.Failure(string.Format("{0} user logout failed. Return value is {1}", user, rtnValue), LoggerName);
                    return false;
                }
            }
            catch (Exception e)
            {
                LogUtil.Exception(e, "", LoggerName);
                return false;
            }
        }


    }
}

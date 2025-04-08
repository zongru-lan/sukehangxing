using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Common.Utilities;

namespace UI.XRay.Flows.Services
{
    public enum VerifySoftResult
    {
        /// <summary>
        /// 没有注册码
        /// </summary>
        None,
        /// <summary>
        /// 解密错误
        /// </summary>
        DecryptFalse,
        /// <summary>
        /// 硬件不匹配
        /// </summary>
        HardwareNotFit,
        /// <summary>
        /// 超期
        /// </summary>
        DateOverdue,
        /// <summary>
        /// 验证成功
        /// </summary>
        VerfySucess,
        /// <summary>
    }

    public static class SoftLockerService
    {
        private const string Head = "Hx";

        public static VerifySoftResult VerifySoft(string requestKey, byte[] bsKey2, out bool PerAuthorized,out uint LockLimitedTimes)
        {
            PerAuthorized = false;
            LockLimitedTimes = 0;
            if (string.IsNullOrWhiteSpace(requestKey))
            {
                return VerifySoftResult.None;
            }
            string strKey = requestKey;
            string strDePCid = "";
            string strLimitedTimes = "";
            string strDeTermDate = "";
            DateTime dtTerminal = new DateTime(1, 1, 1);

            DESCryptoServiceProvider key2 = new DESCryptoServiceProvider();
            
            key2.Key = bsKey2;
            key2.IV = bsKey2;
            var strList = new List<string>();
            try
            {
                byte[] byteArray = HexStringToByte(strKey.Remove(strKey.Length - 7));
                string str = SoftLocker.Decrypt(byteArray, key2);
                if (string.IsNullOrWhiteSpace(str))
                {
                    return VerifySoftResult.DecryptFalse;
                }
                
                strList = str.Split('-').ToList();
                if (strList.Count!=4)
                {
                    return VerifySoftResult.DecryptFalse;
                }
                strDePCid = strList[0];
                strLimitedTimes = strList[1];
                strDeTermDate = strList[2];
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                return VerifySoftResult.DecryptFalse;
            }



            try
            {
                //检测开头是否是Hx
                string hxStr = strDePCid.Substring(0, 2);

                if (hxStr != "Hx")
                {
                    return VerifySoftResult.DecryptFalse;
                }

                //获取本底ID
                string localPCID = DeviceCaps.GetPCID();
                //删除末尾时间
                string localPartIdsStr = localPCID.TrimEnd('-');
                //获取中间的硬件ID部分
                string keyPartIdsStr = strDePCid.Substring(2, 20);

                //验证
                if (localPartIdsStr.Length != 20 || keyPartIdsStr.Length != 20)
                {
                    return VerifySoftResult.HardwareNotFit;
                }

                if (strLimitedTimes.Length != 6)
                {
                    return VerifySoftResult.DecryptFalse;
                }
                LockLimitedTimes = uint.Parse(strLimitedTimes);

                //20个字符中，前5个字符代表CPU，中间5个代表硬盘，后5个代表网卡，最后5个代表主板，只要3个一致就认为符合
                int matchPartCount = 0;

                for (int i = 0; i < 4; i++)
                {
                    if (localPartIdsStr.Substring(i * 5, 5) == keyPartIdsStr.Substring(i * 5, 5))
                    {
                        matchPartCount++;
                    }
                }

                if (matchPartCount >= 3)
                {
                    strDeTermDate = strDeTermDate.Insert(4, "-");
                    strDeTermDate = strDeTermDate.Insert(2, "-");

                    //var ci = new CultureInfo(CultureInfo.CurrentCulture.Name);
                    //ci.Calendar.TwoDigitYearMax = 2099;

                    //if (!DateTime.TryParse(strDeTermDate, ci, DateTimeStyles.AssumeLocal, out dtTerminal))
                    //{
                    //    Tracer.TraceWarning("The DateTime got from the key can not be parsed to DateTime!");
                    //}

                    DateTimeFormatInfo dtFormat = new DateTimeFormatInfo();
                    dtFormat.ShortDatePattern = "yyMMdd";
                    dtFormat.Calendar.TwoDigitYearMax = 2099;

                    try
                    {
                        dtTerminal = Convert.ToDateTime(strDeTermDate, dtFormat);
                    }
                    catch(Exception e)
                    {
                        Tracer.TraceWarning("The DateTime got from the key can not be parsed to DateTime!");
                        Tracer.TraceException(e);
                    }

                    DateTime MaxTerm = new DateTime(2099, 12, 31);//永久授权的期限
                    if (dtTerminal >= MaxTerm)
                    {
                        PerAuthorized = true;
                    }
                    else
                    {
                        PerAuthorized = false;
                    }

                    if (dtTerminal > DateTime.Now)
                    {
                        return VerifySoftResult.VerfySucess;
                    }
                    else
                    {
                        return VerifySoftResult.DateOverdue;
                    }
                }
                else
                {
                    return VerifySoftResult.HardwareNotFit;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }

            return VerifySoftResult.HardwareNotFit;
        }

        public static bool VerifySoftOld(string requestKey)
        {
            byte[] key = HexStringToByte(requestKey);
            //获取CPUID
            string cpuId = DeviceCaps.GetCpuId();

            string keyWord = Head + cpuId;

            //解密字符串CPUId
            string decryptStr = SoftLocker.Decrypt(key);
            return keyWord == decryptStr;
        }

        public static byte[] HexStringToByte(String hex)
        {
            int len = (hex.Length / 2);
            byte[] result = new byte[len];
            char[] achar = hex.ToCharArray();
            for (int i = 0; i < len; i++)
            {
                int pos = i * 2;
                result[i] = (byte)(ToByte(achar[pos]) << 4 | ToByte(achar[pos + 1]));
            }
            return result;
        }

        private static byte ToByte(char c)
        {
            byte b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }
    }
}

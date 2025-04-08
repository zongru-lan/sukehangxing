using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.Common.Tracers;

namespace UI.XRay.Flows.Services
{
    public class ConfigHelper
    {
        public static string GetMachineNumber()
        {
            string machineNumber;
            if (!ScannerConfig.Read(ConfigPath.SystemMachineNum, out machineNumber))
            {
                machineNumber = "Unknown";
            }

            return machineNumber;
        }

        public static string GetMachineDate()
        {
            string machineDate;
            if (!ScannerConfig.Read(ConfigPath.SystemMachineDate, out machineDate))
            {
                machineDate = "Unknown";
            }

            return machineDate;
        }

        public static string GetMachineType()
        {
            string model;
            if (!ScannerConfig.Read(ConfigPath.SystemModel, out model))
            {
                model = "Unknown";
            }

            return model;
        }

        public static string GetSoftwareVersion()
        {
            string version;
            if (!ScannerConfig.Read(ConfigPath.SystemSoftwareVersion,out version))
            {
                version = "unkown";
            }
            return version;
        }

        /// <summary>
        /// 加载某个视角的探测板分布情况
        /// </summary>
        /// <param name="configPath">视角的探测板配置路径</param>
        /// <returns>成功则返回其探测板分布数组，数组长度为4；失败则返回null</returns>
        private static int[] LoadCardsDist(string configPath)
        {
            var cards = "0,0,0,0";
            if (ScannerConfig.Read(configPath, out cards))
            {
                var cardsStr = cards.Split(new[] { ',' });
                if (cardsStr.Length == 4)
                {
                    var result = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        int.TryParse(cardsStr[i], out result[i]);
                    }
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取控制板的Ip地址
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetControlBoardIp()
        {
            string ipString;
            if (ScannerConfig.Read(ConfigPath.ControlSysBoardIp, out ipString))
            {
                IPAddress ip;
                if (IPAddress.TryParse(ipString, out ip))
                {
                    return ip;
                }
            }

            return null;
        }

        public static int GetControlBoardPort()
        {
            int port;
            if (ScannerConfig.Read(ConfigPath.ControlSysBoardPort, out port))
            {
                return port;
            }
            return 0;
        }

        public static bool GetView1IsVerticalFlip()
        {
            bool isver = false;
            if (ScannerConfig.Read(ConfigPath.ImagesImage1VerticalFlip, out isver))
            {
                return isver;
            }
            return isver;
        }

        public static bool GetView2IsVerticalFlip()
        {
            bool isver = false;
            if (ScannerConfig.Read(ConfigPath.ImagesImage2VerticalFlip, out isver))
            {
                return isver;
            }
            return isver;
        }

        public static bool GetIsRight2Left()
        {
            bool isRight2Left = false;
            if (ScannerConfig.Read(ConfigPath.Image1MoveRightToLeft,out isRight2Left))
            {
                return isRight2Left;
            }
            return isRight2Left;
        }

        /// <summary>
        /// 获取与控制板相连的计算机的Ip地址
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetComputerIp()
        {
            string ipString;
            if (ScannerConfig.Read(ConfigPath.ComputerIp, out ipString))
            {
                IPAddress ip;
                if (IPAddress.TryParse(ipString, out ip))
                {
                    return ip;
                }
            }

            return null;
        }

        public static int GetComputerPort()
        {
            int port;
            if (ScannerConfig.Read(ConfigPath.ComputerPort,out port))
            {
                return port;
            }
            return 0;
        }

        /// <summary>
        /// 获取两个视角之间配准时需要调整的线数
        /// </summary>
        /// <returns>两个视角之间匹配时，需要丢弃的线数</returns>
        public static int GetDualViewMatchLines()
        {
            float distance;
            
            if (!ScannerConfig.Read(ConfigPath.MachineViewsDistanceForForward, out distance))
            {
                return 0;
            }

            float speed;
            if (!ScannerConfig.Read(ConfigPath.MachineConveyorSpeed, out speed))
            {
                speed = 0.2f;
            }

            if (speed <= 0.01)
            {
                speed = 0.2f;
            }

            // 线积分时间，单位为毫秒
            float integrationTime;
            if (!ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out integrationTime))
            {
                integrationTime = 4.0f;
            }

            if (integrationTime < 0.01f)
            {
                integrationTime = 4.0f;
            }

            var seconds = distance/speed;
            Tracer.TraceInfo("distance is:", distance);//yxc
            Tracer.TraceInfo("speed is:", speed);//yxc
            Tracer.TraceInfo("integrationTime is:", integrationTime);//yxc
            Tracer.TraceInfo("seconds is:", seconds);//yxc
            return (int)(seconds*1000/integrationTime);
           
        }

        public static string GetExportFileName(OperationUI ui,string path, string type)
        {
            string machinetype = GetMachineType();
            string machineId = GetMachineNumber();
            string userId = PermissionService.Service.CurrentAccount != null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty;

            string dstFile =Path.Combine(path, string.Format("{0}-{1}-{2}-{3}-{4}.csv", machinetype, machineId, userId, type, DateTime.Now.ToString("yyyyMMddHHmmss")));
            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = userId,
                OperateUI = ui,
                OperateTime = DateTime.Now,
                OperateObject = type,
                OperateCommand = OperationCommand.Export,
                OperateContent =  ConfigHelper.AddQuotationForPath(dstFile),
            });
            return dstFile;
        }

        public static string GetDiagnosisExportFileName(OperationUI ui, string path, string type)
        {
            string machinetype = GetMachineType();
            string machineId = GetMachineNumber();
            string userId = PermissionService.Service.CurrentAccount != null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty;

            string dstFile = Path.Combine(path, string.Format("{0}-{1}-{2}-{3}-{4}.txt", machinetype, machineId, userId, type, DateTime.Now.ToString("yyyyMMddHHmmss")));
            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = userId,
                OperateUI = ui,
                OperateTime = DateTime.Now,
                OperateObject = type,
                OperateCommand = OperationCommand.Export,
                OperateContent = ConfigHelper.AddQuotationForPath(dstFile),
            });
            return dstFile;
        }

        public static string GetExportFileName(string path,string type)
        {
            string machinetype = GetMachineType();
            string machineId = GetMachineNumber();
            string userId = PermissionService.Service.CurrentAccount != null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty;

            string dstFile = Path.Combine(path, string.Format("{0}-{1}-{2}-{3}-{4}.xml", machinetype, machineId, userId, type, DateTime.Now.ToString("yyyyMMddHHmmss")));
            return dstFile;
        }

        public static string GetManualIdentifyFileName(string path,string type)
        {
            string machinetype = GetMachineType();
            string machineId = GetMachineNumber();
            string userId = PermissionService.Service.CurrentAccount != null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty;

            string dstFile = Path.Combine(path, string.Format("{0}-{1}-{2}-{3}-{4}.xml", machinetype, machineId, userId, type, DateTime.Now.ToString("yyyyMMddHHmmss")));
            return dstFile;
        }

        public static string GetManualCopyFileName(string path, string file)
        {
            string machinetype = GetMachineType();
            string machineId = GetMachineNumber();
            string userId = PermissionService.Service.CurrentAccount != null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty;

            string dstFile = Path.Combine(path, Path.GetFileName(file));
            return dstFile;
        }

        public static string GetManualIdentifyXRayFileName(string path,string filename)
        {
            string fileName = Path.GetFileName(filename);
            string dstFile = Path.Combine(path, fileName);
            return dstFile;
        }

        public static string AddQuotationForPath(string path)
        {
            return string.Format("\"{0}\"",path);
        }

        public static float XRayGen1Voltage { get; set; }
        public static float XRayGen1Current { get; set; }
        public static float XRayGen2Voltage { get; set; }
        public static float XRayGen2Current { get; set; }

    }
}

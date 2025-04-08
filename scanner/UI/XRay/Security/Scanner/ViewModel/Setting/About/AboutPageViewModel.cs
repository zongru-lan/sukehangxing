using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.About
{
    public class AboutPageViewModel : PageViewModelBase
    {
        private string _imageStoreDiskName;

        public string ImageStoreDiskName
        {
            get { return _imageStoreDiskName; }
            set
            {
                _imageStoreDiskName = value;
                RaisePropertyChanged();
            }
        }

        private double _diskSpaceSizeGb;

        /// <summary>
        /// 磁盘空间总大小
        /// </summary>
        public double DiskSpaceSizeGB
        {
            get { return _diskSpaceSizeGb; }
            set { _diskSpaceSizeGb = value;  RaisePropertyChanged();}
        }

        /// <summary>
        /// 磁盘已经使用的空间
        /// </summary>
        public double DiskSpaceUsedGB
        {
            get { return _diskSpaceUsedGB; }
            set { _diskSpaceUsedGB = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 磁盘空间已使用的比例
        /// </summary>
        public double DiskUsedRatio
        {
            get { return _diskUsedRatio; }
            set { _diskUsedRatio = value; RaisePropertyChanged();}
        }

        public int TotalImagesCountInStorage
        {
            get { return _totalImagesCountInStorage; }
            set { _totalImagesCountInStorage = value; RaisePropertyChanged();}
        }

        public string MachineNumber
        {
            get { return _machineNumber; }
            set { _machineNumber = value; RaisePropertyChanged();}
        }

        private string _productionDate;

        public string ProductionDate
        {
            get { return _productionDate; }
            set { _productionDate = value; RaisePropertyChanged();}
        }
        

        public string MachineType
        {
            get { return _machineType; }
            set { _machineType = value; RaisePropertyChanged();}
        }

        
        public string SoftwareVersion
        {
            get { return _softwareVersion; }
            set { _softwareVersion = value; RaisePropertyChanged();}
        }


        public string AlgorithmVersion
        {
            get { return _algorithmVersion; }
            set { _algorithmVersion = value;RaisePropertyChanged(); }
        }

        public string ControlVersion
        {
            get { return _controlVersion; }
            set { _controlVersion = value; RaisePropertyChanged();}
        }
        

        public int TotalImagesCountSinceInstall
        {
            get { return _totalImagesCountSinceInstall; }
            set { _totalImagesCountSinceInstall = value; RaisePropertyChanged();}
        }

        public double TotalMachineWorkHours
        {
            get { return _totalMachineWorkHours; }
            set { _totalMachineWorkHours = value;RaisePropertyChanged();}
        }

        public double MachineWorkHoursSinceBoot
        {
            get { return _machineWorkHoursSinceBoot; }
            set { _machineWorkHoursSinceBoot = value;RaisePropertyChanged(); }
        }

        public double TotalXRayGenWorkHours
        {
            get { return _totalXRayGenWorkHours; }
            set { _totalXRayGenWorkHours = value;RaisePropertyChanged(); }
        }

        public int XrayGenTotalUsageCount
        {
            get { return _xrayGenTotalUsageCount; }
            set { _xrayGenTotalUsageCount = value; RaisePropertyChanged(); }
        }


        public double XRayGenWorkHoursSinceBoot
        {
            get { return _xRayGenWorkHoursSinceBoot; }
            set { _xRayGenWorkHoursSinceBoot = value;RaisePropertyChanged(); }
        }

        public int XRayGenUsageCountSinceBoot
        {
            get { return _xrayGenUsageCountSinceBoot; }
            set { _xrayGenUsageCountSinceBoot = value; RaisePropertyChanged(); }
        }

        public Visibility Gen2Visibility
        {
            get { return _gen2Visibility; }
            set { _gen2Visibility = value; RaisePropertyChanged(); }
        }

        private double _diskSpaceUsedGB;

        private double _diskUsedRatio;

        private int _totalImagesCountInStorage;

        private int _totalImagesCountSinceInstall;

        private string _machineNumber;

        private string _machineType;

        private string _softwareVersion;

        private string _algorithmVersion;

        private string _controlVersion;

        private double _totalMachineWorkHours;

        private double _machineWorkHoursSinceBoot;

        private double _totalXRayGenWorkHours;

        private double _xRayGenWorkHoursSinceBoot;

        private int _xrayGenUsageCountSinceBoot;

        private int _xrayGenTotalUsageCount;

        private string _calculatingStr;

        private Visibility _gen2Visibility = Visibility.Collapsed;

        public AboutPageViewModel()
        {
            try
            {
                if (IsInDesignMode)
                {
                    ImageStoreDiskName = ImageStoreDiskHelper.DiskName;
                    DiskSpaceSizeGB = ImageStoreDiskHelper.TotalSizeGB;
                    DiskSpaceUsedGB = ImageStoreDiskHelper.TotalUsedSpaceGB;
                    DiskUsedRatio = DiskSpaceUsedGB / DiskSpaceSizeGB * 100;
                    MachineNumber = "00000000";
                    ProductionDate = "";
                    TotalImagesCountInStorage = 100;
                    MachineType = "DW 5030";
                    SoftwareVersion = "1.0.19.08.08";
                    AlgorithmVersion = "1.2.33.4";
                    ControlVersion = "1.2.3.4.5";
                }
                else
                {
                    InitAsync();
                    int genCount;
                    if (!ScannerConfig.Read(ConfigPath.XRayGenCount,out genCount))
                    {
                        genCount = 1;
                    }
                    Gen2Visibility = genCount == 1 ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private async void InitAsync()
        {
            ImageStoreDiskName = ImageStoreDiskHelper.DiskName;
            DiskSpaceSizeGB = ImageStoreDiskHelper.TotalSizeGB;
            DiskSpaceUsedGB = ImageStoreDiskHelper.TotalUsedSpaceGB;
            DiskUsedRatio = DiskSpaceUsedGB / DiskSpaceSizeGB * 100;

            MachineNumber = ConfigHelper.GetMachineNumber();
            ProductionDate = ConfigHelper.GetMachineDate();
            MachineType = ConfigHelper.GetMachineType();
            SoftwareVersion = VersionHelper.Service.GetVersionStr(SubSystemComponents.Software);
            AlgorithmVersion = VersionHelper.Service.GetVersionStr(SubSystemComponents.Algorithm);
            ControlVersion = VersionHelper.Service.GetVersionStr(SubSystemComponents.ControlSys);

            TotalImagesCountSinceInstall = BagCounterService.Service.CountSinceOpen;

            var imageRecordsManager = new ImageRecordDbSet();
            TotalImagesCountInStorage = BagCounterService.Service.TotalCountSinceInstall;

            InitialWorkHours();
        }

        private async void InitialWorkHours()
        {
            //_calculatingStr = TranslationService.FindTranslation("Calculating") + "...";
            //MachineWorkHoursSinceBoot = _calculatingStr;
            //XRayGenWorkHoursSinceBoot = _calculatingStr;
            //TotalMachineWorkHours = _calculatingStr;
            //TotalXRayGenWorkHours = _calculatingStr;


            //设备本次启动前的开机时间
            var totalBootHoursBeforeCurrentBootTask = WorkHoursController.Instance.GetTotalBootHoursBeforeCurrentBoot();
            //查询x光机总的使用时间
            var xrayGenTotalHoursTask = WorkHoursController.Instance.GetTotalXRayGenWorkHours();
            //查询光机当前使用记录
            var xrayGenCurrentWorkHoursTask = WorkHoursController.Instance.GetXRayGenWorkHoursSinceBoot();


            double workHourSinceMachineBoot = WorkHoursController.Instance.WorkHoursOfMachineSinceBoot;
            double xrayGenCurrentWorkHours = await xrayGenCurrentWorkHoursTask;
            double totalBootHoursBeforeCurrentBoot = await totalBootHoursBeforeCurrentBootTask;
            double xrayGenTotalHours = await xrayGenTotalHoursTask;



            MachineWorkHoursSinceBoot = workHourSinceMachineBoot;//.ToString(CultureInfo.CurrentCulture);
            XRayGenWorkHoursSinceBoot = xrayGenCurrentWorkHours + DevicePartsWorkTimingService.ServicePart.GetXrayWorkingTimeSinceBoot();//.ToString(CultureInfo.CurrentCulture);
            TotalMachineWorkHours = (totalBootHoursBeforeCurrentBoot + workHourSinceMachineBoot);//.ToString(CultureInfo.CurrentCulture);
            TotalXRayGenWorkHours = xrayGenTotalHours + DevicePartsWorkTimingService.ServicePart.GetXrayWorkingTimeSinceBoot();//.ToString(CultureInfo.CurrentCulture);
            XRayGenUsageCountSinceBoot = DevicePartsWorkTimingService.ServicePart.XrayWorkingCountSinceBoot;
            XrayGenTotalUsageCount = DevicePartsWorkTimingService.ServicePart.XrayTotalWorkingCount;
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
        }
    }
}

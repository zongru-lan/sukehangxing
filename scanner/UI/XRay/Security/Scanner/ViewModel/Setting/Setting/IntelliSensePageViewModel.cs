using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Setting
{
    public class IntelliSensePageViewModel : ViewModelBase
    {
        /// <summary>
        /// 智能探测开关功能发生
        /// </summary>
        public RelayCommand<string> IntellisenseSwitchChangedCommand { get; private set; }

        public RelayCommand<string> SensitivitySelectionChangedEventCommand { get; private set; }

        /// <summary>
        /// 标记框边框颜色选择命令
        /// </summary>
        public RelayCommand<string> BorderColorSelectionChangedEventCommand { get; private set; }

        /// <summary>
        /// 高密度探测开关是否选中
        /// </summary>
        private bool _isHdiSwitchChecked;

        public bool IsHdiSwitchChecked
        {
            get
            {
                return _isHdiSwitchChecked;
            }
            set
            {
                _isHdiSwitchChecked = value;
                RaisePropertyChanged();
            }
        }

        private int _hdiSensitivity = 2;

        /// <summary>
        /// 高密度告警的敏感度设定框中选定的敏感度索引。
        /// 注意：索引值的范围为0-4，而实际的敏感度范围是1-5
        /// </summary>
        public int HdiSensitivity
        {
            get
            {
                return _hdiSensitivity;
            }
            set
            {
                _hdiSensitivity = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 毒品与爆炸物检测开关
        /// </summary>
        private bool _isDeiSwitchChecked;


        public bool IsDeiSwitchChecked
        {
            get { return _isDeiSwitchChecked; }
            set { _isDeiSwitchChecked = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 毒品检测灵敏度
        /// </summary>
        public int DeiSensitivity
        {
            get { return _deiSensitivity; }
            set { _deiSensitivity = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 爆炸物检测灵敏度
        /// </summary>
        public int EiSensitivity
        {
            get { return _eiSensitivity; }
            set { _eiSensitivity = value; RaisePropertyChanged(); }
        }

        private bool _isEiSwitchChecked;

        public bool IsEiSwitchChecked
        {
            get { return _isEiSwitchChecked; }
            set
            {
                _isEiSwitchChecked = value;
                RaisePropertyChanged();
            }
        }

        public int HdiBorderColor
        {
            get { return _hdiBorderColor; }
            set { _hdiBorderColor = value; RaisePropertyChanged(); }
        }

        public int DeiBorderColor
        {
            get { return _deiBorderColor; }
            set
            {
                _deiBorderColor = value;
                RaisePropertyChanged();
            }
        }

        public int EiBorderColor
        {
            get { return _eiBorderColor; }
            set
            {
                _eiBorderColor = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 毒品爆炸物检测灵敏度
        /// </summary>
        private int _deiSensitivity;

        private int _eiSensitivity;

        private bool _hdiAudibleAlarmEnabled;

        private bool _deiAudibleAlarmEnabled;

        private bool _eiAudibleAlarmEnabled;

        private bool _hdiLightAlarmEnabled;

        private bool _deiLightAlarmEnabled;

        private bool _eiLightAlarmEnabled;

        private bool _hdiStopConveyorEnabled;

        private bool _deiStopConveyorEnabled;

        private bool _eiStopConveyorEnabled;

        private int _hdiBorderColor;

        private int _deiBorderColor;
        private int _eiBorderColor;


        public List<int> Sensitivities { get; private set; }

        public List<System.Drawing.Color> Colors { get; private set; }

        public bool HdiAudibleAlarmEnabled
        {
            get { return _hdiAudibleAlarmEnabled; }
            set { _hdiAudibleAlarmEnabled = value; RaisePropertyChanged(); }
        }

        public bool DeiAudibleAlarmEnabled
        {
            get { return _deiAudibleAlarmEnabled; }
            set { _deiAudibleAlarmEnabled = value; RaisePropertyChanged(); }
        }

        public bool EiAudibleAlarmEnabled
        {
            get { return _eiAudibleAlarmEnabled; }
            set
            {
                _eiAudibleAlarmEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool HdiLightAlarmEnabled
        {
            get { return _hdiLightAlarmEnabled; }
            set { _hdiLightAlarmEnabled = value; RaisePropertyChanged(); }
        }

        public bool DeiLightAlarmEnabled
        {
            get { return _deiLightAlarmEnabled; }
            set { _deiLightAlarmEnabled = value; RaisePropertyChanged(); }
        }

        public bool EiLightAlarmEnabled
        {
            get { return _eiLightAlarmEnabled; }
            set
            {
                _eiLightAlarmEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool HdiStopConveyorEnabled
        {
            get { return _hdiStopConveyorEnabled; }
            set { _hdiStopConveyorEnabled = value; RaisePropertyChanged(); }
        }

        public bool DeiStopConveyorEnabled
        {
            get { return _deiStopConveyorEnabled; }
            set { _deiStopConveyorEnabled = value; RaisePropertyChanged(); }
        }

        public bool EiStopConveyorEnabled
        {
            get { return _eiStopConveyorEnabled; }
            set
            {
                _eiStopConveyorEnabled = value;
                RaisePropertyChanged();
            }
        }

        List<string> ColorsStr = new List<string>() {
                "Black",
                "Blue",
                "Brown",
                "DarkGreen",
                "DarkOrange",
                "DeepPink",
                "Purple",
                "Red",
                "Aqua"
        };

        public IntelliSensePageViewModel()
        {
            IntellisenseSwitchChangedCommand = new RelayCommand<string>(IntellisenseSwitchChangedCommandExecute);
            SensitivitySelectionChangedEventCommand = new RelayCommand<string>(SensitivitySelectionChangedEventCommandExecute);
            BorderColorSelectionChangedEventCommand = new RelayCommand<string>(BorderColorSelectionChangedEventCommandExe);

            Sensitivities = new List<int>() { 1, 2, 3, 4, 5 };
            Colors = new List<Color>()
            {
                System.Drawing.Color.Black,
                System.Drawing.Color.Blue,
                System.Drawing.Color.Brown,
                System.Drawing.Color.DarkGreen,
                System.Drawing.Color.DarkOrange,
                System.Drawing.Color.DeepPink,
                System.Drawing.Color.Purple,
                System.Drawing.Color.Red,
                System.Drawing.Color.Aqua
                
            };
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiEnabled, out _isHdiSwitchChecked))
                {
                    IsHdiSwitchChecked = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiEnabled, out _isDeiSwitchChecked))
                {
                    IsDeiSwitchChecked = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseEiEnabled, out _isEiSwitchChecked))
                {
                    IsEiSwitchChecked = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiSensitivity, out _hdiSensitivity))
                {
                    HdiSensitivity = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiSensitivity, out _deiSensitivity))
                {
                    DeiSensitivity = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseEiSensitivity, out _eiSensitivity))
                {
                    EiSensitivity = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiAudibleAlarm, out _deiAudibleAlarmEnabled))
                {
                    DeiAudibleAlarmEnabled = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiLightAlarm, out _deiLightAlarmEnabled))
                {
                    DeiLightAlarmEnabled = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiStopConveyor, out _deiStopConveyorEnabled))
                {
                    DeiStopConveyorEnabled = false;
                }


                if (!ScannerConfig.Read(ConfigPath.IntellisenseEiAudibleAlarm, out _eiAudibleAlarmEnabled))
                {
                    EiAudibleAlarmEnabled = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseEiLightAlarm, out _eiLightAlarmEnabled))
                {
                    EiLightAlarmEnabled = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseEiStopConveyor, out _eiStopConveyorEnabled))
                {
                    EiStopConveyorEnabled = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiAudibleAlarm, out _hdiAudibleAlarmEnabled))
                {
                    HdiAudibleAlarmEnabled = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiLightAlarm, out _hdiLightAlarmEnabled))
                {
                    HdiLightAlarmEnabled = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiStopConveyor, out _hdiStopConveyorEnabled))
                {
                    HdiStopConveyorEnabled = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiBorderColor, out _deiBorderColor))
                {
                    _deiBorderColor = 3;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiBorderColor, out _hdiBorderColor))
                {
                    _hdiBorderColor = 8;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseEiBorderColor, out _eiBorderColor))
                {
                    _eiBorderColor = 7;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 高密度告警的开关状态发生变化
        /// </summary>
        private void IntellisenseSwitchChangedCommandExecute(string args)
        {
            ScannerConfig.Write(ConfigPath.IntellisenseHdiEnabled, IsHdiSwitchChecked);
            ScannerConfig.Write(ConfigPath.IntellisenseDeiEnabled, IsDeiSwitchChecked);
            ScannerConfig.Write(ConfigPath.IntellisenseEiEnabled, IsEiSwitchChecked);

            ScannerConfig.Write(ConfigPath.IntellisenseDeiAudibleAlarm, _deiAudibleAlarmEnabled);
            ScannerConfig.Write(ConfigPath.IntellisenseDeiLightAlarm, _deiLightAlarmEnabled);
            ScannerConfig.Write(ConfigPath.IntellisenseDeiStopConveyor, _deiStopConveyorEnabled);

            ScannerConfig.Write(ConfigPath.IntellisenseHdiAudibleAlarm, _hdiAudibleAlarmEnabled);
            ScannerConfig.Write(ConfigPath.IntellisenseHdiLightAlarm, _hdiLightAlarmEnabled);
            ScannerConfig.Write(ConfigPath.IntellisenseHdiStopConveyor, _hdiStopConveyorEnabled);

            ScannerConfig.Write(ConfigPath.IntellisenseEiAudibleAlarm, _eiAudibleAlarmEnabled);
            ScannerConfig.Write(ConfigPath.IntellisenseEiLightAlarm, _eiLightAlarmEnabled);
            ScannerConfig.Write(ConfigPath.IntellisenseEiStopConveyor, _eiStopConveyorEnabled);

            RecordOperation(args);
        }

        /// <summary>
        /// 高密度告警的敏感度设置发生变化
        /// </summary>
        /// <param name="args"></param>
        public void SensitivitySelectionChangedEventCommandExecute(string arg)
        {
            ScannerConfig.Write(ConfigPath.IntellisenseDeiSensitivity, DeiSensitivity);
            ScannerConfig.Write(ConfigPath.IntellisenseHdiSensitivity, HdiSensitivity);
            ScannerConfig.Write(ConfigPath.IntellisenseEiSensitivity, EiSensitivity);

            RecordOperation(arg);
        }

        private void BorderColorSelectionChangedEventCommandExe(string arg)
        {
            ScannerConfig.Write(ConfigPath.IntellisenseDeiBorderColor, DeiBorderColor);
            ScannerConfig.Write(ConfigPath.IntellisenseHdiBorderColor, HdiBorderColor);
            ScannerConfig.Write(ConfigPath.IntellisenseEiBorderColor, EiBorderColor);

            RecordOperation(arg);
        }

        private void RecordOperation(string args)
        {
            if (args.Equals("HighDensity"))
            {
                var sb = new StringBuilder();
                sb.Append("IsEnable:").Append(IsHdiSwitchChecked);
                if (IsHdiSwitchChecked)
                {
                    sb.Append(",");
                    sb.Append("Sensitivity:").Append(HdiSensitivity).Append(",");
                    sb.Append("BorderColor:").Append(ColorsStr[HdiBorderColor]).Append(",");
                    sb.Append("AudibleAlarm:").Append(_hdiAudibleAlarmEnabled).Append(",");
                    sb.Append("LightAlarm:").Append(_hdiLightAlarmEnabled).Append(",");
                    sb.Append("StopConveyor:").Append(_hdiStopConveyorEnabled);
                }
                Tracer.TraceInfo(string.Format("{0} change {1}", "HighDensity", "Border"));
                new OperationRecordService().RecordOperation(OperationUI.IntelliSense, "HighDensity", OperationCommand.Setting, sb.ToString());
            }
            else if (args.Equals("Drugs"))
            {
                var sb = new StringBuilder();
                sb.Append("IsEnable:").Append(IsDeiSwitchChecked);
                if (IsDeiSwitchChecked)
                {
                    sb.Append(",");
                    sb.Append("Sensitivity:").Append(DeiSensitivity).Append(",");
                    sb.Append("BorderColor:").Append(ColorsStr[DeiBorderColor]).Append(",");
                    sb.Append("AudibleAlarm:").Append(_deiAudibleAlarmEnabled).Append(",");
                    sb.Append("LightAlarm:").Append(_deiLightAlarmEnabled).Append(",");
                    sb.Append("StopConveyor:").Append(_deiStopConveyorEnabled);
                }                

                new OperationRecordService().RecordOperation(OperationUI.IntelliSense, "Drugs", OperationCommand.Setting, sb.ToString());
            }
            else if (args.Equals("Explosives"))
            {
                var sb = new StringBuilder();
                sb.Append("IsEnable:").Append(IsEiSwitchChecked);

                if (IsEiSwitchChecked)
                {
                    sb.Append(",");
                    sb.Append("Sensitivity:").Append(EiSensitivity).Append(",");
                    sb.Append("BorderColor:").Append(ColorsStr[EiBorderColor]).Append(",");
                    sb.Append("AudibleAlarm:").Append(_eiAudibleAlarmEnabled).Append(",");
                    sb.Append("LightAlarm:").Append(_eiLightAlarmEnabled).Append(",");
                    sb.Append("StopConveyor:").Append(_eiStopConveyorEnabled);
                }               

                new OperationRecordService().RecordOperation(OperationUI.IntelliSense, "Explosives", OperationCommand.Setting, sb.ToString());
            }
        }
    }
}

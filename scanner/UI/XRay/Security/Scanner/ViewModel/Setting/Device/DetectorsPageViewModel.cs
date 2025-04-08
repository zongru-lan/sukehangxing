using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Widgets;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Device
{
    public class DetectViewSelection
    {
        public DetectViewSelection(int viewNum, string display)
        {
            ViewNum = viewNum;
            DisplayViewNum = display;
        }

        public string DisplayViewNum { get; set; }

        /// <summary>
        /// 1或2表示视角编号
        /// </summary>
        public int ViewNum { get; set; }
    }

    public class DetectorsPageViewModel : PageViewModelBase
    {
        #region commands

        public RelayCommand IsXRayOnCheckedChangedCommand { get; set; }

        /// <summary>
        /// 选择的视角发生变化的事件
        /// </summary>
        public RelayCommand ViewSelectionChangedEventCommand { get; set; }

        #endregion commands

        /// <summary>
        /// 所有曲线的数据源，用于绑定到曲线控件
        /// </summary>
        public ObservableCollection<CurveDataSource> Curves { get; private set; }

        public List<DetectViewSelection> DetectViews { get; private set; } 

        /// <summary>
        /// 低能数据曲线
        /// </summary>
        private CurveDataSource _lowCurve;

        /// <summary>
        /// 高能数据曲线
        /// </summary>
        private CurveDataSource _highCurve;

        /// <summary>
        /// 双视角数据设置是否可见
        /// </summary>
        private Visibility _dualViewSettingVisibility;

        private bool _isXRayOn;

        public bool IsXRayOn
        {
            get { return _isXRayOn; }
            set
            {
                _isXRayOn = value;
                RaisePropertyChanged();
            }
        }

        private bool _isFreezed;

        public bool IsFreezed
        {
            get { return _isFreezed; }
            set
            {
                _isFreezed = value;
                RaisePropertyChanged();
            }
        }

        private int _selectedView = 0;

        /// <summary>
        /// 是否交换两个探测视角的数据的顺序
        /// </summary>
        private bool _exchangeViewsOrder;

        /// <summary>
        /// 当前显示的视角数据的帧率
        /// </summary>
        private double _fps;

        /// <summary>
        /// 当前选中的视角索引，1表示视角1，2表示视角2
        /// </summary>
        public int SelectedView
        {
            get { return _selectedView; }
            set { _selectedView = value;RaisePropertyChanged(); }
        }

        /// <summary>
        /// 是否交换两个探测视角的数据的顺序
        /// </summary>
        public bool ExchangeViewsOrder
        {
            get { return _exchangeViewsOrder; }
            set { _exchangeViewsOrder = value; RaisePropertyChanged();}
        }


        /// <summary>
        /// 当前显示的视角数据的帧率
        /// </summary>
        public double Fps
        {
            get { return _fps; }
            set { _fps = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 双视角数据设置是否可见
        /// </summary>
        public Visibility DualViewSettingVisibility
        {
            get { return _dualViewSettingVisibility; }
            set { _dualViewSettingVisibility = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 上一次开始统计Fps的起始时刻
        /// </summary>
        private DateTime _fpsLastCheckTime = DateTime.Now;

        /// <summary>
        /// 用于统计Fps的线数
        /// </summary>
        private int _fpsLinesCounter;

        /// <summary>
        /// 探测视角个数
        /// </summary>
        private int _viewsCount = 1;

        public DetectorsPageViewModel()
        {
            LoadSettings();

            Curves = new ObservableCollection<CurveDataSource>();
            CaptureService.ServicePart.ScanlineCaptured += ServicePartOnScanlineCaptured;

            DetectViews = new List<DetectViewSelection>();
            DetectViews.Add(new DetectViewSelection(1, "1"));
            DualViewSettingVisibility = Visibility.Collapsed;

            if (_viewsCount > 1)
            {
                DetectViews.Add(new DetectViewSelection(2, "2"));

                // 在双视角模式，且当前用户为系统用户时，可以配置双视角的顺序
                if (LoginAccountManager.Service.HasLogin && LoginAccountManager.Service.CurrentAccount.Role == AccountRole.System)
                {
                    DualViewSettingVisibility = Visibility.Visible;
                }
            }

            SelectedView = 1;

            IsXRayOnCheckedChangedCommand = new RelayCommand(IsXRayOnCheckedChangedCommandExecute);
            ViewSelectionChangedEventCommand = new RelayCommand(ViewSelectionChangedEventCommandExecute);

            IsXRayOn = false;
        }

        private void LoadSettings()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewsCount))
                {
                    _viewsCount = 1;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private void IsXRayOnCheckedChangedCommandExecute()
        {
            if (IsXRayOn)
            {
                ControlService.ServicePart.RadiateXRay(true);
            }
            else
            {
                ControlService.ServicePart.RadiateXRay(false);
            }
        }

        /// <summary>
        /// 事件：选择另一个视角
        /// </summary>
        private void ViewSelectionChangedEventCommandExecute()
        {
            var viewIndex = SelectedView;
        }

        /// <summary>
        /// 添加或更新一条曲线到曲线显示控件。
        /// 规则：如果曲线尚未添加到曲线集合中，则添加；
        /// 如果曲线已经存在，则直接更新此曲线。
        /// </summary>
        /// <param name="curve"></param>
        private void AddorUpdateCurve(CurveDataSource curve)
        {
            var index = Curves.IndexOf(curve);
            if (index >= 0)
            {
                Curves[index] = curve;
            }
            else
            {
                Curves.Add(curve);
            }
        }

        /// <summary>
        /// 更新低能及高能数据曲线
        /// </summary>
        /// <param name="scanline"></param>
        private void ShowScanlineCurve(ScanlineData scanline)
        {
            if (scanline.Low != null)
            {
                if (_lowCurve == null)
                {
                    _lowCurve = new CurveDataSource(ToDoubles(scanline.Low),
                        "Low", Colors.Green);
                }
                else
                {
                    _lowCurve.DataPoints = ToDoubles(scanline.Low);
                }

                AddorUpdateCurve(_lowCurve);
            }

            if (scanline.High != null)
            {
                if (_highCurve == null)
                {
                    _highCurve = new CurveDataSource(ToDoubles(scanline.High),
                        "High", Colors.Red);
                }
                else
                {
                    _highCurve.DataPoints = ToDoubles(scanline.High);
                }

                AddorUpdateCurve(_highCurve);
            }
        }

        /// <summary>
        /// 将ushort类型的数组，转换为double类型的数组
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private double[] ToDoubles(ushort[] line)
        {
            double[] result = new double[line.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = line[i];
            }

            return result;
        }

        /// <summary>
        /// 上一次更新曲线的时刻
        /// </summary>
        private DateTime _lastUpDateTime = DateTime.Now;

        private void ServicePartOnScanlineCaptured(object sender, RawScanlineDataBundle scanlineDataBundle)
        {
            UpdateFps(scanlineDataBundle);
            UpdateCurveData(scanlineDataBundle);
        }

        /// <summary>
        /// 更新当前所选择的视角的数据
        /// </summary>
        /// <param name="scanlineDataBundle"></param>
        private void UpdateCurveData(RawScanlineDataBundle scanlineDataBundle)
        {
            RawScanlineData scanline = null;

            if (SelectedView == 1)
            {
                scanline = scanlineDataBundle.View1RawData;
            }
            else
            {
                scanline = scanlineDataBundle.View2RawData;
            }

            if (scanline != null)
            {

                if (DateTime.Now - _lastUpDateTime >= TimeSpan.FromMilliseconds(60))
                {
                    _lastUpDateTime = DateTime.Now;

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ShowScanlineCurve(scanline);
                    });
                }
            }
        }

        /// <summary>
        /// 更新当前所选择视角的数据帧率
        /// </summary>
        /// <param name="scanlineDataBundle"></param>
        private void UpdateFps(RawScanlineDataBundle scanlineDataBundle)
        {
            if (SelectedView == 1)
            {
                if (scanlineDataBundle.View1RawData != null)
                {
                    _fpsLinesCounter++;
                }
            }
            else
            {
                if (scanlineDataBundle.View2RawData != null)
                {
                    _fpsLinesCounter++;
                }
            }

            var ts = DateTime.Now - _fpsLastCheckTime;
            if (ts >= TimeSpan.FromSeconds(1))
            {
                var fps = _fpsLinesCounter / ts.TotalSeconds;

                _fpsLastCheckTime = DateTime.Now;
                _fpsLinesCounter = 0;

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Fps = fps;
                });
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void Cleanup()
        {
            // 页面结束时，务必关闭X射线
            ControlService.ServicePart.RadiateXRay(false);

            // 清理资源时，必须注销事件订阅，防止内存泄露
            CaptureService.ServicePart.ScanlineCaptured -= ServicePartOnScanlineCaptured;
            base.Cleanup();
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.F1:
                    IsFreezed = !IsFreezed;
                    args.Handled = true;
                    break;

                case Key.F2:
                    IsXRayOn = !IsXRayOn;
                    args.Handled = true;
                    break;
            }
        }
    }
}

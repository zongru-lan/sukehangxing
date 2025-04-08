using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;
using UI.XRay.ImagePlant;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Image
{
    /// <summary>
    /// 图像设置页的视图模型
    /// </summary>
    public class ImageSettingPageViewModel : PageViewModelBase
    {
        #region commands
        public RelayCommand RemoveBlankSpaceCheckChangedCommand { get; private set; }

        public RelayCommand ShowUnpenetrateRedCheckChangedCommand { get; private set; }

        public RelayCommand AbsorbIndexChangedCommand { get; private set; }

        public RelayCommand SaveMarginSettingCommand { get; private set; }

        public RelayCommand MarginPixelSettingChangedEventCommand { get; private set; }

        public RelayCommand ImageDefaultEffectsChangedEventCommand { get; private set; }

        #endregion commands

        private bool _removeBlankSpaceBetBags;

        /// <summary>
        /// 是否隐藏包裹图像间的空白区域
        /// </summary>
        public bool RemoveBlankSpaceBetBags
        {
            get { return _removeBlankSpaceBetBags; }
            set
            {
                _removeBlankSpaceBetBags = value;
                RaisePropertyChanged();
            }
        }

        private int _defaultAbsorbIndex;

        /// <summary>
        /// 默认的吸收率值，范围为-25~25，用于UI绑定
        /// </summary>
        public int DefaultAbsorbIndex
        {
            get { return _defaultAbsorbIndex; }
            set
            {
                _defaultAbsorbIndex = value;
                RaisePropertyChanged();
            }
        }

        private bool _showUnpenetratableRed;

        /// <summary>
        /// 是否将穿不透区域显示为红色，true代表显示为红色，false代表不处理，用于UI绑定
        /// </summary>
        public bool ShowUnpenetratableRed
        {
            get { return _showUnpenetratableRed; }
            set
            {
                _showUnpenetratableRed = value;
                RaisePropertyChanged();
            }
        }

        #region 图像上限置白设置
        public int View1MarginPixelsAtBegin
        {
            get { return _view1MarginPixelsAtBegin; }
            set
            {
                if (value >= 0)
                    _view1MarginPixelsAtBegin = value;
                RaisePropertyChanged();
            }
        } 

        public int View1MarginPixelsAtEnd
        {
            get { return _view1MarginPixelsAtEnd; }
            set
            {
                if (value >= 0)
                    _view1MarginPixelsAtEnd = value;
                RaisePropertyChanged();
            }
        }

        public int View2MarginPixelsAtBegin
        {
            get { return _view2MarginPixelsAtBegin; }
            set
            {
                if (value >= 0)
                    _view2MarginPixelsAtBegin = value;
                RaisePropertyChanged();
            }
        }

        public int View2MarginPixelsAtEnd
        {
            get { return _view2MarginPixelsAtEnd; }
            set
            {
                if (value >= 0)
                    _view2MarginPixelsAtEnd = value; 
                RaisePropertyChanged();
            }
        }

        public Visibility View2MarginSettingVisibility
        {
            get { return _view2MarginSettingVisibility; }
            set { _view2MarginSettingVisibility = value; RaisePropertyChanged(); }
        }

        public bool IsSaveMarginSettingButtonEnabled
        {
            get { return _isSaveMarginSettingButtonEnabled; }
            set { _isSaveMarginSettingButtonEnabled = value; RaisePropertyChanged(); }
        }

        private int _view1MarginPixelsAtBegin;

        private int _view1MarginPixelsAtEnd;

        private int _view2MarginPixelsAtBegin;

        private int _view2MarginPixelsAtEnd;

        private Visibility _view2MarginSettingVisibility;

        #endregion 图像上下置白设置

        private int _imagesCount;

        private Visibility _image2EffectsRowVisibility = Visibility.Collapsed;

        public Visibility Image2EffectsRowVisibility
        {
            get { return _image2EffectsRowVisibility; }
            set { _image2EffectsRowVisibility = value; RaisePropertyChanged(); }
        }

        private bool _isSaveMarginSettingButtonEnabled;

        #region 图像默认特效设置

        public List<ValueStringItem<DisplayColorMode>> ColorModesList { get; private set; }
        public List<ValueStringItem<PenetrationMode>> PenetrationList { get; private set; }
        public List<ValueStringItem<bool>> BoolList { get; private set; }

        /// <summary>
        /// 图像1的默认特效
        /// </summary>
        public ImageGeneralSetting Image1Setting
        {
            get { return _image1Setting; }
            set { _image1Setting = value; RaisePropertyChanged();}
        }

        public ImageGeneralSetting Image2Setting
        {
            get { return _image2Setting; }
            set { _image2Setting = value; RaisePropertyChanged();}
        }

        private ImageGeneralSetting _image1Setting;

        private ImageGeneralSetting _image2Setting;

        #endregion 图像默认特效设置

        /// <summary>
        /// 构造函数
        /// </summary>
        public ImageSettingPageViewModel()
        {
            Tracer.TraceEnterFunc("ImagesSettingsViewModel Constructor.");

            RemoveBlankSpaceCheckChangedCommand = new RelayCommand(RemoveBlankSpaceCheckChangedCommandExecute);
            ShowUnpenetrateRedCheckChangedCommand = new RelayCommand(ShowUnpenetrateRedCheckChangedCommandExecute);
            AbsorbIndexChangedCommand = new RelayCommand(AbsorbIndexChangedCommandExecute);
            SaveMarginSettingCommand = new RelayCommand(SaveMarginSettingCommandExecute);
            MarginPixelSettingChangedEventCommand = new RelayCommand(MarginPixelSettingChangedEventCommandExecute);
            ImageDefaultEffectsChangedEventCommand = new RelayCommand(ImageDefaultEffectsChangedEventCommandExecute);

            try
            {
                InitBindingList();
                LoadSettings();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }

            Tracer.TraceExitFunc("ImagesSettingsViewModel Constructor.");
        }

        private void ImageDefaultEffectsChangedEventCommandExecute()
        {
            SaveImageSettings();
        }

        private void SaveImageSettings()
        {
            if (Image1Setting != null)
            {
                ImageGeneralSetting.SaveImage1Setting(Image1Setting);
            }

            if (Image2Setting != null)
            {
                ImageGeneralSetting.SaveImage2Setting(Image2Setting);
            }
        }

        private void InitBindingList()
        {
            ColorModesList = new List<ValueStringItem<DisplayColorMode>>()
            {
                new ValueStringItem<DisplayColorMode>(DisplayColorMode.Grey, TranslationService.FindTranslation(DisplayColorMode.Grey)),
                new ValueStringItem<DisplayColorMode>(DisplayColorMode.MaterialColor, TranslationService.FindTranslation(DisplayColorMode.MaterialColor)),
                new ValueStringItem<DisplayColorMode>(DisplayColorMode.PseudoColor1, TranslationService.FindTranslation(DisplayColorMode.PseudoColor1)),
                new ValueStringItem<DisplayColorMode>(DisplayColorMode.PseudoColor2, TranslationService.FindTranslation(DisplayColorMode.PseudoColor2)),
                new ValueStringItem<DisplayColorMode>(DisplayColorMode.PseudoColor3, TranslationService.FindTranslation(DisplayColorMode.PseudoColor3)),
            };

            PenetrationList = new List<ValueStringItem<PenetrationMode>>()
            {
                //new NullableValueString<PenetrationMode?>(null, string.Empty),
                new ValueStringItem<PenetrationMode>(PenetrationMode.Standard, TranslationService.FindTranslation(PenetrationMode.Standard)),
                new ValueStringItem<PenetrationMode>(PenetrationMode.SlicePenetrate, TranslationService.FindTranslation(PenetrationMode.SlicePenetrate)),
                new ValueStringItem<PenetrationMode>(PenetrationMode.HighPenetrate, TranslationService.FindTranslation(PenetrationMode.HighPenetrate)),
                new ValueStringItem<PenetrationMode>(PenetrationMode.LowPenetrate, TranslationService.FindTranslation(PenetrationMode.LowPenetrate)),
            };

            BoolList = new List<ValueStringItem<bool>>()
            {
                new ValueStringItem<bool>(true, TranslationService.FindTranslation("Yes")),
                new ValueStringItem<bool>(false, TranslationService.FindTranslation("No"))
            };
        }

        /// <summary>
        /// 上下边缘空白设定发生变化后，启用对应的保存按钮
        /// </summary>
        private void MarginPixelSettingChangedEventCommandExecute()
        {
            IsSaveMarginSettingButtonEnabled = true;
        }

        private void RemoveBlankSpaceCheckChangedCommandExecute()
        {
            ScannerConfig.Write(ConfigPath.ImagesShowBlankSpace, !_removeBlankSpaceBetBags);
            new OperationRecordService().RecordOperation(OperationUI.ImageSetting, "ImageBlankSpace", OperationCommand.Setting, (!_removeBlankSpaceBetBags).ToString());
        }

        private void ShowUnpenetrateRedCheckChangedCommandExecute()
        {
            ScannerConfig.Write(ConfigPath.ImagesShowUnpenetratableRed, _showUnpenetratableRed);
            new OperationRecordService().RecordOperation(OperationUI.ImageSetting, "ShowUnpenetrateRed", OperationCommand.Setting, _showUnpenetratableRed.ToString());
        }

        private void AbsorbIndexChangedCommandExecute()
        {
            ScannerConfig.Write(ConfigPath.ImagesDefaultAbsorbIndex, _defaultAbsorbIndex);
            new OperationRecordService().RecordOperation(OperationUI.ImageSetting, "ImagesDefaultAbsorb", OperationCommand.Setting, _defaultAbsorbIndex.ToString());
        }

        private void SaveMarginSettingCommandExecute()
        {
            try
            {
                ScannerConfig.Write(ConfigPath.MachineView1BeltEdgeAtBegin, _view1MarginPixelsAtBegin);
                ScannerConfig.Write(ConfigPath.MachineView1BeltEdgeAtEnd, _view1MarginPixelsAtEnd);

                string content = string.Format("{0},{1}", _view1MarginPixelsAtBegin, _view1MarginPixelsAtEnd);
                if (_imagesCount > 1)
                {
                    ScannerConfig.Write(ConfigPath.MachineView2BeltEdgeAtBegin, _view2MarginPixelsAtBegin);
                    ScannerConfig.Write(ConfigPath.MachineView2BeltEdgeAtEnd, _view2MarginPixelsAtEnd);
                    content = string.Format("{0},{1};{2},{3}", _view1MarginPixelsAtBegin, _view1MarginPixelsAtEnd, _view2MarginPixelsAtBegin, _view2MarginPixelsAtEnd);
                }

                new OperationRecordService().RecordOperation(OperationUI.ImageSetting,"MarginSetting", OperationCommand.Setting, content);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 加载当前的图像设置
        /// </summary>
        public void LoadSettings()
        {
            Tracer.TraceEnterFunc("ImagesSettingsViewModel.LoadSettings.");

            if (!ScannerConfig.Read(ConfigPath.ImagesShowBlankSpace, out _removeBlankSpaceBetBags))
            {
                _removeBlankSpaceBetBags = false;
            }

            RemoveBlankSpaceBetBags = !RemoveBlankSpaceBetBags;

            if (!ScannerConfig.Read(ConfigPath.ImagesCount, out _imagesCount))
            {
                _imagesCount = 1;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesDefaultAbsorbIndex, out _defaultAbsorbIndex))
            {
                DefaultAbsorbIndex = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesShowUnpenetratableRed, out _showUnpenetratableRed))
            {
                ShowUnpenetratableRed = false;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtBegin, out _view1MarginPixelsAtBegin))
            {
                _view1MarginPixelsAtBegin = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtEnd, out _view1MarginPixelsAtEnd))
            {
                _view1MarginPixelsAtEnd = 0;
            }

            Image1Setting = ImageGeneralSetting.LoadImage1Setting();

            int viewsCount = 1;
            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out viewsCount))
            {
                viewsCount = 1;
            }

            if (viewsCount > 1)
            {
                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtBegin, out _view2MarginPixelsAtBegin))
                {
                    _view2MarginPixelsAtBegin = 0;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtEnd, out _view2MarginPixelsAtEnd))
                {
                    _view2MarginPixelsAtEnd = 0;
                }

                View2MarginSettingVisibility = Visibility.Visible;
            }

            if (_imagesCount > 1)
            {
                Image2Setting = ImageGeneralSetting.LoadImage2Setting();
                Image2EffectsRowVisibility = Visibility.Visible;
            }
            else
            {
                View2MarginSettingVisibility = Visibility.Collapsed;
            }

            Tracer.TraceExitFunc("ImagesSettingsViewModel.LoadSettings.");
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
        }
    }
}

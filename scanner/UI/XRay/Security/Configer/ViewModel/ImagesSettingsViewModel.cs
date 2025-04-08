using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;
using UI.XRay.ImagePlant;
using UI.XRay.ImagePlant.Gpu;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// 图像设置界面的ViewModel
    /// </summary>
    public class ImagesSettingsViewModel : ViewModelBase, IViewModel
    {
        private int _imagesCount = 1;
        /// <summary>
        /// 图像数量，用于UI绑定
        /// </summary>
        public int ImagesCount
        {
            get { return _imagesCount; }
            set
            {
                _imagesCount = value;
                if (_imagesCount == 1)
                {
                    ShowImage2Settings = Visibility.Collapsed;
                }
                else
                {
                    ShowImage2Settings = Visibility.Visible;
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 图像数量列表，用于UI绑定
        /// </summary>
        public List<int> ImagesCountSource { get; set; }

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

        private float _maxZoominTimes;
        /// <summary>
        /// 图像最大放大倍数，用于UI绑定
        /// </summary>
        public float MaxZoominTimes
        {
            get { return _maxZoominTimes; }
            set
            {
                _maxZoominTimes = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 图像最大放大倍数列表，用于UI显示
        /// </summary>
        public List<float> MaxZoominTimesSource { get; set; }

        private float _zoominStep;
        /// <summary>
        /// 图像缩放步长，用于UI显示
        /// </summary>
        public float ZoominStep
        {
            get { return _zoominStep; }
            set
            {
                _zoominStep = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 图像缩放步长列表，用于UI显示
        /// </summary>
        public List<float> ZoominSteps { get; set; }

        private bool _image1VerticalFlip;
        /// <summary>
        /// 图像1是否进行垂直翻转，true代表进行垂直翻转，false代表不处理，用于UI绑定
        /// </summary>
        public bool Image1VerticalFlip
        {
            get { return _image1VerticalFlip; }
            set
            {
                _image1VerticalFlip = value;
                RaisePropertyChanged();
            }
        }

        private bool _image2VerticalFlip;
        /// <summary>
        /// 图像2是否进行垂直翻转，true代表进行垂直翻转，false代表不处理，用于UI绑定
        /// </summary>
        public bool Image2VerticalFlip
        {
            get { return _image2VerticalFlip; }
            set
            {
                _image2VerticalFlip = value;
                RaisePropertyChanged();
            }
        }

        private string _image1ShowingDetView = DetectViewIndex.View1.ToString();
        /// <summary>
        /// 图像1显示的视角
        /// </summary>
        public string Image1ShowingDetView
        {
            get { return _image1ShowingDetView; }
            set
            {
                _image1ShowingDetView = value;
                RaisePropertyChanged();
            }
        }

        private string _image2ShowingDetView = DetectViewIndex.View2.ToString();
        /// <summary>
        /// 图像2显示的视角
        /// </summary>
        public string Image2ShowingDetView
        {
            get { return _image2ShowingDetView; }
            set
            {
                _image2ShowingDetView = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 图像显示视角列表
        /// </summary>
        public List<string> ImageShowingDetViews { get; set; }
        private Dictionary<DetectViewIndex, string> _ImageViewsStrMap = new Dictionary<DetectViewIndex, string>(); 

        private bool _image1RightToLeft = true;


        public List<string> ImageAnchors { get; set; }
        private Dictionary<ImagePlant.Gpu.ImageAnchor, string> _ImageAnchorStrMap = new Dictionary<ImagePlant.Gpu.ImageAnchor, string>(); 

        /// <summary>
        /// 图像1移动方向
        /// </summary>
        public bool Image1RightToLeft
        {
            get { return _image1RightToLeft; }
            set
            {
                _image1RightToLeft = value;
                RaisePropertyChanged();
            }
        }

        private bool _image2RightToLeft = true;
        /// <summary>
        /// 图像2移动方向
        /// </summary>
        public bool Image2RightToLeft
        {
            get { return _image2RightToLeft; }
            set
            {
                _image2RightToLeft = value;
                RaisePropertyChanged();
            }
        }

        private string _image1ColorMode = DisplayColorMode.MaterialColor.ToString();

        /// <summary>
        /// 图像1的颜色类型
        /// </summary>
        public string Image1ColorMode
        {
            get { return _image1ColorMode; }
            set
            {
                _image1ColorMode = value;
                RaisePropertyChanged();
            }
        }

        private string _image2ColorMode = DisplayColorMode.MaterialColor.ToString();
        /// <summary>
        /// 图像2的颜色类型
        /// </summary>
        public string Image2ColorMode
        {
            get { return _image2ColorMode; }
            set
            {
                _image2ColorMode = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 图像颜色列表
        /// </summary>
        public List<string> ImageColorModes { get; set; }
        private Dictionary<DisplayColorMode, string> _ImageColorsStrMap = new Dictionary<DisplayColorMode, string>(); 

        private bool _image1EnableEdgeEnhance;
        /// <summary>
        /// 图像1是否使能边缘增强模式
        /// </summary>
        public bool Image1EnableEdgeEnhance
        {
            get { return _image1EnableEdgeEnhance; }
            set
            {
                _image1EnableEdgeEnhance = value;
                RaisePropertyChanged();
            }
        }

        private bool _image2EnableEdgeEnhance;
        /// <summary>
        /// 图像2是否使能边缘增强模式
        /// </summary>
        public bool Image2EnableEdgeEnhance
        {
            get { return _image2EnableEdgeEnhance; }
            set
            {
                _image2EnableEdgeEnhance = value;
                RaisePropertyChanged();
            }
        }

        private bool _image1EnableSuperEnhance;
        /// <summary>
        /// 图像1是否使能超级增强模式
        /// </summary>
        public bool Image1EnableSuperEnhance
        {
            get { return _image1EnableSuperEnhance; }
            set
            {
                _image1EnableSuperEnhance = value;
                RaisePropertyChanged();
            }
        }

        private bool _image2EnableSuperEnhance;
        /// <summary>
        /// 图像2是否使能超级增强模式
        /// </summary>
        public bool Image2EnableSuperEnhance
        {
            get { return _image2EnableSuperEnhance; }
            set
            {
                _image2EnableSuperEnhance = value;
                RaisePropertyChanged();
            }
        }

        private bool _image1Inversed;
        /// <summary>
        /// 图像1是否进行反色
        /// </summary>
        public bool Image1Inversed
        {
            get { return _image1Inversed; }
            set
            {
                _image1Inversed = value;
                RaisePropertyChanged();
            }
        }

        private bool _image2Inversed;
        /// <summary>
        /// 图像2是否进行反色
        /// </summary>
        public bool Image2Inversed
        {
            get { return _image2Inversed; }
            set
            {
                _image2Inversed = value;
                RaisePropertyChanged();
            }
        }

        private string _image1Penetration = PenetrationMode.Standard.ToString();
        /// <summary>
        /// 图像1的穿透模式
        /// </summary>
        public string Image1Penetration
        {
            get { return _image1Penetration; }
            set
            {
                _image1Penetration = value;
                RaisePropertyChanged();
            }
        }

        private string _image2Penetration = PenetrationMode.Standard.ToString();
        /// <summary>
        /// 图像2的穿透模式
        /// </summary>
        public string Image2Penetration
        {
            get { return _image2Penetration; }
            set
            {
                _image2Penetration = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 图像穿透模式列表
        /// </summary>
        public List<string> ImagePenetrationModes { get; set; }
        private Dictionary<PenetrationMode, string> _ImagePeneModeStrMap = new Dictionary<PenetrationMode, string>(); 

        private Visibility _showImage2Settings = Visibility.Collapsed;
        /// <summary>
        /// 是否显示图像2的设置，当选中图像数量为2时，显示图像2的设置，否则不显示
        /// </summary>
        public Visibility ShowImage2Settings
        {
            get { return _showImage2Settings; }
            set
            {
                _showImage2Settings = value;
                RaisePropertyChanged();
            }
        }
        private int _image1Height;

        public int Image1Height
        {
            get { return _image1Height; }
            set { _image1Height = value; RaisePropertyChanged(); }
        }
        private int _image2Height;

        public int Image2Height
        {
            get { return _image2Height; }
            set { _image2Height = value; RaisePropertyChanged(); }
        }

        private string _image1Anchor;

        public string Image1Anchor
        {
            get { return _image1Anchor; }
            set { _image1Anchor = value; RaisePropertyChanged(); }
        }

        private string _image2Anchor;

        public string Image2Anchor
        {
            get { return _image2Anchor; }
            set { _image2Anchor = value; RaisePropertyChanged(); }
        }

        private int _imageMargin;

        public int ImageMargin
        {
            get { return _imageMargin; }
            set { _imageMargin = value; RaisePropertyChanged(); }
        }
        private bool _anchorNewImage;

        public bool AnchorNewImage
        {
            get { return _anchorNewImage; }
            set { _anchorNewImage = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 界面显示用翻译语言，但是保存到配置中要用原始语言
        /// </summary>
        private Dictionary<string, string> TransToSource = new Dictionary<string, string>();

        /// <summary>
        /// 直接使用翻译类库
        /// </summary>
        private readonly string _suspiciousStr = TranslationService.FindTranslation("Configer", "Suspicious");
        private readonly string _allStr = TranslationService.FindTranslation("Configer", "All");

        /// <summary>
        /// 选择图像保存路径命令，用于UIb绑定
        /// </summary>
        public RelayCommand ChangeImageStorePathCommand { get; set; }

        private string _imageStorePath = @"D:\";
        /// <summary>
        /// 图像保存路径
        /// </summary>
        public string ImageStorePath
        {
            get { return _imageStorePath; }
            set
            {
                _imageStorePath = value;
                RaisePropertyChanged();
            }
        }

        private bool _isAutoSaveUpfImage = false;

        /// <summary>
        /// 是否自动保存通用格式图像（Universal Picture Format）
        /// </summary>
        public bool IsAutoSaveUpfImage
        {
            get { return _isAutoSaveUpfImage; }
            set
            {
                _isAutoSaveUpfImage = value;
                ShowUpfImageStoreSetting = _isAutoSaveUpfImage ? Visibility.Visible : Visibility.Collapsed;
                RaisePropertyChanged();
            }
        }

        private Visibility _showUpfImageStoreSetting = Visibility.Collapsed;
        /// <summary>
        /// 是否显示通用图像格式保存设置，只有在电机了自动保存
        /// </summary>
        public Visibility ShowUpfImageStoreSetting
        {
            get { return _showUpfImageStoreSetting; }
            set { _showUpfImageStoreSetting = value; RaisePropertyChanged(); }
        }

        private string _upfImageStorePath;
        /// <summary>
        /// 通用图像格式保存路径
        /// </summary>
        public string UpfImageStorePath
        {
            get { return _upfImageStorePath; }
            set { _upfImageStorePath = value; RaisePropertyChanged(); }
        }
        public RelayCommand ChangeUpfImageStorePathCommand { get; set; }

        private List<string> _upfImageStoreStrategies;
        /// <summary>
        /// 通用图像保存策略
        /// </summary>
        public List<string> UpfImageStoreStrategies
        {
            get { return _upfImageStoreStrategies; }
            set { _upfImageStoreStrategies = value; RaisePropertyChanged(); }
        }

        private string _upfImageStoreStrategy;
        /// <summary>
        /// 当前选定的策略项
        /// </summary>
        public string UpfImageStoreStrategy
        {
            get { return _upfImageStoreStrategy; }
            set { _upfImageStoreStrategy = value; RaisePropertyChanged(); }
        }

        private List<string> _upfImageStoreFormats;
        /// <summary>
        /// 通用图像保存格式
        /// </summary>
        public List<string> UpfImageStoreFormats
        {
            get { return _upfImageStoreFormats; }
            set { _upfImageStoreFormats = value; RaisePropertyChanged(); }
        }

        private string _upfImageStoreFormat;
        /// <summary>
        /// 当前选定的图像保存格式
        /// </summary>
        public string UpfImageStoreFormat
        {
            get { return _upfImageStoreFormat; }
            set { _upfImageStoreFormat = value; RaisePropertyChanged(); }
        }

        private int _exportImageFormat;

        public int ExportImageFormat
        {
            get { return _exportImageFormat; }
            set { _exportImageFormat = value; RaisePropertyChanged(); }
        }


        private Visibility _showMergeTwoViewImage;
        /// <summary>
        /// 是否显示拼接两视角图像选项，只有在双视角模式下显示
        /// </summary>
        public Visibility ShowMergeTwoViewImage
        {
            get { return _showMergeTwoViewImage; }
            set { _showMergeTwoViewImage = value; RaisePropertyChanged(); }
        }

        private bool _isMergeTwoViewImage = false;

        /// <summary>
        /// 是否合并两个视角的图像
        /// </summary>
        public bool IsMergeTwoViewImage
        {
            get { return _isMergeTwoViewImage; }
            set
            {
                _isMergeTwoViewImage = value;
                RaisePropertyChanged();
            }
        }

        private bool _isAutoStoreXrayImage = false;
        /// <summary>
        /// 是否自动保存Xray图像
        /// </summary>
        public bool IsAutoStoreXrayImage
        {
            get { return _isAutoStoreXrayImage; }
            set { _isAutoStoreXrayImage = value; RaisePropertyChanged(); }
        }

        private bool _isLimitAutoStoreUpfImageCount = false;

        /// <summary>
        /// 是否限制自动保存图像个数
        /// </summary>
        public bool IsLimitAutoStoreUpfImageCount
        {
            get { return _isLimitAutoStoreUpfImageCount; }
            set
            {
                _isLimitAutoStoreUpfImageCount = value;
                ShowUpfImageCountLimit = _isLimitAutoStoreUpfImageCount ? Visibility.Visible : Visibility.Collapsed;
                RaisePropertyChanged();
            }
        }

        private int _upfImageCountUpperLimit;
        /// <summary>
        /// 保存图像数量的上限
        /// </summary>
        public int UpfImageCountUpperLimit
        {
            get { return _upfImageCountUpperLimit; }
            set { _upfImageCountUpperLimit = value; RaisePropertyChanged(); }
        }

        private Visibility _showUpfImageCountLimit = Visibility.Collapsed;
        /// <summary>
        /// 是否显示图像数量阈值
        /// </summary>
        public Visibility ShowUpfImageCountLimit
        {
            get { return _showUpfImageCountLimit; }
            set { _showUpfImageCountLimit = value; RaisePropertyChanged(); }
        }
        //图像保存效果
        public List<string> ImageEffects { get; set; }

        private Dictionary<ExportImageEffects, string> _ImageEffectStrMap = new Dictionary<ExportImageEffects, string>(); 


        public string Image1Effect
        {
            get
            {
                return TranslationService.FindTranslation("Configer",ExportImageEffects.Regular.ToString());
            }
            set { }
        }
        public string Image2Effect
        {
            get
            {
                return TranslationService.FindTranslation("Configer",ExportImageEffects.Absorptivity.ToString());
            }
            set { }
        }
        public string Image3Effect
        {
            get
            {
                return TranslationService.FindTranslation("Configer",ExportImageEffects.Grey.ToString());
            }
            set { }
        }
        public string Image4Effect
        {
            get
            {
                return TranslationService.FindTranslation("Configer",ExportImageEffects.SuperEnhance.ToString());
            }
            set { }
        }

        private string _image5Effect;

        public string Image5Effect
        {
            get { return _image5Effect; }
            set { _image5Effect = value; RaisePropertyChanged(); }
        }
        

        /// <summary>
        /// 构造函数
        /// </summary>
        public ImagesSettingsViewModel()
        {
            Tracer.TraceEnterFunc("ImagesSettingsViewModel Constructor.");

            ImagesCountSource = new List<int> { 1, 2 };

            MaxZoominTimesSource = new List<float> { 64, 32, 16 };

            ZoominSteps = new List<float> { 2, 4 };

            InitDictionary();
            ImageShowingDetViews = _ImageViewsStrMap.Values.ToList();

            ImageColorModes = _ImageColorsStrMap.Values.ToList();

            ImageAnchors = _ImageAnchorStrMap.Values.ToList();

            ImagePenetrationModes = _ImagePeneModeStrMap.Values.ToList();

            ChangeImageStorePathCommand = new RelayCommand(ChangeImageStorePathCommandExecute);
            try
            {                
                LoadSettings();
                //初始化自动保存通用格式图像的功能
                InitialAutoSaveUpfImage();
                
                ImageEffects = _ImageEffectStrMap.Values.ToList();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }

            Tracer.TraceExitFunc("ImagesSettingsViewModel Constructor.");
        }

        public void SaveSettings()
        {
            Tracer.TraceEnterFunc("ImagesSettingsViewModel.SaveSettings.");

            ScannerConfig.Write(ConfigPath.ImagesCount, _imagesCount);

            ScannerConfig.Write(ConfigPath.ImageMargin, _imageMargin);

            ScannerConfig.Write(ConfigPath.ImageAnchorNewWhenEnd, _anchorNewImage);

            //ScannerConfig.Write(ConfigPath.ImagesShowUnpenetratableRed, _showUnpenetratableRed);

            //ScannerConfig.Write(ConfigPath.ImagesDefaultAbsorbIndex, _defaultAbsorbIndex);

            //ScannerConfig.Write(ConfigPath.ImagesShowUnpenetratableRed, _showUnpenetratableRed);

            //ScannerConfig.Write(ConfigPath.ImagesMaxZoominTimes, _maxZoominTimes);

            //ScannerConfig.Write(ConfigPath.ImagesZoominStep, _zoominStep);

            ScannerConfig.Write(ConfigPath.ImagesImage1VerticalFlip, _image1VerticalFlip);

            DetectViewIndex view1index = DetectViewIndex.View1;
            foreach (var item in _ImageViewsStrMap)
            {
                if (item.Value == _image1ShowingDetView)
                {
                    view1index = item.Key;
                    break;
                }
            }
            ScannerConfig.Write(ConfigPath.ImagesImage1ShowingDetView, view1index);

            ScannerConfig.Write(ConfigPath.Image1MoveRightToLeft, _image1RightToLeft);

            DisplayColorMode selectedvalue = DisplayColorMode.MaterialColor;
            foreach (var item in _ImageColorsStrMap)
            {
                if (item.Value == _image1ColorMode)
                {
                    selectedvalue = item.Key;
                    break;
                }
            }
            ScannerConfig.Write(ConfigPath.ImagesImage1ColorMode, selectedvalue.ToString());


            ScannerConfig.Write(ConfigPath.ImagesImage1EnableEdgeEnhance, _image1EnableEdgeEnhance);

            ScannerConfig.Write(ConfigPath.ImagesImage1EnableSuperEnhance, _image1EnableSuperEnhance);

            ScannerConfig.Write(ConfigPath.ImagesImage1Inversed, _image1Inversed);

            PenetrationMode image1pene = PenetrationMode.Standard;
            foreach (var item in _ImagePeneModeStrMap)
            {
                if (item.Value == _image1Penetration)
                {
                    image1pene = item.Key;
                    break;
                }
            }
            ScannerConfig.Write(ConfigPath.ImagesImage1Penetration, image1pene);

            ScannerConfig.Write(ConfigPath.ImagesImage1Height, _image1Height);

            ImageAnchor selectedAnchor1 = ImageAnchor.Center;
            foreach (var item in _ImageAnchorStrMap)
            {
                if (item.Value == _image1Anchor)
                {
                    selectedAnchor1 = item.Key;
                    break;
                }
            }
            ScannerConfig.Write(ConfigPath.ImagesImage1Anchor, selectedAnchor1.ToString());

            if (_imagesCount == 2)
            {
                // 图像2的设置
                ScannerConfig.Write(ConfigPath.ImagesImage2VerticalFlip, _image2VerticalFlip);

                DetectViewIndex view2index = DetectViewIndex.View2;
                foreach (var item in _ImageViewsStrMap)
                {
                    if (item.Value == _image2ShowingDetView)
                    {
                        view2index = item.Key;
                        break;
                    }
                }

                ScannerConfig.Write(ConfigPath.ImagesImage2ShowingDetView, view2index);
                ScannerConfig.Write(ConfigPath.Image2MoveRightToLeft, _image2RightToLeft);

                DisplayColorMode selectedvalue2 = DisplayColorMode.MaterialColor;
                foreach (var item in _ImageColorsStrMap)
                {
                    if (item.Value == _image2ColorMode)
                    {
                        selectedvalue2 = item.Key;
                        break;
                    }
                }
                ScannerConfig.Write(ConfigPath.ImagesImage2ColorMode, selectedvalue2.ToString());

                ScannerConfig.Write(ConfigPath.ImagesImage2EnableEdgeEnhance, _image2EnableEdgeEnhance);

                ScannerConfig.Write(ConfigPath.ImagesImage2EnableSuperEnhance, _image2EnableSuperEnhance);

                ScannerConfig.Write(ConfigPath.ImagesImage2Inversed, _image2Inversed);      

                PenetrationMode image2pene = PenetrationMode.Standard;
                foreach (var item in _ImagePeneModeStrMap)
                {
                    if (item.Value == _image2Penetration)
                    {
                        image2pene = item.Key;
                        break;
                    }
                }
                ScannerConfig.Write(ConfigPath.ImagesImage2Penetration, image2pene);

                ScannerConfig.Write(ConfigPath.ImagesImage2Height, _image2Height);

                ImageAnchor selectedAnchor2 = ImageAnchor.Center;
                foreach (var item in _ImageAnchorStrMap)
                {
                    if (item.Value == _image2Anchor)
                    {
                        selectedAnchor2 = item.Key;
                        break;
                    }
                }
                ScannerConfig.Write(ConfigPath.ImagesImage2Anchor, selectedAnchor2.ToString());
            }

            ScannerConfig.Write(ConfigPath.SystemImageStorePath, _imageStorePath);
            SaveAutoStoreUpfImageSetting();
            Tracer.TraceExitFunc("ImagesSettingsViewModel.SaveSettings.");
        }

        public void LoadSettings()
        {
            Tracer.TraceEnterFunc("ImagesSettingsViewModel.LoadSettings.");

            if (!ScannerConfig.Read(ConfigPath.ImagesCount, out _imagesCount))
            {
                ImagesCount = 1;
            }

            if (!ScannerConfig.Read(ConfigPath.ImageMargin, out _imageMargin))
            {
                ImageMargin = 0;
            }
            if (!ScannerConfig.Read(ConfigPath.ImageAnchorNewWhenEnd, out _anchorNewImage))
            {
                AnchorNewImage = false;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesShowBlankSpace, out _removeBlankSpaceBetBags))
            {
                RemoveBlankSpaceBetBags = false;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesDefaultAbsorbIndex, out _defaultAbsorbIndex))
            {
                DefaultAbsorbIndex = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesShowUnpenetratableRed, out _showUnpenetratableRed))
            {
                ShowUnpenetratableRed = false;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesMaxZoominTimes, out _maxZoominTimes))
            {
                MaxZoominTimes = 64;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesZoominStep, out _zoominStep))
            {
                ZoominStep = 1.1f;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1VerticalFlip, out _image1VerticalFlip))
            {
                Image1VerticalFlip = false;
            }
            string image1view;
            if (ScannerConfig.Read(ConfigPath.ImagesImage1ShowingDetView, out image1view))
            {
                try
                {
                    var effect = (DetectViewIndex)Enum.Parse(typeof(DetectViewIndex), image1view);
                    Image1ShowingDetView = _ImageViewsStrMap[effect];
                }
                catch (Exception e)
                {
                    Image1ShowingDetView = _ImageViewsStrMap[DetectViewIndex.View1];
                }

            }
            else
            {
                Image1ShowingDetView = _ImageViewsStrMap[DetectViewIndex.View1];
            }

            if (!ScannerConfig.Read(ConfigPath.Image1MoveRightToLeft, out _image1RightToLeft))
            {
                Image1RightToLeft = false;
            }

            string image1color;
            if (ScannerConfig.Read(ConfigPath.ImagesImage1ColorMode, out image1color))
            {
                try
                {
                    var effect = (DisplayColorMode)Enum.Parse(typeof(DisplayColorMode), image1color);
                    Image1ColorMode = _ImageColorsStrMap[effect];
                }
                catch (Exception e)
                {
                    Image1ColorMode = _ImageColorsStrMap[DisplayColorMode.MaterialColor];
                }

            }
            else
            {
                Image1ColorMode = _ImageColorsStrMap[DisplayColorMode.MaterialColor];
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1EnableEdgeEnhance, out _image1EnableEdgeEnhance))
            {
                Image1EnableEdgeEnhance = true;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1EnableSuperEnhance, out _image1EnableSuperEnhance))
            {
                Image1EnableSuperEnhance = false;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1Inversed, out _image1Inversed))
            {
                Image1Inversed = false;
            }

            string image1pene;
            if (ScannerConfig.Read(ConfigPath.ImagesImage1Penetration, out image1pene))
            {
                try
                {
                    var mode = (PenetrationMode)Enum.Parse(typeof(PenetrationMode), image1pene);
                    Image1Penetration = _ImagePeneModeStrMap[mode];
                }
                catch (Exception e)
                {
                    Image1Penetration = _ImagePeneModeStrMap[PenetrationMode.Standard];
                }

            }
            else
            {
                Image1Penetration = _ImagePeneModeStrMap[PenetrationMode.Standard];
            }

            


            if (!ScannerConfig.Read(ConfigPath.ImagesImage1Height, out _image1Height))
            {
                Image1Height = 1152;
            }

            string image1Anchor;
            if (ScannerConfig.Read(ConfigPath.ImagesImage1Anchor, out image1Anchor))
            {
                try
                {
                    var effect = (ImageAnchor)Enum.Parse(typeof(ImageAnchor), image1Anchor);
                    Image1Anchor = _ImageAnchorStrMap[effect];
                }
                catch (Exception e)
                {
                    Image1Anchor = _ImageAnchorStrMap[ImageAnchor.Center];
                }

            }
            else
            {
                Image1Anchor = _ImageAnchorStrMap[ImageAnchor.Center];
            }

            if (_imagesCount >= 2)
            {

                ShowImage2Settings = Visibility.Visible;

                if (!ScannerConfig.Read(ConfigPath.ImagesImage2VerticalFlip, out _image2VerticalFlip))
                {
                    Image2VerticalFlip = false;
                }

                string image2view;
                if (ScannerConfig.Read(ConfigPath.ImagesImage2ShowingDetView, out image2view))
                {
                    try
                    {
                        var effect = (DetectViewIndex)Enum.Parse(typeof(DetectViewIndex), image2view);
                        Image2ShowingDetView = _ImageViewsStrMap[effect];
                    }
                    catch (Exception e)
                    {
                        Image2ShowingDetView = _ImageViewsStrMap[DetectViewIndex.View2];
                    }

                }
                else
                {
                    Image2ShowingDetView = _ImageViewsStrMap[DetectViewIndex.View2];
                }

                if (!ScannerConfig.Read(ConfigPath.Image2MoveRightToLeft, out _image2RightToLeft))
                {
                    Image2RightToLeft = false;
                }

                string image2color;
                if (ScannerConfig.Read(ConfigPath.ImagesImage2ColorMode, out image2color))
                {
                    try
                    {
                        var effect = (DisplayColorMode)Enum.Parse(typeof(DisplayColorMode), image2color);
                        Image2ColorMode = _ImageColorsStrMap[effect];
                    }
                    catch (Exception e)
                    {
                        Image2ColorMode = _ImageColorsStrMap[DisplayColorMode.MaterialColor];
                    }

                }
                else
                {
                    Image2ColorMode = _ImageColorsStrMap[DisplayColorMode.MaterialColor];
                }

                if (!ScannerConfig.Read(ConfigPath.ImagesImage2EnableEdgeEnhance, out _image2EnableEdgeEnhance))
                {
                    Image2EnableEdgeEnhance = true;
                }

                if (!ScannerConfig.Read(ConfigPath.ImagesImage2EnableSuperEnhance, out _image2EnableSuperEnhance))
                {
                    Image2EnableSuperEnhance = false;
                }

                if (!ScannerConfig.Read(ConfigPath.ImagesImage2Inversed, out _image2Inversed))
                {
                    Image2Inversed = false;
                }

                string image2pene;
                if (ScannerConfig.Read(ConfigPath.ImagesImage2Penetration, out image2pene))
                {
                    try
                    {
                        var mode = (PenetrationMode)Enum.Parse(typeof(PenetrationMode), image2pene);
                        Image2Penetration = _ImagePeneModeStrMap[mode];
                    }
                    catch (Exception e)
                    {
                        Image2Penetration = _ImagePeneModeStrMap[PenetrationMode.Standard];
                    }

                }
                else
                {
                    Image2Penetration = _ImagePeneModeStrMap[PenetrationMode.Standard];
                }

                if (!ScannerConfig.Read(ConfigPath.ImagesImage2Height, out _image2Height))
                {
                    Image2Height = 1152;
                }

                string image2Anchor;
                if (ScannerConfig.Read(ConfigPath.ImagesImage2Anchor, out image2Anchor))
                {
                    try
                    {
                        var effect = (ImageAnchor)Enum.Parse(typeof(ImageAnchor), image2Anchor);
                        Image2Anchor = _ImageAnchorStrMap[effect];
                    }
                    catch (Exception e)
                    {
                        Image2Anchor = _ImageAnchorStrMap[ImageAnchor.Center];
                    }

                }
                else
                {
                    Image2Anchor = _ImageAnchorStrMap[ImageAnchor.Center];
                }
            }
            else
            {
                ShowImage2Settings = Visibility.Collapsed;
            }
            //image save
            string result;
            if (ScannerConfig.Read(ConfigPath.SystemImageStorePath, out result))
            {
                ImageStorePath = result;
            }

            string image5;
            if (ScannerConfig.Read(ConfigPath.ImagesExportImage5Effect,out image5))
            {
                try
                {
                    var effect = (ExportImageEffects)Enum.Parse(typeof(ExportImageEffects), image5);
                    Image5Effect = _ImageEffectStrMap[effect];
                }
                catch (Exception e)
                {
                    Image5Effect = _ImageEffectStrMap[ExportImageEffects.None];
                }
                
            }
            else
            {
                Image5Effect = _ImageEffectStrMap[ExportImageEffects.None];
            }
            Tracer.TraceExitFunc("ImagesSettingsViewModel.LoadSettings.");
        }

        private void InitialAutoSaveUpfImage()
        {
            try
            {
                TransToSource.Add(_allStr, "All");
                TransToSource.Add(_suspiciousStr, "Suspicious");

                //保存路径
                string outTemp;
                ScannerConfig.Read(ConfigPath.AutoStoreUpfImagePath, out outTemp);
                UpfImageStorePath = outTemp;

                ChangeUpfImageStorePathCommand = new RelayCommand(ChangeUpfImageStorePathCommandExe);

                //保存策略
                UpfImageStoreStrategies = new List<string> { _suspiciousStr, _allStr };
                ScannerConfig.Read(ConfigPath.AutoStoreUpfImageStrategy, out outTemp);
                UpfImageStoreStrategy = TranslationService.FindTranslation("Configer", outTemp);

                //保存格式
                UpfImageStoreFormats = new List<string> { "jpg", "png", "bmp", "tiff" };
                ScannerConfig.Read(ConfigPath.AutoStoreUpfImageFormat, out outTemp);
                UpfImageStoreFormat = outTemp;

                if (!ScannerConfig.Read(ConfigPath.ImagesExportImageFormat,out _exportImageFormat))
                {
                    _exportImageFormat = 0;
                }

                //是否合并双视角图像
                int viewCount;
                ScannerConfig.Read(ConfigPath.MachineViewsCount, out viewCount);
                if (viewCount == 2)
                {
                    //如果是双视角
                    ShowMergeTwoViewImage = Visibility.Visible;
                    bool mergeTwoView;
                    ScannerConfig.Read(ConfigPath.MergeTwoViewImage, out mergeTwoView);
                    IsMergeTwoViewImage = mergeTwoView;
                }
                else
                {
                    ShowMergeTwoViewImage = Visibility.Collapsed;
                }

                bool autoStoreXray;
                ScannerConfig.Read(ConfigPath.AutoStoreXrayImage, out autoStoreXray);
                IsAutoStoreXrayImage = autoStoreXray;

                //读取功能开关
                bool isAutoStoreUpfImage;
                ScannerConfig.Read(ConfigPath.AutoStoreUpfImage, out isAutoStoreUpfImage);
                IsAutoSaveUpfImage = isAutoStoreUpfImage;

                ScannerConfig.Read(ConfigPath.LimitAutoStoreUpfImageCount, out _isLimitAutoStoreUpfImageCount);

                ShowUpfImageCountLimit = _isLimitAutoStoreUpfImageCount ? Visibility.Visible : Visibility.Collapsed;

                ScannerConfig.Read(ConfigPath.AutoStoreUpfImageCount, out _upfImageCountUpperLimit);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                IsAutoSaveUpfImage = false;
            }
        }

        private void ChangeUpfImageStorePathCommandExe()
        {
            var selectFolderDialog = new FolderBrowserDialog();

            if (selectFolderDialog.ShowDialog() == DialogResult.OK)
            {
                UpfImageStorePath = selectFolderDialog.SelectedPath;
            }
        }

        /// <summary>
        /// 选择图像保存路径命令执行体
        /// </summary>
        private void ChangeImageStorePathCommandExecute()
        {
            var selectFolderDialog = new FolderBrowserDialog();

            if (selectFolderDialog.ShowDialog() == DialogResult.OK)
            {
                ImageStorePath = selectFolderDialog.SelectedPath;
            }
        }
        private void SaveAutoStoreUpfImageSetting()
        {
            ScannerConfig.Write(ConfigPath.AutoStoreUpfImage, _isAutoSaveUpfImage);
            ScannerConfig.Write(ConfigPath.AutoStoreUpfImagePath, _upfImageStorePath);
            ScannerConfig.Write(ConfigPath.AutoStoreUpfImageStrategy, TransToSource[_upfImageStoreStrategy]);
            ScannerConfig.Write(ConfigPath.AutoStoreUpfImageFormat, _upfImageStoreFormat);
            ScannerConfig.Write(ConfigPath.MergeTwoViewImage, _isMergeTwoViewImage);
            ScannerConfig.Write(ConfigPath.AutoStoreXrayImage, _isAutoStoreXrayImage);
            ScannerConfig.Write(ConfigPath.LimitAutoStoreUpfImageCount, _isLimitAutoStoreUpfImageCount);
            ScannerConfig.Write(ConfigPath.AutoStoreUpfImageCount, _upfImageCountUpperLimit);

            ExportImageEffects selectedvalue = ExportImageEffects.None;
            foreach (var item in _ImageEffectStrMap)
            {
                if(item.Value == Image5Effect)
                {
                    selectedvalue = item.Key;
                    break;
                }
            }
            ScannerConfig.Write(ConfigPath.ImagesExportImage5Effect, selectedvalue.ToString());
            ScannerConfig.Write(ConfigPath.ImagesExportImageFormat, ExportImageFormat);
        }

        private void InitDictionary()
        {
            if (_ImageViewsStrMap == null)
            {
                _ImageViewsStrMap = new Dictionary<DetectViewIndex, string>();
            }
            if (_ImageEffectStrMap == null)
            {
                _ImageEffectStrMap = new Dictionary<ExportImageEffects, string>();
            }
            if (_ImageColorsStrMap == null)
            {
                _ImageColorsStrMap = new Dictionary<DisplayColorMode, string>();
            }
            if (_ImageAnchorStrMap == null)
            {
                _ImageAnchorStrMap = new Dictionary<ImageAnchor, string>();
            }
            if (_ImagePeneModeStrMap == null)
            {
                _ImagePeneModeStrMap = new Dictionary<PenetrationMode, string>();
            }
            try
            {
                _ImageViewsStrMap.Add(DetectViewIndex.View1, TranslationService.FindTranslation("Configer", DetectViewIndex.View1.ToString()));
                _ImageViewsStrMap.Add(DetectViewIndex.View2, TranslationService.FindTranslation("Configer", DetectViewIndex.View2.ToString()));

                _ImageEffectStrMap.Add(ExportImageEffects.None,TranslationService.FindTranslation("Configer",ExportImageEffects.None.ToString()));
                //_ImageEffectStrMap.Add(ExportImageEffects.Regular, TranslationService.FindTranslation("Configer", ExportImageEffects.Regular.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.SuperEnhance, TranslationService.FindTranslation("Configer", ExportImageEffects.SuperEnhance.ToString()));
                //_ImageEffectStrMap.Add(ExportImageEffects.Absorptivity, TranslationService.FindTranslation("Configer", ExportImageEffects.Absorptivity.ToString()));
                //_ImageEffectStrMap.Add(ExportImageEffects.Grey, TranslationService.FindTranslation("Configer", ExportImageEffects.Grey.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.OS, TranslationService.FindTranslation("Configer", ExportImageEffects.OS.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.MS, TranslationService.FindTranslation("Configer", ExportImageEffects.MS.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.Inverse, TranslationService.FindTranslation("Configer", ExportImageEffects.Inverse.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.SlicePenetrate, TranslationService.FindTranslation("Configer", ExportImageEffects.SlicePenetrate.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.HighPenetrate, TranslationService.FindTranslation("Configer", ExportImageEffects.HighPenetrate.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.LowPenetrate, TranslationService.FindTranslation("Configer", ExportImageEffects.LowPenetrate.ToString()));
                //_ImageEffectStrMap.Add(ExportImageEffects.SuperPenetrate, TranslationService.FindTranslation("Configer", ExportImageEffects.SuperPenetrate.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.Zeff7, TranslationService.FindTranslation("Configer", ExportImageEffects.Zeff7.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.Zeff8, TranslationService.FindTranslation("Configer", ExportImageEffects.Zeff8.ToString()));
                _ImageEffectStrMap.Add(ExportImageEffects.Zeff9, TranslationService.FindTranslation("Configer", ExportImageEffects.Zeff9.ToString()));

                _ImageColorsStrMap.Add(DisplayColorMode.MaterialColor, TranslationService.FindTranslation("Configer", DisplayColorMode.MaterialColor.ToString()));
                _ImageColorsStrMap.Add(DisplayColorMode.Grey, TranslationService.FindTranslation("Configer", DisplayColorMode.Grey.ToString()));
                _ImageColorsStrMap.Add(DisplayColorMode.OS, TranslationService.FindTranslation("Configer", DisplayColorMode.OS.ToString()));
                _ImageColorsStrMap.Add(DisplayColorMode.MS, TranslationService.FindTranslation("Configer", DisplayColorMode.MS.ToString()));
                _ImageColorsStrMap.Add(DisplayColorMode.Zeff7, TranslationService.FindTranslation("Configer", DisplayColorMode.Zeff7.ToString()));
                _ImageColorsStrMap.Add(DisplayColorMode.Zeff8, TranslationService.FindTranslation("Configer", DisplayColorMode.Zeff8.ToString()));
                _ImageColorsStrMap.Add(DisplayColorMode.Zeff9, TranslationService.FindTranslation("Configer", DisplayColorMode.Zeff9.ToString()));

                _ImageAnchorStrMap.Add(ImagePlant.Gpu.ImageAnchor.Center, TranslationService.FindTranslation("Configer", ImagePlant.Gpu.ImageAnchor.Center.ToString()));
                _ImageAnchorStrMap.Add(ImagePlant.Gpu.ImageAnchor.Top, TranslationService.FindTranslation("Configer", ImagePlant.Gpu.ImageAnchor.Top.ToString()));
                _ImageAnchorStrMap.Add(ImagePlant.Gpu.ImageAnchor.Bottom, TranslationService.FindTranslation("Configer", ImagePlant.Gpu.ImageAnchor.Bottom.ToString()));
                
                _ImagePeneModeStrMap.Add(PenetrationMode.SlicePenetrate, TranslationService.FindTranslation("Configer", PenetrationMode.SlicePenetrate.ToString()));
                _ImagePeneModeStrMap.Add(PenetrationMode.HighPenetrate, TranslationService.FindTranslation("Configer", PenetrationMode.HighPenetrate.ToString()));
                _ImagePeneModeStrMap.Add(PenetrationMode.LowPenetrate, TranslationService.FindTranslation("Configer", PenetrationMode.LowPenetrate.ToString()));

                _ImagePeneModeStrMap.Add(PenetrationMode.Standard, TranslationService.FindTranslation("Configer", PenetrationMode.Standard.ToString()));
                _ImagePeneModeStrMap.Add(PenetrationMode.SuperPenetrate, TranslationService.FindTranslation("Configer", PenetrationMode.SuperPenetrate.ToString()));
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }
    }
}

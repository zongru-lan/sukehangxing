using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GalaSoft.MvvmLight;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;
using UI.XRay.ImagePlant;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Controllers;
using UI.XRay.ImagePlant.Gpu;

namespace UI.XRay.Security.Scanner
{
    public class ImageGeneralSetting : ObservableObject
    {
        public bool Inversed
        {
            get { return _inversed; }
            set { _inversed = value; RaisePropertyChanged(); }
        }

        public PenetrationMode Penetration
        {
            get { return _penetration; }
            set { _penetration = value; RaisePropertyChanged(); }
        }

        public DisplayColorMode ColorMode
        {
            get { return _colorMode; }
            set { _colorMode = value; RaisePropertyChanged(); }
        }

        public bool SuperEnhance
        {
            get { return _superEnhance; }
            set { _superEnhance = value; RaisePropertyChanged(); }
        }

        public bool VerticalFlip
        {
            get { return _verticalFlip; }
            set { _verticalFlip = value; RaisePropertyChanged(); }
        }

        public bool DynamicGrayscaleTransform
        {
            get { return _dynamicGrayscaleTransform; }
            set { _dynamicGrayscaleTransform = value; RaisePropertyChanged(); }
        }

        public int Absorbtivity
        {
            get { return _absorbtivity; }
            set { _absorbtivity = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 吸收率变换开关（如果为true，则按下+-键时，将不会进行放大，而是调整吸收率）
        /// </summary>
        public bool AbsorbTransformSwitchOn
        {
            get { return _absorbTransformSwitchOn; }
            set { _absorbTransformSwitchOn = value; RaisePropertyChanged(); }
        }

        public bool ShowUnpenetrateRed
        {
            get { return _showUnpenetrateRed; }
            set { _showUnpenetrateRed = value; RaisePropertyChanged(); }
        }

        public bool MoveFromRightToLeft
        {
            get { return _moveFromRightToLeft; }
            set { _moveFromRightToLeft = value; RaisePropertyChanged(); }
        }

        public bool EdgeEnhance
        {
            get { return _edgeEnhance; }
            set { _edgeEnhance = value; RaisePropertyChanged(); }
        }

        public bool PenetrationEnhance
        {
            get { return _penetrationEnhance; }
            set { _penetrationEnhance = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 图像显示的探测视角数据
        /// </summary>
        public DetectViewIndex ShowingViewIndex
        {
            get { return _showingViewIndex; }
            set { _showingViewIndex = value; RaisePropertyChanged(); }
        }

        public ImageAnchor ImageAnchor
        {
            get { return _imageAnchor; }
            set { _imageAnchor = value; RaisePropertyChanged(); }
        }

        public float VerticalScale
        {
            get { return _verticalScale; }
            set { _verticalScale = value; RaisePropertyChanged(); }
        }
        public float HorizonalScale
        {
            get { return _horizonalScale; }
            set { _horizonalScale = value; RaisePropertyChanged(); }
        }

        private bool _inversed;

        private PenetrationMode _penetration;

        private DisplayColorMode _colorMode;

        private bool _superEnhance;

        private bool _verticalFlip;

        private bool _dynamicGrayscaleTransform;

        private int _absorbtivity;

        private bool _absorbTransformSwitchOn;

        private bool _edgeEnhance;

        private bool _penetrationEnhance;

        private bool _showUnpenetrateRed;

        private bool _moveFromRightToLeft;

        private ImageAnchor _imageAnchor = ImageAnchor.Center;

        private float _verticalScale;

        private float _horizonalScale;

        /// <summary>
        /// 图像显示的探测视角数据
        /// </summary>
        private DetectViewIndex _showingViewIndex;

        public ImageGeneralSetting()
        {
            try
            {
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 保存为图像1的配置
        /// </summary>
        /// <param name="setting"></param>
        public static void SaveImage1Setting(ImageGeneralSetting setting)
        {
            if (setting != null)
            {
                ScannerConfig.Write(ConfigPath.ImagesShowUnpenetratableRed, setting._showUnpenetrateRed);
                ScannerConfig.Write(ConfigPath.ImagesDefaultAbsorbIndex, setting._absorbtivity);

                ScannerConfig.Write(ConfigPath.ImagesImage1ColorMode, setting._colorMode);
                ScannerConfig.Write(ConfigPath.ImagesImage1Inversed, setting._inversed) ;
                ScannerConfig.Write(ConfigPath.ImagesImage1Penetration, setting._penetration) ;
                ScannerConfig.Write(ConfigPath.ImagesImage1EnableEdgeEnhance, setting._edgeEnhance) ;
                ScannerConfig.Write(ConfigPath.ImagesImage1EnableSuperEnhance, setting._superEnhance) ;
                ScannerConfig.Write(ConfigPath.ImagesImage1VerticalFlip, setting._verticalFlip) ;
                ScannerConfig.Write(ConfigPath.Image1MoveRightToLeft, setting._moveFromRightToLeft) ;
                ScannerConfig.Write(ConfigPath.ImagesImage1ShowingDetView, setting._showingViewIndex);
                ScannerConfig.Write(ConfigPath.ImagesImage1Anchor, setting._imageAnchor);
            }
        }

        /// <summary>
        /// 保存为图像2的配置
        /// </summary>
        /// <param name="setting"></param>
        public static void SaveImage2Setting(ImageGeneralSetting setting)
        {
            if (setting != null)
            {
                ScannerConfig.Write(ConfigPath.ImagesShowUnpenetratableRed, setting._showUnpenetrateRed);
                ScannerConfig.Write(ConfigPath.ImagesDefaultAbsorbIndex, setting._absorbtivity);

                ScannerConfig.Write(ConfigPath.ImagesImage2ColorMode, setting._colorMode);
                ScannerConfig.Write(ConfigPath.ImagesImage2Inversed, setting._inversed);
                ScannerConfig.Write(ConfigPath.ImagesImage2Penetration, setting._penetration);
                ScannerConfig.Write(ConfigPath.ImagesImage2EnableEdgeEnhance, setting._edgeEnhance);
                ScannerConfig.Write(ConfigPath.ImagesImage2EnableSuperEnhance, setting._superEnhance);
                ScannerConfig.Write(ConfigPath.ImagesImage2VerticalFlip, setting._verticalFlip);
                ScannerConfig.Write(ConfigPath.Image2MoveRightToLeft, setting._moveFromRightToLeft);
                ScannerConfig.Write(ConfigPath.ImagesImage2ShowingDetView, setting._showingViewIndex);
                ScannerConfig.Write(ConfigPath.ImagesImage2Anchor, setting._imageAnchor);
            }
        }


        public static ImageGeneralSetting LoadImage1Setting()
        {
            var setting = new ImageGeneralSetting();

            if (!ScannerConfig.Read(ConfigPath.ImagesShowUnpenetratableRed, out setting._showUnpenetrateRed))
            {
                setting._showUnpenetrateRed = false;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1ShowingDetView, out setting._showingViewIndex))
            {
                setting._showingViewIndex = DetectViewIndex.View1;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1ColorMode, out setting._colorMode))
            {
                setting._colorMode = DisplayColorMode.Grey;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1Inversed, out setting._inversed))
            {
                setting._inversed = false;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1Penetration, out setting._penetration))
            {
                setting._penetration = PenetrationMode.Standard;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesDefaultAbsorbIndex, out setting._absorbtivity))
            {
                setting._absorbtivity = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1EnableEdgeEnhance, out setting._edgeEnhance))
            {
                setting.EdgeEnhance = true;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1EnableSuperEnhance, out setting._superEnhance))
            {
                setting._superEnhance = true;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1VerticalFlip, out setting._verticalFlip))
            {
                setting._verticalFlip = false;
            }

            if (!ScannerConfig.Read(ConfigPath.Image1MoveRightToLeft, out setting._moveFromRightToLeft))
            {
                setting._moveFromRightToLeft = false;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage1Anchor, out setting._imageAnchor))
            {
                setting._imageAnchor = ImageAnchor.Center;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1VerticalScale, out setting._verticalScale))
            {
                setting._verticalScale = 1.0f;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage1HorizonalScale, out setting._horizonalScale))
            {
                setting._horizonalScale = 1.0f;
            }
            return setting;
        }

        public static ImageGeneralSetting LoadImage2Setting()
        {
            var setting = new ImageGeneralSetting();

            if (!ScannerConfig.Read(ConfigPath.ImagesShowUnpenetratableRed, out setting._showUnpenetrateRed))
            {
                setting._showUnpenetrateRed = false;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage2ShowingDetView, out setting._showingViewIndex))
            {
                setting._showingViewIndex = DetectViewIndex.View1;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage2ColorMode, out setting._colorMode))
            {
                setting._colorMode = DisplayColorMode.Grey;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage2Inversed, out setting._inversed))
            {
                setting._inversed = false;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage2Penetration, out setting._penetration))
            {
                setting._penetration = PenetrationMode.Standard;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesDefaultAbsorbIndex, out setting._absorbtivity))
            {
                setting._absorbtivity = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage2EnableEdgeEnhance, out setting._edgeEnhance))
            {
                setting.EdgeEnhance = true;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage2EnableSuperEnhance, out setting._superEnhance))
            {
                setting._superEnhance = true;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage2VerticalFlip, out setting._verticalFlip))
            {
                setting._verticalFlip = false;
            }

            if (!ScannerConfig.Read(ConfigPath.Image2MoveRightToLeft, out setting._moveFromRightToLeft))
            {
                setting._moveFromRightToLeft = false;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage2Anchor, out setting._imageAnchor))
            {
                setting._imageAnchor = ImageAnchor.Center;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage2VerticalScale, out setting._verticalScale))
            {
                setting._verticalScale = 1.0f;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage2HorizonalScale, out setting._horizonalScale))
            {
                setting._horizonalScale = 1.0f;
            }
            return setting;
        }


        public string GetEffectsString()
        {
            return FormatImageEffectsString(_inversed, _superEnhance, _colorMode, _penetration,
                _dynamicGrayscaleTransform, _absorbtivity,_penetrationEnhance, _edgeEnhance);
        }

        public static string FormatImageEffectsString(bool inversed, bool superEnhance, DisplayColorMode colorMode,
            PenetrationMode penetration,
            bool gst, int absorbtivity, bool edgeEnhance,bool penetrationEnhance, bool isMulti = false)
        {
            const string seperator = ", ";
            var builder = new StringBuilder(100);
            var recordStr = new StringBuilder(100);

            builder.Append(TranslationService.FindTranslation(colorMode));
            recordStr.Append(colorMode.ToString());

            if (penetration != PenetrationMode.Standard)
            {
                builder.Append(seperator).Append(TranslationService.FindTranslation(penetration));
                recordStr.Append(seperator).Append(penetration.ToString());
            }

            if (superEnhance)
            {
                builder.Append(seperator).Append(TranslationService.FindTranslation("SuperEnhance"));
                recordStr.Append(seperator).Append("SuperEnhance");
            }

            if (inversed)
            {
                builder.Append(seperator).Append(TranslationService.FindTranslation("Inversed"));
                recordStr.Append(seperator).Append("Inversed");
            }

            if (gst)
            {
                builder.Append(seperator).Append(TranslationService.FindTranslation("Dynamic GST"));
                recordStr.Append(seperator).Append("Dynamic GST");
            }

            if (absorbtivity != 0)
            {
                builder.Append(seperator).Append(TranslationService.FindTranslation("Absorbtivity")).Append(absorbtivity);
                recordStr.Append(seperator).Append("Absorbtivity");
                recordStr.Append(seperator).Append(absorbtivity.ToString());
            }
            if (edgeEnhance)
            {
                builder.Append(seperator).Append(TranslationService.FindTranslation("EdgeEnhance"));
                recordStr.Append(seperator).Append("EdgeEnhance");
            }
            if (penetrationEnhance)
            {
                builder.Append(seperator).Append(TranslationService.FindTranslation("PenetrationEnhance"));
                recordStr.Append(seperator).Append("PenetrationEnhance");
            }
            try
            {
                int sum = CalcImageEffects(inversed, superEnhance, colorMode, penetration, gst, absorbtivity, edgeEnhance, penetrationEnhance, isMulti);
                if (sum != lastSum)
                {
                    if (!isMulti && LoginAccountManager.Service.CurrentAccount != null)
                    {
                        new OperationRecordService().AddRecord(new OperationRecord()
                        {
                            AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                            OperateUI = OperationUI.MainUI,
                            OperateTime = DateTime.Now,
                            OperateObject = "Image",
                            OperateCommand = OperationCommand.Setting,
                            OperateContent = recordStr.ToString(),
                        });
                    }
                }
                lastSum = sum;
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            
            
            return builder.ToString();
        }

        static int absorb = 0;
        static int lastSum = 0;
        private static int CalcImageEffects(bool inversed, bool superEnhance, DisplayColorMode colorMode,PenetrationMode penetration,
            bool gst, int absorbtivity, bool edgeEnhance, bool penetrationEnhance, bool isMulti = false)
        {
            int sum = 0;
            if (inversed) sum += 0x01;
            if (superEnhance) sum += 0x02;
            switch(colorMode)
            {
                case DisplayColorMode.Grey:
                    sum += 0x04;
                    break;
                case DisplayColorMode.MaterialColor:
                    sum += 0x08;
                    break;
                case DisplayColorMode.MS:
                    sum += 0x10;
                    break;
                case DisplayColorMode.OS:
                    sum += 0x20;
                    break;
                case DisplayColorMode.Zeff7:
                    sum += 0x40;
                    break;
                case DisplayColorMode.Zeff8:
                    sum += 0x80;
                    break;
                case DisplayColorMode.Zeff9:
                    sum += 0x100;
                    break;
            }
            switch(penetration)
            {
                case PenetrationMode.HighPenetrate:
                    sum += 0x200;
                    break;
                case PenetrationMode.LowPenetrate:
                    sum += 0x400;
                    break;
                case PenetrationMode.SlicePenetrate:
                    sum += 0x800;
                    break;
                case PenetrationMode.Standard:
                    sum += 0x1000;
                    break;
                case PenetrationMode.SuperPenetrate:
                    sum += 0x2000;
                    break;
            }
            if (gst) sum += 0x4000;
            if (absorbtivity != absorb) sum += 0x8000;
            absorb = absorbtivity;
            if (edgeEnhance) sum += 0x10000;
            if (penetrationEnhance) sum += 0x20000;
            if (isMulti) sum += 0x40000;

            return sum;
        }
    }
}

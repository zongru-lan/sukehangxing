using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.ImagePlant;
using UI.XRay.ImagePlant.Cpu;

namespace UI.XRay.Flows.Controllers
{
    public class XRayImageProcessor
    {
        private ImageProcessor _processor;
        /// <summary>
        /// 默认吸收率，导出图像时与控件保持一致
        /// </summary>
        int defaultAbsorbIndex = 0;
        /// <summary>
        /// 加亮图像的吸收率
        /// </summary>
        int lightenAbsorbIndex = -15;
        public XRayImageProcessor()
        {
            if (!ScannerConfig.Read(ConfigPath.ImagesDefaultAbsorbIndex,out defaultAbsorbIndex))
            {
                defaultAbsorbIndex = 0;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesLightenAbsorbIndex, out lightenAbsorbIndex))
                lightenAbsorbIndex = -15;

            _processor = new ImageProcessor(DisplayColorMode.MaterialColor, defaultAbsorbIndex);
            _processor.EdgeEnhanceEnabled = false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AttachImageData(ImageViewData viewData)
        {
            var scanlines = viewData.ScanLines;
            if (scanlines.Length < 1) return;
            var data = new ushort[scanlines.Length][];
            var material = new ushort[scanlines.Length][];
            
            for (int i = 0; i < scanlines.Length; i++)
            {
                if (scanlines[i].XRayDataEnhanced != null)
                {
                    data[i] = scanlines[i].XRayDataEnhanced;
                }
                else
                {
                    data[i] = scanlines[i].XRayData;
                }
                
                material[i] = scanlines[i].Material;
            }            
            _processor.AttachImageData(data, material);
        }

        public void AttachImageData(List<DisplayScanlineData> bundles)
        {
            var data = new ushort[bundles.Count] [];
            var material = new ushort[bundles.Count][];

            for (int i = 0; i < bundles.Count; i++)
            {
                if (bundles[i].XRayDataEnhanced != null)
                {
                    data[i] = bundles[i].XRayDataEnhanced;
                }
                else
                {
                    data[i] = bundles[i].XRayData;
                }

                material[i] = bundles[i].Material;
            }
            _processor.AttachImageData(data, material);
        }

        /// <summary>
        /// 获取通用格式图像
        /// </summary>
        /// <param name="index">0：彩色图；1：超级穿透；2：加亮图像；3：:灰度图像</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Bitmap GetBitmap(ExportImageEffects effect = ExportImageEffects.Regular)
        {
            switch (effect)
            {
                case ExportImageEffects.Regular:
                    _processor.ColorMode = DisplayColorMode.MaterialColor;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                case ExportImageEffects.SuperEnhance:
                    _processor.ColorMode = DisplayColorMode.MaterialColor;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = true;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                case ExportImageEffects.Absorptivity:
                    _processor.ColorMode = DisplayColorMode.MaterialColor;
                    _processor.AbsorptivityIndex = lightenAbsorbIndex;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                case ExportImageEffects.Grey:
                    _processor.ColorMode = DisplayColorMode.Grey;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                case ExportImageEffects.OS:
                    _processor.ColorMode = DisplayColorMode.OS;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                case ExportImageEffects.MS:
                    _processor.ColorMode = DisplayColorMode.MS;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                case ExportImageEffects.Inverse:
                    _processor.ColorMode = DisplayColorMode.MaterialColor;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = true;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                case ExportImageEffects.SlicePenetrate:
                    _processor.ColorMode = DisplayColorMode.MaterialColor;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.SlicePenetrate;
                    break;
                case ExportImageEffects.SuperPenetrate:
                    _processor.ColorMode = DisplayColorMode.MaterialColor;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.SuperPenetrate;
                    break;
                case ExportImageEffects.HighPenetrate:
                    _processor.ColorMode = DisplayColorMode.MaterialColor;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.HighPenetrate;
                    break;
                case ExportImageEffects.LowPenetrate:
                    _processor.ColorMode = DisplayColorMode.MaterialColor;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.LowPenetrate;
                    break;
                case ExportImageEffects.Zeff7:
                    _processor.ColorMode = DisplayColorMode.Zeff7;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                case ExportImageEffects.Zeff8:
                    _processor.ColorMode = DisplayColorMode.Zeff8;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                case ExportImageEffects.Zeff9:
                    _processor.ColorMode = DisplayColorMode.Zeff9;
                    _processor.AbsorptivityIndex = 0;
                    _processor.SuperEnhanceEnabled = false;
                    _processor.Inverse = false;
                    _processor.Penetration = PenetrationMode.Standard;
                    break;
                default:
                    break;
            }
            return _processor.GetBitmap();
        }
    }

}

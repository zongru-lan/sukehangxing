using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.ImagePlant;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 功能键类型：功能键可作为一些动作键，比如打印等，也可以作为图像特效组合键
    /// </summary>
    public enum FunctionKeyType
    {
        /// <summary>
        /// 功能键未使用
        /// </summary>
        Empty,

        /// <summary>
        /// 功能键定义为其它动作。暂时未使用
        /// </summary>
        ActionKey,

        /// <summary>
        /// 图像特效组合键
        /// </summary>
        ImageEffectsKey
    }

    /// <summary>
    /// 功能键的动作键值定义
    /// </summary>
    public enum ActionKey
    {
        ConveyorStop,
        ConveyorRight,
        ConveyorLeft,
        Print,
        ImageProcessRecommend, //特殊算法
        ImagePenetration,
        StartShapeCorrection,
        StopShapeCorrection,
        ImageBrighter,
        ImageDarker,
    }

    /// <summary>
    /// 图像特效组合
    /// </summary>
    public class ImageEffectsComposition : PropertyNotifiableObject
    {
        public ImageEffectsComposition()
        {
            
        }

        public ImageEffectsComposition(DisplayColorMode? colorMode, PenetrationMode? penetration, bool? isInversed, bool? isSenOn)
        {
            ColorMode = colorMode;
            Penetration = penetration;
            IsInversed = isInversed;
            IsSenEnabled = isSenOn;
        }

        private DisplayColorMode? _colorMode;

        private bool? _isInversed;

        private bool? _isSenEnabled;

        private PenetrationMode? _penetration;

        public DisplayColorMode? ColorMode
        {
            get { return _colorMode; }
            set { _colorMode = value; RaisePropertyChanged();}
        }

        public bool? IsInversed
        {
            get { return _isInversed; }
            set { _isInversed = value; RaisePropertyChanged();}
        }

        public bool? IsSenEnabled
        {
            get { return _isSenEnabled; }
            set { _isSenEnabled = value; RaisePropertyChanged();}
        }

        public PenetrationMode? Penetration
        {
            get { return _penetration; }
            set { _penetration = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 转换为以下格式的字符串：ColorMode,Penetration,inversed,senEnabled
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var builder = new StringBuilder(100);
            if (ColorMode != null)
            {
                builder.Append(ColorMode);
            }
            builder.Append(",");

            if (Penetration != null)
            {
                builder.Append(Penetration);
            }
            builder.Append(",");

            if (IsInversed != null)
            {
                builder.Append(IsInversed);
            }
            builder.Append(",");

            if (IsSenEnabled != null)
            {
                builder.Append(IsSenEnabled);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 从"ColorMode,Penetration,inversed,senEnabled"格式的字符串中解析出图像特效组合
        /// </summary>
        /// <param name="formattedStr"></param>
        /// <returns></returns>
        public static bool TryParse(string formattedStr, out ImageEffectsComposition result)
        {
            if (string.IsNullOrWhiteSpace(formattedStr))
            {
                throw new ArgumentNullException("formattedStr");
            }

            var strList = formattedStr.Trim().Split(new char[] { ',' });
            if (strList.Length == 4)
            {
                DisplayColorMode? colorMode = null;
                if (!string.IsNullOrWhiteSpace(strList[0]))
                {
                    DisplayColorMode colorModeParsed;
                    if (!Enum.TryParse(strList[0], out colorModeParsed))
                    {
                        goto falseReturn;
                    }
                    colorMode = colorModeParsed;
                }

                PenetrationMode? penetration = null;
                if (!string.IsNullOrWhiteSpace(strList[1]))
                {
                    PenetrationMode penetrationParsed;
                    if (!Enum.TryParse(strList[1], out penetrationParsed))
                    {
                        goto falseReturn;
                    }
                    penetration = penetrationParsed;
                }

                bool? isInversed = null;
                if (!string.IsNullOrWhiteSpace(strList[2]))
                {
                    bool inversedParsed;
                    if (!bool.TryParse(strList[2], out inversedParsed))
                    {
                        goto falseReturn;
                    }
                    isInversed = inversedParsed;
                }

                bool? isSenEnabled = null;
                if (!string.IsNullOrWhiteSpace(strList[3]))
                {
                    bool isSenEnabledParsed;
                    if (!bool.TryParse(strList[3], out isSenEnabledParsed))
                    {
                        goto falseReturn;
                    }
                    isSenEnabled = isSenEnabledParsed;
                }

                result = new ImageEffectsComposition(colorMode, penetration, isInversed, isSenEnabled);
                return true;
            }

        falseReturn:
            result = null;
            return false;
        }
    }

    /// <summary>
    /// 功能键定义
    /// </summary>
    public class FunctionKey
    {
        /// <summary>
        /// 功能键类型，当前仅支持图像特效组合
        /// </summary>
        public FunctionKeyType Type { get; private set; }

        public ImageEffectsComposition ImageEffects { get; private set; }

        public ActionKey? Action { get; private set; }

        protected FunctionKey(ImageEffectsComposition effects)
        {
            Type = FunctionKeyType.ImageEffectsKey;
            ImageEffects = effects;
        }

        protected FunctionKey(ActionKey action)
        {
            Type = FunctionKeyType.ActionKey;
            Action = action;
        }

        protected FunctionKey()
        {
            Type = FunctionKeyType.Empty;
        }

        public static FunctionKey CreateActionKey(ActionKey action)
        {
            return new FunctionKey(action);
        }

        public static FunctionKey CreateImageEffectsKey(ImageEffectsComposition effects)
        {
            return new FunctionKey(effects);
        }

        public static FunctionKey CreateEmptyKey()
        {
            return new FunctionKey();
        }
    }

    class FunctionKeyService
    {
    }
}

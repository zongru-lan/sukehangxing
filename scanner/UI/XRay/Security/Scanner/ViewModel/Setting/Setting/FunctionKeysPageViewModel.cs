using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;
using UI.XRay.ImagePlant;
using UI.XRay.Flows.Controllers;
using System.Text;
using UI.XRay.Flows.Services.Db;
using GalaSoft.MvvmLight.Messaging;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Setting
{
    public class FunctionKeysPageViewModel : ViewModelBase
    {
        /// <summary>
        /// 当用户更改功能键的某一定义时触发此命令
        /// </summary>
        public RelayCommand FunctionKeySelectionChangedEventCommand { get; private set; }
        public RelayCommand SaveMarginSettingCommand { get; private set; }


        /// <summary>
        /// 颜色模式选择列表
        /// </summary>
        public List<NullableValueStringItem<DisplayColorMode?>> ColorModesList { get; set; }

        /// <summary>
        /// 穿透模式绑定列表
        /// </summary>
        public List<NullableValueStringItem<PenetrationMode?>> PenetrationList { get; set; }

        /// <summary>
        /// 穿透模式绑定列表
        /// </summary>
        public List<NullableValueStringItem<ActionKey?>> ActionList { get; set; }

        /// <summary>
        /// 反色、超增的绑定列表
        /// </summary>
        public List<NullableValueStringItem<bool?>> BoolList { get; set; }

        private ImageEffectsComposition _f1EffectsComposition;

        /// <summary>
        /// 功能键F2的图像特效组合
        /// </summary>
        private ImageEffectsComposition _f2EffectsComposition;

        /// <summary>
        /// 功能键F3的图像特效组合
        /// </summary>
        private ImageEffectsComposition _f3EffectsComposition;

        public ImageEffectsComposition F1EffectsComposition
        {
            get { return _f1EffectsComposition; }
            set { _f1EffectsComposition = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 功能键F2的图像特效组合
        /// </summary>
        public ImageEffectsComposition F2EffectsComposition
        {
            get { return _f2EffectsComposition; }
            set { _f2EffectsComposition = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 功能键F3的图像特效组合
        /// </summary>
        public ImageEffectsComposition F3EffectsComposition
        {
            get { return _f3EffectsComposition; }
            set { _f3EffectsComposition = value; RaisePropertyChanged(); }
        }

        private bool _isKeyActionEnable = true;

        public bool IsKeyActionEnable
        {
            get { return _isKeyActionEnable; }
            set { _isKeyActionEnable = value; RaisePropertyChanged(); }
        }

        private ActionKey? _f1KeyAction;
        private ActionKey? _f2KeyAction;
        private ActionKey? _f3KeyAction;
        /// <summary>
        /// 功能键F1的动作行为
        /// </summary>
        public ActionKey? F1KeyAction
        {
            get { return _f1KeyAction; }
            set { _f1KeyAction = value; RaisePropertyChanged(); }
        }
        /// <summary>
        /// 功能键F2的动作行为
        /// </summary>
        public ActionKey? F2KeyAction
        {
            get { return _f2KeyAction; }
            set { _f2KeyAction = value; RaisePropertyChanged(); }
        }
        /// <summary>
        /// 功能键F3的动作行为
        /// </summary>
        public ActionKey? F3KeyAction
        {
            get { return _f3KeyAction; }
            set { _f3KeyAction = value; RaisePropertyChanged(); }
        }


        public FunctionKeysPageViewModel()
        {
            //FunctionKeySelectionChangedEventCommand = new RelayCommand(FunctionKeySelectionChangedEventCommandExecute);
            SaveMarginSettingCommand = new RelayCommand(SaveMarginSettingCommandExecute);

            F1EffectsComposition = new ImageEffectsComposition();
            F2EffectsComposition = new ImageEffectsComposition();
            F3EffectsComposition = new ImageEffectsComposition();

            InitBindingList();

            try
            {
                LoadSettings();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private void SaveMarginSettingCommandExecute()
        {
            var builder = new StringBuilder(200);

            if (F1EffectsComposition != null)
            {
                //ScannerConfig.Write(ConfigPath.KeyboardF1Effects, F1EffectsComposition.ToString());
                builder.Append(F1EffectsComposition.ToString());
            }
            
            builder.Append(";");

            if (F2EffectsComposition != null)
            {
                //ScannerConfig.Write(ConfigPath.KeyboardF2Effects, F2EffectsComposition.ToString());
                builder.Append(F2EffectsComposition.ToString());

            }
           
            builder.Append(";");

            if (F3EffectsComposition != null)
            {
                //ScannerConfig.Write(ConfigPath.KeyboardF3Effects, F3EffectsComposition.ToString());
                builder.Append(F3EffectsComposition.ToString());

            }

            LoginAccountManager.Service.CurrentAccount.EffectsCompositions = builder.ToString();
           

            var builder1 = new StringBuilder(100);
            
            string actionStr = string.Empty;

            if (F1KeyAction != null)
            {
                actionStr = F1KeyAction.ToString();
            }
            builder1.Append(actionStr);
            builder1.Append(";");

            RecordOperation("F1", F1EffectsComposition.ToString() + "," + actionStr);

            actionStr = string.Empty;

            if (F2KeyAction != null)
            {
                actionStr = F2KeyAction.ToString();
            }
            builder1.Append(actionStr);
            builder1.Append(";");

            RecordOperation("F2", F2EffectsComposition.ToString() + "," + actionStr);

            actionStr = string.Empty;

            if (F3KeyAction != null)
            {
                actionStr = F3KeyAction.ToString();
            }
            builder1.Append(actionStr);
            RecordOperation("F3", F3EffectsComposition.ToString() + "," + actionStr);
            LoginAccountManager.Service.CurrentAccount.ActionTypes = builder1.ToString();

            var accountSet = new AccountDbSet();
            accountSet.AddOrUpdate(LoginAccountManager.Service.CurrentAccount);

            Messenger.Default.Send(new LoadKeyFunctionMessage());

          

        }

       

        private void InitBindingList()
        {
            ColorModesList = new List<NullableValueStringItem<DisplayColorMode?>>()
            {
                new NullableValueStringItem<DisplayColorMode?>(null, TranslationService.FindTranslation("OperationRecord", "null")),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.Grey, TranslationService.FindTranslation(DisplayColorMode.Grey)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.MaterialColor, TranslationService.FindTranslation(DisplayColorMode.MaterialColor)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.PseudoColor1, TranslationService.FindTranslation(DisplayColorMode.PseudoColor1)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.PseudoColor2, TranslationService.FindTranslation(DisplayColorMode.PseudoColor2)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.PseudoColor3, TranslationService.FindTranslation(DisplayColorMode.PseudoColor3)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.PseudoColor4, TranslationService.FindTranslation(DisplayColorMode.PseudoColor4)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.PseudoColor5, TranslationService.FindTranslation(DisplayColorMode.PseudoColor5)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.PseudoColor6, TranslationService.FindTranslation(DisplayColorMode.PseudoColor6)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.OS, TranslationService.FindTranslation(DisplayColorMode.OS)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.MS, TranslationService.FindTranslation(DisplayColorMode.MS)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.Zeff7, TranslationService.FindTranslation(DisplayColorMode.Zeff7)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.Zeff8, TranslationService.FindTranslation(DisplayColorMode.Zeff8)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.Zeff9, TranslationService.FindTranslation(DisplayColorMode.Zeff9)),
                new NullableValueStringItem<DisplayColorMode?>(DisplayColorMode.SixColors,TranslationService.FindTranslation(DisplayColorMode.SixColors)),
            };

            PenetrationList = new List<NullableValueStringItem<PenetrationMode?>>()
            {
                new NullableValueStringItem<PenetrationMode?>(null, TranslationService.FindTranslation("OperationRecord", "null")),
                new NullableValueStringItem<PenetrationMode?>(PenetrationMode.Standard, TranslationService.FindTranslation(PenetrationMode.Standard)),
                new NullableValueStringItem<PenetrationMode?>(PenetrationMode.SlicePenetrate, TranslationService.FindTranslation(PenetrationMode.SlicePenetrate)),
                new NullableValueStringItem<PenetrationMode?>(PenetrationMode.HighPenetrate, TranslationService.FindTranslation(PenetrationMode.HighPenetrate)),
                new NullableValueStringItem<PenetrationMode?>(PenetrationMode.LowPenetrate, TranslationService.FindTranslation(PenetrationMode.LowPenetrate)),
                new NullableValueStringItem<PenetrationMode?>(PenetrationMode.SuperPenetrate, TranslationService.FindTranslation(PenetrationMode.SuperPenetrate)),
            };

            BoolList = new List<NullableValueStringItem<bool?>>()
            {
                new NullableValueStringItem<bool?>(null, TranslationService.FindTranslation("OperationRecord", "null")),
                new NullableValueStringItem<bool?>(true, TranslationService.FindTranslation("Yes")),
                new NullableValueStringItem<bool?>(false, TranslationService.FindTranslation("No"))
            };

            ActionList = new List<NullableValueStringItem<ActionKey?>>()
            {
                new NullableValueStringItem<ActionKey?>(null, TranslationService.FindTranslation("OperationRecord", "null")),
                new NullableValueStringItem<ActionKey?>(ActionKey.ImagePenetration,TranslationService.FindTranslation("Raw Image")),
                new NullableValueStringItem<ActionKey?>(ActionKey.StartShapeCorrection,TranslationService.FindTranslation("TurnOn Image Correction")),
                new NullableValueStringItem<ActionKey?>(ActionKey.StopShapeCorrection,TranslationService.FindTranslation("TurnOff Image Correction")),
                new NullableValueStringItem<ActionKey?>(ActionKey.ImageBrighter,TranslationService.FindTranslation("Image Brighter")),
                new NullableValueStringItem<ActionKey?>(ActionKey.ImageDarker,TranslationService.FindTranslation("Image Darker")),
            };
        }

        //public void SaveSettings()
        //{
        //    if (F1EffectsComposition != null)
        //    {
        //        ScannerConfig.Write(ConfigPath.KeyboardF1Effects, F1EffectsComposition.ToString());
        //    }

        //    if (F2EffectsComposition != null)
        //    {
        //        ScannerConfig.Write(ConfigPath.KeyboardF2Effects, F2EffectsComposition.ToString());
        //    }

        //    if (F3EffectsComposition != null)
        //    {
        //        ScannerConfig.Write(ConfigPath.KeyboardF3Effects, F3EffectsComposition.ToString());
        //    }

        //    string actionStr = string.Empty;

        //    if (F1KeyAction != null)
        //    {
        //        actionStr = F1KeyAction.ToString();
        //    }
        //    ScannerConfig.Write(ConfigPath.KeyboardF1ActionType, actionStr);
        //    RecordOperation("F1", F1EffectsComposition.ToString() + "," + actionStr);

        //    actionStr = string.Empty;

        //    if (F2KeyAction != null)
        //    {
        //        actionStr = F2KeyAction.ToString();
        //    }

        //    ScannerConfig.Write(ConfigPath.KeyboardF2ActionType, actionStr);
        //    RecordOperation("F2", F2EffectsComposition.ToString() + "," + actionStr);

        //    actionStr = string.Empty;

        //    if (F3KeyAction != null)
        //    {
        //        actionStr = F3KeyAction.ToString();
        //    }
        //    ScannerConfig.Write(ConfigPath.KeyboardF3ActionType, actionStr);
        //    RecordOperation("F3", F3EffectsComposition.ToString() + "," + actionStr);
        //}
        
        public void LoadSettings()
        {
            string effectsCompositions = LoginAccountManager.Service.CurrentAccount.EffectsCompositions;
            string[] strings = effectsCompositions.Split(';');
            ImageEffectsComposition composition;
            F1EffectsComposition = ImageEffectsComposition.TryParse(strings[0], out composition) ? composition : new ImageEffectsComposition();
            F2EffectsComposition = ImageEffectsComposition.TryParse(strings[1], out composition) ? composition : new ImageEffectsComposition();
            F3EffectsComposition = ImageEffectsComposition.TryParse(strings[2], out composition) ? composition : new ImageEffectsComposition();

            string actionTypes = LoginAccountManager.Service.CurrentAccount.ActionTypes;
            string[] strings0 = actionTypes.Split(';');
            ActionKey keyAction;
            if (!Enum.TryParse(strings0[0], out keyAction))
            {
                F1KeyAction = null;
            }
            else
            {
                F1KeyAction = keyAction;
            }
            if (!Enum.TryParse(strings0[1], out keyAction))
            {
                F2KeyAction = null;
            }
            else
            {
                F2KeyAction = keyAction;
            }
            if (!Enum.TryParse(strings0[2], out keyAction))
            {
                F3KeyAction = null;
            }
            else
            {
                F3KeyAction = keyAction;
            }
        }

        //public void LoadSettings()
        //{
        //    bool isActionKeyEnable = false;
        //    if (!ScannerConfig.Read(ConfigPath.KeyboardIsFuctionKeyActionEnable, out isActionKeyEnable))
        //    {
        //        IsKeyActionEnable = false;
        //    }
        //    else
        //    {
        //        IsKeyActionEnable = isActionKeyEnable;
        //    }

        //    string keyEffects;
        //    if (!ScannerConfig.Read(ConfigPath.KeyboardF1Effects, out keyEffects))
        //    {
        //        keyEffects = ",,,";
        //    }

        //    ImageEffectsComposition composition;
        //    F1EffectsComposition = ImageEffectsComposition.TryParse(keyEffects, out composition) ? composition : new ImageEffectsComposition();

        //    if (!ScannerConfig.Read(ConfigPath.KeyboardF2Effects, out keyEffects))
        //    {
        //        keyEffects = ",,,";
        //    }
        //    F2EffectsComposition = ImageEffectsComposition.TryParse(keyEffects, out composition) ? composition : new ImageEffectsComposition();

        //    if (!ScannerConfig.Read(ConfigPath.KeyboardF3Effects, out keyEffects))
        //    {
        //        keyEffects = ",,,";
        //    }
        //    F3EffectsComposition = ImageEffectsComposition.TryParse(keyEffects, out composition) ? composition : new ImageEffectsComposition();

        //    if (isActionKeyEnable)
        //    {
        //        string keyActionStr;
        //        ActionKey keyAction;
        //        if (!ScannerConfig.Read(ConfigPath.KeyboardF1ActionType, out keyActionStr))
        //        {
        //            keyActionStr = String.Empty;
        //        }

        //        if (!Enum.TryParse(keyActionStr, out keyAction))
        //        {
        //            F1KeyAction = null;
        //        }
        //        else
        //        {
        //            F1KeyAction = keyAction;
        //        }

        //        if (!ScannerConfig.Read(ConfigPath.KeyboardF2ActionType, out keyActionStr))
        //        {
        //            keyActionStr = String.Empty;
        //        }

        //        if (!Enum.TryParse(keyActionStr, out keyAction))
        //        {
        //            F2KeyAction = null;
        //        }
        //        else
        //        {
        //            F2KeyAction = keyAction;
        //        }

        //        if (!ScannerConfig.Read(ConfigPath.KeyboardF3ActionType, out keyActionStr))
        //        {
        //            keyActionStr = String.Empty;
        //        }

        //        if (!Enum.TryParse(keyActionStr, out keyAction))
        //        {
        //            F3KeyAction = null;
        //        }
        //        else
        //        {
        //            F3KeyAction = keyAction;
        //        }
        //    }
        //}

        private void RecordOperation(string orobject, string content)
        {
            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.FunctionKeys,
                OperateTime = DateTime.Now,
                OperateObject = orobject,
                OperateCommand = OperationCommand.KeyPress,
                OperateContent = content,
            });
        }
    }
}

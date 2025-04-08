using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Xml.Serialization;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;
using UI.XRay.Security.Scanner.Converters;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Tip
{
    public class TipPlansPageViewModel : PageViewModelBase
    {
        /// <summary>
        /// 显示的计划列表
        /// </summary>
        private ObservableCollection<TipPlan> _plans;
        public ObservableCollection<TipPlan> Plans
        {
            get
            {
                AliasSuffix(_plans);
                return _plans;
            }
            set
            {
                AliasSuffix(value);
                _plans = value;
                RaisePropertyChanged();
            }
        }

        private void AliasSuffix(ObservableCollection<TipPlan> Plans)
        {
            if (Plans.Count > 1)
            {
                for (int i = Plans.Count - 1; i >= 0; i--)
                {
                    if (Plans[i].StartTime > Plans[i].EndTime)
                        Plans[i].EndTime = Plans[i].StartTime.AddDays(1);
                    for (int j = 0; j < i; j++)
                    {
                        if (Plans[i].Alias.Trim() == Plans[j].Alias.Trim())
                        {
                            string suffix = rnd.Next(100, 1000).ToString();
                            if (Plans[i].Alias.Trim().Length >= 6)
                                Plans[i].Alias = Plans[i].Alias.Trim().Substring(0, 6) + "_" + suffix;
                            else
                                Plans[i].Alias = Plans[i].Alias.Trim() + "_" + suffix;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 当前选中的Plan
        /// </summary>
        private TipPlan _selectedPlan;
        public TipPlan SelectedPlan
        {
            get { return _selectedPlan; }
            set
            {
                AliasSuffix(Plans);
                _selectedPlan = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 按钮提交更改、放弃更改、导出使能状态
        /// </summary>
        private bool _noChanged;
        public bool NoChanged
        {
            get { return _noChanged; }
            set { _noChanged = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 按钮添加计划使能状态
        /// </summary>
        private bool _isAddable;
        public bool IsAddable
        {
            get { return _isAddable; }
            set { _isAddable = value; RaisePropertyChanged(); }
        }

        private Timer timer;

        private Random rnd = new Random();

        private TipPlansController _controller;
        private TipPlanandImageController _imagecontroller;
        private ObservableCollection<TipPlan> _removeTipPlan;
        private List<TipPlanandImage> _removeTipPlanandImage;
        private TipImagesManagementController _managementControllerKnives;
        private TipImagesManagementController _managementControllerExplosives;
        private TipImagesManagementController _managementControllerGuns;
        private TipImagesManagementController _managementControllerOthers;


        // 弹窗标志，如果有权重但对应项没有选图片，则弹窗提示
        private bool _showWarning = false;
        // 选择TIP图像之前会更新SelectedPlan，这时如果发出弹窗信息会导致设置菜单无法关闭
        private bool _beforeSelectingImages = false;

        public RelayCommand DeleteCommand { get; private set; }
        public RelayCommand SelectCommand { get; private set; }
        public RelayCommand AddPlanCommand { get; set; }
        public RelayCommand SaveChangesCommand { get; set; }
        public RelayCommand DiscardChangesCommand { get; set; }
        public RelayCommand ExportCommand { get; set; }
        public RelayCommand ImportCommand { get; set; }

        public TipPlansPageViewModel()
        {
            DeleteCommand = new RelayCommand(DeleteCommandExecute);
            SelectCommand = new RelayCommand(SelectCommandExecute);
            AddPlanCommand = new RelayCommand(AddPlanCommandExecute);
            SaveChangesCommand = new RelayCommand(SaveChangesCommandExecute);
            DiscardChangesCommand = new RelayCommand(DiscardChangedCommandExecute);
            ExportCommand = new RelayCommand(ExportCommandExecute);
            ImportCommand = new RelayCommand(ImportCommandExecute);
            IsAddable = true;
            //timer = new Timer(new TimerCallback(Timer_Identify), null, 10, Timeout.Infinite);
            Messenger.Default.Register<PlanandImageToDbMessage>(this, PlanandImageToDbAction);

            UIScannerKeyboard1.SetWitchInputStates(true);

            if (IsInDesignMode)
            {
                Plans = new ObservableCollection<TipPlan>();
                Plans.Add(new TipPlan());
            }
            else
            {
                try
                {
                    _controller = new TipPlansController();
                    _imagecontroller = new TipPlanandImageController();
                    string _accountxmlFile = "D:\\SecurityScanner\\Database\\" + "plan.xml";
                    var fi = new FileInfo(_accountxmlFile);
                    if (fi.Exists){ 
                    UpdateRemoteTipLib();
                    }
                    Plans = _controller.GetAllPlans();
                    if (Plans.Count > 0)
                        NoChanged = true;
                    else
                        NoChanged = false;
                    _removeTipPlan = new ObservableCollection<TipPlan>();
                    _removeTipPlanandImage = new List<TipPlanandImage>();
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            }
        }


        private void UpdateSelectPlanToDb()
        {
            // TODO: 民航认证，避免远程和本地产生冲突
            if (!Transmission.IsRemoteTipProcessing)
            {
                Tracer.TraceInfo("change enable paln");
                for (int i = 0; i < Plans.Count; i++)
                {
                    Tracer.TraceInfo("plan " + i + ",alias:" + Plans[i].Alias + ",enable:" + Plans[i].IsEnabled.ToString());
                }
                if (SelectedPlan != null)
                {
                    Tracer.TraceInfo("select plan  alias:" + SelectedPlan.Alias + ",enable:" + SelectedPlan.IsEnabled.ToString());
                }
                foreach (var plan in Plans)
                {
                    AddOrUpdate(plan);
                }
            }
        }
        /// <summary>
        /// 添加新项，或者更新现有项
        /// </summary>
        /// <param name="plan"></param>
        private void AddOrUpdate(TipPlan plan)
        {
            if (_controller != null)
            {
                _controller.AddOrUpdate(plan);
            }
        }

        private void DeleteCommandExecute()
        {
            if (SelectedPlan != null)
            {
                var plansandImage = _imagecontroller.GetAllPlans();
                var planwithImageDelets = plansandImage.Where(p => p.TipPlanId == SelectedPlan.TipPlanId).ToList();

                foreach (var planwithImageDelet in planwithImageDelets)
                {
                    _removeTipPlanandImage.Add(planwithImageDelet);
                }

                var index = Plans.IndexOf(SelectedPlan);
                if (index >= 0)
                {
                    _removeTipPlan.Add(SelectedPlan);
                    Plans.Remove(SelectedPlan);

                    if (Plans.Count > 0)
                    {
                        if (index <= Plans.Count - 1)
                        {
                            SelectedPlan = Plans[index];
                        }
                        else if (index - 1 >= 0 && index - 1 <= Plans.Count - 1)
                        {
                            SelectedPlan = Plans[index - 1];
                        }
                        //else
                        //{
                        //    SelectedPlan = Plans[0];
                        //}
                    }
                }
            }
        }

        private void SelectCommandExecute()
        {
            // TODO: 民航认证，避免远程和本地产生冲突
            if (Transmission.IsRemoteTipProcessing)
            {
                return;
            }
            UpdateSelectPlanToDb();
            if (SelectedPlan.TipPlanId == 0)
            {
                AddOrUpdate(SelectedPlan);
            }
            _beforeSelectingImages = true;
            UpdateSelectTipLib(SelectedPlan);
            var msg = new ShowTipImageSelectWindowAction("SettingWindow", conditions =>
            {
                // TODO: 目前完全没有意义，考虑在后续版本重做或删除
                try
                {
                    if (conditions != null)
                    {
                        var plan = conditions;
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            });
            _beforeSelectingImages = false;

            Messenger.Default.Send(msg);
        }

        private void UpdateSelectTipLib(TipPlan plan)
        {
            var plansandImage = _imagecontroller.GetAllPlans();
            List<string> TipImageName = new List<string>();

            var _managementController = new TipImagesManagementController(TipLibrary.Explosives);
            _managementController.DeleteSelectAllImages();
            var planwithImage = plansandImage.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Explosives").ToList();
            if (plan.ExplosivesWeight != 0 && planwithImage.Count() == 0)
            {
                _showWarning = true;
            }
            foreach (var planwithimage in planwithImage)
            {
                TipImageName.Add(planwithimage.ImageName);
            }
            _managementController.ChangeSelectTipLib(TipLibrary.Explosives, TipImageName);
            planwithImage.Clear();
            TipImageName.Clear();

            _managementController = new TipImagesManagementController(TipLibrary.Guns);
            _managementController.DeleteSelectAllImages();
            planwithImage = plansandImage.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Guns").ToList();
            if (plan.GunsWeight != 0 && planwithImage.Count() == 0)
            {
                _showWarning = true;
            }
            foreach (var planwithimage in planwithImage)
            {
                TipImageName.Add(planwithimage.ImageName);
            }
            _managementController.ChangeSelectTipLib(TipLibrary.Guns, TipImageName);
            planwithImage.Clear();
            TipImageName.Clear();

            _managementController = new TipImagesManagementController(TipLibrary.Knives);
            _managementController.DeleteSelectAllImages();
            planwithImage = plansandImage.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Knives").ToList();
            if (plan.KnivesWeight != 0 && planwithImage.Count() == 0)
            {
                _showWarning = true;
            }
            foreach (var planwithimage in planwithImage)
            {
                TipImageName.Add(planwithimage.ImageName);
            }
            _managementController.ChangeSelectTipLib(TipLibrary.Knives, TipImageName);
            planwithImage.Clear();
            TipImageName.Clear();

            _managementController = new TipImagesManagementController(TipLibrary.Others);
            _managementController.DeleteSelectAllImages();
            planwithImage = plansandImage.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Others").ToList();
            if (plan.OtherObjectsWeight != 0 && planwithImage.Count() == 0)
            {
                _showWarning = true;
            }
            foreach (var planwithimage in planwithImage)
            {
                TipImageName.Add(planwithimage.ImageName);
            }
            _managementController.ChangeSelectTipLib(TipLibrary.Others, TipImageName);
            planwithImage.Clear();
            TipImageName.Clear();

            if (_showWarning && !_beforeSelectingImages)
            {
                var message = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Warning"),
                    TranslationService.FindTranslation("Didn't select images but weight doesn't equals 0, this may reduce injection rate!"), MetroDialogButtons.Ok, result => { });
                // TODO: 目前弹窗出现了很多问题，为了不影响认证暂时取消，认证后考虑是否保留
                //this.MessengerInstance.Send(message);

                _showWarning = false;
            }
        }

        private void UpdateRemoteTipLib()
        {
            Tracer.TraceInfo("进入远程tip更新");
            //解析接收的TipPlan计划并添加到数据库
            List<TipPlan> allplans = new List<TipPlan>(_controller.GetAllPlans());
            List<TipPlan> tipplanList = updateTipPlan();
            if (allplans.Count > 0)
            {
                foreach (var item in allplans) {
                    _controller.Remove(item);
                    Tracer.TraceInfo("删除本地tip");
                }
            }
            foreach (var item in tipplanList)
            {
                _controller.AddOrUpdate(item);
                Tracer.TraceInfo("添加远程tip");
            }
           
            //解析接收的TipPlanImage并添加到数据库
            List<TipPlanandImage> tipplanandimageList = updateTipPlanandimage();
            foreach (var item1 in tipplanandimageList)
                _imagecontroller.AddOrUpdate(item1);            
            Transmission.RemoteTipUpdated = false;
        }
        //从xml文件中解析接收的TipPlan计划
        private List<TipPlan> updateTipPlan()
        {
            List<TipPlan> tipplanList = new List<TipPlan>();
            try
            {
                string _accountxmlFile = "D:\\SecurityScanner\\Database\\" + "plan.xml";
                var fileInfo= new FileInfo(_accountxmlFile);
                tipplanList = ReadWriteXml.ReadXmlFromFile<List<TipPlan>>(_accountxmlFile, System.Text.Encoding.UTF8);
                Tracer.TraceInfo("MainViewModel update tip plans count " + tipplanList.Count);
                fileInfo.Delete();
                return tipplanList;
            }
            catch (Exception e)
            {
                return tipplanList;
            }
        }

        //从xml文件中解析接收的TipPlanImage
        private List<TipPlanandImage> updateTipPlanandimage()
        {
            List<TipPlanandImage> tipplanandimageList = new List<TipPlanandImage>();
            try
            {
                string _accountxmlFile = "D:\\SecurityScanner\\Database\\" + "planandimage.xml";
                tipplanandimageList = ReadWriteXml.ReadXmlFromFile<List<TipPlanandImage>>(_accountxmlFile, System.Text.Encoding.UTF8);
                Tracer.TraceInfo("MainViewModel update tip plan images count " + tipplanandimageList.Count);
                return tipplanandimageList;
            }
            catch (Exception e)
            {
                return tipplanandimageList;
            }
        }

        private void AddPlanCommandExecute()
        {
            var newPlan = new TipPlan();
            if (newPlan != null)
            {
                Plans.Add(newPlan);
                NoChanged = true;
                IsAddable = false;
            }
        }
        private void SaveChangesCommandExecute()
        {
            var Planss = _controller.GetAllPlans();//获取当前数据库中存在的计划 
            
            
            // TODO: 民航认证，避免远程和本地产生冲突
            if (Transmission.IsRemoteTipProcessing)
            {
                return;
            }
            IsAddable = true;
            var plansandImage = _imagecontroller.GetAllPlans();
            if (_removeTipPlanandImage.Count > 0)
            {
                _imagecontroller.RemoveRange(_removeTipPlanandImage);
            }
            if (_removeTipPlan.Count > 0)
            {
                foreach (var item in _removeTipPlan)   //如果要删除的计划在数据库中存在那么才执行删除操作  yxc
                {
                    foreach (var a in Planss)
                    {
                        if (item == a)
                        {
                            _controller.Remove(item);
                        }
                    }
                }
                //foreach (var item in _removeTipPlan)
                //{
                //    _controller.Remove(item);
                //}
            }
            _removeTipPlan = new ObservableCollection<TipPlan>();
            _removeTipPlanandImage = new List<TipPlanandImage>();
            foreach (var plan in Plans)
            {
                if (plan.IsEnabled)
                    UpdateSelectTipLib(plan);
                AddOrUpdate(plan);
                var sb = new StringBuilder();
                sb.Append("Alias:").Append(plan.Alias).Append(",");
                sb.Append("IsEnabled:").Append(plan.IsEnabled).Append(",");
                sb.Append("Probability:").Append(plan.Probability).Append(",");
                sb.Append("CreationTime:").Append(DateFormatHelper.DateTime2String(plan.CreationTime)).Append(",");
                sb.Append("StartTime:").Append(DateFormatHelper.DateTime2String(plan.StartTime)).Append(",");
                sb.Append("EndTime:").Append(DateFormatHelper.DateTime2String(plan.EndTime)).Append(",");

                //sb.Append("OnlineRecMaxSeconds:").Append(plan.OnlineRecMaxSeconds).Append(",");
                sb.Append("OfflineRecMaxSeconds:").Append(plan.OfflineRecMaxSeconds).Append(",");

                sb.Append("KnivesWeight:").Append(plan.KnivesWeight).Append(",");
                sb.Append("GunsWeight:").Append(plan.GunsWeight).Append(",");
                sb.Append("ExplosivesWeight:").Append(plan.ExplosivesWeight).Append(",");
                sb.Append("OtherObjectsWeight:").Append(plan.OtherObjectsWeight);

                new OperationRecordService().AddRecord(new OperationRecord()
                {
                    AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                    OperateUI = OperationUI.TipPlans,
                    OperateTime = DateTime.Now,
                    OperateObject = "TipPlans",
                    OperateCommand = OperationCommand.Setting,
                    OperateContent = sb.ToString(),
                });
            }
            if (Plans.Count > 0)
                NoChanged = true;
            else
                NoChanged = false;
        }
        private void DiscardChangedCommandExecute()
        {
            IsAddable = true;
            _controller = new TipPlansController();
            Plans = _controller.GetAllPlans();
            _removeTipPlan = new ObservableCollection<TipPlan>();
            _removeTipPlanandImage = new List<TipPlanandImage>();
            if (Plans.Count > 0)
                NoChanged = true;
            else
                NoChanged = false;
        }
        private void ExportCommandExecute()
        {
            var msg = new ShowFolderBrowserDialogMessageAction("SettingWindow", s =>
            {
                if (s != null)
                {
                    string dstfile = ConfigHelper.GetExportFileName(s, "TipPlans");
                    var plansandImage = _imagecontroller.GetAllPlans();
                    using (StringWriter sw = new StringWriter())
                    {
                        //序列化
                        XmlSerializer serializer = new XmlSerializer(typeof(List<TipImage>));
                        List<TipImage> TipImageList = new List<TipImage>();
                        foreach (var plan in Plans)
                        {
                            TipImage tipImage = new TipImage(plan.Alias, plan.CreationTime, plan.TipPlanId, plan.Probability, plan.IsEnabled, plan.StartTime, plan.EndTime,
                                plan.OfflineRecMaxSeconds, plan.KnivesWeight, plan.GunsWeight, plan.ExplosivesWeight, plan.OtherObjectsWeight);
                            var planwithExplosives = plansandImage.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Explosives").ToList();
                            foreach (var image in planwithExplosives)
                                tipImage.TipImageList.Explosives.Add(image.ImageName);
                            var planwithGuns = plansandImage.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Guns").ToList();
                            foreach (var image in planwithGuns)
                                tipImage.TipImageList.Guns.Add(image.ImageName);
                            var planwithKnives = plansandImage.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Knives").ToList();
                            foreach (var image in planwithKnives)
                                tipImage.TipImageList.Knives.Add(image.ImageName);
                            var planwithOthers = plansandImage.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Others").ToList();
                            foreach (var image in planwithOthers)
                                tipImage.TipImageList.Others.Add(image.ImageName);
                            TipImageList.Add(tipImage);
                        }
                        serializer.Serialize(sw, TipImageList);
                        using (FileStream fsWrite = new FileStream(dstfile, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            byte[] buffer = Encoding.Default.GetBytes(sw.ToString());
                            fsWrite.Write(buffer, 0, buffer.Length);
                        }
                    }
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                            "", TranslationService.FindTranslation("Dump completely"), MetroDialogButtons.Ok,
                            result =>
                            {

                            }));
                    });

                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                        OperateUI = OperationUI.TipPlans,
                        OperateTime = DateTime.Now,
                        OperateObject = "TipPlans",
                        OperateCommand = OperationCommand.Export,
                        OperateContent = ConfigHelper.AddQuotationForPath(dstfile),
                    });
                }
            });
            MessengerInstance.Send(msg);
        }
        private void ImportCommandExecute()
        {
            var msg = new ShowOpenFilesDialogMessageAction("SettingWindow", "Xml file | *.xml", false, s =>
            {
                List<TipImage> _list = new List<TipImage>();
                if (s != null && s.Length > 0)
                {
                    using (FileStream fsRead = new FileStream(s[0], FileMode.Open))
                    {
                        try
                        {
                            int fsLen = (int)fsRead.Length;
                            byte[] buffer = new byte[fsLen];
                            int result = fsRead.Read(buffer, 0, buffer.Length);
                            string xml = System.Text.Encoding.UTF8.GetString(buffer);
                            //反序列化
                            using (StringReader sr = new StringReader(xml))
                            {
                                XmlSerializer serializer = new XmlSerializer(typeof(List<TipImage>));
                                _list = serializer.Deserialize(sr) as List<TipImage>;
                            }
                        }
                        catch (Exception)
                        {
                            return;
                        }
                        foreach (var plan in _list)
                        {
                            NoChanged = true;
                            // 新添加的TIp计划，还没有入数据库
                            if (plan.TipPlanId == 0)
                            {
                                TipPlan temp = new TipPlan(plan.Alias, plan.CreationTime, plan.TipPlanId, plan.Probability, plan.IsEnabled, plan.StartTime, plan.EndTime,
                                plan.OfflineRecMaxSeconds, plan.KnivesWeight, plan.GunsWeight, plan.ExplosivesWeight, plan.OtherObjectsWeight);
                                AddOrUpdate(temp);
                                Plans = _controller.GetAllPlans();
                                foreach (var explosive in plan.TipImageList.Explosives)
                                    _imagecontroller.AddOrUpdate(new TipPlanandImage()
                                    {
                                        TipPlanId = temp.TipPlanId,
                                        Library = "Explosives",
                                        ImageName = explosive
                                    });
                                foreach (var gun in plan.TipImageList.Guns)
                                    _imagecontroller.AddOrUpdate(new TipPlanandImage()
                                    {
                                        TipPlanId = temp.TipPlanId,
                                        Library = "Guns",
                                        ImageName = gun
                                    });
                                foreach (var knife in plan.TipImageList.Knives)
                                    _imagecontroller.AddOrUpdate(new TipPlanandImage()
                                    {
                                        TipPlanId = temp.TipPlanId,
                                        Library = "Knives",
                                        ImageName = knife
                                    });
                                foreach (var other in plan.TipImageList.Others)
                                    _imagecontroller.AddOrUpdate(new TipPlanandImage()
                                    {
                                        TipPlanId = temp.TipPlanId,
                                        Library = "Others",
                                        ImageName = other
                                    });

                            }
                            else
                            {
                                TipPlan temp = new TipPlan(plan.Alias, plan.CreationTime, plan.TipPlanId, plan.Probability, plan.IsEnabled, plan.StartTime, plan.EndTime,
                                plan.OfflineRecMaxSeconds, plan.KnivesWeight, plan.GunsWeight, plan.ExplosivesWeight, plan.OtherObjectsWeight);
                                if (Plans.Contains(temp))
                                {

                                }
                                else
                                {
                                    AddOrUpdate(temp);
                                    Plans = _controller.GetAllPlans();
                                    foreach (var explosive in plan.TipImageList.Explosives)
                                        _imagecontroller.AddOrUpdate(new TipPlanandImage()
                                        {
                                            TipPlanId = temp.TipPlanId,
                                            Library = "Explosives",
                                            ImageName = explosive
                                        });
                                    foreach (var gun in plan.TipImageList.Guns)
                                        _imagecontroller.AddOrUpdate(new TipPlanandImage()
                                        {
                                            TipPlanId = temp.TipPlanId,
                                            Library = "Guns",
                                            ImageName = gun
                                        });
                                    foreach (var knife in plan.TipImageList.Knives)
                                        _imagecontroller.AddOrUpdate(new TipPlanandImage()
                                        {
                                            TipPlanId = temp.TipPlanId,
                                            Library = "Knives",
                                            ImageName = knife
                                        });
                                    foreach (var other in plan.TipImageList.Others)
                                        _imagecontroller.AddOrUpdate(new TipPlanandImage()
                                        {
                                            TipPlanId = temp.TipPlanId,
                                            Library = "Others",
                                            ImageName = other
                                        });
                                }

                            }
                            //Plans.Insert(0, new TipPlan();
                        }
                        ////去除id重复
                        //for (int i = 0; i < Plans.Count; i++)
                        //{
                        //    for (int j = Plans.Count - 1; j > i; j--)
                        //    {
                        //        if (Plans[i].TipPlanId == Plans[j].TipPlanId)
                        //        {
                        //            Plans.RemoveAt(j);
                        //        }
                        //    }
                        //}
                    }
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                            "", TranslationService.FindTranslation("Import completely"), MetroDialogButtons.Ok,
                            result =>
                            {

                            }));
                    });

                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                        OperateUI = OperationUI.TipPlans,
                        OperateTime = DateTime.Now,
                        OperateObject = "TipPlans",
                        OperateCommand = OperationCommand.Import,
                        OperateContent = ConfigHelper.AddQuotationForPath(s[0]),
                    });
                }
            });
            MessengerInstance.Send(msg);
        }

        /// <summary>
        /// 上传Tip计划至服务器的命令执行
        /// </summary>
        //private void UploadCommandExecute()
        //{
        //    string _fileName = "TipPlans_" +  DateTime.Now.ToString("yyyyMMdd") + ".xml";
        //    string _xmlFile = System.Environment.CurrentDirectory + "\\Recv\\" + _fileName;
        //    try
        //    {
        //        using (StringWriter sw = new StringWriter())
        //        {
        //            XmlSerializer serializer = new XmlSerializer(typeof(List<TipPlan>));
        //            serializer.Serialize(sw, Plans.ToList());

        //            using (FileStream fsWrite = new FileStream(_xmlFile, FileMode.OpenOrCreate, FileAccess.Write))
        //            {
        //                byte[] buffer = Encoding.Default.GetBytes(sw.ToString());
        //                fsWrite.Write(buffer, 0, buffer.Length);
        //            }
        //        }
        //        if (HttpNetworkController.Controller.IsConnected)
        //        {
        //            HttpNetworkController.Controller.SendFileTo(_fileName, _xmlFile, "text/plain");
        //            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        //                            {
        //                                MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
        //                                    "", TranslationService.FindTranslation("Dump to network completely"), MetroDialogButtons.Ok,
        //                                    result =>
        //                                    {

        //                                    }));
        //                            });
        //        }
        //        else
        //        {
        //            Application.Current.Dispatcher.InvokeAsync(() =>
        //            {
        //                MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
        //                    "", TranslationService.FindTranslation("Send Failed"), MetroDialogButtons.Ok,
        //                    result =>
        //                    {

        //                    }));
        //            });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Tracer.TraceException(e);
        //        Application.Current.Dispatcher.InvokeAsync(() =>
        //        {
        //            MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
        //                "", TranslationService.FindTranslation("Send Failed"), MetroDialogButtons.Ok,
        //                result =>
        //                {

        //                }));
        //        });
        //    }

        //}

        public void PlanandImageToDbAction(PlanandImageToDbMessage obj)
        {
            try
            {
                // TODO: 民航认证，避免远程和本地产生冲突
                if (Transmission.IsRemoteTipProcessing)
                {
                    return;
                }
                if (SelectedPlan != null)
                {
                    var plansandImage = _imagecontroller.GetAllPlans();
                    var planwithKnivesImageDelets = plansandImage.Where(p => p.TipPlanId == SelectedPlan.TipPlanId && p.Library == "Knives").ToList();
                    if (planwithKnivesImageDelets.Count > 0)
                        _imagecontroller.RemoveRange(planwithKnivesImageDelets);
                    _managementControllerKnives = new TipImagesManagementController(TipLibrary.Knives);
                    var TotalImagesCountKnives = _managementControllerKnives.SelectImagesCount;
                    if (TotalImagesCountKnives <= 0)
                    {
                        // TODO: 原本没有图像时会将对应权重置为0，暂时弃用
                        //SelectedPlan.KnivesWeight = 0;
                    }
                    else
                    {
                        try
                        {
                            var ImageName = _managementControllerKnives.TipLibImageName();
                            foreach (var imageName in ImageName)
                            {
                                _imagecontroller.AddOrUpdate(new TipPlanandImage()
                                {
                                    TipPlanId = SelectedPlan.TipPlanId,
                                    Library = "Knives",
                                    ImageName = imageName
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Tracer.TraceException(ex);
                        }
                    }
                    _managementControllerExplosives = new TipImagesManagementController(TipLibrary.Explosives);
                    var planwithExplosivesImageDelets = plansandImage.Where(p => p.TipPlanId == SelectedPlan.TipPlanId && p.Library == "Explosives").ToList();
                    if (planwithExplosivesImageDelets.Count > 0)
                        _imagecontroller.RemoveRange(planwithExplosivesImageDelets);
                    var TotalImagesCountExplosives = _managementControllerExplosives.SelectImagesCount;
                    if (TotalImagesCountExplosives <= 0)
                    {
                        //SelectedPlan.ExplosivesWeight = 0;
                    }
                    else
                    {
                        var ImageName = _managementControllerExplosives.TipLibImageName();
                        foreach (var imageName in ImageName)
                        {
                            _imagecontroller.AddOrUpdate(new TipPlanandImage()
                            {
                                TipPlanId = SelectedPlan.TipPlanId,
                                Library = "Explosives",
                                ImageName = imageName
                            });
                        }
                    }
                    _managementControllerGuns = new TipImagesManagementController(TipLibrary.Guns);
                    var planwithGunsImageDelets = plansandImage.Where(p => p.TipPlanId == SelectedPlan.TipPlanId && p.Library == "Guns").ToList();
                    if (planwithGunsImageDelets.Count > 0)
                        _imagecontroller.RemoveRange(planwithGunsImageDelets);
                    var TotalImagesCountGuns = _managementControllerGuns.SelectImagesCount;
                    if (TotalImagesCountGuns <= 0)
                    {
                        //SelectedPlan.GunsWeight = 0;
                    }
                    else
                    {
                        var ImageName = _managementControllerGuns.TipLibImageName();
                        foreach (var imageName in ImageName)
                        {
                            _imagecontroller.AddOrUpdate(new TipPlanandImage()
                            {
                                TipPlanId = SelectedPlan.TipPlanId,
                                Library = "Guns",
                                ImageName = imageName
                            });
                        }
                    }
                    _managementControllerOthers = new TipImagesManagementController(TipLibrary.Others);
                    var planwithOthersImageDelets = plansandImage.Where(p => p.TipPlanId == SelectedPlan.TipPlanId && p.Library == "Others").ToList();
                    if (planwithOthersImageDelets.Count > 0)
                        _imagecontroller.RemoveRange(planwithOthersImageDelets);
                    var TotalImagesCountOthers = _managementControllerOthers.SelectImagesCount;
                    if (TotalImagesCountOthers <= 0)
                    {
                        //SelectedPlan.OtherObjectsWeight = 0;
                    }
                    else
                    {
                        var ImageName = _managementControllerOthers.TipLibImageName();
                        foreach (var imageName in ImageName)
                        {
                            _imagecontroller.AddOrUpdate(new TipPlanandImage()
                            {
                                TipPlanId = SelectedPlan.TipPlanId,
                                Library = "Others",
                                ImageName = imageName
                            });
                        }
                    }
                }
                foreach (var plan in Plans)
                {
                    if (plan.IsEnabled)
                        UpdateSelectTipLib(plan);
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceException(ex);
            }
        }
        bool _isNumLocked = true;
        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.O:
                    if (_isNumLocked)
                    {
                        SendKeyEvents.Press(Key.Back);
                        System.Threading.Thread.Sleep(100);
                        SendKeyEvents.Release(Key.Back);
                    }
                    break;
                case Key.I:
                    if (_isNumLocked)
                    {
                        SendKeyEvents.Press(Key.Tab);
                        System.Threading.Thread.Sleep(100);
                        SendKeyEvents.Release(Key.Tab);
                    }
                    break;
                case Key.System:
                    _isNumLocked = true;
                    break;
                case Key.F9:
                    _isNumLocked = false;
                    break;
            }
            bool isNumber = args.Key >= Key.D0 && args.Key <= Key.D9 || args.Key >= Key.NumPad0 && args.Key <= Key.NumPad9;
            bool isLetter = args.Key >= Key.A && args.Key <= Key.Z && args.KeyboardDevice.Modifiers != ModifierKeys.Shift;

            if (ScannerKeyboardPart.Keyboard.IsUSBCommonKeyboard)
            {
                if (_isNumLocked)
                {
                    if (isLetter)
                    {
                        args.Handled = true;
                        return;
                    }
                }

                if (isNumber && !_isNumLocked)
                {
                    ScannerKeyboardPart.Keyboard.AddKey((byte)args.Key);
                    args.Handled = true;
                    return;
                }
            }
        }

        public override void Cleanup()
        {
            UpdateSelectPlanToDb();
            UIScannerKeyboard1.SetWitchInputStates(false);
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            base.Cleanup();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 表示一次Tip图像插入选择，包括图像、库类型及文件名
    /// </summary>
    public class TipImageSelection
    {
        public XRayScanlinesImage TipImage { get; private set; }

        public TipLibrary Library { get; private set;}

        public string FileName { get; private set; }

        public TipImageSelection(XRayScanlinesImage image, TipLibrary library, string fileName)
        {
            TipImage = image;
            Library = library;
            FileName = fileName;
        }
    }

    /// <summary>
    /// 根据当前的策略类型、Tip图库情况，随机选择一个用于注入的Tip图像
    /// </summary>
    public class TipImageRandomSelector
    {
        /// <summary>
        /// 随机数生成器，用于随机选取一个tip图像
        /// </summary>
        private Random _random = new Random();

        /// <summary>
        /// 当前正在应用的Tip计划
        /// </summary>
        public TipPlan CurrentPlan { get; private set; }

        /// <summary>
        /// 刀具库的所有图像文件的信息
        /// </summary>
        private List<FileInfo> _knivesFileInfos;
        /// <summary>
        /// 枪支库的所有图像文件的信息
        /// </summary>
        private List<FileInfo> _gunsFileInfos;
        /// <summary>
        /// 爆炸物库的所有图像文件的信息
        /// </summary>
        private List<FileInfo> _explosivesFileInfos;
        /// <summary>
        /// 其他物品库的所有图像文件的信息
        /// </summary>
        private List<FileInfo> _othersFileInfos;

        /// <summary>
        /// 定时器，每隔1分钟检测一次TipPlan数据库，获取最新
        /// </summary>
        private Timer _planUpdateTimer;

        /// <summary>
        /// 监测枪支库的文件变化
        /// </summary>
        private FileSystemWatcher _gunsLibWatcher;
        private FileSystemWatcher _knivesLibWatcher;
        private FileSystemWatcher _explosivesLibWatcher;
        private FileSystemWatcher _othersLibWatcher;

        public TipImageRandomSelector()
        {
            try
            {
                _planUpdateTimer = new Timer(PlanUpdateTimerCallback, null, 0, 1000);                
                Task.Run(() =>
                    {
                        LoadTipFileInfos();
                        CreateFileWatchers();
                        var plan = GetMostSuitablePlan();                       
                        lock (this)
                        {
                            CurrentPlan = plan;
                        }
                    });
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 线程定时器响应：定时从数据库中获取最新的TipPlan
        /// </summary>
        /// <param name="state"></param>
        private void PlanUpdateTimerCallback(object state)
        {
            try
            {
                var plan = GetMostSuitablePlan();
                CurrentPlan = plan;
                //lock (this)
                //{
                //    CurrentPlan = plan;
                //}
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
            _planUpdateTimer.Change(2000, Timeout.Infinite);
        }

        /// <summary>
        /// 根据当前的登录用户,获取最合适的Tip考核计划
        /// </summary>
        /// <returns>当前最合适的tip计划,或者为空</returns>
        private TipPlan GetMostSuitablePlan()
        {
            try
            {
                var tipPlanManager = new TipPlanDbSet();
                var plans = tipPlanManager.SelectAll();
                if (plans.Count > 0)
                {
                    if (LoginAccountManager.Service.HasLogin && LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Operator)
                    {
                        return plans.FirstOrDefault(plan =>
                            plan.StartTime.Date <= DateTime.Now.Date &&
                            plan.EndTime >= DateTime.Now.Date &&
                            plan.IsEnabled);
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return null;
        }

        /// <summary>
        /// 加载已选择的所有的Tip库文件的信息
        /// </summary>
        private void LoadTipFileInfos()
        {
            List<FileInfo> knives = null;
            List<FileInfo> explosives = null;
            List<FileInfo> guns = null;
            List<FileInfo> others = null;

            try
            {
                //knives = TipImagesManagementController.GetTipImageFileInfos(TipLibrary.Knives);
                //explosives = TipImagesManagementController.GetTipImageFileInfos(TipLibrary.Explosives);
                //guns = TipImagesManagementController.GetTipImageFileInfos(TipLibrary.Guns);
                //others = TipImagesManagementController.GetTipImageFileInfos(TipLibrary.Others);
                
                knives = TipImagesManagementController.GetSelectTipImageFileInfos(TipLibrary.Knives);
                explosives = TipImagesManagementController.GetSelectTipImageFileInfos(TipLibrary.Explosives);
                guns = TipImagesManagementController.GetSelectTipImageFileInfos(TipLibrary.Guns);
                others = TipImagesManagementController.GetSelectTipImageFileInfos(TipLibrary.Others);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }

            // 更新各个库文件的信息
            lock (this)
            {
                _knivesFileInfos = knives;
                _explosivesFileInfos = explosives;
                _gunsFileInfos = guns;
                _othersFileInfos = others;
            }
        }

        /// <summary>
        /// 创建各个子库的文件监视器，在增删库文件时及时更新文件索引
        /// </summary>
        private void CreateFileWatchers()
        {
            //_gunsLibWatcher = new FileSystemWatcher(TipImagesManagementController.GetSubLibFullPath(TipLibrary.Guns));
            //_knivesLibWatcher = new FileSystemWatcher(TipImagesManagementController.GetSubLibFullPath(TipLibrary.Knives));
            //_explosivesLibWatcher = new FileSystemWatcher(TipImagesManagementController.GetSubLibFullPath(TipLibrary.Explosives));
            //_othersLibWatcher = new FileSystemWatcher(TipImagesManagementController.GetSubLibFullPath(TipLibrary.Others));

            if (!Directory.Exists(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Guns)))
            {
                Directory.CreateDirectory(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Guns));
            }
            if (!Directory.Exists(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Knives)))
            {
                Directory.CreateDirectory(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Knives));
            }
            if (!Directory.Exists(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Explosives)))
            {
                Directory.CreateDirectory(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Explosives));
            }
            if (!Directory.Exists(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Others)))
            {
                Directory.CreateDirectory(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Others));
            }
            _gunsLibWatcher = new FileSystemWatcher(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Guns));
            _knivesLibWatcher = new FileSystemWatcher(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Knives));
            _explosivesLibWatcher = new FileSystemWatcher(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Explosives));
            _othersLibWatcher = new FileSystemWatcher(TipImagesManagementController.GetSelectSubLibFullPath(TipLibrary.Others));

            _gunsLibWatcher.EnableRaisingEvents = true;
            _knivesLibWatcher.EnableRaisingEvents = true;
            _explosivesLibWatcher.EnableRaisingEvents = true;
            _othersLibWatcher.EnableRaisingEvents = true;

            _gunsLibWatcher.Filter = "*.xray";
            _explosivesLibWatcher.Filter = "*.xray";
            _knivesLibWatcher.Filter = "*.xray";
            _othersLibWatcher.Filter = "*.xray";

            _gunsLibWatcher.Created += GunsLibWatcherOnChanged;
            _knivesLibWatcher.Created += KnivesLibWatcherOnChanged;
            _explosivesLibWatcher.Created += ExplosivesLibWatcherOnChanged;
            _othersLibWatcher.Created += OthersLibWatcherOnChanged;

            _gunsLibWatcher.Deleted += GunsLibWatcherOnChanged;
            _knivesLibWatcher.Deleted += KnivesLibWatcherOnChanged;
            _explosivesLibWatcher.Deleted += ExplosivesLibWatcherOnChanged;
            _othersLibWatcher.Deleted += OthersLibWatcherOnChanged;
        }
      
        /// <summary>
        /// 根据当前的Tip注入计划，随机获取一个Tip图像。
        /// 如果没有合适的注入计划，或者找不到相应的图像，则返回null
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public TipImageSelection RandomGetImageSelection()
        {
            if (CurrentPlan != null)
                return RandomGetTipImageByPlan(CurrentPlan);
            return null;
        }

        /// <summary>
        /// 根据TipPlan，随机获取一个Tip图像
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        /// 
        private TipImageSelection RandomGetTipImageByPlan(TipPlan plan)
        {
            try
            {
                var lib = RandomGetTipLibByPlan(plan, _random);

                if (lib != null)
                {
                    // 根据插入比率，随机觉定本次是否选取tip图像
                    var pb = _random.Next(1, 101);
                    if (pb <= plan.Probability)
                    {
                        var fi = RandomGetImageFileInfoFromLib(lib.Value, _random);
                        if (fi != null)
                        {
                            var image = XRayScanlinesImage.LoadFromDiskFile(fi.FullName);
                            return new TipImageSelection(image, lib.Value, Path.GetFileName(fi.FullName));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return null;
        }

        /// <summary>
        /// 根据TipPlan中各个库的插入比重，随机获取Tip库类型
        /// </summary>
        /// <param name="plan">所依据的Tip注入计划</param>
        /// <param name="random">所使用的随机数生成器</param>
        /// <returns>根据注入计划随机获取的注入库类型，或者为null</returns>
        private TipLibrary? RandomGetTipLibByPlan(TipPlan plan, Random random)
        {
            //if (plan == null)
            //    return null;
            int totalWeight = plan.TotalWeights;
            if (totalWeight <= 0)
                return null;

            int rdIndex = random.Next(1, totalWeight + 1);

            if (rdIndex <= plan.ExplosivesWeight)
                return TipLibrary.Explosives;

            if (rdIndex <= plan.ExplosivesWeight + plan.GunsWeight)
                return TipLibrary.Guns;

            if (rdIndex <= plan.ExplosivesWeight + plan.GunsWeight + plan.KnivesWeight)
                return TipLibrary.Knives;

            return TipLibrary.Others;
        }

        /// <summary>
        /// 从指定的Tip图库中，随机获取一个图像的文件信息。如果此库中无Tip图像，则返回为空
        /// </summary>
        /// <param name="lib">指定的tip图哭类型</param>
        /// <param name="random">随机数生成器</param>
        /// <returns>随机获取的tip图像文件信息对象或者null</returns>
        private FileInfo RandomGetImageFileInfoFromLib(TipLibrary lib, Random random)
        {
            List<FileInfo> fileInfos = null;

            switch (lib)
            {
                case TipLibrary.Explosives:
                    fileInfos = _explosivesFileInfos;
                    break;
                case TipLibrary.Guns:
                    fileInfos = _gunsFileInfos;
                    break;
                case TipLibrary.Knives:
                    fileInfos = _knivesFileInfos;
                    break;
                default:
                    fileInfos = _othersFileInfos;
                    break;
            }

            if (fileInfos != null && fileInfos.Count > 0)
            {
                var index = random.Next(fileInfos.Count - 1);
                return fileInfos[index];
            }

            return null;
        }
      
        private void OthersLibWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            OnTipLibFileCreatedOrDeleted(_othersFileInfos, fileSystemEventArgs);
        }
        private void ExplosivesLibWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            OnTipLibFileCreatedOrDeleted(_explosivesFileInfos, fileSystemEventArgs);
        }
        private void KnivesLibWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            OnTipLibFileCreatedOrDeleted(_knivesFileInfos, fileSystemEventArgs);
        }
        private void GunsLibWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            OnTipLibFileCreatedOrDeleted(_gunsFileInfos, fileSystemEventArgs);
        }

        /// <summary>
        /// 处理某个tip库文件的增加或删除事件
        /// </summary>
        /// <param name="fileInfos"></param>
        /// <param name="fileSystemEventArgs"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void OnTipLibFileCreatedOrDeleted(List<FileInfo> fileInfos, FileSystemEventArgs fileSystemEventArgs)
        {
            if (fileInfos == null)
            {
                return;
            }

            try
            {
                if (fileSystemEventArgs.ChangeType == WatcherChangeTypes.Created)
                {
                    var fi = new FileInfo(fileSystemEventArgs.FullPath);
                    fileInfos.Add(fi);
                }
                else if (fileSystemEventArgs.ChangeType == WatcherChangeTypes.Deleted)
                {
                    var fi =
                        fileInfos.Find(
                            info =>
                                string.Equals(info.FullName, fileSystemEventArgs.FullPath,
                                    StringComparison.OrdinalIgnoreCase));
                    if (fi != null)
                    {
                        fileInfos.Remove(fi);
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }                                    
    }
}

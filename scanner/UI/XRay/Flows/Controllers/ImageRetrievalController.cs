using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 图像检索控制器：实现检索图像、删除图像等
    /// </summary>
    public class ImageRetrievalController
    {
        /// <summary>
        /// 在图像检索期间，使用一个独立的数据库连接，保证可以随时在context中进行增、删查改，提高效率
        /// </summary>
        private ImageRecordDbSet _recordDbSet;

        private ImageRetrievalConditions _conditions;

        /// <summary>
        /// 记录总数
        /// </summary>
        private int _resultRecordsCount;
        /// <summary>
        /// 检索结果记录总数
        /// </summary>
        public int ResultRecordsCount
        {
            get
            {
                return _resultRecordsCount;
            }
            set 
            {
                _resultRecordsCount = _resultRecords.Count >= 20 ? value : Math.Min(value, _resultRecords.Count);
            }
        }
        //{
        //    get
        //    {
        //        if (_resultRecords == null)
        //        {
        //            return 0;
        //        }
        //        else
        //        {
        //            return _resultRecords.Count;
        //        }
        //    }
        //}

        /// <summary>
        /// 每页显示的图像记录数量
        /// </summary>
        public int PageSize { get; private set; }

        /// <summary>
        /// 当前显示的图像记录的起始索引编号
        /// </summary>
        public int ShowingMinIndex { get; set; }

        /// <summary>
        /// 当前显示的图像记录的结束索引编号
        /// </summary>
        public int ShowingMaxIndex { get; protected set; }

        /// <summary>
        /// 所有的图像检索记录
        /// </summary>
        public List<ImageRecord> _resultRecords = null;

        /// <summary>
        /// 图像检索控制器的构造函数
        /// </summary>
        /// <param name="pageSize">每页显示的图像个数</param>
        /// <param name="conditions">图像检索条件，如果为空，则检索最近的一页图像</param>
        public ImageRetrievalController(int pageSize = 10, ImageRetrievalConditions conditions = null)
        {
            PageSize = pageSize;
            _conditions = conditions;
            _recordDbSet = new ImageRecordDbSet();
        }

        /// <summary>
        /// 重新设置检索条件
        /// </summary>
        /// <param name="conditions"></param>
        public void ResetConditions(ImageRetrievalConditions conditions)
        {
            _conditions = conditions;
        }

        /// <summary>
        /// 重新设置检索条件
        /// </summary>
        /// <update>姜毅改</update>
        /// <param name="conditions"></param>
        public bool ResetConditions1(ImageRetrievalConditions conditions)
        {
            bool result = false;
            if (_conditions != null)
            {
                result = _conditions.Equals(conditions);
            }
            _conditions = conditions;
            return result;
        }

        /// <summary>
        /// 根据当前的检索条件，重新检索图像记录. 并返回检索图像的总数
        /// </summary>
        public int ReLoadRecords()
        {
            if (_conditions == null)
            {
                int refday = 1;
                int i = 0;
                _resultRecords = _recordDbSet.TakeByConditions(DateTime.Now.AddDays(-1), DateTime.Now, LoginAccountManager.Service.AllAccountId, false, false);
                while (_resultRecords.Count < 100&&i<6)
                {
                    refday+=2;
                    _resultRecords = _recordDbSet.TakeByConditions(DateTime.Now.AddDays(-refday), DateTime.Now, LoginAccountManager.Service.AllAccountId, false, false);
                    i++;
                }
            }
            else
            {
                _resultRecords = _recordDbSet.TakeByConditions(_conditions.StartTime, _conditions.EndTime,
                    string.IsNullOrWhiteSpace(_conditions.AccountId) ? LoginAccountManager.Service.AllAccountId : new List<string>() { _conditions.AccountId }, 
                    _conditions.OnlyLocked,_conditions.OnlyMarked);
            }

            return ResultRecordsCount;
        }

        /// <summary>
        /// 根据当前的检索条件，重新检索图像记录. 并返回检索图像的总数
        /// </summary>
        /// <update>姜毅改</update>
        public void ReLoadRecords1()
        {
            DateTime scanTimeStart;
            DateTime scanTimeEnd;
            List<string> accountId;
            bool onlyLocked;
            bool onlyMarked;
            
            if (_conditions == null)
            {
                //_conditions = new ImageRetrievalConditions(new DateTime(2016, 1, 1), DateTime.Now, TimeRange.LastHour, LoginAccountManager.Service.CurrentAccount.AccountId, false, false);
                scanTimeStart = new DateTime(2016, 1, 1);
                scanTimeEnd = DateTime.Now;
                accountId = LoginAccountManager.Service.AllAccountId;
                onlyLocked = false;
                onlyMarked = false;
                //int refday = 1;
                //int i = 0;
                //_resultRecords = _recordDbSet.TakeByConditions(DateTime.Now.AddDays(-1), DateTime.Now, LoginAccountManager.Service.AllAccountId, false, false,ShowingMinIndex,PageSize);
                //while (_resultRecords.Count < 100 && i < 6)
                //{
                //    refday += 2;
                //    _resultRecords = _recordDbSet.TakeByConditions(DateTime.Now.AddDays(-refday), DateTime.Now, LoginAccountManager.Service.AllAccountId, false, false, ShowingMinIndex, PageSize);
                //    i++;
                //}
            }
            else
            {
                scanTimeStart = _conditions.StartTime;
                scanTimeEnd = _conditions.EndTime;
                accountId = string.IsNullOrWhiteSpace(_conditions.AccountId) ? LoginAccountManager.Service.AllAccountId : new List<string>() { _conditions.AccountId };
                onlyLocked = _conditions.OnlyLocked;
                onlyMarked = _conditions.OnlyMarked;
            }
            _resultRecords = _recordDbSet.TakeByConditions(scanTimeStart, scanTimeEnd, accountId, onlyLocked, onlyMarked, ShowingMinIndex, PageSize);
        }

        //public int GetTableTotalCount()
        //{
        //    return _recordDbSet.Count();
        //}

        /// <summary>
        /// 查询总数量
        /// </summary>
        /// <returns></returns>
        public int GetTableTotalCount()
        {
            ResultRecordsCount = _recordDbSet.Count();
            return ResultRecordsCount;
        }

        /// <summary>
        /// 查询总数量
        /// </summary>
        /// <update>姜毅改</update>
        /// <returns></returns>
        public int GetTableTotalCount1()
        {
            DateTime scanTimeStart;
            DateTime scanTimeEnd;
            List<string> accountId;
            bool onlyLocked;
            bool onlyMarked;

            if (_conditions == null)
            {
                //_conditions = new ImageRetrievalConditions(new DateTime(2016, 1, 1), DateTime.Now, TimeRange.LastHour, LoginAccountManager.Service.CurrentAccount.AccountId, false, false);
                scanTimeStart = new DateTime(2016, 1, 1);
                scanTimeEnd = DateTime.Now;
                accountId = LoginAccountManager.Service.AllAccountId;
                onlyLocked = false;
                onlyMarked = false;
                //int refday = 1;
                //int i = 0;
                //_resultRecords = _recordDbSet.TakeByConditions(DateTime.Now.AddDays(-1), DateTime.Now, LoginAccountManager.Service.AllAccountId, false, false,ShowingMinIndex,PageSize);
                //while (_resultRecords.Count < 100 && i < 6)
                //{
                //    refday += 2;
                //    _resultRecords = _recordDbSet.TakeByConditions(DateTime.Now.AddDays(-refday), DateTime.Now, LoginAccountManager.Service.AllAccountId, false, false, ShowingMinIndex, PageSize);
                //    i++;
                //}
            }
            else
            {
                scanTimeStart = _conditions.StartTime;
                scanTimeEnd = _conditions.EndTime;
                accountId = string.IsNullOrWhiteSpace(_conditions.AccountId) ? LoginAccountManager.Service.AllAccountId : new List<string>() { _conditions.AccountId };
                onlyLocked = _conditions.OnlyLocked;
                onlyMarked = _conditions.OnlyMarked;
            }
            ResultRecordsCount = _recordDbSet.CountByConditions(scanTimeStart, scanTimeEnd, accountId, onlyLocked, onlyMarked);
            return ResultRecordsCount;
        }

        /// <summary>
        /// 获取检索结果的第一页
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<BindableImage> MoveToFirstPage()
        {
            if (_resultRecords == null || _resultRecords.Count == 0)
            {
                return null;
            }

            ShowingMinIndex = 0;
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);

            return GetCurrentPageImages();
        }

        /// <summary>
        /// 新获取检索结果的第一页
        /// </summary>
        /// <update>姜毅改</update>
        /// <returns></returns>
        public ObservableCollection<BindableImage> MoveToFirstPage1()
        {
            if (_resultRecords == null || _resultRecords.Count == 0)
            {
                ShowingMinIndex = 0;
                ShowingMaxIndex = 0;
                return null;
            }
            ShowingMinIndex = 0;
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);
            ReLoadRecords1();
            return ConvertRecordsToBindableImages(_resultRecords);
        }

        /// <summary>
        /// 获取当前页范围内的记录
        /// </summary>
        /// <returns></returns>
        protected List<ImageRecord> GetCurrentPageRecords()
        {
            var count = Math.Min(PageSize, ResultRecordsCount - ShowingMinIndex);
            if (count > 0 && ShowingMinIndex != -1)
            {
                return _resultRecords.GetRange(ShowingMinIndex, count);
            }

            return null;
        }



        /// <summary>
        /// 为指定的图像记录，生成视角1的彩色图像
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public BitmapImage GenerateImage(ImageRecord record)
        {
            if (record == null)
            {
                return null;
            }

            try
            {
                var processor = new XRayImageProcessor();

                if (!string.IsNullOrWhiteSpace(record.StorePath) && File.Exists(record.StorePath))
                {
                    var image = XRayScanlinesImage.LoadFromDiskFile(record.StorePath);
                    if (image != null)
                    {
                        processor.AttachImageData(image.View1Data);
                    }

                    var bmp = processor.GetBitmap();
                    return BitmapHelper.ConvertToBitmapImage(bmp);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return null;
        }

        /// <summary>
        /// 生成当前页图像缩略图等列表，用于显示
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<BindableImage> GetCurrentPageImages()
        {
            var pageRecords = GetCurrentPageRecords();
            if (pageRecords != null)
            {
                return ConvertRecordsToBindableImages(pageRecords);
            }

            return null;
        }

        /// <summary>
        /// 原获取检索结果的下一页
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<BindableImage> MoveToNextPage()
        {
            if (ShowingMaxIndex == ResultRecordsCount - 1)
            {
                return null;
            }

            ShowingMinIndex += PageSize;
            ShowingMinIndex = Math.Min(ShowingMinIndex, ResultRecordsCount - 1);
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);

            return GetCurrentPageImages();
        }

        /// <summary>
        /// 新获取检索结果的下一页
        /// </summary>
        /// <update>姜毅改</update>
        /// <returns></returns>
        public ObservableCollection<BindableImage> MoveToNextPage1()
        {
            if (ShowingMaxIndex == ResultRecordsCount - 1)
            {
                return null;
            }

            ShowingMinIndex += PageSize;
            ShowingMinIndex = Math.Min(ShowingMinIndex, ResultRecordsCount - 1);
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);
            ReLoadRecords1();
            return ConvertRecordsToBindableImages(_resultRecords);
        }

        /// <summary>
        /// 原获取检索结果的上一页
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<BindableImage> MoveToPreviousPage()
        {
            if (ShowingMinIndex == 0)
            {
                return null;
            }

            ShowingMinIndex -= PageSize;
            ShowingMinIndex = Math.Max(ShowingMinIndex, 0);
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);

            return GetCurrentPageImages();
        }

        /// <summary>
        /// 新获取检索结果的上一页
        /// </summary>
        /// <upate>姜毅改</upate>
        /// <returns></returns>
        public ObservableCollection<BindableImage> MoveToPreviousPage1()
        {
            if (ShowingMinIndex == 0)
            {
                return null;
            }

            ShowingMinIndex -= PageSize;
            ShowingMinIndex = Math.Max(ShowingMinIndex, 0);
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);
            ReLoadRecords1();
            return ConvertRecordsToBindableImages(_resultRecords);
        }

        /// <summary>
        /// 更新当前页信息
        /// 用途：当删除了某些图像后，更新显示
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<BindableImage> UpdateCurrentPage()
        {
            if (ShowingMinIndex == ResultRecordsCount - 1)
            {
                ShowingMinIndex -= PageSize;
            }

            ShowingMinIndex = Math.Max(ShowingMinIndex, 0);
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);

            return GetCurrentPageImages();
        }

        /// <summary>
        /// 获取最近的若干图像
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public ObservableCollection<BindableImage> GetRecentImages(int count)
        {
            var records = _recordDbSet.TakeLatest(count);

            return ConvertRecordsToBindableImages(records);
        }

        /// <summary>
        /// 基于图像记录，生成用于显示的图像
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        public static ObservableCollection<BindableImage> ConvertRecordsToBindableImages(IEnumerable<ImageRecord> records)
        {
            var collections = new ObservableCollection<BindableImage>();
            if (records != null)
            {
                foreach (var record in records)
                {
                    List<string> list = new List<string>(record.StorePath.Split('\\'));
                    List<string> list1 = new List<string>(list.Last().Split('_'));
                    string channalid = list1.First();
                    var bindImage = new BindableImage(record);
                    try
                    {
                        var image = XRayScanlinesImage.LoadFromDiskFile(record.StorePath);
                        bindImage.Thumbnail = BitmapHelper.ConvertToBitmapImage(image.Thumbnail);
                        bindImage.IsManualMark = record.IsManualSaved;
                        if(channalid == "")
                        {
                            bindImage.ChannalId = "null";
                        }
                        else
                        {
                            bindImage.ChannalId = channalid;

                        }

                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }

                    collections.Add(bindImage);
                }
            }

            return collections;
        }

        /// <summary>
        /// 锁定指定的图像记录：被锁定后，将不会被自动删除
        /// </summary>
        public void Update(IEnumerable<ImageRecord> records)
        {
            _recordDbSet.Update(records);
        }

        /// <summary>
        /// 删除图像记录，同时删除图像
        /// </summary>
        /// <param name="records"></param>
        public void RemoveRecords(IEnumerable<ImageRecord> records)
        {
            // 先删除磁盘文件，然后再从数据库中删除
            if (records == null)
            {
                return;
            }

            foreach (var record in records)
            {
                try
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(record.StorePath);
                    var filePath = Path.GetDirectoryName(record.StorePath);
                    IEnumerable files = Directory.EnumerateFiles(filePath).Where(name => Regex.IsMatch(name, $"{fileNameWithoutExtension}*"));
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            }

            _recordDbSet.RemoveRange(records);

            // 从检索的记录集中移除
            foreach (var record in records)
            {
                _resultRecords.Remove(record);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 培训图像管理Controller：提供对培训图像管理的增删查改等功能
    /// </summary>
    public class TrainingImageManagementController
    {
        private const string TraingImageLibRootPath = @"D:\SecurityScanner\TrainingImages";

        /// <summary>
        /// 图像库下的所有文件信息
        /// </summary>
        private List<FileInfo> _fileInfos;

        /// <summary>
        /// 图像个数
        /// </summary>
        public int ImagesCount
        {
            get
            {
                if (_fileInfos != null)
                {
                    return _fileInfos.Count;
                }

                return 0;
            }
        }

        /// <summary>
        /// 每页显示的图像记录数量
        /// </summary>
        public int PageSize { get; private set; }

        /// <summary>
        /// 当前显示的图像记录的起始索引编号
        /// </summary>
        public int ShowingMinIndex { get; private set; }

        /// <summary>
        /// 当前显示的图像记录的结束索引编号
        /// </summary>
        public int ShowingMaxIndex { get; private set; }

        public TrainingImageManagementController()
        {
            try
            {
                // 保证培训图像库文件夹一定存在
                Directory.CreateDirectory(TraingImageLibRootPath);

                PageSize = 25;
                ReloadImages();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 将指定的一系列图像，导入到库中，如果发现同名图像，则覆盖
        /// </summary>
        /// <param name="imageFilePaths"></param>
        public void ImportImagesToLib(IEnumerable<string> imageFilePaths)
        {
            foreach (var filePath in imageFilePaths)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        var fileName = Path.GetFileName(filePath);
                        var destFilePath = Path.Combine(TraingImageLibRootPath, fileName);
                        File.Copy(filePath, destFilePath, true);
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to copy xray image file to lib. The source file is: " + filePath);
                }
            }
        }

        /// <summary>
        /// 获取检索结果的第一页
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<BindableImage> MoveToFirstPage()
        {
            if (_fileInfos == null || _fileInfos.Count == 0)
            {
                return null;
            }

            ShowingMinIndex = 0;
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ImagesCount - 1);

            return GetCurrentPageImages();
        }

        /// <summary>
        /// 获取检索结果的下一页
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<BindableImage> MoveToNextPage()
        {
            if (ShowingMaxIndex == ImagesCount - 1)
            {
                return null;
            }

            ShowingMinIndex += PageSize;
            ShowingMinIndex = Math.Min(ShowingMinIndex, ImagesCount - 1);
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ImagesCount - 1);

            return GetCurrentPageImages();
        }

        /// <summary>
        /// 获取检索结果的下一页
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
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ImagesCount - 1);

            return GetCurrentPageImages();
        }

        /// <summary>
        /// 生成当前页图像缩略图等列表，用于显示
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<BindableImage> GetCurrentPageImages()
        {
            if (_fileInfos != null && _fileInfos.Count > 0 && ShowingMinIndex <= _fileInfos.Count - 1)
            {
                if (ShowingMaxIndex >= _fileInfos.Count - 1)
                {
                    ShowingMaxIndex = _fileInfos.Count - 1;
                }

                var result = new ObservableCollection<BindableImage>();
                for (var i = ShowingMinIndex; i <= ShowingMaxIndex; i++)
                {
                    var bi = XRayImageHelper.ToBindableImage(_fileInfos[i].FullName);
                    if (bi != null)
                    {
                        result.Add(bi);
                    }
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// 删除指定的图像
        /// </summary>
        /// <param name="filePaths"></param>
        public void DeleteImages(IEnumerable<string> filePaths)
        {
            if (_fileInfos != null && filePaths != null)
            {
                foreach (var path in filePaths)
                {
                    try
                    {
                        var fi = _fileInfos.Find(info => string.Equals(info.FullName, path, StringComparison.OrdinalIgnoreCase));
                        if (fi != null)
                        {
                            _fileInfos.Remove(fi);
                        }

                        File.Delete(path);
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e);
                    }
                }
            }
        }

        /// <summary>
        /// 返回指定范围内的可用于绑定的图像
        /// </summary>
        /// <param name="startIndex">起始索引，最小值为0</param>
        /// <param name="endIndex">结束索引</param>
        /// <returns></returns>
        public ObservableCollection<BindableImage> GetImagesByRange(int startIndex, int endIndex)
        {
            if (_fileInfos != null && _fileInfos.Count > 0 && startIndex <= _fileInfos.Count - 1)
            {
                if (endIndex >= _fileInfos.Count - 1)
                {
                    endIndex = _fileInfos.Count - 1;
                }

                var result = new ObservableCollection<BindableImage>();
                for (var i = startIndex; i <= endIndex; i++)
                {
                    var bi = XRayImageHelper.ToBindableImage(_fileInfos[i].FullName);
                    if (bi != null)
                    {
                        result.Add(bi);
                    }
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// 获取库目录下的所有文件信息
        /// </summary>
        /// <returns></returns>
        public void ReloadImages()
        {
            var di = new DirectoryInfo(TraingImageLibRootPath);
            _fileInfos = di.GetFiles("*.xray").ToList();
        }
    }
}

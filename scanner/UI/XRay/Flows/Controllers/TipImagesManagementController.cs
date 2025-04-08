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
    /// Tip图像库管理控制器，仅管理指定的某一个子库的图像
    /// </summary>
    public class TipImagesManagementController
    {
        private const string TipLibRootPath = @"D:\SecurityScanner\TipLib";
        private const string SelectTipLibRootPath = @"D:\SecurityScanner\TipLib\SelectTipLib";      

        /// <summary>
        /// 当前Tip图像库下的所有文件信息
        /// </summary>
        private List<FileInfo> _fileInfos;
        private List<FileInfo> _selectfileInfos;
        private List<FileInfo> _unselectfileInfos;

        /// <summary>
        /// 当前Tip库的图像个数
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
        public int SelectImagesCount
        {
            get
            {
                if (_selectfileInfos != null)
                {
                    if (_selectfileInfos.Count <= PageSize)
                        ShowingSelectMaxIndex = _selectfileInfos.Count - 1;
                    return _selectfileInfos.Count;
                }

                return 0;
            }
        }
        public int UnSelectImagesCount
        {
            get
            {
                if (_unselectfileInfos != null)
                {
                    if (_unselectfileInfos.Count <= PageSize)
                        ShowingUnSelectMaxIndex = _unselectfileInfos.Count - 1;
                    return _unselectfileInfos.Count;
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
        /// <summary>
        /// 当前显示的图像记录的起始索引编号
        /// </summary>
        public int ShowingSelectMinIndex { get; private set; }
        /// <summary>
        /// 当前显示的图像记录的结束索引编号
        /// </summary>
        public int ShowingSelectMaxIndex { get; private set; }

        public int ShowingUnSelectMinIndex { get; private set; }
        /// <summary>
        /// 当前显示的图像记录的结束索引编号
        /// </summary>
        public int ShowingUnSelectMaxIndex { get; private set; }

        /// <summary>
        /// 当前管理的tip库类型
        /// </summary>
        private TipLibrary _library;          

        public TipImagesManagementController(TipLibrary library)
        {
            try
            {
                CreateSubDirs();
                _library = library;
                PageSize = 25;
                LoadTipImages(library);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }
        public List<string> TipLibImageName()
        {
            List<string> ImageName = new List<string>(10);
            foreach (var _selectfileInfo in _selectfileInfos)
            {
                ImageName.Add(_selectfileInfo.Name);
            }
            return ImageName;
        }

        public void ChangeSelectTipLib(TipLibrary library, List<string> imageName)
        {
            _selectfileInfos.Clear();
            var di = GetSubLibFullPath(library);
            List<string> imageFilePaths = new List<string>();  
            foreach (var name in imageName)
            {
                imageFilePaths.Add(Path.Combine(di, name));
            }
            ImportSelectImagesToLib(imageFilePaths);
        }

        /// <summary>
        /// 创建所有的Tip子目录(包括选择好的图像)
        /// </summary>
        private void CreateSubDirs()
        {
            try
            {
                Directory.CreateDirectory(GetSubLibFullPath(TipLibrary.Explosives));
                Directory.CreateDirectory(GetSubLibFullPath(TipLibrary.Guns));
                Directory.CreateDirectory(GetSubLibFullPath(TipLibrary.Knives));
                Directory.CreateDirectory(GetSubLibFullPath(TipLibrary.Others));
                Directory.CreateDirectory(GetSelectSubLibFullPath(TipLibrary.Explosives));
                Directory.CreateDirectory(GetSelectSubLibFullPath(TipLibrary.Guns));
                Directory.CreateDirectory(GetSelectSubLibFullPath(TipLibrary.Knives));
                Directory.CreateDirectory(GetSelectSubLibFullPath(TipLibrary.Others));
                //Directory.CreateDirectory(GetUnSelectSubLibFullPath(TipLibrary.Explosives));
                //Directory.CreateDirectory(GetUnSelectSubLibFullPath(TipLibrary.Guns));
                //Directory.CreateDirectory(GetUnSelectSubLibFullPath(TipLibrary.Knives));
                //Directory.CreateDirectory(GetUnSelectSubLibFullPath(TipLibrary.Others));
            }
            catch (Exception ex)
            {
                Tracer.TraceException(ex);
            }
        }

        /// <summary>
        /// 获取指定Tip库目录下的所有文件信息
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        private void LoadTipImages(TipLibrary library)
        {
            var di = new DirectoryInfo(GetSubLibFullPath(library));
            _fileInfos = di.GetFiles("*.xray").ToList();
            var sdi = new DirectoryInfo(GetSelectSubLibFullPath(library));
            _selectfileInfos = sdi.GetFiles("*.xray").ToList();
            _unselectfileInfos = new List<FileInfo>();            
            _fileInfos.ForEach(i => _unselectfileInfos.Add(i));
            foreach (var selectfileInfo in _selectfileInfos)
            {               
                for (int i = 0; i < _unselectfileInfos.Count; i++ )
                {
                    if (_unselectfileInfos[i].Name == selectfileInfo.Name)
                        _unselectfileInfos.Remove(_unselectfileInfos[i]);
                }                             
            }           
        }       

        /// <summary>
        /// 将指定的一系列图像，导入到当前的tip库中，如果发现同名图像，则覆盖
        /// </summary>
        /// <param name="imageFilePaths"></param>
        public void ImportImagesToLib(IEnumerable<string> imageFilePaths)
        {
            var destDir = GetSubLibFullPath(_library);

            foreach (var filePath in imageFilePaths)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        var fileName = Path.GetFileName(filePath);
                        var destFilePath = Path.Combine(destDir, fileName);
                        if (File.Exists(destDir)) File.Delete(destDir);
                        File.Copy(filePath, destFilePath, true);
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to copy xray image file to tip lib. The source file is: " + filePath);
                }
            }
        }
        public void ImportSelectImagesToLib(IEnumerable<string> imageFilePaths)
        {
            var destDir = GetSelectSubLibFullPath(_library);

            foreach (var filePath in imageFilePaths)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(filePath))
                    {
                        var fileName = Path.GetFileName(filePath);
                        var destFilePath = Path.Combine(destDir, fileName);
                        if (File.Exists(destDir)) File.Delete(destDir);
                        File.Copy(filePath, destFilePath, true);                       
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to copy xray image file to tip lib. The source file is: " + filePath);
                }
            }
            LoadTipImages(_library);
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
        public ObservableCollection<BindableImage> MoveToSelectFirstPage()
        {
            if (_selectfileInfos == null || _selectfileInfos.Count == 0)
            {
                return null;
            }

            ShowingSelectMinIndex = 0;
            ShowingSelectMaxIndex = Math.Min(ShowingSelectMinIndex + PageSize - 1, SelectImagesCount - 1);

            return GetCurrentSelectPageImages();
        }
        public ObservableCollection<BindableImage> MoveToUnSelectFirstPage()
        {
            if (_unselectfileInfos == null || _unselectfileInfos.Count == 0)
            {
                return null;
            }

            ShowingUnSelectMinIndex = 0;
            ShowingUnSelectMaxIndex = Math.Min(ShowingUnSelectMinIndex + PageSize - 1, UnSelectImagesCount - 1);

            return GetCurrentUnSelectPageImages();
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
        public ObservableCollection<BindableImage> MoveToSelectNextPage()
        {
            if (ShowingSelectMaxIndex == ImagesCount - 1)
            {
                return null;
            }

            ShowingSelectMinIndex += PageSize;
            ShowingSelectMinIndex = Math.Min(ShowingSelectMinIndex, ImagesCount - 1);
            ShowingSelectMaxIndex = Math.Min(ShowingSelectMinIndex + PageSize - 1, SelectImagesCount - 1);

            return GetCurrentSelectPageImages();
        }
        public ObservableCollection<BindableImage> MoveToUnSelectNextPage()
        {
            if (ShowingUnSelectMaxIndex == ImagesCount - 1)
            {
                return null;
            }

            ShowingUnSelectMinIndex += PageSize;
            ShowingUnSelectMinIndex = Math.Min(ShowingUnSelectMinIndex, ImagesCount - 1);
            ShowingUnSelectMaxIndex = Math.Min(ShowingUnSelectMinIndex + PageSize - 1, UnSelectImagesCount - 1);

            return GetCurrentUnSelectPageImages();
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
        public ObservableCollection<BindableImage> MoveToSelectPreviousPage()
        {
            if (ShowingSelectMinIndex == 0)
            {
                return null;
            }

            ShowingSelectMinIndex -= PageSize;
            ShowingSelectMinIndex = Math.Max(ShowingSelectMinIndex, 0);
            ShowingSelectMaxIndex = Math.Min(ShowingSelectMinIndex + PageSize - 1, SelectImagesCount - 1);

            return GetCurrentSelectPageImages();
        }
        public ObservableCollection<BindableImage> MoveToUnSelectPreviousPage()
        {
            if (ShowingUnSelectMinIndex == 0)
            {
                return null;
            }

            ShowingUnSelectMinIndex -= PageSize;
            ShowingUnSelectMinIndex = Math.Max(ShowingUnSelectMinIndex, 0);
            ShowingUnSelectMaxIndex = Math.Min(ShowingUnSelectMinIndex + PageSize - 1, UnSelectImagesCount - 1);

            return GetCurrentUnSelectPageImages();
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
        public ObservableCollection<BindableImage> GetCurrentSelectPageImages()
        {
            if (_selectfileInfos != null && _selectfileInfos.Count > 0 && ShowingSelectMinIndex <= _selectfileInfos.Count - 1)
            {
                if (ShowingSelectMaxIndex >= _selectfileInfos.Count - 1)
                {
                    ShowingSelectMaxIndex = _selectfileInfos.Count - 1;
                }

                var result = new ObservableCollection<BindableImage>();
                for (var i = ShowingSelectMinIndex; i <= ShowingSelectMaxIndex; i++)
                {
                    var bi = XRayImageHelper.ToBindableImage(_selectfileInfos[i].FullName);
                    if (bi != null)
                    {
                        result.Add(bi);
                    }
                }

                return result;
            }

            return null;
        }
        public ObservableCollection<BindableImage> GetCurrentUnSelectPageImages()
        {
            if (_unselectfileInfos != null && _unselectfileInfos.Count > 0 && ShowingUnSelectMinIndex <= _unselectfileInfos.Count - 1)
            {
                if (ShowingUnSelectMaxIndex >= _unselectfileInfos.Count - 1)
                {
                    ShowingUnSelectMaxIndex = _unselectfileInfos.Count - 1;
                }

                var result = new ObservableCollection<BindableImage>();
                for (var i = ShowingUnSelectMinIndex; i <= ShowingUnSelectMaxIndex; i++)
                {
                    var bi = XRayImageHelper.ToBindableImage(_unselectfileInfos[i].FullName);
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
        public void DeleteSelectImages(IEnumerable<string> filePaths)
        {
            if (_selectfileInfos != null && filePaths != null)
            {
                foreach (var path in filePaths)
                {
                    try
                    {
                        var fi = _selectfileInfos.Find(info => string.Equals(info.FullName, path, StringComparison.OrdinalIgnoreCase));
                        if (fi != null)
                        {
                            _selectfileInfos.Remove(fi);
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
        public void DeleteAllImages()
        {
            var destDir = GetSubLibFullPath(_library);
            var files = Directory.GetFiles(destDir, "*.xray");
            for (int i = 0; i < files.Count(); i++)
            {
                File.Delete(files[i]);
            }
        }
        public void DeleteSelectAllImages()
        {
            var destDir = GetSelectSubLibFullPath(_library);
            var files = Directory.GetFiles(destDir, "*.xray");
            for (int i = 0; i < files.Count(); i++)
            {
                File.Delete(files[i]);
            }
        }

        public void UpDateUnSelectImages(List<string> filenames)
        {
            foreach (var filename in filenames)
            {
                for (int i = 0; i < _fileInfos.Count; i++)
                {
                    if (_fileInfos[i].Name == filename)
                        _unselectfileInfos.Add(_fileInfos[i]);
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
        /// 获取指定Tip库目录下的所有Tip图像文件信息
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns>
        public static List<FileInfo> GetTipImageFileInfos(TipLibrary library)
        {
            var di = new DirectoryInfo(GetSubLibFullPath(library));
            return di.GetFiles("*.xray").ToList();
        }
        public static List<FileInfo> GetSelectTipImageFileInfos(TipLibrary library)
        {
            var di = new DirectoryInfo(GetSelectSubLibFullPath(library));
            return di.GetFiles("*.xray").ToList();
        }       

        /// <summary>
        /// 获取指定Tip库的文件夹路径
        /// </summary>
        /// <param name="lib"></param>
        /// <returns></returns>
        public static string GetSubLibFullPath(TipLibrary lib)
        {
            switch (lib)
            {
                case TipLibrary.Explosives:
                    return Path.Combine(TipLibRootPath, "Explosives");

                case TipLibrary.Guns:
                    return Path.Combine(TipLibRootPath, "Guns");

                case TipLibrary.Knives:
                    return Path.Combine(TipLibRootPath, "Knives");

                case TipLibrary.Others:
                    return Path.Combine(TipLibRootPath, "Others");

                default:
                    return null;
            }
        }
        public static string GetSelectSubLibFullPath(TipLibrary lib)
        {
            switch (lib)
            {
                case TipLibrary.Explosives:
                    return Path.Combine(SelectTipLibRootPath, "Explosives");

                case TipLibrary.Guns:
                    return Path.Combine(SelectTipLibRootPath, "Guns");

                case TipLibrary.Knives:
                    return Path.Combine(SelectTipLibRootPath, "Knives");

                case TipLibrary.Others:
                    return Path.Combine(SelectTipLibRootPath, "Others");

                default:
                    return null;
            }
        }        
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using UI.Common.Tracers;

namespace UI.XRay.Common.Utilities
{
    /// <summary>
    /// 文件夹监控类，主要用于保存通用格式图像时对文件夹的监控，不具备通用性，
    /// 监控某个文件夹（不包括子文件夹和新文件夹的创建以及文件的删除，只包括创建新文件）指定格式的文件的
    /// 创建和个数
    /// </summary>
    public class FileWatcher
    {
        private FileSystemWatcher _watcher;

        private readonly string _path;

        private readonly string _filter = string.Empty;

        /// <summary>
        /// 监控文件夹中文件个数的上限
        /// </summary>
        private readonly int _capacity;

        /// <summary>
        /// 是否正在监控，如果路径有问题或者初始化时出现了异常则不进行监控
        /// </summary>
        private bool _isWatch;

        /// <summary>
        /// 目前只监测新文件的创建，不检测删减，todo：使用队列是为了防止出现大批量生成的情况
        /// </summary>
        public ConcurrentQueue<string> FileQueue { get; private set; }

        /// <summary>
        /// 监测文件夹
        /// </summary>
        /// <param name="path">检测目录</param>
        /// <param name="capacity">保存检测文件夹文件个数上限</param>
        public FileWatcher(string path, int capacity)
        {
            _path = path;
            _capacity = capacity;

            //todo：获取文件夹中文件的信息，比如文件的个数
            GetFileList();
            StartFileWatcher();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filter"></param>
        /// <param name="capacity"></param>
        public FileWatcher(string path, string filter, int capacity)
        {
            _path = path;
            _filter = filter;
            _capacity = capacity;

            GetFileList();
            StartFileWatcher();
        }

        /// <summary>
        /// 初次运行获取文件件下文件的信息
        /// </summary>
        private void GetFileList()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_path))
                {
                    FileQueue = new ConcurrentQueue<string>(GetOrderedSpecialFiles(_path));
                    ProcessBySetting();
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);

                _isWatch = false;
            }
            _isWatch = true;
        }

        private IEnumerable<string> GetOrderedSpecialFiles(string path)
        {
            var di = new DirectoryInfo(path);
            var files = di.GetFiles();
            string fileName;
            List<FileInfo> list = new List<FileInfo>();
            for (int i = 0; i < files.LongLength; i++)
            {
                fileName = files[i].Name.ToLower();
                if (Verify(fileName))
                {
                    list.Add(files[i]);
                }
            }
            var orderedList = list.OrderBy(f => f.CreationTime);
            return orderedList.Select(i => i.FullName);
        }

        private bool Verify(string fileName)
        {
            return fileName.EndsWith(".jpg") || fileName.EndsWith(".bmp") || fileName.EndsWith(".png") ||
                   fileName.EndsWith(".tiff") || fileName.EndsWith(".xray");
        }
        /// <summary>
        /// 开启监控
        /// </summary>
        private void StartFileWatcher()
        {
            try
            {
                _watcher = new FileSystemWatcher(_path);
                //不监控子目录
                _watcher.IncludeSubdirectories = false;
                _watcher.Created += _watcher_Created;
                _watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName | NotifyFilters.LastWrite |
                        NotifyFilters.Attributes | NotifyFilters.LastWrite;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                _isWatch = false;
            }
        }

        void _watcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (_isWatch && e.ChangeType == WatcherChangeTypes.Created && Verify(e.Name))
                {
                    //不理会文件夹的变化，只监控文件的变化
                    if (File.Exists(e.FullPath))
                    {
                        FileQueue.Enqueue(e.FullPath);

                        ProcessBySetting();
                    }
                }
            }
            catch (Exception exception)
            {
             Tracer.TraceException(exception);   
            }
        }

        public void Shutdown()
        {
            this._isWatch = false;
            _watcher.Created -= _watcher_Created;
        }

        void ProcessBySetting()
        {
            //处理多余的项
            while (CanProcess)
            {
                Process();
            }
        }

        bool CanProcess
        {
            get { return this.FileQueue.Count > this._capacity; }
        }

        void Process()
        {
            try
            {
                string fileInfo;
                var sucess = FileQueue.TryDequeue(out fileInfo);
                if (sucess && !string.IsNullOrWhiteSpace(fileInfo))
                {
                    File.Delete(fileInfo);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }
    }
}

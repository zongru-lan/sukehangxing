using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.IO;
using UI.Common.Tracers;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// 更改设备型号窗口的视图模型
    /// </summary>
    public class ChangeModelWindowViewModel : ViewModelBase
    {
        public RelayCommand ChangeCommand { get; private set; }

        public List<string> ModelList { get; set; }

        /// <summary>
        /// 当前被用户选中的设备型号名称，即文件名，不包括文件后缀xml
        /// </summary>
        public string SelectedModelName
        {
            get { return _selectedModelName; }
            set
            {
                _selectedModelName = value;
                RaisePropertyChanged();
            }
        }

        private string _selectedModelName;

        public ChangeModelWindowViewModel()
        {
            ChangeCommand = new RelayCommand(ChangeCommandExecute);

            try
            {
                LoadModelsFromDisk();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private void ChangeCommandExecute()
        {
            if (!string.IsNullOrWhiteSpace(SelectedModelName))
            {
                var fileName = SelectedModelName + ".xml";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "../Models", fileName);

                DeleteAndCopyFile();

                MessengerInstance.Send(new ChangeModelMessageAction(filePath, b =>
                {
                }));

                MessengerInstance.Send(new CloseWindowMessage("ChangeModelWindow"));
            }
        }

        private void DeleteAndCopyFile()
        {
            string path = System.Environment.CurrentDirectory;
            //侧视角
            string dstPath = Path.Combine(path, "ce.csv");
            string srcPath = Path.Combine(path, "../Models", "Mat", SelectedModelName, "ce.csv");
            CopyFile(srcPath, dstPath);

            //正视角
            dstPath = Path.Combine(path, "zheng.csv");
            srcPath = Path.Combine(path, "../Models", "Mat", SelectedModelName, "zheng.csv");
            CopyFile(srcPath, dstPath);

            dstPath = Path.Combine(path, "Mat.class");
            srcPath = Path.Combine(path, "../Models", "Mat", SelectedModelName, "Mat.class");
            CopyFile(srcPath, dstPath);

            dstPath = Path.Combine(path, "Mat2.class");
            srcPath = Path.Combine(path, "../Models", "Mat", SelectedModelName, "Mat2.class");
            CopyFile(srcPath, dstPath);

            dstPath = Path.Combine(path, "Mat3.class");
            srcPath = Path.Combine(path, "../Models", "Mat", SelectedModelName, "Mat3.class");
            CopyFile(srcPath, dstPath);
            
            string algorithmViewPath = "D:\\SecurityScanner\\ChannelBadFlags";
            if(!Directory.Exists(algorithmViewPath))
                Directory.CreateDirectory(algorithmViewPath);
            dstPath = Path.Combine(algorithmViewPath, "SecurityAlgorithmView.txt");
            srcPath = Path.Combine(path, "../Models", "Mat", SelectedModelName, "SecurityAlgorithmView.txt");
            CopyFile(srcPath, dstPath);
        }

        void CopyFile(string srcPath, string dstPath)
        {
            if (File.Exists(srcPath))
            {
                if (File.Exists(dstPath))
                {
                    File.Delete(dstPath);
                }
                File.Copy(srcPath, dstPath);
            }
        }

        /// <summary>
        /// 从磁盘的指定位置加载设备型号配置文件
        /// </summary>
        private void LoadModelsFromDisk()
        {
            ModelList = new List<string>();
            var modelFiles = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "../Models"), "*.xml");

            foreach (var modelFile in modelFiles)
            {
                ModelList.Add(Path.GetFileNameWithoutExtension(modelFile));
            }
        }
    }
}

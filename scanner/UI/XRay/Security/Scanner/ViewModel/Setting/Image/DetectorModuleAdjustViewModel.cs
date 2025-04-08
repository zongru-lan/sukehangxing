using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services.DataProcess;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Image
{
    public class DetectorModuleAdjustViewModel : ViewModelBase
    {
        private string _machineType;
        public string MachineType { get { return _machineType; } set { _machineType = value; RaisePropertyChanged(); } }

        private Bitmap _showBitmap;
        public Bitmap ShowBitmap { get { return _showBitmap; } set { _showBitmap = value; RaisePropertyChanged(); } }

        private int _selectedView = 0;
        public int SelectedView { get { return _selectedView; } set { _selectedView = value; RaisePropertyChanged(); } }

        private ObservableCollection<int> _viewList;
        public ObservableCollection<int> ViewList { get { return _viewList; } set { _viewList = value; RaisePropertyChanged(); } }

        private ObservableCollection<ModuleInfo> _listModulesInfo;
        public ObservableCollection<ModuleInfo> ListModulesInfo { get { return _listModulesInfo; } set { _listModulesInfo = value; RaisePropertyChanged(); } }

        public RelayCommand ImageViewChangedEventCommand { get; set; }
        public RelayCommand<ModuleInfo> SelectionChangedCommand { get; set; }
        public RelayCommand SaveModuleOffsetCommand { get; set; }

        private XRayImageProcessor _processor;
        private List<List<ModuleInfo>> _listViewModulesInfo;
        private XRayScanlinesImage _xrayImage;
        private int[] _modulesView;
        private List<string> _viewDislocationOffsets;
        private int[] _viewCardsDist;

        public DetectorModuleAdjustViewModel(string filePath)
        {
            _viewDislocationOffsets = new List<string>();
            _modulesView = new int[2];
            _viewCardsDist = new int[2];
            if (ScannerConfig.Read(ConfigPath.MachineView1DislocationOffsets, out string offsets1))
                _viewDislocationOffsets.Add(offsets1);
            if (ScannerConfig.Read(ConfigPath.MachineView2DislocationOffsets, out string offsets2))
                _viewDislocationOffsets.Add(offsets2);
            if (ExchangeDirectionConfig.Service.GetView1CardsDist(out int[] cardsDist))
            {
                _viewCardsDist[0] = cardsDist.Sum();
            }
            else
            {
                _viewCardsDist[0] = 0;
            }
            if (ExchangeDirectionConfig.Service.GetView2CardsDist(out cardsDist))
            {
                _viewCardsDist[1] = cardsDist.Sum();
            }
            else
            {
                _viewCardsDist[1] = 0;
            }
            ViewList = new ObservableCollection<int>();
            _xrayImage = XRayScanlinesImage.LoadFromDiskFile(filePath);
            for (int i = 0; i < _xrayImage.ViewsCount; i++)
            {
                ViewList.Add(i);
            }
            ImageViewChangedEventCommand = new RelayCommand(ImageViewChangedExecute);
            SelectionChangedCommand = new RelayCommand<ModuleInfo>(SelectionChangedExecute);
            SaveModuleOffsetCommand = new RelayCommand(SaveModuleOffsetExecute);
            _processor = new XRayImageProcessor();
            _listViewModulesInfo = new List<List<ModuleInfo>>();
        }

        private void SaveModuleOffsetExecute()
        {
            var array = ListModulesInfo.Select(a=>a.ModuleOffset).ToArray();
            DislocationCorrector corrector = new DislocationCorrector(64, array);
            ImageViewData viewData = null;
            if (SelectedView == 0)
                viewData = _xrayImage.View1Data;
            else
                viewData = _xrayImage.View2Data;
            var lineArr = corrector.GetCorrectedLineList(viewData.ScanLines);
            ImageViewData newViewData = new ImageViewData(lineArr, viewData.ViewIndex, viewData.ChannelsCount, viewData.StartChannelNumber, viewData.VerticalScale);
            _processor.AttachImageData(newViewData);
            var bmp = _processor.GetBitmap();
            if (bmp != null)
                ShowBitmap = bmp;
            if (SelectedView == 0)
                ScannerConfig.Write(ConfigPath.MachineView1DislocationOffsets, string.Join(",", array));
            else
                ScannerConfig.Write(ConfigPath.MachineView2DislocationOffsets, string.Join(",", array));
        }

        private void SelectionChangedExecute(ModuleInfo info)
        {
            if (info == null)
            {
                return;
            }
            var module = info;
            //中间模块为基准模块
            if (module.ModuleId != _modulesView[SelectedView] / 2)
            {

            }
        }

        private void ImageViewChangedExecute()
        {
            UpdateViewOffsetRegion();
        }

        public void UpdateViewOffsetRegion()
        {
            //if(SelectedView == 0)
            //    _modulesView[SelectedView] = _xrayImage.View1Data.ScanLineLength / 64;
            //else
            //    _modulesView[SelectedView] = _xrayImage.View1Data.ScanLineLength / 64;
            _modulesView[SelectedView] = _viewCardsDist[SelectedView];
            int[] offsets = new int[_modulesView[SelectedView]];
            List<ModuleInfo> moduleInfos = new List<ModuleInfo>();
            if (_viewDislocationOffsets.Count > SelectedView && !string.IsNullOrWhiteSpace(_viewDislocationOffsets[SelectedView]))
            {
                var splitOffsetsArr = _viewDislocationOffsets[SelectedView].Split(',');
                for (int i = 0; i < _modulesView[SelectedView]; i++)
                {
                    offsets[i] = int.Parse(splitOffsetsArr[i]);
                    moduleInfos.Add(new ModuleInfo(i, offsets[i]));
                }
            }
            if(moduleInfos.Count == 0)
            {
                for (int i = 0; i < _modulesView[SelectedView]; i++)
                {
                    moduleInfos.Add(new ModuleInfo(i));
                }
            }
            ListModulesInfo = new ObservableCollection<ModuleInfo>(moduleInfos);
            if (SelectedView >= _listViewModulesInfo.Count - 1)
                _listViewModulesInfo.Add(moduleInfos);
            else
                _listViewModulesInfo[SelectedView] = moduleInfos;
            var corrector = new DislocationCorrector(64, offsets);
            ImageViewData viewData = null;
            if (SelectedView == 0)
                viewData = _xrayImage.View1Data;
            else
                viewData = _xrayImage.View2Data;
            var lineArr = corrector.GetCorrectedLineList(viewData.ScanLines);
            ImageViewData newViewData = new ImageViewData(lineArr, viewData.ViewIndex, viewData.ChannelsCount, viewData.StartChannelNumber, viewData.VerticalScale);
            _processor.AttachImageData(newViewData);
            var bmp = _processor.GetBitmap();
            if (bmp != null)
                ShowBitmap = bmp;
        }
    }

    public class ModuleInfo
    {
        public int ModuleId { get; set; }
        public int ModuleOffset { get; set; }

        public ModuleInfo(int id,int offset = 0)
        {
            ModuleId = id;
            ModuleOffset = offset;
        }
    }
}

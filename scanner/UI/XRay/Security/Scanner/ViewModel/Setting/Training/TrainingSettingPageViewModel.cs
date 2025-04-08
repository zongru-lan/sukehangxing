using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Gui.Framework;
using UI.XRay.Flows.Services;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Training
{
    /// <summary>
    /// 培训功能设置页面的视图模型
    /// </summary>
    public class TrainingSettingPageViewModel : PageViewModelBase
    {
        public RelayCommand SelectedItemChangedEventCommand { get; private set; }

        /// <summary>
        /// 开始培训命令
        /// </summary>
        public RelayCommand StartTrainingCommand { get; private set; }

        /// <summary>
        /// 结束培训命令
        /// </summary>
        public RelayCommand EndTrainingCommand { get; private set; }

        /// <summary>
        /// 图像模拟间隔：单位为秒
        /// </summary>
        public List<int> SimulationIntervalsList { get; set; }

        public List<ValueStringItem<TrainingImageLoopMode>> LoopModeList { get; set; }

        private TrainingSettingController _controller;

        /// <summary>
        /// 当前选中的模拟间隔
        /// </summary>
        public int SelectedInterval
        {
            get { return _controller.Interval; }
            set { _controller.Interval = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前选中的循环模式
        /// </summary>
        public TrainingImageLoopMode SelectedLoopMode
        {
            get { return _controller.LoopMode; }
            set { _controller.LoopMode = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 开始培训按钮及相关设置是否可见
        /// </summary>
        public Visibility StartTrainingVisibility
        {
            get { return _controller.IsTrainingStarted ? Visibility.Collapsed : Visibility.Visible; }
        }

        /// <summary>
        /// 结束培训按钮是否可见
        /// </summary>
        public Visibility EndTrainingButtonVisibility
        {
            get { return _controller.IsTrainingStarted ? Visibility.Visible : Visibility.Collapsed; }
        }

        public TrainingSettingPageViewModel()
        {
            SelectedItemChangedEventCommand = new RelayCommand(SelectedItemChangedEventCommandExecute);
            StartTrainingCommand = new RelayCommand(StartTrainingCommandExecute);
            EndTrainingCommand = new RelayCommand(EndTrainingCommandExecute);

            SimulationIntervalsList = new List<int>(60);
            for (int i = 0; i < 61; i++)
            {
                SimulationIntervalsList.Add(i);
            }

            LoopModeList = new List<ValueStringItem<TrainingImageLoopMode>>(3);
            LoopModeList.Add(new ValueStringItem<TrainingImageLoopMode>(TrainingImageLoopMode.NoLoop,
                TranslationService.FindTranslation(TrainingImageLoopMode.NoLoop)));
            LoopModeList.Add(new ValueStringItem<TrainingImageLoopMode>(TrainingImageLoopMode.SequentialLoop,
                TranslationService.FindTranslation(TrainingImageLoopMode.SequentialLoop))); 
            LoopModeList.Add(new ValueStringItem<TrainingImageLoopMode>(TrainingImageLoopMode.RandomLoop,
                TranslationService.FindTranslation(TrainingImageLoopMode.RandomLoop)));

            _controller = new TrainingSettingController();
            _controller.LoadSettings();
        }

        /// <summary>
        /// 开始培训
        /// </summary>
        private void StartTrainingCommandExecute()
        {
            _controller.IsTrainingStarted = true;
            _controller.SaveSettings();

            RaisePropertyChanged("StartTrainingVisibility");
            RaisePropertyChanged("EndTrainingButtonVisibility");
        }

        /// <summary>
        /// 结束培训
        /// </summary>
        private void EndTrainingCommandExecute()
        {
            _controller.IsTrainingStarted = false;
            _controller.SaveSettings();

            RaisePropertyChanged("StartTrainingVisibility");
            RaisePropertyChanged("EndTrainingButtonVisibility");
            MessengerInstance.Send(new MotorDirectionMessage(MotorDirection.Stop));
            bool x = true;  //yxc
            Transmission.emergency2(x);//yxc
        }

        /// <summary>
        /// ComboBox选项发生变化
        /// </summary>
        private void SelectedItemChangedEventCommandExecute()
        {
            _controller.SaveSettings();
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
        }
    }
}

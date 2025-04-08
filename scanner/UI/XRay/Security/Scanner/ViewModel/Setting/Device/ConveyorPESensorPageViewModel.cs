using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
//using UI.XRay.Control;
using UI.Common.Tracers;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using PESensorState = UI.XRay.Gui.Widgets.PESensorState;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Algo;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Device
{
    public class ConveyorPESensorPageViewModel : PageViewModelBase
    {
        #region commands
        public RelayCommand MoveLeftCommand { get; private set; }
        public RelayCommand MoveRightCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }

        public RelayCommand PowerOnCommand { get; private set; }

        public RelayCommand PowerOffCommand { get; private set; }

        public RelayCommand SaveConveyorSpeed { get; private set; }

        public PESensorState Pes1WidgetState
        {
            get { return _pes1WidgetState; }
            set { _pes1WidgetState = value; RaisePropertyChanged(); }
        }

        public PESensorState Pes3WidgetState
        {
            get { return _pes3WidgetState; }
            set { _pes3WidgetState = value; RaisePropertyChanged(); }
        }

        #endregion commands

        private PESensorState _pes1WidgetState;

        private PESensorState _pes3WidgetState;

        private bool _isExchangePESensor;

        public ConveyorPESensorPageViewModel()
        {
            CreateCommands();

            try
            {
                ControlService.ServicePart.PESensorStateChanged += ServicePartOnPeSensorStateChanged;

                // 进入光障电机测试页，切换至维护模式，可以单独测试光障和电机
                ControlService.ServicePart.SetWorkMode(ScannerWorkMode.Maintenance);

                //由于光电传感器接线改动，默认是开启
                ControlService.ServicePart.PowerOnPESensors(false);

                _isExchangePESensor = ExchangeDirectionConfig.Service.IsExchangePeSensor;
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
            LoadConveyorParam();
        }

        private void ServicePartOnPeSensorStateChanged(object sender, PESensorStateChangedEventArgs args)
        {
            if (!_isExchangePESensor)
            {
                if ((args.PESensor & PESensorIndex.PESensor1) == PESensorIndex.PESensor1)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Pes1WidgetState = ConvertState(args.NewState);
                    });
                }
                else if ((args.PESensor & PESensorIndex.PESensor3) == PESensorIndex.PESensor3)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Pes3WidgetState = ConvertState(args.NewState);
                    });
                }
            }
            else
            {
                if ((args.PESensor & PESensorIndex.PESensor1) == PESensorIndex.PESensor1)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Pes3WidgetState = ConvertState(args.NewState);
                    });
                }
                else if ((args.PESensor & PESensorIndex.PESensor3) == PESensorIndex.PESensor3)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Pes1WidgetState = ConvertState(args.NewState);
                    });
                }
            }
            
        }

        /// <summary>
        /// 将光电传感器的状态，转换为光电传感器窗体的状态
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private PESensorState ConvertState(Control.PESensorState state)
        {
            switch (state)
            {
                //case Control.PESensorState.Broken:
                //    return PESensorWidgetState.Broken;

                case Control.PESensorState.DetectObject:
                    return PESensorState.DetectObject;
                    
                   case Control.PESensorState.PowerOff:
                    return PESensorState.PowerOff;

                case Control.PESensorState.Ready:
                    return PESensorState.Ready;
            }

            return PESensorState.PowerOff;
        }

        private void CreateCommands()
        {
            MoveLeftCommand = new RelayCommand(MoveLeftCommandExecute);
            MoveRightCommand = new RelayCommand(MoveRightCommandExecute);
            StopCommand = new RelayCommand(StopCommandExecute);
            PowerOnCommand = new RelayCommand(PowerOnCommandExecute);
            PowerOffCommand = new RelayCommand(PowerOffCommandExecute);
            SaveConveyorSpeed = new RelayCommand(SaveConveyorSpeedExecute);
        }

        private void MoveLeftCommandExecute()
        {
            ConveyorController.Controller.MoveLeft();
            new OperationRecordService().RecordOperation(OperationUI.ConveyorPESensor,"Conveyor",OperationCommand.Open, "Backward");
        }

        private void MoveRightCommandExecute()
        {
            ConveyorController.Controller.MoveRight();
            new OperationRecordService().RecordOperation(OperationUI.ConveyorPESensor, "Conveyor", OperationCommand.Open, "Forward");
        }

        private void StopCommandExecute()
        {
            ConveyorController.Controller.Stop();
            new OperationRecordService().RecordOperation(OperationUI.ConveyorPESensor, "Conveyor", OperationCommand.Close, "Stop");
        }

        private void PowerOnCommandExecute()
        {
            ControlService.ServicePart.PowerOnPESensors(true);
            new OperationRecordService().RecordOperation(OperationUI.ConveyorPESensor, "PESensors", OperationCommand.Open, "True");
        }

        private void PowerOffCommandExecute()
        {
            ControlService.ServicePart.PowerOnPESensors(false);
            new OperationRecordService().RecordOperation(OperationUI.ConveyorPESensor, "PESensors", OperationCommand.Close, "False");
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {

        }

        public override void Cleanup()
        {
            try
            {
                // 页面退出时，必须切换回常规扫描模式
                ControlService.ServicePart.SetWorkMode(ScannerWorkMode.Regular);
                ControlService.ServicePart.PowerOnPESensors(false);
                ConveyorController.Controller.Stop();
                ControlService.ServicePart.PESensorStateChanged -= ServicePartOnPeSensorStateChanged;
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            base.Cleanup();
        }

        private float _conveyorSpeed;

        public float ConveyorSpeed
        {
            get { return _conveyorSpeed; }
            set
            {
                if (value < _conveyorMinSpeed)
                {
                    value = _conveyorMinSpeed;
                }
                else if (value > _conveyorMaxSpeed)
                {
                    value = _conveyorMaxSpeed;
                }
                _conveyorSpeed = value;
                RaisePropertyChanged();
            }
        }

        private bool _canChangeConveyorSpeed;

        public bool CanChangeConveyorSpeed
        {
            get
            {
                SetConveyorSpeed(_canChangeConveyorSpeed);
                return _canChangeConveyorSpeed;
            }
            set
            {
                SetConveyorSpeed(_canChangeConveyorSpeed);
                _canChangeConveyorSpeed = value;
                RaisePropertyChanged();
            }
        }

        private void SetConveyorSpeed(bool visible)
        {
            if (visible)
            {
                SetConveyorSpeedVisibility = Visibility.Visible;
            }
            else
            {
                SetConveyorSpeedVisibility = Visibility.Collapsed;
            }
        }

        private Visibility _setConveyorSpeedVisibility;

        public Visibility SetConveyorSpeedVisibility
        {
            get { return _setConveyorSpeedVisibility; }
            set { _setConveyorSpeedVisibility = value; RaisePropertyChanged(); }
        }

        float _conveyorMinSpeed;
        float _conveyorMaxSpeed;
        float lineIntegrationTime;
        float _oldConveyorSpeed;

        public void SaveConveyorSpeedExecute()
        {
            SaveConveyorParam();
        }

        void LoadConveyorParam()
        {
            if (!ScannerConfig.Read(ConfigPath.MachineConveyorSpeed, out _conveyorSpeed))
            {
                ConveyorSpeed = 0.2f;
            }
            _oldConveyorSpeed = _conveyorSpeed;

            if (!ScannerConfig.Read(ConfigPath.MachineCanChangeConveyorSpeed, out _canChangeConveyorSpeed))
            {
                CanChangeConveyorSpeed = false;
            }
            CanChangeConveyorSpeed = _canChangeConveyorSpeed;

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorMinSpeed, out _conveyorMinSpeed))
            {
                _conveyorMinSpeed = 0.1f;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorMaxSpeed, out _conveyorMaxSpeed))
            {
                _conveyorMaxSpeed = 0.5f;
            }

            if (!ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out lineIntegrationTime))
            {
                lineIntegrationTime = 5;
            }
        }

        void SaveConveyorParam()
        {
            bool boolSetResult = ControlService.ServicePart.SetConveyorSpeed(_conveyorSpeed);
            if (boolSetResult)
            {
                ScannerConfig.Write(ConfigPath.MachineConveyorSpeed, _conveyorSpeed);
                _oldConveyorSpeed = _conveyorSpeed;
                MessageBox.Show("Set Conveyor Speed Successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ConveyorSpeed = _oldConveyorSpeed;
                MessageBox.Show("Set Conveyor Speed Failed!","Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

    }
}

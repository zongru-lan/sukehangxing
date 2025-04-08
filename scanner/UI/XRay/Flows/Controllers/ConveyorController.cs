using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.Flows.Services;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 输送机控制：根据当前的配置，控制输送机的运转
    /// </summary>
    public class ConveyorController
    {
        private bool _isConveyorKeyReversed = false;

        public static ConveyorController Controller { get; private set; }

        static ConveyorController()
        {
            Controller = new ConveyorController();
        }

        protected ConveyorController()
        {
            ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
            LoadDirectionKeySetting();
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            LoadDirectionKeySetting();
        }

        private void LoadDirectionKeySetting()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.KeyboardIsConveyorKeyReversed, out _isConveyorKeyReversed))
                {
                    _isConveyorKeyReversed = false;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 按下向左转
        /// </summary>
        public bool MoveLeft()
        {
            if (_isConveyorKeyReversed)
            {
                return ControlService.ServicePart.DriveConveyor(ConveyorDirection.MoveForward);
            }

            return ControlService.ServicePart.DriveConveyor(ConveyorDirection.MoveBackward);
        }

        /// <summary>
        /// 按下向右转
        /// </summary>
        public bool MoveRight()
        {
            if (_isConveyorKeyReversed)
            {
                return ControlService.ServicePart.DriveConveyor(ConveyorDirection.MoveBackward);
            }

            return ControlService.ServicePart.DriveConveyor(ConveyorDirection.MoveForward);
        }

        /// <summary>
        /// 停止输送机
        /// </summary>
        public bool Stop()
        {
            return ControlService.ServicePart.DriveConveyor(ConveyorDirection.Stop);
        }
    }
}

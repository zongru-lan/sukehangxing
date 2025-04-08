using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 培训设置控制器
    /// </summary>
    public class TrainingSettingController
    {
        private bool _isTrainingStarted;

        private TrainingImageLoopMode _loopMode;

        private int _interval;

        public int Interval
        {
            get { return _interval; }
            set { _interval = value; }
        }

        public TrainingImageLoopMode LoopMode
        {
            get { return _loopMode; }
            set { _loopMode = value; }
        }

        public bool IsTrainingStarted
        {
            get { return _isTrainingStarted; }
            set { _isTrainingStarted = value; }
        }

        public void LoadSettings()
        {
            if (!ScannerConfig.Read(ConfigPath.TrainingIsEnabled, out _isTrainingStarted))
            {
                _isTrainingStarted = false;
            }

            if (!ScannerConfig.Read(ConfigPath.TrainingImageInterval, out _interval))
            {
                _interval = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.TrainingLoopMode, out _loopMode))
            {
                _loopMode = TrainingImageLoopMode.RandomLoop;
            }
        }

        public void SaveSettings()
        {
            ScannerConfig.Write(ConfigPath.TrainingIsEnabled, _isTrainingStarted);
            ScannerConfig.Write(ConfigPath.TrainingImageInterval, _interval);
            ScannerConfig.Write(ConfigPath.TrainingLoopMode, _loopMode) ;
        }
    }
}

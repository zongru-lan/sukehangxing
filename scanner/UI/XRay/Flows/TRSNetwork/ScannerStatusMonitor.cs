using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRayNetEntities;
using UI.XRay.Business.Entities;
using XRayNetEntities.Tools;

namespace UI.XRay.Flows.TRSNetwork
{
    class ScannerStatusMonitor
    {
        #region Instance


        private static ScannerStatusMonitor _instance;
        public static ScannerStatusMonitor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ScannerStatusMonitor();
                return _instance;
            }
        }

        public void Start()
        {
            Status = new ScannerStatus()
            {
                WorkMode = SystemStatus.Instance.WorkMode,
                StatusTime = DateTime.Now,
                XRayReady = SystemStatus.Instance.XRayReady,
            };
            StatusLocker = new object();
            SystemStatus.Instance.PropertyChanged += Instance_PropertyChanged;
        }


        public void Stop()
        {
            SystemStatus.Instance.PropertyChanged -= Instance_PropertyChanged;
        }

        public ScannerStatus Status { get; private set; }
        public object StatusLocker { get; private set; }

        #endregion

        void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            object propertyValue = SystemStatus.Instance.GetValue(e.PropertyName);
            if (propertyValue == null)
            {
                return;
            }
            lock (StatusLocker)
            {
                try
                {
                    if (e.PropertyName == "HasLogin")
                    {
                        Status.HasLogin = (bool)propertyValue;
                        Status.AccountID = string.Empty;
                        Status.StatusTime = DateTime.Now;
                    }
                    else if (e.PropertyName == "ConveyorState")
                    {

                        Status.RunDirection = (int)propertyValue;
                        Status.StatusTime = DateTime.Now;
                    }
                    else if (e.PropertyName == "CurrentUser")
                    {
                        var account = propertyValue as Account;
                        if (account == null)
                        {
                            Status.HasLogin = false;
                            Status.AccountID = string.Empty;
                        }
                        else
                        {
                            Status.AccountID = account.AccountId;
                            Status.StatusTime = DateTime.Now;
                        }
                    }
                    else if (e.PropertyName == "IsTraining")
                    {
                        Status.IsTraining = (bool)propertyValue;
                        Status.StatusTime = DateTime.Now;
                    }
                    else if (e.PropertyName == "XRayReady")
                    {
                        Status.XRayReady = (bool)propertyValue;
                        Status.StatusTime = DateTime.Now;
                    }
                    else if (e.PropertyName == "Generator1Scanning" || e.PropertyName == "Generator2Scanning")
                    {
                        if (SystemStatus.Instance.Generator1Scanning || SystemStatus.Instance.Generator2Scanning)
                            Status.IsScanning = true;
                        else
                            Status.IsScanning = false;
                    }
                    else if (e.PropertyName == "WorkMode")
                    {
                        Status.WorkMode = (int)propertyValue;
                        Status.StatusTime = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Exception(ex);
                }
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using UI.Common.Tracers;

namespace UI.XRay.Business.Entities
{
    [Serializable]
    public class SystemStatus : System.ComponentModel.INotifyPropertyChanged
    {
        #region Instance & Constructor
        private static SystemStatus _instance;
        private static object _statusLocker = new object();

        public static SystemStatus Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SystemStatus();
                return _instance;
            }
        }

        public SystemStatus()
        {

        }
        #endregion

        #region Properties & Fields
        private bool _ctrlBoardStatus;

        /// <summary>
        /// 控制板连接状态
        /// </summary>
        public bool CtrlBoardStatus
        {
            get { return _ctrlBoardStatus; }
            set
            {
                if (_ctrlBoardStatus.Equals(value))
                    return;
                _emergencyStop = value;
                OnPropertyChanged();
            }
        }

        private string _ctrlBoardVersion;
        /// <summary>
        /// 控制板版本
        /// </summary>
        public string CtrlBoardVersion
        {
            get { return _ctrlBoardVersion; }
            set
            {
                if (_ctrlBoardVersion != null && _ctrlBoardVersion.Equals(value))
                    return;
                _ctrlBoardVersion = value;
                OnPropertyChanged();
            }
        }

        private float _ctrlBoardTemperature;
        /// <summary>
        /// 控制板温度
        /// </summary>
        public float CtrlBoardTemperature
        {
            get { return _ctrlBoardTemperature; }
            set
            {
                if (CtrlBoardTemperature.Equals(value))
                    return;
                _ctrlBoardTemperature = value;
                OnPropertyChanged();
            }
        }

        private bool _emergencyStop;
        /// <summary>
        /// 急停
        /// 拍下急停的情况下是false，正常情况下是true
        /// </summary>
        public bool EmergencyStop
        {
            get { return _emergencyStop; }
            set
            {
                if (_emergencyStop.Equals(value))
                    return;
                _emergencyStop = value; OnPropertyChanged();
            }
        }

        private bool _generator1Online;
        /// <summary>
        /// 射线源1在线
        /// </summary>
        public bool Generator1Online
        {
            get { return _generator1Online; }
            set
            {
                if (_generator1Online.Equals(value))
                    return;
                _generator1Online = value; OnPropertyChanged();
            }
        }

        private bool _generator1Scanning;
        /// <summary>
        /// 射线源1出束
        /// </summary>
        public bool Generator1Scanning
        {
            get { return _generator1Scanning; }
            set
            {
                if (_generator1Scanning.Equals(value))
                    return;
                _generator1Scanning = value; OnPropertyChanged();
            }
        }

        private string _generatorType;
        /// <summary>
        /// 数字板型号
        /// </summary>
        public string GeneratorType
        {
            get { return _generatorType; }
            set
            {
                _generatorType = value; OnPropertyChanged();
            }
        }


        private float _generator1Voltage;
        /// <summary>
        /// 射线源1电压
        /// </summary>
        public float Generator1Voltage
        {
            get { return _generator1Voltage; }
            set
            {
                if (_generator1Voltage.Equals(value))
                    return;
                _generator1Voltage = value; OnPropertyChanged();
            }
        }


        private float _generator1Current;
        /// <summary>
        /// 射线源1电流
        /// </summary>
        public float Generator1Current
        {
            get { return _generator1Current; }
            set
            {
                if (_generator1Current.Equals(value))
                    return;
                _generator1Current = value; OnPropertyChanged();
            }
        }

        private int _generator1ErrorCode;
        /// <summary>
        /// 射线源1错误码
        /// </summary>
        public int Generator1ErrorCode
        {
            get { return _generator1ErrorCode; }
            set
            {
                if (_generator1ErrorCode.Equals(value))
                    return;
                _generator1ErrorCode = value; OnPropertyChanged();
            }
        }


        private bool _generator2Online;
        /// <summary>
        /// 射线源2在线
        /// </summary>
        public bool Generator2Online
        {
            get { return _generator2Online; }
            set
            {
                if (_generator2Online.Equals(value))
                    return;
                _generator2Online = value; OnPropertyChanged();
            }
        }

        private bool _generator2Scanning;

        /// <summary>
        /// 射线源2出束
        /// </summary>
        public bool Generator2Scanning
        {
            get { return _generator2Scanning; }
            set
            {
                if (_generator2Scanning.Equals(value))
                    return;
                _generator2Scanning = value; OnPropertyChanged();
            }
        }


        private float _generator2Voltage;
        /// <summary>
        /// 射线源2电压
        /// </summary>
        public float Generator2Voltage
        {
            get { return _generator2Voltage; }
            set
            {
                if (_generator2Voltage.Equals(value))
                    return;
                _generator2Voltage = value; OnPropertyChanged();
            }
        }


        private float _generator2Current;
        /// <summary>
        /// 射线源2电流
        /// </summary>
        public float Generator2Current
        {
            get { return _generator2Current; }
            set
            {
                if (_generator2Current.Equals(value))
                    return;
                _generator2Current = value; OnPropertyChanged();
            }
        }

        private int _generator2ErrorCode;
        /// <summary>
        /// 射线源2错误码
        /// </summary>
        public int Generator2ErrorCode
        {
            get { return _generator2ErrorCode; }
            set
            {
                if (_generator2ErrorCode.Equals(value))
                    return;
                _generator2ErrorCode = value; OnPropertyChanged();
            }
        }


        private int _workMode;
        /// <summary>
        /// 工作模式
        /// </summary>
        public int WorkMode
        {
            get { return _workMode; }
            set
            {
                if (_workMode.Equals(value))
                    return;
                _workMode = value; OnPropertyChanged();
            }
        }


        private float _conveyorSpeed;
        /// <summary>
        /// 输送带速度
        /// </summary>
        public float ConveyorSpeed
        {
            get { return _conveyorSpeed; }
            set
            {
                if (_conveyorSpeed.Equals(value))
                    return;
                _conveyorSpeed = value; OnPropertyChanged();
            }
        }

        private int _conveyorState;
        /// <summary>
        /// Stop = 0,        MoveForward = 1,        MoveBackward = 2,
        /// </summary>
        public int ConveyorState
        {
            get { return _conveyorState; }
            set
            {
                if (_conveyorState.Equals(value))
                {
                    return;
                }
                _conveyorState = value; OnPropertyChanged();
            }
        }


        private string _machineModel;
        /// <summary>
        /// 机器型号
        /// </summary>
        public string MachineModel
        {
            get { return _machineModel; }
            set
            {
                if (_machineModel != null && _machineModel.Equals(value))
                    return;
                _machineModel = value; OnPropertyChanged();
            }
        }

        private string _machineNumber;
        /// <summary>
        /// 机器编号
        /// </summary>
        public string MachineNumber
        {
            get { return _machineNumber; }
            set
            {
                _machineNumber = value; OnPropertyChanged();
            }
        }


        private string _appVersion;
        /// <summary>
        /// 软件版本
        /// </summary>
        public string AppVersion
        {
            get { return _appVersion; }
            set
            {
                if (_appVersion != null && _appVersion.Equals(value))
                    return;
                _appVersion = value; OnPropertyChanged();
            }
        }

        private string _algoVersion;
        /// <summary>
        /// 算法版本
        /// </summary>
        public string AlgoVersion
        {
            get { return _algoVersion; }
            set
            {
                _algoVersion = value; OnPropertyChanged();
            }
        }


        private double _diskRemainingSpace;
        /// <summary>
        /// 硬盘剩余空间（GB）
        /// </summary>
        public double DiskRemainingSpace
        {
            get { return _diskRemainingSpace; }
            set
            {
                if (_diskRemainingSpace.Equals(value))
                    return;
                _diskRemainingSpace = value; OnPropertyChanged();
            }
        }

        private bool _GCUConnection;
        /// <summary>
        /// 采集板连接
        /// </summary>
        public bool GCUConnection
        {
            get { return _GCUConnection; }
            set
            {
                if (_GCUConnection.Equals(value))
                    return;
                _GCUConnection = value; OnPropertyChanged();
            }
        }

        private string _captureType;
        /// <summary>
        /// 探测板类型
        /// </summary>
        public string CaptureType
        {
            get { return _captureType; }
            set
            {
                _captureType = value; OnPropertyChanged();
            }
        }


        private int _backgroundCalibration;
        /// <summary>
        /// 本地校正
        /// </summary>
        public int BackgroundCalibration
        {
            get { return _backgroundCalibration; }
            set
            {
                if (_backgroundCalibration.Equals(value))
                    return;
                _backgroundCalibration = value; OnPropertyChanged();
            }
        }

        private float _integration;
        /// <summary>
        /// 采样时间
        /// </summary>
        public float LineIntegration
        {
            get { return _integration; }
            set
            {
                _integration = value; OnPropertyChanged();
            }
        }


        private int _fullnessCalibration;
        /// <summary>
        /// 满度校正
        /// </summary>
        public int FullnessCalibration
        {
            get { return _fullnessCalibration; }
            set
            {
                if (_fullnessCalibration.Equals(value))
                    return;
                _fullnessCalibration = value; OnPropertyChanged();
            }
        }

        private int _calibrationResult;
        /// <summary>
        /// 校正结果
        /// </summary>
        public int CalibrationResult
        {
            get { return _calibrationResult; }
            set
            {
                if (_calibrationResult.Equals(value))
                    return;
                _calibrationResult = value; OnPropertyChanged();
            }
        }


        private Account _currentUser;
        /// <summary>
        /// 当前用户角色
        /// </summary>
        public Account CurrentUser
        {
            get { return _currentUser; }
            set
            {
                if (_currentUser != null && _currentUser.Equals(value))
                    return;
                _currentUser = value; OnPropertyChanged();
            }
        }

        private bool _hasLogin;
        /// <summary>
        /// 是否处于登录状态
        /// </summary>
        public bool HasLogin
        {
            get { return _hasLogin; }
            set
            {
                if (_hasLogin.Equals(value))
                    return;
                _hasLogin = value; OnPropertyChanged();
            }
        }

        private bool _isTraining;
        /// <summary>
        /// 是否培训模式
        /// </summary>
        public bool IsTraining
        {
            get { return _isTraining; }
            set
            {
                if (_isTraining.Equals(value))
                    return;
                _isTraining = value; OnPropertyChanged();
            }
        }


        private bool _xrayReady;
        /// <summary>
        /// 是否处于就绪状态(可以正常过包)
        /// </summary>
        public bool XRayReady
        {
            get { return _xrayReady; }
            set
            {
                if (_xrayReady.Equals(value))
                    return;
                _xrayReady = value; OnPropertyChanged();
            }
        }

        private bool _canDiagnosis;
        /// <summary>
        /// 是否处于可诊断状态
        /// </summary>
        public bool CanDiagnosis
        {
            get { return _canDiagnosis; }
            set
            {
                if (_canDiagnosis.Equals(value))
                    return;
                _canDiagnosis = value; OnPropertyChanged();
            }
        }

        private bool _calibrationSucceed;

        public bool CarlibrationSucceed
        {
            get { return _calibrationSucceed; }
            set
            {
                if (_calibrationSucceed.Equals(value))
                    return;
                _calibrationSucceed = value; OnPropertyChanged();
            }
        }

        #region 网络连接状态
        private byte _networkState = 0b00000000;
        /// <summary>
        /// 总长1字节，用于存放网络状态信息<br />
        /// 按照76543210的顺序，每位功能为:<br />
        /// 0位: FTP服务器连接状态<br />
        /// 1位: innplc连接状态<br />
        /// 2位: outplc连接状态<br />
        /// 3位: 数据库连接状态<br />
        /// 4位: 数据库服务器连接状态<br />
        /// 5-7位: 预留
        /// </summary>
        public byte NetworkState
        {
            private get
            {
                return _networkState;
            }
            set
            {
                if (value != _networkState)
                {
                    _networkState = value;
                    Tracer.TraceInfo($"[NetState] State changed from {Convert.ToString(_networkState, 2)} to {Convert.ToString(value, 2)}");
                }
            }
        }

        /// <summary>FTP服务器连接状态</summary>
        public bool IsFTPServerConnected
        {
            get
            {
                return (NetworkState & 0b00000001) == 0b00000001;
            }
        }

        // TODO: 我也不知道是啥，待会有空的话没准去问
        /// <summary>innplc连接状态</summary>
        public bool IsInnPLCConnected
        {
            get
            {
                return (NetworkState & 0b00000010) == 0b00000010;
            }
        }

        // TODO: 我也不知道是啥，待会有空的话没准去问
        /// <summary>outplc连接状态</summary>
        public bool IsOutPLCConnected
        {
            get
            {
                return (NetworkState & 0b00000100) == 0b00000100;
            }
        }

        /// <summary>数据库连接状态</summary>
        public bool IsDatabaseConnected
        {
            get
            {
                return (NetworkState & 0b00001000) == 0b00001000;
            }
        }

        /// <summary>数据库服务器连接状态</summary>
        public bool IsDatabaseServerConnected
        {
            get
            {
                return (NetworkState & 0b00010000) == 0b00010000;
            }
        }
        #endregion

        #endregion

        #region Event
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;


        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
            {
                return;
            }
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public object GetValue(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return null;
            }
            lock (_statusLocker)
            {
                try
                {
                    return this.GetType()
                        .GetProperty(propertyName)
                        .GetValue(this);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}

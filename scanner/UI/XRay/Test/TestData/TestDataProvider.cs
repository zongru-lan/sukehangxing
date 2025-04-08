using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ImageProcessor.Common.Tiff;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using System.IO;

namespace UI.XRay.Test.TestData
{
    /// <summary>
    /// 测试数据提供者，从TestData文件夹中获取测试数据，按照不同的时机发送本底，满度，图像数据。
    /// 
    /// 测试数据是DT工具采集的原始高低能数据，分为满度，本底和图像三个Tiff文件，其中Tiff文件第一层是低能，第二层是高能
    /// 
    /// </summary>
    public class TestDataProvider
    {
        public enum Status
        {
            MotoStop,
            MotoStart,
            XrayOn,
            ImageCome,
            ImageEnd,
            XrayOff
        }

        /// <summary>
        /// 模拟的射线状态
        /// </summary>
        public enum SimuXRayState
        {
            On,
            Off
        }

        /// <summary>
        /// 模拟电机状态，没有后退一说
        /// </summary>
        public enum SimuMotorState
        {
            Moveforward,
            Stop
        }

        /// <summary>
        /// 模拟入口光障的状态
        /// </summary>
        public enum SimuEnterSensorState
        {
            Trigger,
            NotTrigger
        }

        private string GroundTiffPath = @"..\TestData\Ground.Tiff";
        private string AirTiffPath = @"..\TestData\Air.Tiff";
        private string ImageTiffPath = @"..\TestData\Image.Tiff";

        private readonly List<RawScanlineDataBundle> _grounds = new List<RawScanlineDataBundle>();
        private readonly List<RawScanlineDataBundle> _airs = new List<RawScanlineDataBundle>();
        private readonly List<RawScanlineDataBundle> _images = new List<RawScanlineDataBundle>();

        private bool _canStartServer = false;

        private int _viewCount;
        private bool _isDualEnergy;

        private Status _currentStatus = Status.MotoStop;

        public event EventHandler<RawScanlineDataBundle> SimuScanlineCaptured;
        public event EventHandler<SimuXRayState> SimuXRayStateChanged;
        public event EventHandler<SimuMotorState> SimuMotorStateChanged;

        public TestDataProvider()
        {
            GroundTiffPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\TestData\Ground.Tiff");
            AirTiffPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\TestData\Air.Tiff");
            ImageTiffPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\TestData\Image.Tiff");

            InitialParameters();
            //初始化数据
            InitialData();
        }

        private void InitialParameters()
        {
            ScannerConfig.Read(ConfigPath.ImagesCount, out _viewCount);
            ScannerConfig.Read(ConfigPath.CaptureSysIsDualEnergy, out _isDualEnergy);
        }

        //开始数据发送流程，在点击电机键后启动
        public void MotoStart()
        {
            //数据加载失败，不开始程序
            if (!_canStartServer)
                return;

            if (_exit)
            {
                _exit = false;
                _startMotorTask.Wait();
                return;
            }

            _startMotorTask = Task.Run(() =>
            {
                if (!_groundUpdated)
                {
                    FireGrounds();
                    Thread.Sleep(8000);
                    _groundUpdated = true;
                }

                StartMotor();
            });
        }

        private Task _startMotorTask;

        private bool _exit;

        private bool _groundUpdated;

        private void StartMotor()
        {
            _exit = !_exit;
            while (_exit)
            {
                //模拟电机信号
                FireMotorState(SimuMotorState.Moveforward);
                FireGrounds();
                FireXRayState(SimuXRayState.On);
                FireAir();
                FireImages();
                FireAir();
                FireXRayState(SimuXRayState.Off);
                FireMotorState(SimuMotorState.Stop);

                Thread.Sleep(15);
            }
        }

        private void FireGrounds()
        {
            for (int i = _grounds.Count /2; i < _grounds.Count; i++)
            {
                FireScanline(_grounds[i]);
                Thread.Sleep(3);
            }
        }

        private void FireAir()
        {
            for (int i = _airs.Count/2; i < _airs.Count; i++)
            {
                FireScanline(_airs[i]);
                Thread.Sleep(3);
            }
        }

        private void FireImages()
        {
            foreach (var line in _images)
            {
                FireScanline(line);
                Thread.Sleep(3);
            }
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitialData()
        {
            try
            {
                InitialData(GroundTiffPath, _grounds);
                InitialData(AirTiffPath, _airs);
                InitialData(ImageTiffPath, _images);

                _canStartServer = true;
            }
            catch (Exception e)
            {
                _canStartServer = false;
                Tracer.TraceException(e);
            }
        }

        /// <summary>
        /// toInitialData必须初始化
        /// </summary>
        /// <param name="path"></param>
        /// <param name="toInitialData"></param>
        private void InitialData(string path, List<RawScanlineDataBundle> toInitialData)
        {
            var data = TiffHelper.ReadDoubleByteTiff(path);

            if (data.Count == 0)
            {
                throw new ArgumentException("data");
            }
            //考虑只有低能的情况，不过默认是高低能图像
            int imageHeight = data[0].Height;

            for (int i = 0; i < imageHeight; i++)
            {
                var low = data[0].UshortBuffer[i];
                ushort[] high = null;
                if (data.Count > 1 && _isDualEnergy)
                {
                    high = data[1].UshortBuffer[i];
                }


                var rawScanlineDataView1 = new RawScanlineData(DetectViewIndex.View1, low, high);
                RawScanlineData rawScanlineDataView2 = null;
                if (_viewCount == 2)
                {
                    rawScanlineDataView2 = new RawScanlineData(DetectViewIndex.View2, low, high);
                }

                toInitialData.Add(new RawScanlineDataBundle(rawScanlineDataView1, rawScanlineDataView2));
            }
        }

        private void FireScanline(RawScanlineDataBundle bundle)
        {
            if (bundle != null)
            {
                var handler = SimuScanlineCaptured;
                if (handler != null)
                {
                    handler.BeginInvoke(this, CloneRawData(bundle),null,null);
                }
            }
        }

        private void FireXRayState(SimuXRayState state)
        {
            var handler = SimuXRayStateChanged;
            if (handler != null)
            {
                handler.Invoke(this, state);
            }
        }

        private void FireMotorState(SimuMotorState state)
        {
            var handler = SimuMotorStateChanged;
            if (handler != null)
            {
                handler.Invoke(this, state);
            }
        }

        private RawScanlineDataBundle CloneRawData(RawScanlineDataBundle bundle)
        {
            return  new RawScanlineDataBundle(CloneViewData(bundle.View1RawData),
                CloneViewData(bundle.View2RawData));
        }

        private RawScanlineData CloneViewData(RawScanlineData viewData)
        {
            if (viewData == null)
                return null;

            ushort[] le = null, he = null;

            var pixelCount = viewData.LineLength*sizeof (ushort);

            if (viewData.Low != null)
            {
                le = new ushort[viewData.LineLength];
                Buffer.BlockCopy(viewData.Low, 0, le, 0, pixelCount);
            }
            if (viewData.High != null)
            {
                he = new ushort[viewData.LineLength];
                Buffer.BlockCopy(viewData.High, 0, he, 0, pixelCount);
            }
            return new RawScanlineData(viewData.ViewIndex, le, he);
        }
    }
}

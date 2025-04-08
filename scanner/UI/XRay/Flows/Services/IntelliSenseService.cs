using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 智能识别服务
    /// </summary>
    public class IntelliSenseService 
    {
        /// <summary>
        /// 高密度检测
        /// </summary>
        private IntelliSenseOperator _hdiOperator;

        /// <summary>
        /// 毒品检测
        /// </summary>
        private IntelliSenseOperator _drugOperator;

        /// <summary>
        /// 爆炸物将检测
        /// </summary>
        private IntelliSenseOperator _explosiveOperator;
        
        public IntelliSenseService() 
        {
            ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;

            try
            {
                InitHdiOperator();
                InitDeiOperators();
                InitEiOperator();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            InitHdiOperator();
            InitDeiOperators();
            InitEiOperator();
        }

        /// <summary>
        /// 初始化高密度检测算子
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void InitHdiOperator()
        {
            bool hdiEnabled;
            if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiEnabled, out hdiEnabled))
            {
                hdiEnabled = true;
            }

            if (_hdiOperator != null)
            {
                _hdiOperator.RegionDetected -= HdiOperatorRegionDetected;
                _hdiOperator = null;
            }

            if (hdiEnabled)
            {
                int sensitivity;
                if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiSensitivity, out sensitivity))
                {
                    sensitivity = 3;
                }

                ushort unpenetratableTh;
                if (!ScannerConfig.Read(ConfigPath.PreProcUnpenetratableUpper, out unpenetratableTh))
                {
                    unpenetratableTh = 3000;
                }

                int sizeTh, pointsTh;
                GetHdiThresholdsFromSensitivity(sensitivity, out sizeTh, out pointsTh);


                _hdiOperator = new IntelliSenseOperator((x, m) => x <= unpenetratableTh,
                    MarkerRegionType.UnPenetratable, sizeTh, pointsTh);

                _hdiOperator.RegionDetected += HdiOperatorRegionDetected;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void InitDeiOperators()
        {
            int sensitivity;
            if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiSensitivity, out sensitivity))
            {
                sensitivity = 3;
            }

            if (_drugOperator != null)
            {
                _drugOperator.RegionDetected -= DeiOperatorRegionDetected;
                _drugOperator = null;
            }

            bool deiEnabled;
            if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiEnabled, out deiEnabled))
            {
                deiEnabled = true;
            }

            if (deiEnabled)
            {
                InitDrugOperator(sensitivity);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void InitEiOperator()
        {
            int sensitivity;
            if (!ScannerConfig.Read(ConfigPath.IntellisenseEiSensitivity, out sensitivity))
            {
                sensitivity = 3;
            }

            if (_explosiveOperator != null)
            {
                _explosiveOperator.RegionDetected += DeiOperatorRegionDetected;
                _explosiveOperator = null;
            }

            bool eiEnabled;
            if (!ScannerConfig.Read(ConfigPath.IntellisenseEiEnabled, out eiEnabled))
            {
                eiEnabled = true;
            }

            if (eiEnabled)
            {
                InitExplosivesOperator(sensitivity);
            }
        }

        /// <summary>
        /// 初始化毒品检测算子
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void InitDrugOperator(int sensitivity)
        {
            int sizeTh, pointsTh;
            GetDeiThresholdsFromSensitivity(sensitivity, out sizeTh, out pointsTh);

            if (_drugOperator != null)
            {
                _drugOperator.RegionDetected -= DeiOperatorRegionDetected;
            }

            float drugLowZ, drugHighZ;
            ushort drugHighX;
            if (!ScannerConfig.Read(ConfigPath.IntellisenseDrugLowZ, out drugLowZ))
            {
                drugLowZ = 8.5f;
            }

            if (!ScannerConfig.Read(ConfigPath.IntellisenseDrugHighZ, out drugHighZ))
            {
                drugHighZ = 9.5f;
            }

            if (!ScannerConfig.Read(ConfigPath.IntellisenseDrugHighX,out drugHighX))
            {
                drugHighX = 40000;
            }

            _drugOperator = new IntelliSenseOperator((x, m) => (m >= drugLowZ && m <= drugHighZ && x < drugHighX), MarkerRegionType.Drug,
                sizeTh, pointsTh);
            _drugOperator.RegionDetected += DeiOperatorRegionDetected;
        }

        /// <summary>
        /// 初始化爆炸物检测算子
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void InitExplosivesOperator(int sensitivity)
        {
            int sizeTh, pointsTh;
            GetDeiThresholdsFromSensitivity(sensitivity, out sizeTh, out pointsTh);

            if (_explosiveOperator != null)
            {
                _explosiveOperator.RegionDetected -= DeiOperatorRegionDetected;
            }

            float explosiveLowZ, explosiveHighZ;
            ushort explosiveHighX;
            if (!ScannerConfig.Read(ConfigPath.IntellisenseExplosivesLowZ, out explosiveLowZ))
            {
                explosiveLowZ = 7;
            }

            if (!ScannerConfig.Read(ConfigPath.IntellisenseExplosivesHighZ, out explosiveHighZ))
            {
                explosiveHighZ = 8;
            }

            if (!ScannerConfig.Read(ConfigPath.IntellisenseExplosivesHighX,out explosiveHighX))
            {
                explosiveHighX = 46000;
            }

            _explosiveOperator = new IntelliSenseOperator((x, m) => (m >= explosiveLowZ && m <= explosiveHighZ && x < explosiveHighX),
                MarkerRegionType.Explosives, sizeTh, pointsTh);
            _explosiveOperator.RegionDetected += DeiOperatorRegionDetected;
        }

        /// <summary>
        /// 事件响应：一个毒品或爆炸物自动探测区域探测成功,触发事件RegionDetected，根据区域类型的不同做不同的处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="markerRegion">毒品或爆炸物的自动探测区域</param>
        private void DeiOperatorRegionDetected(object sender, MarkerRegion markerRegion)
        {
            FireRegionDetectedEvent(markerRegion);
        }

        /// <summary>
        /// 事件响应：一个穿不透区域或者危险品区域探测成功
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">探测成功的一个穿不透区域或者危险品区域 </param>
        private void HdiOperatorRegionDetected(object sender, MarkerRegion e)
        {
            FireRegionDetectedEvent(e);
        }

        /// <summary>
        /// 区域检测，对不同类型区域进行检测，这里可能要通过读取配置文件确定究竟要检测什么类型区域，这里默认目标和危险品均检测
        /// </summary>
        /// <param name="bundle"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Detect(DisplayScanlineData bundle)
        {
            if (_hdiOperator != null)
            {
                _hdiOperator.Detect(bundle);
            }

            if (_drugOperator != null)
            {
                _drugOperator.Detect(bundle);
            }

            if (_explosiveOperator != null)
            {
                _explosiveOperator.Detect(bundle);
            }
        }

        public event EventHandler<MarkerRegion> RegionDetected;

        /// <summary>
        /// 激发区域检测成功的事件RegionDetected,向外部传递一个新探测的Cad区域
        /// </summary>
        /// <param name="markerRegion">检测到的穿不透区域或者危险品区域</param>
        private void FireRegionDetectedEvent(MarkerRegion markerRegion)
        {
            if (RegionDetected != null)
            {
                switch (markerRegion.RegionType)
                {
                    case MarkerRegionType.UnPenetratable:
                        markerRegion.Name = TranslationService.FindTranslation("UnPenetratable");
                        break;
                    case MarkerRegionType.Explosives:
                        markerRegion.Name = TranslationService.FindTranslation("Explosives");
                        break;
                    case MarkerRegionType.Drug:
                        markerRegion.Name = TranslationService.FindTranslation("Drug");
                        break;
                    case MarkerRegionType.Gun:
                        markerRegion.Name = TranslationService.FindTranslation("Guns");
                        break;
                    case MarkerRegionType.Knife:
                        markerRegion.Name = TranslationService.FindTranslation("Knives");
                        break;
                    default:
                        markerRegion.Name = TranslationService.FindTranslation("UnPenetratable");
                        break;
                }
                RegionDetected(this, markerRegion);
            }
        }

        /// <summary>
        /// 实现ICadProcess接口中的void Complete()，当调用此函数时，结束所有Operators的所有未完成的regions，
        /// 并触发事件，向外传递，最后清空所有regions，用于图像的前后拖动的情况。当图像反方向拖动时，最后一线数据中
        /// 可能存在未闭合的矩形区域，此时认为其已经闭合，输出到图像。
        /// </summary>
        private void Complete()
        {
            SortedSet<MarkerRegion> unpenetratableCadList = _hdiOperator.UnCompletedCadRegionsList;
            SortedSet< MarkerRegion> drugCadOperatorList = _drugOperator.UnCompletedCadRegionsList;
            SortedSet<MarkerRegion> explosiveCadOperatorList = _explosiveOperator.UnCompletedCadRegionsList;


            //将穿不透区域所有节点输出
            foreach (MarkerRegion region in unpenetratableCadList)
            {
                //if (region.RegionAreaSize>=_hdaRegionSizeThreshold)
                {
                    FireRegionDetectedEvent(region);
                }
            }

            //将毒品区域所有节点输出
            foreach (MarkerRegion region in drugCadOperatorList)
            {
                //if (region.RegionAreaSize>=_deiRegionSizeThreshold)
                {
                    FireRegionDetectedEvent(region);
                }
            }

            //将爆炸物区域所有节点输出
            foreach (MarkerRegion region in explosiveCadOperatorList)
            {
                //if (region.RegionAreaSize >= _deiRegionSizeThreshold)
                {
                    FireRegionDetectedEvent(region);
                }
            }
            //清空所有regions
            unpenetratableCadList.Clear();
            drugCadOperatorList.Clear();
            explosiveCadOperatorList.Clear();
        }

        /// <summary>
        /// 根据灵敏度设定，获取用于高密度检测的有关阈值设定
        /// </summary>
        /// <param name="sensitivity"></param>
        /// <param name="regionSizeTh"></param>
        /// <param name="continuePointsTh"></param>
        private void GetHdiThresholdsFromSensitivity(int sensitivity, out int regionSizeTh, out int continuePointsTh)
        {
            switch (sensitivity)
            {
                case 5:
                    regionSizeTh = 1000;
                    continuePointsTh = 2;
                    break;
                case 4:
                    regionSizeTh = 3000;
                    continuePointsTh = 3;
                    break;
                case 3:
                    regionSizeTh = 5000;
                    continuePointsTh = 5;
                    break;
                case 2:
                    regionSizeTh = 10000;
                    continuePointsTh = 10;
                    break;
                case 1:
                    regionSizeTh = 15000;
                    continuePointsTh = 15;
                    break;
                default:
                    regionSizeTh = 5000;
                    continuePointsTh = 5;
                    break;
            }
        }

        /// <summary>
        /// 根据灵敏度设定，获取用于毒品爆炸物检测的有关阈值设定
        /// </summary>
        /// <param name="sensitivity"></param>
        /// <param name="regionSizeTh"></param>
        /// <param name="continuePointsTh"></param>
        private void GetDeiThresholdsFromSensitivity(int sensitivity, out int regionSizeTh, out int continuePointsTh)
        {
            switch (sensitivity)
            {
                case 5:
                    regionSizeTh = 800;
                    continuePointsTh = 2;
                    break;
                case 4:
                    regionSizeTh = 2000;
                    continuePointsTh = 2;
                    break;
                case 3:
                    regionSizeTh = 4000;
                    continuePointsTh = 4;
                    break;
                case 2:
                    regionSizeTh = 8000;
                    continuePointsTh = 8;
                    break;
                case 1:
                    regionSizeTh = 10000;
                    continuePointsTh = 10;
                    break;
                default:
                    regionSizeTh = 4000;
                    continuePointsTh = 4;
                    break;
            }
        }
    }
}

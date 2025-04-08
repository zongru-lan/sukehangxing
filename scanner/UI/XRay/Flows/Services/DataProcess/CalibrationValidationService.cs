using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.DataProcess
{
    /// <summary>
    /// 本底满度标定的验证服务：验证本底、满度是否满度一般性要求
    /// </summary>
    public class CalibrationValidationService
    {
        /// <summary>
        /// 高能满度均值下限
        /// </summary>
        private int _airValueHighAverageLower;

        /// <summary>
        /// 低能满度均值下限
        /// </summary>
        private int _airValueLowAverageLower;

        /// <summary>
        /// 高能本底均值上限
        /// </summary>
        private int _groundValueHighAverageUpper;

        /// <summary>
        /// 低能本底均值上限
        /// </summary>
        private int _groundValueLowAverageUpper;

        public CalibrationValidationService()
        {
            LoadSettings();
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.PreProcAirHighAvgLower, out _airValueHighAverageLower))
                {
                    _airValueHighAverageLower = 7000;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcAirLowAvgLower, out _airValueLowAverageLower))
                {
                    _airValueLowAverageLower = 7000;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcGroundHighAvgUpper, out _groundValueHighAverageUpper))
                {
                    _groundValueHighAverageUpper = 4500;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcGroundLowAvgUpper, out _groundValueLowAverageUpper))
                {
                    _groundValueLowAverageUpper = 4500;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Failed to load settings in ManualCalibrationService");
            }
        }


        public bool IsGroundValid(ScanlineDataBundle ground)
        {
            var line = ground.View1LineData;
            if (line != null)
            {
                if (line.Low != null)
                {
                    var lowAvg = (ushort)line.Low.Average(arg => arg);
                    if (lowAvg >= _groundValueLowAverageUpper)
                    {
                        Tracer.TraceWarning(
                            "The calibrated ground is invalid because average of View1.Low is "+ lowAvg + ", greator than threshold " +
                            _groundValueLowAverageUpper);
                        return false;
                    }
                }

                if (line.High != null)
                {
                    var highAvg = (ushort)line.High.Average(arg => arg);
                    if (highAvg >= _groundValueHighAverageUpper)
                    {
                        Tracer.TraceWarning(
                            "The calibrated ground is invalid because average of View1.High is " +highAvg + ", greator than threshold " +
                            _groundValueHighAverageUpper);
                        return false;
                    }
                }
            }

            line = ground.View2LineData;
            if (line != null)
            {
                if (line.Low != null)
                {
                    var lowAvg = (ushort)line.Low.Average(arg => arg);
                    if (lowAvg >= _groundValueLowAverageUpper)
                    {
                        Tracer.TraceWarning(
                            "The calibrated ground is invalid because average of View2.Low is " + lowAvg + ", greator than threshold " +
                            _groundValueLowAverageUpper);
                        return false;
                    }
                }

                if (line.High != null)
                {
                    var highAvg = (ushort)line.High.Average(arg => arg);
                    if (highAvg >= _groundValueHighAverageUpper)
                    {
                        Tracer.TraceWarning(
                            "The calibrated ground is invalid because average of View2.High is " + highAvg + ", greator than threshold " +
                            _groundValueHighAverageUpper);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 判断某个视角的本底数据是否合法
        /// </summary>
        /// <param name="ground"></param>
        /// <returns></returns>
        public bool IsGroundValid(ScanlineData ground)
        {
            var line = ground;
            if (line != null)
            {
                if (line.Low != null)
                {
                    var lowAvg = (ushort)line.Low.Average(arg => arg);
                    if (lowAvg >= _groundValueLowAverageUpper)
                    {
                        Tracer.TraceWarning(
                            "The calibrated ground is invalid because average of Low is " + lowAvg + ", greator than threshold " +
                            _groundValueLowAverageUpper);
                        return false;
                    }
                }

                if (line.High != null)
                {
                    var highAvg = (ushort)line.High.Average(arg => arg);
                    if (highAvg >= _groundValueHighAverageUpper)
                    {
                        Tracer.TraceWarning(
                            "The calibrated ground is invalid because average of High is " + highAvg + ", greator than threshold " +
                            _groundValueHighAverageUpper);
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// 判断满度是否满足一般性验证需要
        /// </summary>
        /// <param name="newAir"></param>
        /// <returns></returns>
        public bool IsAirValid(ScanlineData newAir)
        {
            // 依此检查视角1和视角2的满度是否合规
            if (newAir != null)
            {
                if (newAir.Low != null)
                {
                    var lowAvg = (ushort)newAir.Low.Average(arg => arg);
                    if (lowAvg <= _airValueLowAverageLower)
                    {
                        Tracer.TraceWarning(
                            "The calibrated air is invalid because average of View1.Low is " + lowAvg + ", less than threshold " +
                            _airValueLowAverageLower);
                        return false;
                    }
                }

                if (newAir.High != null)
                {
                    var highAvg = (ushort)newAir.High.Average(arg => arg);
                    if (highAvg <= _airValueHighAverageLower)
                    {
                        Tracer.TraceWarning(
                            "The calibrated air is invalid because average of View1.High is " + highAvg + ", less than threshold " +
                            _airValueHighAverageLower);
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断满度是否满足一般性验证需要
        /// </summary>
        /// <param name="newAir"></param>
        /// <returns></returns>
        public bool IsAirValid(ScanlineDataBundle newAir)
        {
            // 依此检查视角1和视角2的满度是否合规
            if (newAir.View1LineData != null)
            {
                if (newAir.View1LineData.Low != null)
                {
                    var lowAvg = (ushort)newAir.View1LineData.Low.Average(arg => arg);
                    if (lowAvg <= _airValueLowAverageLower)
                    {
                        Tracer.TraceWarning(
                            "The calibrated air is invalid because average of View1.Low is " + lowAvg + ", less than threshold " +
                            _airValueLowAverageLower);
                        return false;
                    }
                }

                if (newAir.View1LineData.High != null)
                {
                    var highAvg = (ushort)newAir.View1LineData.High.Average(arg => arg);
                    if (highAvg <= _airValueHighAverageLower)
                    {
                        Tracer.TraceWarning(
                            "The calibrated air is invalid because average of View1.High is " + highAvg + ", less than threshold " +
                            _airValueHighAverageLower);
                        return false;
                    }
                }
            }

            if (newAir.View2LineData != null)
            {
                if (newAir.View2LineData.Low != null)
                {
                    var lowAvg = (ushort)newAir.View2LineData.Low.Average(arg => arg);
                    if (lowAvg <= _airValueLowAverageLower)
                    {
                        Tracer.TraceWarning(
                            "The calibrated air is invalid because average of View2.Low is " + lowAvg + ", less than threshold " +
                            _airValueLowAverageLower);
                        return false;
                    }
                }

                if (newAir.View2LineData.High != null)
                {
                    var highAvg = (ushort)newAir.View2LineData.High.Average(arg => arg);
                    if (highAvg <= _airValueHighAverageLower)
                    {
                        Tracer.TraceWarning(
                            "The calibrated air is invalid because average of View2.High is " + highAvg + ", less than threshold " +
                            _airValueHighAverageLower);
                        return false;
                    }
                }
            }

            return true;
        }

    }
}

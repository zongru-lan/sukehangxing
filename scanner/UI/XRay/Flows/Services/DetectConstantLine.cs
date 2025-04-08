using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 检测是否是包前的渐变线
    /// 
    /// </summary>
    public class DetectConstantLine
    {
        public static DetectConstantLine Service { get; private set; }

        static DetectConstantLine()
        {
            Service = new DetectConstantLine();
        }
        public DetectConstantLine()
        {
            ReadConfig();
        }

        void ReadConfig()
        {
            if (!ScannerConfig.Read(ConfigPath.PreProcDetectConstantLineIsEnable, out _isEnable))
            {
                _isEnable = false;
            }
            ExchangeDirectionConfig.Service.GetView1ChannelsCount(out int _view1ChannelCount);
            ExchangeDirectionConfig.Service.GetView2ChannelsCount(out int _view2ChannelCount);


            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangeDetector, out bool _exchangeViewsOrder))
            {
                _exchangeViewsOrder = false;
            }

            if (_exchangeViewsOrder)
            {
                int temp = _view1ChannelCount;
                _view1ChannelCount = _view2ChannelCount;
                _view2ChannelCount = temp;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcDetectConstantLineView1AirThr, out _view1AirThr))
            {
                _view1AirThr = 62000;
            }

            string _view1IndexStr;
            if (!ScannerConfig.Read(ConfigPath.PreProcDetectConstantLineView1PointIndex, out _view1IndexStr))
            {
                _view1IndexStr = string.Empty;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcDetectConstantLineView1Diff, out _view1Diff))
            {
                _view1Diff = 65535;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtBegin, out int _view1MarginChannelsAtBegin))
            {
                _view1MarginChannelsAtBegin = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtEnd, out int _view1MarginChannelsAtEnd))
            {
                _view1MarginChannelsAtEnd = 0;
            }

            if (!string.IsNullOrEmpty(_view1IndexStr))
            {
                Tracer.TraceDebug($"[ExceptionTail] PreProcDetectConstantLineView1PointIndex :{_view1IndexStr}");
                _view1IndexArray = Array.ConvertAll(_view1IndexStr.Trim().Split(','), delegate (string s)
                {
                    var value = int.Parse(s);
                    if (value >= _view1MarginChannelsAtBegin && value < (_view1ChannelCount - _view1MarginChannelsAtEnd))
                    {
                        return value;
                    }
                    else
                    {
                        return _view1ChannelCount / 2;
                    }
                }).Distinct().ToArray();
                _view1InitSuccess = true;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcDetectConstantLineView1AirThr, out _view2AirThr))
            {
                _view2AirThr = 62000;
            }

            string _view2IndexStr;
            if (!ScannerConfig.Read(ConfigPath.PreProcDetectConstantLineView2PointIndex, out _view2IndexStr))
            {
                _view2IndexStr = string.Empty;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcDetectConstantLineView2Diff, out _view2Diff))
            {
                _view2Diff = 65535;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtBegin, out int _view2MarginChannelsAtBegin))
            {
                _view2MarginChannelsAtBegin = 5;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtEnd, out int _view2MarginChannelsAtEnd))
            {
                _view2MarginChannelsAtEnd = 5;
            }

            if (!string.IsNullOrEmpty(_view2IndexStr))
            {
                Tracer.TraceDebug($"[ExceptionTail] PreProcDetectConstantLineView2PointIndex :{_view2IndexStr}");
                _view2IndexArray = Array.ConvertAll(_view2IndexStr.Trim().Split(','), delegate (string s)
                {
                    var value = int.Parse(s);
                    if (value >= _view2MarginChannelsAtBegin && value < (_view2ChannelCount - _view2MarginChannelsAtEnd))
                    {
                        return value;
                    }
                    else
                    {
                        return _view2ChannelCount / 2;
                    }
                }).Distinct().ToArray();
                _view2InitSuccess = true;
            }
        }

        public void Init()
        {
            //
        }

        public bool HaveConstantLine(ushort[] data, DetectViewIndex view)
        {
            if (!_isEnable)
            {
                return false;
            }
            _detectPoints.Clear();

            bool _haveConstantLine = true;
            if (view == DetectViewIndex.View1)
            {
                if (!_view1InitSuccess)
                {
                    return false;
                }

                for (int i = 0; i < _view1IndexArray.Length; i++)
                {
                    _detectPoints.Add(data[_view1IndexArray[i]]);
                    if (data[_view1IndexArray[i]] > _view1AirThr)
                    {
                        return false;
                    }
                }
            }
            else if (view == DetectViewIndex.View2)
            {
                if (!_view2InitSuccess)
                {
                    return false;
                }
                for (int i = 0; i < _view2IndexArray.Length; i++)
                {
                    _detectPoints.Add(data[_view2IndexArray[i]]);
                    if (data[_view2IndexArray[i]] > _view2AirThr)
                    {
                        return false;
                    }
                }
            }

            //可能有脏图
            var max = _detectPoints.Max();
            var min = _detectPoints.Min();
            var diff = max - min;
            if (view == DetectViewIndex.View1)
            {
                if (diff > _view1Diff)
                {
                    _haveConstantLine = false;
                }
            }
            else if (view == DetectViewIndex.View2)
            {
                if (diff > _view2Diff)
                {
                    _haveConstantLine = false;
                }
            }

            return _haveConstantLine;
        }

        bool _isEnable = false;
        bool _view1InitSuccess = false;
        bool _view2InitSuccess = false;

        int[] _view1IndexArray;
        int _view1AirThr;
        int _view1Diff;

        int[] _view2IndexArray;
        int _view2AirThr;
        int _view2Diff;

        List<ushort> _detectPoints = new List<ushort>();
    }
}

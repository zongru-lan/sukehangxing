using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 白线检测服务，检测
    /// </summary>
    public class AirLineDetectService
    {
        private int _view1MarginBegin;

        private int _view1MarginEnd;

        private int _view2MarginBegin;

        private int _view2MarginEnd;

        private ushort _background;

        public AirLineDetectService()
        {
            ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
            LoadSettingsAndInit();
        }

        /// <summary>
        /// 白线检测
        /// </summary>
        /// <param name="lineData"></param>
        /// <param name="viewIndex"></param>
        /// <returns></returns>
        public bool Detect(ushort[] lineData, DetectViewIndex viewIndex)
        {
            if (lineData == null || lineData.Length == 0)
            {
                return true;
            }

            var startMarginCount = viewIndex == DetectViewIndex.View1 ? _view1MarginBegin : _view2MarginBegin;
            var endMarginCount = viewIndex == DetectViewIndex.View1 ? _view1MarginEnd : _view2MarginEnd;

            long sumLow = 0;
            var count = 0;
            var start = Math.Min(startMarginCount, lineData.Length - 1);
            var end = Math.Min(lineData.Length - endMarginCount - 1, lineData.Length - 1);
            start = Math.Max(start, 0);

            if (end >= lineData.Length) return false;

            for (int i = start; i <= end; i++)
            {
                sumLow += lineData[i];
                count++;
            }

            if (count > 0)
            {
                // 将均值与阈值进行比较，判断是否为空气值
                return (sumLow / count >= _background);
            }

            return true;
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            LoadSettingsAndInit();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void LoadSettingsAndInit()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtBegin, out _view1MarginBegin))
                {
                    _view1MarginBegin = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtEnd, out _view1MarginEnd))
                {
                    _view1MarginEnd = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtBegin, out _view2MarginBegin))
                {
                    _view2MarginBegin = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtEnd, out _view2MarginEnd))
                {
                    _view2MarginEnd = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineInterruptModeBackgroundValue, out _background))
                {
                    _background = 63500;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }
    }
}

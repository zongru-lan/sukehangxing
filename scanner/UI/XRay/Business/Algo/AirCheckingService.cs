using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    public abstract class AirCheckingService
    {
        protected int _view1MarginBegin;

        protected int _view1MarginEnd;

        protected int _view2MarginBegin;

        protected int _view2MarginEnd;

        protected ushort background;

        public AirCheckingService()
        {
            ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
            LoadSettingsAndInit();
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
                
                if (!ScannerConfig.Read(ConfigPath.PreProcBkgThreshold, out background))
                {
                    background = 63500;
                }
                Tracer.TraceInfo($"[AirCheckingService] V1MarginBegin: {_view1MarginBegin}, V1MarginEnd: {_view1MarginEnd}, " +
                    $"V2MarginBegin: {_view2MarginBegin}, V2MarginEnd: {_view2MarginEnd}, background: {background}");
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }
        public abstract List<ClassifiedLineDataBundle> CheckAirLine(ClassifiedLineDataBundle line);

        public abstract void ClearCacheLines();
    }
}

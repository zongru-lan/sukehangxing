using ImageProcessor.Common.Tiff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services
{
    public class RecordCalibrateAirService
    {
        public static RecordCalibrateAirService Service { get; set; }
        static RecordCalibrateAirService()
        {
            Service = new RecordCalibrateAirService();
        }
        protected RecordCalibrateAirService()
        {
            AirList = new List<ScanlineDataBundle>();
        }

        List<ScanlineDataBundle> AirList = null;
        public int Width { get; set; }

        public void Add(ScanlineDataBundle air)
        {
            AirList.Add(air.Clone());
        }
        public void Clear()
        {
            AirList.Clear();
            Tracer.TraceInfo("Clear AirList");
        }

        public void SaveToTiff()
        {
            
            if (AirList.Count < 1)
            {
                return;
            }

            var lowdata = new List<ushort[]>();
            var highdata = new List<ushort[]>();

            foreach (var line in AirList)
            {
                if (line.View1LineData!=null)
                {
                    lowdata.Add(line.View1LineData.Low);
                    highdata.Add(line.View1LineData.High);
                }               
            }

            string path = System.Environment.CurrentDirectory;
            if (lowdata.Count > 0)
            {
                TiffHelper.Create16BitGrayScaleTiff(lowdata, lowdata[0].Length, lowdata.Count, Path.Combine(path, "low.tiff"));
            }
            if (highdata.Count > 0)
            {
                TiffHelper.Create16BitGrayScaleTiff(highdata, highdata[0].Length, highdata.Count, Path.Combine(path, "high.tiff"));
            }            
        }

    }
}

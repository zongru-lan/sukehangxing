using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.ImagePlant;

namespace UI.XRay.Business.Algo
{
    public class DualEnergyFuse
    {
        private HighLowEnergyFusion _fuser;
        public DualEnergyFuse()
        {
            _fuser = new HighLowEnergyFusion();
        }
        public void Fuse(ScanlineDataBundle bundle)
        {
            if (bundle.View1LineData != null)
            {
                Calculate(bundle.View1LineData);
            }

            if (bundle.View2LineData != null)
            {
                Calculate(bundle.View2LineData);
            }

        }
        private void Calculate(ScanlineData line)
        {
            if (line.XRaySensor == XRaySensorType.Dual)
            {
                ushort[] fused = null;
                _fuser.Fuse(line.Low, line.High, out fused);
                line.Fused = fused;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    [Serializable]
    public class PaintingRectangle
    {
        public int View { get; set; }
        public bool ManualMark { get; set; }
        public bool Vertical { get; set; }
        public bool Right2Left { get; set; }
        public int FromLine { get; set; }
        public int ToLine { get; set; }
        public int FromChannel { get; set; }
        public int ToChannel { get; set; }
        public string StorePath { get; set; }
        public PaintingRectangle()
        {

        }
    }
}

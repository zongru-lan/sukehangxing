using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    public class XRayFileInfoForMark
    {
        public XRayFileInfoForMark()
        {

        }
        public XRayFileInfoForMark(ImageRecord imageRecord, string filepath, int sln, int eln)
        {
            ImageRecord = ImageRecord;
            FilePath = filepath;
            StartLineNumber = sln;
            EndLineNumber = eln;
        }

        public int EndLineNumber { get; set; }
        public string FilePath { get; set; }
        public int StartLineNumber { get; set; }

        public ImageRecord ImageRecord { get; set; }

    }
}

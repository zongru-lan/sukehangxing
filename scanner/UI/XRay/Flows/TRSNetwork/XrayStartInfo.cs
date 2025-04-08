using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Flows.TRSNetwork
{
    public class XrayStartInfo
    {
        public DateTime BeginScanTime { get; set; }

        public string XrayFilePath { get; set; }

        public int BeginScanLineNumber { get; set; }

        public DateTime EndScanTime { get; set; }

        public int EndScanLineNumber { get; set; }

        public string AccountId { get; set; }
    }
}

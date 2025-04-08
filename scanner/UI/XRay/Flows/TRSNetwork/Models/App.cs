using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace UI.XRay.Flows.TRSNetwork.Models
{
    [XmlRoot("App")]
    public class App
    {
        public string DBString { get; set; }
        public App()
        {
            DBString = "Data Source=192.168.5.10; Initial Catalog = HIWINGDB_IPCSIS; User ID = hw_sysnet; Password = hwsims";
        }
    }
}

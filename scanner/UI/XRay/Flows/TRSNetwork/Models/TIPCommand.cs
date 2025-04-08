using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Flows.TRSNetwork.Models
{
    public class TIPCommand
    {
        public TIPCommand(TIPCommandType tipCmdType, byte[] fileData, string fileName)
        {
            CommandType = tipCmdType;
            fileData = FileData;
            FileName = fileName;
        }
        /// <summary>
        /// TIP命令类型
        /// </summary>
        public TIPCommandType CommandType { get; set; }

        /// <summary>
        /// TIP命令附带的XML文件，若为空，则不包含文件数据
        /// </summary>
        public byte[] FileData { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        public string FileName { get; set; }
    }
}

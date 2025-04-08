using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Flows.TRSNetwork.Models
{
    public class DcsBagInfoDto
    {
        /// <summary>行李件数</summary>
        public int? BAGCOUNT { get; set; }
        /// <summary>行李重量,单位:公斤</summary>
        public decimal? BAGWEIGHT { get; set; }
        /// <summary>值机时间</summary>
        public DateTime? CHECKINTIME { get; set; }
        /// <summary>值机柜台号</summary>
        public string COUNTERNO { get; set; }
        /// <summary>始发站3字码</summary>
        public string DEPARTAIRPORT { get; set; }
        /// <summary>离港日期</summary>
        public DateTime? DEPARTDATE { get; set; }
        /// <summary>目的航站3字码</summary>
        public string DESTAIRPORT { get; set; }
        /// <summary>航班号</summary>
        public string FLIGHTNO { get; set; }
        /// <summary>到港旅客登机序号</summary>
        public string FromBoardNo { get; set; }
        /// <summary>到港日期</summary>
        public DateTime? FROMDATE { get; set; }
        /// <summary>到港始发地</summary>
        public string FROMDEPARTAIRPORT { get; set; }
        /// <summary>到港目的地</summary>
        public string FROMDESTAIRPORT { get; set; }
        /// <summary>到港航班号</summary>
        public string FROMFLIGHTNO { get; set; }
        /// <summary>到港旅客座位号</summary>
        public string FROMSeatNO { get; set; }
        /// <summary>行李牌号</summary>
        public string IATACODE { get; set; }
        /// <summary>中转行李标识，1-中转，0-正常行李</summary>
        public short? ISTRANSIT { get; set; }
        /// <summary>	登机号</summary>
        public string PSGBOARDNO { get; set; }
        /// <summary>旅客姓名(全拼)</summary>
        public string PSGNAMEEN { get; set; }
        /// <summary>	座位号</summary>
        public string PSGSEATNO { get; set; }
        /// <summary>发送方消息编号</summary>
        public int? SENDERID { get; set; }
    }
}

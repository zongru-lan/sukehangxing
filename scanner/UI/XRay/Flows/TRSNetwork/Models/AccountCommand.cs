using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Flows.TRSNetwork.Models
{
    public class AccountCommand
    {
        public AccountCommandType CommandType { get; set; }
        public byte[] FileData { get; set; }

        public AccountCommand(AccountCommandType cmdType, byte[] data = null)
        {
            CommandType = cmdType;
            FileData = data;
        }
    }

    /// <summary>
    /// 发送至网络的文件类型
    /// </summary>
    public enum FileType
    {
        Accounts=0,
        CurrentAccount,

        TipPlans,
        TipImageLib,
        TipImages,
        TipLogs,

        Diagnosis,
        DiagnosisFailed,
        DiagnosisCanceled,

        Image,
    }
}

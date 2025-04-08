using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using UI.XRay.Business.Entities;

namespace UI.XRay.Security.Configer
{
    public class ChangeModelMessageAction : NotificationMessageAction<bool>
    {
        /// <summary>
        /// 设备配置文件路径
        /// </summary>
        public string ModelFilePath { get; private set; }

        public ChangeModelMessageAction(string filePath, Action<bool> action)
            : base(null, action)
        {
            ModelFilePath = filePath;
        }
    }
}

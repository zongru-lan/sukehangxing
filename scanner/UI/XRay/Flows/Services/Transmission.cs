using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UI.XRay.Flows.Services
{
    public static class Transmission 
    {

        public static bool emergency;//yxc  用于解决按下急停按钮并且重新上电后按传送带前进键失效的问题
        public static void emergency2(bool a)//yxc  用于解决按下急停按钮并且重新上电后按传送带前进键失效的问题
        {
            emergency = a;
        }

        public static bool IsRemoteTipProcessing = false;

        public static bool diagnose=true;  //yxc 用于解决诊断触发X射线的问题  diagnose为true 允许光障出数

        public static bool RemoteTipUpdated = false;

        public static void diagnose2(bool a) //yxc 用于解决诊断触发X射线的问题 
        {
            diagnose = a;
        }

        public static string ImagePath1=null;


        public static bool IsRemoteDiagnosing = false; 

        public static bool IsRemoteDiagnosingOnSettingWindow=false;
        
    }
}

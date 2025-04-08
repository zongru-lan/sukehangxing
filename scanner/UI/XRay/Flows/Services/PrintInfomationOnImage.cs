using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Flows.Services
{
    public class PrintInfomationOnImage
    {
        private static bool enablePrintingService;

        static PrintInfomationOnImage()
        {
            if (!ScannerConfig.Read(ConfigPath.PrintInfoOnImage, out enablePrintingService))
            {
                enablePrintingService = true;
            }
        }

        public static void PrintOnImage(Bitmap bitmap, string filename, string scantime)
        {
            if (!enablePrintingService)
            {
                return;
            }
            try
            {
                var infoList = filename.Split('_');
                if (infoList.Length != 5) return;

                var ChannelID = infoList[0];
                var machineNum = infoList[1];
                var id = infoList[2];

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    string channelIdStr = TranslationService.FindTranslation("Configer", "Channel ID") + ": ";

                    string machineIdStr = TranslationService.FindTranslation("Configer", "Machine Number") + ": ";

                    string scantimeStr = TranslationService.FindTranslation("Scan Time") + ": ";

                    string idStr = TranslationService.FindTranslation("AccountRole", "Operator") + ": ";

                    g.DrawString(machineIdStr, new Font("Arial", 15), Brushes.Black, new PointF(0, 5));
                    g.DrawString(machineNum, new Font("Arial", 15), Brushes.Black, new PointF(20, 25));
                    g.DrawString(scantimeStr, new Font("Arial", 15), Brushes.Black, new PointF(0, 55));
                    g.DrawString(scantime, new Font("Arial", 15), Brushes.Black, new PointF(20, 75));
                    g.DrawString(idStr, new Font("Arial", 15), Brushes.Black, new PointF(0, 105));
                    g.DrawString(id, new Font("Arial", 15), Brushes.Black, new PointF(20, 125));
                    g.DrawString(channelIdStr, new Font("Arial", 15), Brushes.Black, new PointF(0, 155));
                    g.DrawString(ChannelID, new Font("Arial", 15), Brushes.Black, new PointF(20, 175));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}

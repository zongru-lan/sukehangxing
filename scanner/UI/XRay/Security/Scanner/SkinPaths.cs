using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// 皮肤文件的路径管理：其中的路径都是相对于软件安装目录的相对路径，并非绝对路径
    /// </summary>
    public static class SkinPaths
    {
        /// <summary>
        /// 软件初始化时的欢迎页面
        /// </summary>
        public static string WelcomeImage = "skin/welcome.jpg";
    }

    public static class SkinHelper
    {
        /// <summary>
        /// 从软件安装目录下的skin中加载皮肤图像
        /// </summary>
        /// <param name="skinImagePath"></param>
        /// <returns></returns>
        public static Bitmap LoadSkinImage(string skinImagePath)
        {
            var image = Bitmap.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\", SkinPaths.WelcomeImage));
            return image as Bitmap;
        }
    }
}

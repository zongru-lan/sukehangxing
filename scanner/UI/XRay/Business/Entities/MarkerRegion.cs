using System;
using System.Collections.Generic;
using System.Drawing;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 计算机辅助探测区域的类型
    /// </summary>
    [Serializable]
    public enum MarkerRegionType
    {
        /// <summary>
        /// Regular contraband object 一般违禁品，可能是以下任意一种具体的物品类型
        /// </summary>
        Contraband = 0,

        /// <summary>
        /// unpenetratable region
        /// </summary>
        UnPenetratable,

        /// <summary>
        /// suspicious explosives region
        /// </summary>
        Explosives,

        /// <summary>
        /// drgus region
        /// </summary>
        Drug,

        /// <summary>
        /// gun
        /// </summary>
        Gun,

        /// <summary>
        /// 图像包含刀具
        /// </summary>
        Knife,

        Tip
    }

    /// <summary>
    /// 计算机辅助探测区域边框颜色
    /// </summary>
    public enum MarkerRegionBorderColor
    {
        Black,
        Blue,
        Brown,
        DarkGreen,
        DarkOriange,
        DeepPink,
        Purple,
        Red,
        //水绿色，TIP专用，暂时不使用在其他方面
        Aqua
    }

    /// <summary>
    /// 枚举中的颜色和实际的颜色的映射类
    /// </summary>
    public static class MarkerRegionBorderColorMapper
    {
        private static readonly Dictionary<MarkerRegionBorderColor, System.Drawing.Color> Map = new Dictionary<MarkerRegionBorderColor, Color>();

        static MarkerRegionBorderColorMapper()
        {
            Map.Add(MarkerRegionBorderColor.Black, System.Drawing.Color.Black);
            Map.Add(MarkerRegionBorderColor.Blue, System.Drawing.Color.Blue);
            Map.Add(MarkerRegionBorderColor.Brown, System.Drawing.Color.Brown);
            Map.Add(MarkerRegionBorderColor.DarkGreen, System.Drawing.Color.DarkGreen);
            Map.Add(MarkerRegionBorderColor.DarkOriange, System.Drawing.Color.DarkOrange);
            Map.Add(MarkerRegionBorderColor.DeepPink, System.Drawing.Color.DeepPink);
            Map.Add(MarkerRegionBorderColor.Purple, System.Drawing.Color.Purple);
            Map.Add(MarkerRegionBorderColor.Red, System.Drawing.Color.Red);
            Map.Add(MarkerRegionBorderColor.Aqua, System.Drawing.Color.Aqua);
        }

        public static System.Drawing.Color? Mapper(MarkerRegionBorderColor color)
        {
            if (Map != null && Map.Count > 0)
            {
                System.Drawing.Color mapColor;
                if (Map.TryGetValue(color, out mapColor))
                {
                    return mapColor;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 表示计算机辅助探测的一个区域
    /// </summary>
    /// <remarks>
    /// 关于标记区域的定位：标记区域使用矩形Rectangle，x方向为探测器编号，取值范围为（0，探测器个数-1）；
    /// y方向为扫描线方向，在XRayImage中存储时，取值范围为（0，扫描线数量-1），在内存中用于标记屏幕图像时，取值范围根据图像
    /// 在内存中的相对位置进行计算
    /// Rect的location中的x表示探测器编号，y表示扫描线编号, size.Width表示沿x方向的大小，size.Height表示沿Y方向的大小
    /// </remarks>
    [Serializable]
    public class MarkerRegion:IComparable<MarkerRegion>
    {
        /// <summary>
        /// 创建一个探测区域，可能是自动探测的区域，也可能是手工标记的探测区域
        /// </summary>
        /// <param name="type">探测区域的具体类型</param>
        /// <param name="rectangle">探测区域的矩形</param>
        /// <param name="mannualTag">true表示是由人手动标记的，false表示是计算机自动识别的</param>
        public MarkerRegion(MarkerRegionType type, Rectangle rectangle, bool mannualTag = false)
        {
            RegionType = type;
            Rect = rectangle;
            ManualTag = mannualTag;
        }

        /// <summary>
        /// 创建一个探测区域，可能是自动探测的区域，也可能是手工标记的探测区域
        /// </summary>
        /// <param name="type">探测区域的具体类型</param>
        /// <param name="fromLine">起始线号</param>
        /// <param name="toLine">结束线号</param>
        /// <param name="fromChannel">起始探测通道号</param>
        /// <param name="toChannel">结束探测通道号</param>
        /// <param name="manualTag">true表示是由人手动标记的，false表示是计算机自动识别的</param>
        public MarkerRegion(MarkerRegionType type, int fromLine, int toLine, int fromChannel, int toChannel, bool manualTag = false)
        {
            RegionType = type;
            Rect = new Rectangle(fromChannel, fromLine, toChannel - fromChannel + 1, toLine - fromLine + 1);
            ManualTag = manualTag;
        }

        /// <summary>
        /// 手动标记
        /// </summary>
        public bool ManualTag { get; private set; }

        /// <summary>
        /// 标记名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 框颜色
        /// </summary>
        public Color RectColor { get; set; }

        /// <summary>
        /// 是否是最后一个标识框
        /// </summary>
        public bool IsLastRect { get; set; }

        /// <summary>
        /// 探测区域的类型
        /// </summary>
        public MarkerRegionType RegionType { get; private set; }

        /// <summary>
        /// 具体的矩形探测区域。
        /// </summary>
        /// <remarks>location中的x表示探测器编号，y表示扫描线编号, size.Width表示沿x方向的大小，size.Height表示沿Y方向的大小</remarks>
        public Rectangle Rect { get; private set; }

        /// <summary>
        /// 区域像素总数，用于区域输出时与区域大小 阈值比较，大于阈值才能输出
        /// </summary>
        public int RegionAreaSize
        {
            get { return Rect.Width * Rect.Height; }
        }

        /// <summary>
        /// 矩形区域的起始线编号
        /// </summary>
        public int FromLine
        {
            get { return Rect.Y; }
        }

        /// <summary>
        /// 矩形区域的终止线编号
        /// </summary>
        public int ToLine
        {
            get { return Rect.Bottom; }
        }

        /// <summary>
        /// 矩形区域的起始探测通道编号
        /// </summary>
        public int FromChannel
        {
            get { return Rect.X; }
        }

        /// <summary>
        /// 矩形区域的终止探测通道编号
        /// </summary>
        public int ToChannel
        {
            get { return Rect.Right; }
        }

        /// <summary>
        /// 矩形区域的宽度
        /// </summary>
        public int Width
        {
            get { return Rect.Width; }
        }

        /// <summary>
        /// 矩形区域的高度
        /// </summary>
        public int Height
        {
            get { return Rect.Height; }
        }

        public int LabelHeightPos { get; set; }

        /// <summary>
        /// 将当前region与参数中的region进行合并，
        /// todo:并且合并后修改当前region，使得当前region膨胀变大；
        /// </summary>
        /// <param name="region">要合并的CadRegion结构</param>
        public void InflateWith(MarkerRegion region)
        {
            Rect = Rectangle.Union(Rect, region.Rect);
        }

        /// <summary>
        /// 判断一个CadRegion区域的Rect结构与另一个Rectangle结构是否在X方向上相交，仅用于区域探测算法中
        /// </summary>
        /// <param name="region">判断是否相交的另一个CadRegion结构</param>
        /// <returns>返回true，表示两个CadRegion结构有相交；返回false，没有相交</returns>
        public bool IntersectInXWith(MarkerRegion region)
        {
            //如果本CadRegion区域的起始探测通道编号FromChannel在region的FromChannel和ToChannel之间，
            //或者region的FromChannel在本CadRegion区域的FromChannel和ToChannel之间，表明两区域在X方向上有交集
            if ((FromChannel>=region.FromChannel&&FromChannel<=region.ToChannel)
                ||(region.FromChannel>=FromChannel&&region.FromChannel<=ToChannel))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 实现接口IComparable，用于两个CadRegion区域的排序，根据区域的起始探测通道标号（FromChannel）排序,
        /// >0,此对象起始探测通道编号大于region的起始探测通道编号，region排在前面
        /// 小于0，此对象起始探测通道编号小于region的起始探测通道编号，此对象排在前面
        /// =0，此对象起始探测通道编号等于region的起始探测通道编号
        /// </summary>
        /// <param name="other">用于比较的CadRegion区域</param>
        /// <returns>>0表示other排在前面，当前CadRegion排在后面，如果返回负数，other排在后面，如果=0表示两区域相同</returns>
        public int CompareTo(MarkerRegion other)
        {
            if (other==null)
            {
                return 1;
            }
            int iResult = FromChannel - other.FromChannel;
            //如果iResult==0，则表明两个CadRegion区域起始探测通道编号相等，经过合并的区域可能存在这种情况，但两个区域的Rect结构可能不同
            //当两个区域的起始探测通道编号相同，即FromChannel相同，如果两个区域的Rect结构相同，表明两个区域相同，可能的情况是：
            //1. 正在执行删除操作，找到符合条件的CadRegion区域；2.合并后的两个区域完全相同，则只存一个到链表即可
            if (iResult == 0)
            {
                if (Rect != other.Rect)
                {
                    iResult = -1;
                }
            }
            return iResult;
        }
    }

    /// <summary>
    /// 表示一个视角的计算机辅助探测的一个结果
    /// </summary>
    public class XRayViewCadRegion
    {
        public XRayViewCadRegion(MarkerRegion region, DetectViewIndex viewIndex)
        {
            Region = region;
            ViewIndex = viewIndex;
        }

        public MarkerRegion Region { get; private set; }

        public DetectViewIndex ViewIndex { get; private set; }
    }


    /// <summary>
    /// 保存一个物品所有视角（目前最多为两个）的区域，用于发出报警和将探测到的区域发送到需要的端口或标记到jpg图像上，todo：在物品图像过完保存后需要清空此中链表
    /// </summary>
    public class XrayCadRegions
    {
        public List<MarkerRegion> View1MarkerRegions = new List<MarkerRegion>();

        public List<MarkerRegion> View2MarkerRegions = new List<MarkerRegion>();

        public string FileName { get; set; }

        /// <summary>
        /// 一个物品中检测到了探测区域，只要有一个视角有探测区域，则认为探测到
        /// </summary>
        public bool MarkerRegionsDetected
        {
            get { return View1MarkerRegions.Count != 0 || View2MarkerRegions.Count != 0; }
        }

        public bool View1MarkerRegionsDetected
        {
            get { return View1MarkerRegions.Count != 0; }
        }

        public bool View2MarkerRegionsDetected
        {
            get { return View2MarkerRegions.Count != 0; }
        }

        public void Add(MarkerRegion region, DetectViewIndex viewIndex)
        {
            if (viewIndex == DetectViewIndex.View1)
            {
                View1MarkerRegions.Add(region);
            }
            else if (viewIndex == DetectViewIndex.View2)
            {
                View2MarkerRegions.Add(region);
            }
        }

        public void Clear()
        {
            View1MarkerRegions.Clear();
            View2MarkerRegions.Clear();
            FileName = string.Empty;
        }

        public XrayCadRegions Clone()
        {
            var regions = new XrayCadRegions();
            
            foreach (MarkerRegion region in this.View1MarkerRegions)
            {
                regions.Add(region, DetectViewIndex.View1);
            }

            foreach (var region in View2MarkerRegions)
            {
                regions.Add(region, DetectViewIndex.View2);
            }

            regions.FileName = this.FileName;
            return regions;
        }
    }
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
// 
// Description: NavigatableScanLinesPool.cs定义了NavigatableScanLinesCache、MemoryScanLinesCache、NavigatableImage、以及
// NavigatableScanLinesPool等类型。
// 其中NavigatableScanLinesCache是抽象类，用于定义可导航的扫描线缓存基类；
// MemoryScanLinesCache 与 NavigatableImage 都集成自NavigatableScanLinesCache，其中前者表示内存中的扫描线集合，这些扫描线尚未存储到磁盘；
//      后者表示已经存储为图像的扫描线集合
// NavigatableScanLinesPool中包括一个NavigatableScanLinesCache链表，其中按时间先后顺序，存储扫描线。链表的最后一个节点始终存储当前
// 内存中尚未保存完毕的扫描线。在回拉的时候，如果内存中的扫描线已经全部显示完，则会根据时间先后顺序，选择距离当前时间最近的前一个
// 图像，读入内存，并对图像所包含的扫描线进行编号，然后回拉显示。
// 
// 回拉时的内存管理：默认情况下，支持无限制回拉：只要有历史图像，即可一直回拉直到导航至最老的一个图像为止，因此可能会占据大量内存。
// 其实此处仍可进行优化：当某个文件显示完毕后，及时关闭文件，以释放内存，当再次回拉到该文件时，再次读入内存即可。
// 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

using System;
using System.Collections.Generic;
using System.Linq;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Controllers
{
    abstract class OrderedImageDataPool
    {
        /// <summary>
        /// 同步Tip区域链表
        /// </summary>
        private readonly object _tipCadRegionLock = new object();

        /// <summary>
        /// 软件可同时显示的扫描线的数量，一般为显示器的横向分辨率
        /// </summary>
        public int ScreenMaxLinesCount { get; private set; }

        /// <summary>
        /// 每次导航的线数：每次回拉或者前拉，更新的线数
        /// </summary>
        public const int NavigateLines = 100;

        /// <summary>
        /// 当前显示的扫描线的最小编号
        /// </summary>
        public int ShowingMinNumber { get; protected set; }

        /// <summary>
        /// 当前显示的扫描线的最大编号
        /// </summary>
        public int ShowingMaxNumber { get; protected set; }

        /// <summary>
        /// 存放根据保存的图像中TIP注入实例获取的TIP报警框区域，用于记录TIP报警框的位置
        /// </summary>
        public List<MarkerRegion> TipInsertionCadRegions { get; set; }

        /// <summary>
        /// 构造一个图像列数据缓冲池
        /// </summary>.
        /// <param name="screenMaxLinesCount">屏幕所能显示的扫描线的最大数量</param>
        protected OrderedImageDataPool(int screenMaxLinesCount)
        {
            ScreenMaxLinesCount = screenMaxLinesCount;

            //初始化TIP报警框链表
            TipInsertionCadRegions = new List<MarkerRegion>();
        }

        /// <summary>
        /// 重置当前正在显示的数据的编号
        /// </summary>
        /// <param name="minNum"></param>
        /// <param name="maxNum"></param>
        public void ResetShowingRange(int minNum, int maxNum)
        {
            ShowingMaxNumber = maxNum;
            ShowingMinNumber = minNum;
        }

        /// <summary>
        /// 添加Tip标识框到链表
        /// </summary>
        /// <param name="maxXrayImageLineNum"></param>
        /// <param name="image"></param>
        //public void AddTipCadRegionToList(int maxXrayImageLineNum, XRayImage image)
        //{
        //    //todo：在基类中加锁是否有问题，
        //    lock (_tipCadRegionLock)
        //    {
        //        //构造TIP报警框，并保存到链表中。加载Tip报警框时要注意实际插入到图像中的Tip不一定完全，可能只插入了一部分，
        //        //因此在构造CadRegion时要注意报警框的高度
        //        if (image.TipInjection != null)
        //        {
        //            //计算Tip报警框的起始线编号。TipInsertion中记录的线编号是相对于图像第一线数据的,因此要根据实际情况转换为显示图像的线编号。
        //            int startLineNum = maxXrayImageLineNum + 1 - image.View1.ScanLinesCount + image.TipInjection.StartScanLine;

        //            //如果Tip图像最后一线的显示时的线编号大于此宿主图像的最大线编号，则标记框的最大线编号为宿主图像的最大线编号，
        //            //防止出现Tip未完全插入，造成标记框位置的错误
        //            int tipCadRegionHeight = (startLineNum + image.TipInjection.TipImage.View1.ScanLinesCount) >
        //                                     maxXrayImageLineNum
        //                ? (maxXrayImageLineNum + 1 - startLineNum)
        //                : image.TipInjection.TipImage.View1.ScanLinesCount;

        //            var rectangle = new Rectangle(image.TipInjection.StartChannel, startLineNum,
        //                image.TipInjection.TipImage.View1.ScanLineLength,
        //                tipCadRegionHeight);

        //            TipInsertionCadRegions.Add(new MarkerRegion(CadRegionType.Tip, rectangle));
        //        }
        //    }
        //}

        /// <summary>
        /// 将Tip区域添加到链表
        /// </summary>
        /// <param name="tipCadRegion"></param>
        //public void AddTipCadRegionToList(MarkerRegion tipCadRegion)
        //{
        //    lock (_tipCadRegionLock)
        //    {
        //        TipInsertionCadRegions.Add(tipCadRegion);
        //    }
        //}

        /// <summary>
        /// 清除包含在要删除的图像中Tip标识框
        /// </summary>
        /// <param name="scanLinesCache"></param>
        //protected void DeleteTipCadRegion(NavigatableScanLinesCache scanLinesCache)
        //{
        //    lock (_tipCadRegionLock)
        //    {
        //        //清除TIP区域链表中要删除图像中的TIP区域，图像中的标记框在标记框不再显示后即被删除
        //        TipInsertionCadRegions.RemoveAll(t =>
        //            scanLinesCache.MaxLineNumber >= t.ToLine &&
        //            scanLinesCache.MinLineNumber <= t.FromLine);
        //    }
        //}

        /// <summary>
        /// 获取向前导航的若干扫描线数据，用于图像前拉时离当前时间较近的图像
        /// </summary>
        /// <returns>返回最多不超过NavigateLines数量的图像扫描线</returns>
        abstract public IEnumerable<DisplayScanlineDataBundle> NavigateFront();

        /// <summary>
        /// 获取向后导航的若干扫描线数据，用于图像后拉时离当前时间较远的图像
        /// </summary>
        /// <returns>返回最多不超过NavigateLines数量的图像扫描线</returns>
        abstract public IEnumerable<DisplayScanlineDataBundle> NavigateBack();

        /// <summary>
        /// 导航至最新的一屏幕数据
        /// </summary>
        /// <returns>离当前时间最近的不超过一屏幕的图像数据；如果对于图像浏览，则是最后浏览的一屏数据</returns>
        abstract public IEnumerable<DisplayScanlineDataBundle> NavigateToLastScreen();
    }

    /// <summary>
    /// 表示可导航的scanlines的集合类
    /// </summary>
    abstract class NavigatableScanLinesCache
    {
        /// <summary>
        /// 是否为空集合：即不包含任何扫描线
        /// </summary>
        public bool Empty
        {
            get { return ScanLines == null || ScanLines.Count == 0; }
        }

        /// <summary>
        /// 最小的线编号，如果数据为空，则返回 int.MaxValue
        /// <remarks>如果Empty为true，此值的意义为未定义。因此需要在使用之前，对Empty进行判断</remarks>
        /// </summary>
        public int MinLineNumber
        {
            get
            {
                return !Empty ? ScanLines.First.Value.LineNumber : 0;
            }
        }

        /// <summary>
        /// 最大的线编号,如果数据为空，则返回int.MaxValue
        /// <remarks>如果Empty为true，此值的意义为未定义。因此需要在使用之前，对Empty进行判断</remarks>
        /// </summary>
        public int MaxLineNumber
        {
            get
            {
                return !Empty ? ScanLines.Last.Value.LineNumber : 0;
            }
        }

        /// <summary>
        /// 此集合中扫描线总数量
        /// </summary>
        public int LinesCount
        {
            get { return Empty ? 0 : MaxLineNumber - MinLineNumber + 1; }
        }

        /// <summary>
        /// 此集合中的所有扫描线
        /// </summary>
        public abstract LinkedList<DisplayScanlineDataBundle> ScanLines { get; }

        /// <summary>
        /// 此图像是否是本次工作期间产生的。true表示此图像是本次运行以来新扫描的
        /// <remarks>设备运行后，对新采集的扫描线从0开始进行编号</remarks>
        /// </summary>
        public bool ScannedThisTime
        {
            get { return MinLineNumber >= 0; }
        }


        /// <summary>
        /// 图像在数据库中对应的记录：如果为空，则表示该图像数据尚未存储完毕
        /// </summary>
        public ImageRecord Record { get; protected set; }
    }

    /// <summary>
    /// 表示位于内存中尚未存储完毕的扫描线数据集合
    /// </summary>
    class MemoryScanLinesCache : NavigatableScanLinesCache
    {
        public MemoryScanLinesCache()
        {
            _scanLines = new LinkedList<DisplayScanlineDataBundle>();
        }

        private readonly LinkedList<DisplayScanlineDataBundle> _scanLines;

        /// <summary>
        /// 扫描线缓存：编号最小的位于队列的头部，编号最大的位于队列的尾部
        /// </summary>
        public override LinkedList<DisplayScanlineDataBundle> ScanLines
        {
            get { return _scanLines; }
        }
    }

    /// <summary>
    /// 表示时间轴上的一幅图像及其相应的数据编号，用于图像导航
    /// <remarks>每个实例表示一幅保存完毕的图像的数据</remarks>
    /// </summary>
    class NavtigatableImage : NavigatableScanLinesCache
    {
        /// <summary>
        /// 根据一幅图像所包含的数据信息，构造节点
        /// </summary>
        /// <param name="record">图像在数据库中的存储记录</param>
        /// <param name="scanLines">图像数据区，其中的数据已经按数据生成时间进行了编号，数据编号从小到大，不允许为null或空</param>
        public NavtigatableImage(ImageRecord record, IEnumerable<DisplayScanlineDataBundle> scanLines)
        {
            if (scanLines == null || !scanLines.Any())
            {
                throw new ArgumentNullException("scanLines");
            }

            Record = record;
            _scanLines = new LinkedList<DisplayScanlineDataBundle>(scanLines);
        }

        private readonly LinkedList<DisplayScanlineDataBundle> _scanLines = new LinkedList<DisplayScanlineDataBundle>();

        /// <summary>
        /// 图像中所包含的扫描线，所有的扫描线已经根据图像所处的缓冲Pool进行了有序编号，编号从小到大
        /// </summary>
        override public LinkedList<DisplayScanlineDataBundle> ScanLines
        {
            get
            {
                // todo: 如果尚未加载图像，则需要重新加载
                return _scanLines;
            }
        }
    }

    /// <summary>
    /// 表示图像回放时的一幅图像及其相应的数据编号，用于图像导航
    /// <remarks>每个实例表示一幅完整的图像的数据</remarks>
    /// </summary>
    class ReplayingNavtigatableImage : NavigatableScanLinesCache
    {
        /// <summary>
        /// 根据一幅图像所包含的数据信息，构造节点
        /// </summary>
        /// <param name="imageFileFullPath">图像完整路径</param>
        /// <param name="scanLines">图像数据区，其中的数据已经按数据生成时间进行了编号，数据编号从小到大，不允许为null或空</param>
        public ReplayingNavtigatableImage(string imageFileFullPath, IEnumerable<DisplayScanlineDataBundle> scanLines)
        {
            if (scanLines == null || !scanLines.Any())
            {
                throw new ArgumentNullException("scanLines");
            }

            FullPath = imageFileFullPath;
            _scanLines = new LinkedList<DisplayScanlineDataBundle>(scanLines);
        }

        /// <summary>
        /// 图像的完整路径
        /// </summary>
        public string FullPath { get; private set; }

        private readonly LinkedList<DisplayScanlineDataBundle> _scanLines = new LinkedList<DisplayScanlineDataBundle>();

        /// <summary>
        /// 图像中所包含的扫描线，所有的扫描线已经根据图像所处的缓冲Pool进行了有序编号，编号从小到大
        /// </summary>
        override public LinkedList<DisplayScanlineDataBundle> ScanLines
        {
            get
            {
                // todo: 如果尚未加载图像，则需要重新加载
                return _scanLines;
            }
        }
    }
}

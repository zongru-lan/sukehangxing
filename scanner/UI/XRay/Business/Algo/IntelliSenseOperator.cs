using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 表示计算机辅助探测的一个算子，在使用该算子的时候，通过传入一个逻辑判断函数，可进行扩展，从而用于穿不透区域判断、毒品与爆炸物
    /// 判断等
    /// </summary>
    public class IntelliSenseOperator
    {
        /// <summary>
        /// 判断条件函数，如果满足条件，该函数将返回true，泛型委托
        /// </summary>
        private readonly Func<ushort, ushort, bool> _conditionFunc;

        /// <summary>
        /// 事件：当一个区域探测成功后，激发此事件，向外部传递探测成功的区域
        /// </summary>
        public event EventHandler<MarkerRegion> RegionDetected;

        //探测区域类型
        private readonly MarkerRegionType _regionType;


        /// <summary>
        /// 一线数据中探测器点数，即一线数据的宽度
        /// </summary>
        private int _channelsCount;

        /// <summary>
        /// 旧的线编号
        /// </summary>
        private int _oldLineNum;

        /// <summary>
        /// 临时存放未闭合的CadRegion区域，用于当前列的连续区域与链表中矩形区域的合并以及链表中节点间的合并
        /// </summary>
        public SortedSet<MarkerRegion> UnCompletedCadRegionsList { get; private set; }

        /// <summary>
        /// 当检测到的一个区域中含有的像素总数大于此值时才认为是可输出的区域。
        /// </summary>
        public int RegionSizeThreshold { get; private set; }

        /// <summary>
        /// 对一线数据中的连续区域宽度进行灵敏度限制，区域宽度小于此值的区域不进行合并或者输出，应该从配置文件中获取
        /// </summary>
        public int ContinuousPointsThreshold { get; private set; }

        /// <summary>
        /// CadOperator构造函数：使用指定的判断条件，构造算子
        /// </summary>
        /// <param name="conditionFunc">判断条件函数。其中函数的第一个参数表示高低能融合的探测值，第二个参数表示
        /// 与探测值对应的物质分类值，第三个返回值类型，即如果满足条件返回true</param>
        /// <param name="regionType">探测区域类型</param>
        /// <param name="nRegionSizeThreshold">区域总像素数阈值</param>
        /// <param name="regionInLineWidthThres">一线数据中连续区域宽度灵敏度</param>
        public IntelliSenseOperator(Func<ushort, ushort, bool> conditionFunc,MarkerRegionType regionType,
                           int nRegionSizeThreshold,int regionInLineWidthThres)
        {
            //传入判断条件
            _conditionFunc = conditionFunc;

            //初始化区域总像素数阈值
            RegionSizeThreshold = nRegionSizeThreshold;

            //初始化探测区域类型
            _regionType = regionType;

            //一线数据中连续区域宽度的灵敏度
            ContinuousPointsThreshold = regionInLineWidthThres;


            _oldLineNum = 0;

            //根据比较方法new一个SortedSet，用于存放临时CadRegion区域，并自动实现排序
            UnCompletedCadRegionsList = new SortedSet<MarkerRegion>();

        }

        /// <summary>
        /// 根据输入的判断条件函数，自动识别某一视角的一线扫描数据，区域探测算法的主函数
        /// </summary>
        /// <param name="scanLineData">要进行检测的一线数据</param>
        public void Detect(DisplayScanlineData scanLineData)
        {
            //获得图像数据和物质分类数据
            var xdata = scanLineData.XRayData;

            var material = scanLineData.Material;

            //一线有多少个探测点，即一线数据的宽度
            _channelsCount = xdata.Length;

            //本线数据在整个数据区编号，用于计算区域在Y方向上的高度，x方向为探测器编号，y方向为扫描线编号
            int lineNum = scanLineData.LineNumber;

            //如果当前线编号既不等于_oldLineNum + 1，也不等于_oldLineNum-1，表明当前操作是突然前拉或者回拉，则将当前探测区域全部输出并清空链表，
            //因为突然前拉或回拉会造成线编号突然变大或者变小，如果某一区域没有完全闭合，且与当前线编号突然变大或者变小的线数据有合并的情况，
            //就会造成探测区域终止线编号被设置成突然变大或者变小的线编号，引起探测区域变得较长。
            if ((_oldLineNum != 0 && lineNum != 0) && ((_oldLineNum + 1) != lineNum && (_oldLineNum-1) != lineNum) )
            {
                //将链表中的探测区域输出到图像
                foreach (MarkerRegion cadRegion in UnCompletedCadRegionsList)
                {
                    FireRegionDetectedEvent(cadRegion);
                }
                //清空链表
                UnCompletedCadRegionsList.Clear();
            }
            //更新先前线编号
            _oldLineNum = lineNum;
            
            //在一线数据中检测到连续区域的标志位
            bool findRegion = false;

            //临时存储一线数据中穿不透区域或者危险品区域的起始位置和终止位置
            int continuousPointsStart = 0;
            int continuousPointsEnd = 0;

            //根据判断条件对数据进行分割，检测一线数据中连续区域的位置，并将其合并或者添加到UnCompletedCadRegionsList中
            for (int i = 0; i < _channelsCount; i++)
            {
                if (_conditionFunc(xdata[i], material[i]))
                {
                    //如果当前点的前一个数据点不满足判断条件，则表明此点是连续区域的起始点，将位置复制给continuousPointsStart
                    if (!findRegion)
                    {
                        findRegion = true;
                        continuousPointsStart = i;
                        continuousPointsEnd = i;
                    }
                    //如果此点前面也是连续区域，则只改变continuousPointsEnd
                    else
                    {
                        continuousPointsEnd++;
                    }
                }
                //如果当前点不满足分割判断条件，表明此处不是连续区域
                else
                {
                    //如果前一个点是连续区域，表明找到了一个完整的连续区域，
                    //将此区域与链表中的TempCadRegionList区域合并或者新加入链表
                    if (findRegion)
                    {
                        //处理检测到的连续区域（连续区域起始位置，终止位置，线编号）
                        ProcessSequenceRegion(continuousPointsStart,continuousPointsEnd, lineNum);
                        //重置是否发现连续区域标识位
                        findRegion = false;
                    }
                }
            }

            //一线数据处理结束，在一线数据的末尾可能存在连续区域，将其进行处理
            if (findRegion)
            {
               //处理检测到的连续区域（连续区域起始位置，终止位置，线编号）
               ProcessSequenceRegion(continuousPointsStart,continuousPointsEnd,lineNum);
            }
            //将前面未合并的区域输出
            
            //以下说明是对于从左向右运动的图像
            //显示数据的线编号：大---------------(正数) 0  （负数）------------------->小
            //                      Cadregion结构（矩形框）
            //                             ------
            //  ToLine(即rect.bottom)<--  |      |  --> FromLine(即rect.Top)
            //                            |      |
            //                             ------
            // 对于Cadregion结构，总是认为左侧是ToLine(即rect.bottom)，右侧是FromLine(即rect.Top)

            //对于前拉的图像：
            //判断未合并的区域时根据CadRegion的ToLine 判断，前拉时图像从左向右走图，当前线编号是最左侧的线数据的编号，
            //如果某个Cadregion区域的ToLine属性<当前线编号lineNum，即位于当前线的右侧，则表明没有经过合并,则后续也不可能有合并，因此输出到图像

            //对于回拉的图像
            //判断未合并的区域时根据CadRegion的FromLine（终止线编号）判断，回拉时图像从右向左运行，当前线编号是最右侧的线数据的编号，
            //如果某个Cadregion区域的FromLine属性>当前线编号lineNum，即位于当前线左侧，则表明没有经过合并,则后续也不可能有合并，因此输出到图像
            foreach (var cadRegion in UnCompletedCadRegionsList)
            {
                //遍历链表，如果没有合并且区域大小大于区域阈值，则将其输出到图像
                if ((cadRegion.ToLine < lineNum || cadRegion.FromLine > lineNum) && cadRegion.RegionAreaSize >= RegionSizeThreshold)
                {
                    FireRegionDetectedEvent(cadRegion);
                }
            }

            //将所有未合并的区域删除,判断未合并的区域时根据CadRegion的ToLine（终止线编号）判断，因为新建CadRegion
            //总是将终止线编号设置为当前处理线编号，因此如果区域的ToLine属性!=当前线编号lineNum，则表明没有经过合并
            UnCompletedCadRegionsList.RemoveWhere(cad => (cad.ToLine < lineNum || cad.FromLine > lineNum));

            //如果链表中节点数大于1个才进行合并
            if (UnCompletedCadRegionsList.Count>1)
            {
                //合并UnCompletedRegionsList中有交集的节点区域
                MergeCadRegions();
            }
        }

        /// <summary>
        /// 在一线数据中检测到一个连续区域后的处理过程：将其合并或者添加进UnCompletedRegionsList链表中
        /// </summary>
        /// <param name="continuousPointsStart">连续区域开始的位置</param>
        /// <param name="continuousPointsEnd">连续区域结束位置</param>
        /// <param name="lineNum">当前线数据编号</param>
        private void ProcessSequenceRegion(int continuousPointsStart, int continuousPointsEnd, int lineNum)
        {

            //检测到的连续区域的宽度
            int continuousRegionWidth = continuousPointsEnd - continuousPointsStart+1;

            //如果连续区域的宽度大于阈值，才进行处理
            if (continuousRegionWidth > ContinuousPointsThreshold)
            {
                //创建一线数据中一个CadRegion矩形区域，方便合并和添加
                var rectangle = new Rectangle(continuousPointsStart, lineNum, continuousRegionWidth, 0);
                var newRegion=new MarkerRegion(_regionType,rectangle,false);

                //如果UnCompletedRegionsList中为空，表示链表中没有数据，将新CadRegion添加到UnCompletedRegionsList中
                if (UnCompletedCadRegionsList.Count == 0)
                {
                    UnCompletedCadRegionsList.Add(newRegion);
                }
                //遍历UnCompletedRegionsList链表，若连续区域newRegion与链表中CadRegion区域在X方向上有交集，
                //将此区域暂存到链表hasCrossAreaCadRegions，因为在foreach中无法执行删除操作
                else
                {
                    //检测到的线连续区域是否与已存在的CadRegion在X方向上有交集的标志位
                    bool beMerged = false;

                    //存放与连续区域有交集的区域,因为在foreach中不能删除元素和修改元素
                    var hasCrossAreaCadRegionsList = new List<MarkerRegion>();

                    //选择UnCompletedRegionsList中位于newRegion前面的节点，这里要不要加？
                    var regionsBeforeNewRegion = UnCompletedCadRegionsList.Where(cad => cad.FromChannel <= newRegion.ToChannel);

                    //如果选择的区域与newRegion有交集，则将其暂存
                    foreach (var cadRegion in regionsBeforeNewRegion.Where(cadRegion => cadRegion.IntersectInXWith(newRegion)))
                    {
                        //将此区域暂存到链表hasCrossAreaCadRegions
                        hasCrossAreaCadRegionsList.Add(cadRegion);
                        //此newRegion区域有进行合并
                        beMerged = true;
                    }

                    //如果newRegion没有合并，则直接将此连续区域添加到UnCompletedRegionsList中
                    if (!beMerged)
                    {
                        UnCompletedCadRegionsList.Add(newRegion);
                    }
                    else
                    {
                        //删除与连续区域有交集的CadRegion区域，因为合并后其起始探测通道编号可能改变了，需要重新排序
                        foreach (var hasCrossAreaCad in hasCrossAreaCadRegionsList)
                        {
                           UnCompletedCadRegionsList.Remove(hasCrossAreaCad);
                        }
                        //将有交集的区域合并后重新添加到UnCompletedRegionsList
                        foreach (var hasCrossAreaCadRegion in hasCrossAreaCadRegionsList)
                        {
                            //将newRegion合并到hasCrossAreaCadRegion
                            hasCrossAreaCadRegion.InflateWith(newRegion);
                            //添加到UnCompletedRegionsList中
                            UnCompletedCadRegionsList.Add(hasCrossAreaCadRegion);
                        }                       
                    }
                }
            }
        }

        /// <summary>
        /// 合并临时链表UnCompletedRegionsList中有交集的区域
        /// </summary>
        private void MergeCadRegions()
        {
            //因合并涉及到相邻两个元素的比较和更改，不能在UnCompletedRegionsList中操作，先放入list中
            List<MarkerRegion> newList = UnCompletedCadRegionsList.ToList();
            //因为List是有序的，因此只需比较前后两个区域，从后向前比较，
            //如果后面一个与前面一个区域有交集，则将后面一个区域合并到前面一个区域
            for (int i = newList.Count-1; i >0; i--)
            {
                MarkerRegion regionSrc = newList[i];

                MarkerRegion regionDest = newList[i - 1];

                //如果两个区域在X方向上 有重合区域
                if (regionSrc.IntersectInXWith(regionDest))
                {
                    //将UnCompletedRegionsList链表中的比较的两个元素删除，因为合并改变了元素，需要重新排序
                    UnCompletedCadRegionsList.Remove(regionDest);
                    UnCompletedCadRegionsList.Remove(regionSrc);
                    //将regionSrc合并到regionDest
                    regionDest.InflateWith(regionSrc);
                    //将合并的区域添加到UnCompletedRegionsList，添加时会自动排序
                    UnCompletedCadRegionsList.Add(regionDest);
                }
            }
        }
        /// <summary>
        /// 激发区域检测成功的事件RegionDetected,向外部传递一个新探测的Cad区域。
        /// </summary>
        /// <param name="markerRegion">检测到的穿不透区域或者危险品区域</param>
        private void FireRegionDetectedEvent(MarkerRegion markerRegion)
        {
            if (RegionDetected != null)
            {
                RegionDetected(this, markerRegion);
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 白线检测 -- 多线
    /// </summary>
    public class AirLinesChecker
    {
        /// <summary>
        /// 背景阈值的下限，一般为64000左右。
        /// </summary>
        private int backgroundLower = 60000;

        /// <summary>
        /// 通道开始置白位置
        /// </summary>
        private int startMarginCount;

        /// <summary>
        /// 通道末尾置白位置
        /// </summary>
        private int endMarginCount;

        /// <summary>
        /// 视角
        /// </summary>
        private DetectViewIndex view;

        /// <summary>
        /// 线数据数量
        /// </summary>
        private int arrayRows;

        /// <summary>
        /// 通道长度
        /// </summary>
        private int arrayCols;

        /// <summary>
        /// 边缘回退
        /// </summary>
        private int bagEdgeReserved = 16;

        /// <summary>
        /// 置白像素值
        /// </summary>
        private ushort setPointBlank = 65530;

        /// <summary>
        /// 核尺寸
        /// </summary>
        private int kernelSize = 3;

        /// <summary>
        /// 平滑方法
        /// </summary>
        private int smoothFunc = 0;

        /// <summary>
        /// 物体最小高度
        /// </summary>
        private int airRegionsMinDistance = 8;

        /// <summary>
        /// 皮带边参数
        /// </summary>
        private BeltEdgePosition belt = new BeltEdgePosition(DetectViewIndex.View1);

        /// <summary>
        /// 计算均值临时变量
        /// </summary>
        private LinkedList<int> sumsOfCols = new LinkedList<int>();

        /// <summary>
        /// 计算中值临时变量
        /// </summary>
        private LinkedList<int> medianOfCols = new LinkedList<int>();

        /// <summary>
        /// 计算中值临时变量1
        /// </summary>
        private LinkedList<int> medianOfRows0 = new LinkedList<int>();
        private LinkedList<int> medianOfRows1 = new LinkedList<int>();
        private LinkedList<int> medianOfRows2 = new LinkedList<int>();

        private LinkedList<int> medianOfRowsRESULT = new LinkedList<int>();

        /// <summary>
        /// 连续n个点有物体判断为物体边缘，否则为空气，暂时未启用
        /// </summary>
        private int findBorder = 5;

        /// <summary>
        /// 构造空气扫描线判定实例
        /// </summary>
        /// <param name="view">视角</param>
        /// <param name="startMarginCount">计算起始的探测通道号，最小值为0</param>
        /// <param name="endMarginCount">计算终结的探测通道号，最小值为通道总数-1</param>
        /// <param name="backgroundLower">空气扫描线均值的下限</param>
        public AirLinesChecker(DetectViewIndex view, int startMarginCount, int endMarginCount)
        {
            if (startMarginCount < 0)
            {
                throw new ArgumentException("startMarginCount");
            }

            this.startMarginCount = startMarginCount;
            this.endMarginCount = endMarginCount;


            //为了去除噪声这部分单独使用一个背景阈值变量，不去调用以前的背景阈值，构造函数也同步修改，现在背景噪声去除的
            //背景阈值是从注册表读取的"PreProc/Filter/DirtyBKG"  初步测试60000可行  58000-61500之间调节
            //this.backgroundLower = backgroundLower;  

            this.view = view;

            this.GetViewBeltEdgePosition(view, ref belt);
            ScannerConfig.ConfigChanged += ScannerConfig_ConfigChanged;
        }

        /// <summary>
        /// 注册表更改后执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScannerConfig_ConfigChanged(object sender, EventArgs e)
        {
            this.GetViewBeltEdgePosition(view, ref belt);
        }

        /// <summary>
        /// 判断一线数据是否为空气扫描,同时更新其内部属性
        /// </summary>
        /// <param name="line">要进行判定的数据</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void TestAirLine(List<ClassifiedLineDataBundle> lines)
        {
            this.arrayRows = lines.Count;
            this.arrayCols = view == DetectViewIndex.View1 ? lines[0].View1Data.XRayData.Length : lines[0].View2Data.XRayData.Length;

            ushort[][] rawData = new ushort[this.arrayRows][];
            ushort[][] smoothData = new ushort[this.arrayRows][];

            for (int row = 0; row < this.arrayRows; row++)
            {
                rawData[row] = new ushort[this.arrayCols];
                smoothData[row] = new ushort[this.arrayCols];
                if (view == DetectViewIndex.View1)
                {
                    Buffer.BlockCopy(lines[row].View1Data.XRayData, 0, rawData[row], 0, this.arrayCols * sizeof(ushort));
                    Buffer.BlockCopy(lines[row].View1Data.XRayData, 0, smoothData[row], 0, this.arrayCols * sizeof(ushort));
                }
                else
                {
                    if (lines[row].View2Data !=null && lines[row].View2Data.XRayData != null)
                    {
                        Buffer.BlockCopy(lines[row].View2Data.XRayData, 0, rawData[row], 0, this.arrayCols * sizeof(ushort));
                        Buffer.BlockCopy(lines[row].View2Data.XRayData, 0, smoothData[row], 0, this.arrayCols * sizeof(ushort));
                    }                    
                }
            }

            this.ArrayFiltrFunc(rawData, smoothData, kernelSize);

            /*
            if (belt.Edge1End - belt.Edge1Start > 0)
            {
                start = Math.Max(belt.Edge1Start-1, 0);
                end = Math.Min(belt.Edge1End-1, this.arrayCols);
                var recs = this.GetAirRecs(smoothData, start, end);

                var rec = this.CalContinuousAirChannelsInAllLines(recs, start, end);

                SetRecsDataBlank(rec, recs, lines);
            }

            if (belt.Edge2End - belt.Edge2Start > 0)
            {
                start = Math.Max(belt.Edge2Start - 1, 0);
                end = Math.Min(belt.Edge2End - 1, this.arrayCols);
                var recs = this.GetAirRecs(smoothData, start, end);

                var rec = this.CalContinuousAirChannelsInAllLines(recs, start, end);

                SetRecsDataBlank(rec, recs, lines);
            }
            */

            int start = Math.Max(startMarginCount, 0);
            int end = Math.Min(this.arrayCols - this.endMarginCount, this.arrayCols);
            var recsAll = this.GetAirRecs(smoothData, start, end);

            var rec = this.CalContinuousAirChannelsInAllLines(recsAll, start, end);

            SetRecsDataBlank(rec, recsAll, lines);

            //判断是否为白线
            TestLineIsAir(recsAll, lines);

            return;
        }

        /// <summary>
        /// 滤波处理
        /// </summary>
        /// <param name="originalArray">原始图像数组</param>
        /// <param name="destinationArray">滤波后数组</param>
        /// <param name="filterSize">核尺寸</param>
        /// <param name="start">起始通道</param>
        /// <param name="end">终止通道</param>
        private void ArrayFiltrFunc(ushort[][] originalArray, ushort[][] destinationArray, int filterSize)
        {
            if (smoothFunc == 0)
            {
                GetAverage(originalArray, destinationArray, filterSize);
            }
            else if (smoothFunc == 1)
            {
                GetMax(originalArray, destinationArray, filterSize);
            }
            else if (smoothFunc == 2)
            {
                GetMedian0(originalArray, destinationArray, filterSize);
            }
            else if (smoothFunc == 3)
            {
                GetMedian1(originalArray, destinationArray, filterSize);
            }
        }

        /// <summary>
        /// 均值滤波
        /// </summary>
        /// <param name="originalArray">源数组</param>
        /// <param name="destinationArray">滤波后数组</param>
        /// <param name="filterSize">滤波尺寸</param>
        private void GetAverage(ushort[][] originalArray, ushort[][] destinationArray, int filterSize)
        {
            int rows = this.arrayRows;
            int cols = this.arrayCols;
            int filterRadius = (filterSize - 1) / 2;
            int sum = 0;
            try
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        if ((row < filterRadius) || (row >= rows - filterRadius) || (col < filterRadius) || (col >= cols - filterRadius))
                        {
                            //destinationArray[row][col] = originalArray[row][col];
                            destinationArray[row][col] = 65530;
                            continue;
                        }

                        if (col == filterRadius)
                        {
                            sum = 0;
                            sumsOfCols.Clear();

                            for (int j = col - filterRadius; j <= col + filterRadius; j++)
                            {
                                int temp = 0;
                                for (int i = row - filterRadius; i <= row + filterRadius; i++)
                                {
                                    temp += originalArray[i][j];
                                }

                                sum += temp;
                                sumsOfCols.AddLast(temp);
                            }

                            destinationArray[row][col] = (ushort)(sum / (filterSize * filterSize));
                        }
                        else
                        {
                            sum -= sumsOfCols.First.Value;
                            sumsOfCols.RemoveFirst();

                            int temp = 0;
                            for (int i = row - filterRadius; i <= row + filterRadius; i++)
                            {
                                temp += originalArray[i][col + filterRadius];
                            }

                            sum += temp;
                            sumsOfCols.AddLast(temp);
                            destinationArray[row][col] = (ushort)(sum / (filterSize * filterSize));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        /// <summary>
        /// 最大值滤波
        /// </summary>
        /// <param name="originalArray">源数组</param>
        /// <param name="destinationArray">滤波后数组</param>
        /// <param name="filterSize">滤波尺寸</param>
        private void GetMax(ushort[][] originalArray, ushort[][] destinationArray, int filterSize)
        {
            int rows = this.arrayRows;
            int cols = this.arrayCols;
            int filterRadius = (filterSize - 1) / 2;
            ushort max = 0;
            try
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        max = 0;
                        for (int i = row - filterRadius; i <= row + filterRadius; i++)
                        {
                            for (int j = col - filterRadius; j <= col + filterRadius; j++)
                            {
                                if ((i < 0) || (i >= rows) || (j < 0) || (j >= cols))
                                {
                                    continue;
                                }

                                max = Math.Max(max, originalArray[i][j]);
                            }
                        }

                        destinationArray[row][col] = max;
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }


        /// <summary>
        /// 中值滤波
        /// </summary>
        /// <param name="originalArray">源数组</param>
        /// <param name="destinationArray">滤波后数组</param>
        /// <param name="filterSize">滤波尺寸</param>
        private void GetMedian0(ushort[][] originalArray, ushort[][] destinationArray, int filterSize)
        {
            int rows = this.arrayRows;
            int cols = this.arrayCols;
            int filterRadius = (filterSize - 1) / 2;


            try
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        medianOfCols.Clear();
                        for (int i = row - filterRadius; i <= row + filterRadius; i++)
                        {
                            for (int j = col - filterRadius; j <= col + filterRadius; j++)
                            {
                                int temp = 0;
                                if ((i < 0) || (i >= rows) || (j < 0) || (j >= cols))
                                {
                                    temp = originalArray[row][col];
                                    medianOfCols.AddLast(temp);
                                    continue;
                                }

                                temp = originalArray[i][j];
                                medianOfCols.AddLast(temp);
                                
                            }
                        }

                        var result = SortLinkedList(medianOfCols);
                        destinationArray[row][col] = (ushort)result.ElementAt((filterSize* filterSize -1)/2);


                    }
                }
            }



          
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }


        /// <summary>
        /// 中值滤波
        /// </summary>
        /// <param name="originalArray">源数组</param>
        /// <param name="destinationArray">滤波后数组</param>
        /// <param name="filterSize">滤波尺寸</param>
        private void GetMedian1(ushort[][] originalArray, ushort[][] destinationArray, int filterSize)
        {
            int rows = this.arrayRows;
            int cols = this.arrayCols;
            //int filterRadius = 1;
            //int max0 = 0;
            //int max1 = 0;
            //int max2 = 0;
            //int min0 = 0;
            //int min1 = 0;
            //int min2 = 0;


            try
            {
                for (int row = 1; row < rows - 1; row++)
                {
                    for (int col = 1; col < cols - 1; col++)
                    {
                        medianOfRows0.Clear();
                        medianOfRows1.Clear();
                        medianOfRows2.Clear();

                        medianOfRowsRESULT.Clear();

                        medianOfRows0.AddFirst((int)originalArray[row - 1][col - 1]);
                        medianOfRows0.AddFirst((int)originalArray[row - 1][col]);
                        medianOfRows0.AddFirst((int)originalArray[row - 1][col + 1]);

                        medianOfRows1.AddFirst((int)originalArray[row][col - 1]);
                        medianOfRows1.AddFirst((int)originalArray[row][col]);
                        medianOfRows1.AddFirst((int)originalArray[row][col + 1]);

                        medianOfRows2.AddFirst((int)originalArray[row + 1][col - 1]);
                        medianOfRows2.AddFirst((int)originalArray[row + 1][col]);
                        medianOfRows2.AddFirst((int)originalArray[row + 1][col + 1]);

                        medianOfRows0.Remove(medianOfRows0.Max());
                        medianOfRows0.Remove(medianOfRows0.Min());

                        medianOfRows1.Remove(medianOfRows1.Max());
                        medianOfRows1.Remove(medianOfRows1.Min());

                        medianOfRows2.Remove(medianOfRows2.Max());
                        medianOfRows2.Remove(medianOfRows2.Min());

                        medianOfRowsRESULT.AddFirst(medianOfRows0.First());
                        medianOfRowsRESULT.AddFirst(medianOfRows1.First());
                        medianOfRowsRESULT.AddFirst(medianOfRows2.First());

                        medianOfRowsRESULT.Remove(medianOfRowsRESULT.Max());
                        medianOfRowsRESULT.Remove(medianOfRowsRESULT.Min());


                        destinationArray[row][col] = (ushort)medianOfRowsRESULT.First.Value;


                    }

                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private LinkedList<int> SortLinkedList(LinkedList<int> linkedlist)
        {
            LinkedList<int> result = new LinkedList<int>();
            foreach(int nodevalue in linkedlist)
            {
                LinkedListNode<int> lln = result.First;
                while(true)
                {
                    if(lln ==null)
                    {
                        result.AddLast(nodevalue);
                        break;
                    }
                    else if (nodevalue <= lln.Value)
                    {
                        result.AddBefore(lln, nodevalue);
                        break;
                    }
                    else
                    {
                        lln = lln.Next;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 背景置白
        /// </summary>
        /// <param name="recs">空白并集</param>
        /// <param name="recss">各线空白区域</param>
        /// <param name="lines">线数据</param>
        private void SetRecsDataBlank(List<AirRecInLine> recs, List<AirRecsInLine> recss, List<ClassifiedLineDataBundle> lines)
        {
            if (recs != null && recs.Count > 0)
            {
                for (int i = 0; i < recs.Count; i++)
                {
                    SetRecDataBlank(recs[i], recss, lines);
                }
            }
        }

        private void SetRecDataBlank(AirRecInLine rec, List<AirRecsInLine> recs, List<ClassifiedLineDataBundle> lines)
        {
            if (recs.Count != lines.Count)
            {
                return;
            }

            if (view == DetectViewIndex.View1)
            {
                for (int row = 0; row < this.arrayRows; row++)
                {
                    //if (recs[row].IsAir)
                    //{
                        for (int col = rec.XStart; col < rec.XEnd; col++)
                        {
                            if (lines[row].View1Data.XRayData[col] > 40000)
                            {
                                lines[row].View1Data.XRayData[col] = setPointBlank;
                                lines[row].View1Data.Material[col] = 50;
                                if (lines[row].View1Data.XRayDataEnhanced != null)
                                {
                                    lines[row].View1Data.XRayDataEnhanced[col] = setPointBlank;
                                }
                            }
                        }
                    //}
                }
            }
            else
            {
                for (int row = 0; row < this.arrayRows; row++)
                {
                    //if (recs[row].IsAir)
                    //{
                        for (int col = rec.XStart; col < rec.XEnd; col++)
                        {
                            if (lines[row].View2Data.XRayData[col] > 40000)
                            {
                                lines[row].View2Data.XRayData[col] = setPointBlank;
                                lines[row].View2Data.Material[col] = 50;
                                if (lines[row].View2Data.XRayDataEnhanced != null)
                                {
                                    lines[row].View2Data.XRayDataEnhanced[col] = setPointBlank;
                                }
                            }
                        }
                    //}
                }
            }
        }
        
        /// <summary>
        /// 判断是否是空白线
        /// </summary>
        /// <param name="lines"></param>
        private void TestLinesIsAir(List<ClassifiedLineDataBundle> lines)
        {
            if (view == DetectViewIndex.View1)
            {
                foreach (var line in lines)
                {
                    TestLineIsAir(line.View1Data);
                }
                if (lines.All(line=>line.View1Data.IsAir))
                {
                    foreach (var line in lines)
                    {
                        line.View1Data.IsAir = true;
                    }
                }
                else
                {
                    foreach (var line in lines)
                    {
                        line.View1Data.IsAir = false;
                    }
                }
            }
            else
            {
                foreach (var line in lines)
                {
                    TestLineIsAir(line.View2Data);
                }
                if (lines.All(line => line.View2Data.IsAir))
                {
                    foreach (var line in lines)
                    {
                        line.View2Data.IsAir = true;
                    }
                }
                else
                {
                    foreach (var line in lines)
                    {
                        line.View2Data.IsAir = false;
                    }
                }
            }
        }

        /// <summary>
        /// 查找空白区域
        /// </summary>
        /// <param name="destinationArray">数组</param>
        /// <param name="start">起始像素位置</param>
        /// <param name="end">终止像素位置</param>
        /// <returns>空白区域并集</returns>
        private List<AirRecsInLine> GetAirRecs(ushort[][] destinationArray, int start, int end)
        {
            var airRecsInLines = new List<AirRecsInLine>();

            for (int i = 0; i < this.arrayRows; i++)
            {
                var rec = GetAirRegionInLine(destinationArray, i, start, end);
                airRecsInLines.Add(rec);
            }

            return airRecsInLines;
        }

        /// <summary>
        /// 多个数组取并集
        /// </summary>
        /// <param name="airRecsInLines">多个空白区域</param>
        /// <param name="channelStart">起始通道</param>
        /// <param name="channelEnd">终止通道</param>
        /// <returns>空白区域</returns>
        private List<AirRecInLine> CalContinuousAirChannelsInAllLines(List<AirRecsInLine> airRecsInLines, int channelStart, int channelEnd)
        {
            if (airRecsInLines == null || airRecsInLines.Count == 0)
            {
                return null;
            }

            var result = new List<AirRecInLine>();

            ////先判断是否全是白线，如果全是白线，则只返回一个rec，否则返回左右两个rec
            if (airRecsInLines.All(airRecsInLine => airRecsInLine != null && airRecsInLine.IsAir))
            {
                var start = airRecsInLines[0].AirRec.XStart;
                var end = airRecsInLines[0].AirRec.XEnd;

                if (start < channelStart)
                {
                    start = channelStart;
                }

                if (end > channelEnd)
                {
                    end = channelEnd;
                }

                result.Add(new AirRecInLine { XStart = start, XEnd = end });
            }
            else
            {
                var left = GetLeftOrRightAirRec(airRecsInLines, true);

                if (left != null)
                {
                    if (left.XStart < channelStart)
                    {
                        left.XStart = channelStart;
                    }

                    if (left.XEnd > channelEnd)
                    {
                        left.XEnd = channelEnd;
                    }

                    result.Add(left);
                }

                var right = GetLeftOrRightAirRec(airRecsInLines, false);

                if (right != null)
                {
                    if (right.XStart < channelStart)
                    {
                        right.XStart = channelStart;
                    }

                    if (right.XEnd > channelEnd)
                    {
                        right.XEnd = channelEnd;
                    }

                    result.Add(right);
                }
            }

            return result;
        }

        private AirRecInLine GetLeftOrRightAirRec(List<AirRecsInLine> airRecsInLines, bool left)
        {
            var recs =
                airRecsInLines.Select(
                    airRecsInLIne =>
                        airRecsInLIne.IsAir
                            ? airRecsInLIne.AirRec
                            : (left ? airRecsInLIne.LeftRec : airRecsInLIne.RightRec));

            if (recs.Count(rec => rec == null) == 0)
            {
                int start = recs.Max(i => i.XStart);
                int end = recs.Min(i => i.XEnd);

                if (left)
                {
                    end -= this.bagEdgeReserved;
                    end = Math.Max(0, end);
                }
                else
                {
                    start += this.bagEdgeReserved;
                    start = Math.Min(start, end);
                }

                if (end - start + 1 < this.airRegionsMinDistance)
                {
                    return null;
                }

                return new AirRecInLine() { XStart = start, XEnd = end };
            }

            return null;
        }

        /// <summary>
        /// 查找每一线数据的空白区域
        /// </summary>
        /// <param name="smoothedImage">图像数组</param>
        /// <param name="lineIndex">线索引</param>
        /// <param name="start">起始通道</param>
        /// <param name="end">终止通道</param>
        /// <returns></returns>
        private AirRecsInLine GetAirRegionInLine(ushort[][] smoothedImage, int lineIndex, int start, int end)
        {
            if (smoothedImage == null || end <= start || lineIndex < 0)
            {
                return null;
            }

            AirRecsInLine result = new AirRecsInLine();
            int recStart = start;
            int recEnd = start;

            for (int i = start; i < end - findBorder; i++)
            {
                if (findRecEnd(smoothedImage, lineIndex, i))
                {
                    recEnd = i;
                    continue;
                }

                break;
            }

            ////最终找到的边界和end比较,如果小于两个像素，则认为是白线，直接返回，不再进行右边空白线的判断
            if (Math.Abs(recEnd - end) <= this.airRegionsMinDistance)
            {
                recEnd = end;
                result.AirRec = new AirRecInLine { XStart = recStart, XEnd = recEnd };
                return result;
            }

            var leftRec = new AirRecInLine { XStart = recStart, XEnd = recEnd };

            ////记录找到的左边界的结尾
            int leftRecEnd = recEnd;

            recStart = end;
            recEnd = end;

            for (int i = end - 1; i > leftRecEnd + findBorder; i--)
            {
                if (findRecStart(smoothedImage, lineIndex, i))
                {
                    recStart = i;
                    continue;
                }

                break;
            }

            if (Math.Abs(recStart - start) <= this.airRegionsMinDistance)
            {
                recStart = start;
                result.AirRec = new AirRecInLine { XStart = recStart, XEnd = recEnd };
                return result;
            }

            var rightRec = new AirRecInLine { XStart = recStart, XEnd = recEnd };

            if (Math.Abs(leftRec.XEnd - rightRec.XStart) <= this.airRegionsMinDistance)
            {
                result.AirRec = new AirRecInLine() { XStart = leftRec.XStart, XEnd = rightRec.XEnd };
                return result;
            }

            result.LeftRec = leftRec;
            result.RightRec = rightRec;
            return result;
        }


        private bool findRecEnd(ushort[][] smoothedImage, int lineIndex, int currentIndex)
        {
            for (int j = 0; j < findBorder; j++)
            {
                int current = smoothedImage[lineIndex][currentIndex + j];
                if (current >= this.backgroundLower)
                    return true;
            }
            return false;
        }

        private bool findRecStart(ushort[][] smoothedImage, int lineIndex, int currentIndex)
        {
            for (int j = 0; j < findBorder; j++)
            {
                int current = smoothedImage[lineIndex][currentIndex - j];
                if (current >= this.backgroundLower)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 一组都为空气，
        /// </summary>
        /// <param name="recs"></param>
        /// <param name="lines"></param>
        private void TestLineIsAir(List<AirRecsInLine> recs, List<ClassifiedLineDataBundle> lines)
        {
            bool isAllAir = false;
            if (recs.All(rec => rec.IsAir))
            {
                isAllAir = true;
            }

            if (view == DetectViewIndex.View1)
            {
                foreach (var line in lines)
                {
                    line.View1Data.IsAir = isAllAir;
                }
            }
            else
            {
                foreach (var line in lines)
                {
                    line.View2Data.IsAir = isAllAir;
                }
            }
        }

        public bool TestLineIsAir(ClassifiedLineData line)
        {
            var start = 0;
            var end = line.XRayData.Length ;
            int block = 32;
            for (int i = start; i < end - block; i += block)
            {
                long sumLow = 0;
                var count = 0;
                for (int j = 0; j < block; j++)
                {
                    sumLow += line.XRayData[i + j];
                    count++;
                }
                if (count > 0)
                {
                    // 将均值与阈值进行比较，判断是否为空气值
                    if (sumLow / count <= backgroundLower)
                    {
                        line.IsAir = false;
                        return false;
                    }
                }
            }
            line.IsAir = true;
            return true;
        }
        private void GetViewBeltEdgePosition(DetectViewIndex view, ref BeltEdgePosition viewBeltEdgePosition)
        {
            try
            {
                int postion;
                if (ScannerConfig.Read(view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdge1Start : ConfigPath.MachineView2BeltEdge1Start, out postion))
                {
                    viewBeltEdgePosition.Edge1Start = postion;
                }

                if (ScannerConfig.Read(view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdge1End : ConfigPath.MachineView2BeltEdge1End, out postion))
                {
                    viewBeltEdgePosition.Edge1End = postion;
                }

                if (ScannerConfig.Read(view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdge2Start : ConfigPath.MachineView2BeltEdge2Start, out postion))
                {
                    viewBeltEdgePosition.Edge2Start = postion;
                }

                if (ScannerConfig.Read(view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdge2End : ConfigPath.MachineView2BeltEdge2End, out postion))
                {
                    viewBeltEdgePosition.Edge2End = postion;
                }

                if (ScannerConfig.Read(ConfigPath.PreProcBagEdgeReserved, out postion))
                {
                    this.bagEdgeReserved = postion;
                }

                ScannerConfig.Read(ConfigPath.PreProcBlankPoint, out setPointBlank);
                ScannerConfig.Read(ConfigPath.PreProcFilterKernelSize, out kernelSize);

                if (!ScannerConfig.Read(ConfigPath.PreProcFilterSmoothFunc, out smoothFunc))
                {
                    smoothFunc = 0;
                }

                if (!ScannerConfig.Read(
                    view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdgeAtBegin : ConfigPath.MachineView2BeltEdgeAtBegin, out startMarginCount))
                {
                    startMarginCount = 5;
                }

                if (!ScannerConfig.Read(view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdgeAtEnd : ConfigPath.MachineView2BeltEdgeAtEnd, out endMarginCount))
                {
                    this.endMarginCount = 5;
                }


                if (!ScannerConfig.Read(ConfigPath.PreProcFindBorder, out findBorder))
                {
                    findBorder = 2;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcAirRegionsMinDistance, out airRegionsMinDistance))
                {
                    airRegionsMinDistance = 10;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcFilterDirtyBKG, out backgroundLower))
                {
                    backgroundLower = 60000;
                }

                Tracer.TraceInfo($"[AirLinesChecker] Edge1Start: {viewBeltEdgePosition.Edge1Start}, Edge1End: {viewBeltEdgePosition.Edge1End}, Edge2Start: {viewBeltEdgePosition.Edge2Start}, " +
                    $"Edge2End: {viewBeltEdgePosition.Edge2End}, bagEdgeReserved: {bagEdgeReserved}, setPointBlank: {setPointBlank}, kernelSize: {kernelSize}, smoothFunc: {smoothFunc}, " +
                    $"startMarginCount: {startMarginCount}, endMarginCount: {endMarginCount}，findBorder:{findBorder},airRegionMinDistance:{airRegionsMinDistance},DirtyBKG:{backgroundLower}");
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }
    }
}

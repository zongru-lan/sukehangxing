using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 动态满度更新器
    /// 
    /// todo:图像中的区域包括两种，一种是包裹两边的区域，一种是白线区域，因此检测是只需要检测两边和白线区域，
    /// TODO：图像中间的区域检测起来比较危险，增加了判断的难度，也容易出现异常情况。
    /// </summary>
    public class DynamicAirDataAutoUpdater
    {
        private DetectViewIndex _viewIndex;

        private readonly ConcurrentQueue<DynamicUpdateAirDatas> _dynamicAirUpdateDatasQueue =
            new ConcurrentQueue<DynamicUpdateAirDatas>();

        private readonly LinkedList<ScanlineData> _tempAirDatas = new LinkedList<ScanlineData>();

        private Task _dynamicAirDataUpdateTask;

        private bool _exit;

        private bool _onlyAllAirLineUpdate = false;

        private ushort _airTh;

        private int _marginBegin;
        private int _marginEnd;

        private ScanlineData _referenceAir;

        /// <summary>
        /// 非包裹区域宽度最小要10个像素，左右各去2，实际最小区域大小为6个像素
        /// </summary>
        private const int MinRecWidth = 10;

        /// <summary>
        /// 检测到非包裹区域后左右各去2
        /// </summary>
        private const int RecLeftRightOffset = 2;

        /// <summary>
        /// 判断air区域的容忍度，左右两个air区域间的间隔在这个容忍度之内认为是同一个区域
        /// </summary>
        private const int AirRegionTolerance = 4;

        private int _avgFilterWinSize = 5;

        private int _halAvgFilterWinSize;

        private FastAvgFilterHelper _fastAvgFilterHelper;

        /// <summary>
        /// 连续n个点有物体判断为物体边缘，否则为空气
        /// </summary>
        private int _findBorderUpdate = 6;


        /// <summary>
        /// 参考满度值
        /// </summary>
        public ScanlineData ReferenceAir
        {
            get { return _referenceAir; }
            set
            {
                _referenceAir =DeepCopy.DeepCopyByBin( value);
            }
        }

        public DynamicAirDataAutoUpdater(DetectViewIndex view)
        {
            _viewIndex = view;
        }

        public void StartService(ushort airTh, int marginBegin, int marginEnd, int avgFilterSize, bool onlyAllAirLineUpdate)
        {
            StopService();

            _airTh = airTh;
            _marginBegin = marginBegin;
            _marginEnd = marginEnd;

            _avgFilterWinSize = avgFilterSize;
            _halAvgFilterWinSize = _avgFilterWinSize >> 1;

            //因为采用5*5的滤波，查找边界的时候,去掉窗口大小的影响
            _marginBegin = Math.Max(_halAvgFilterWinSize, _marginBegin);
            _marginEnd = Math.Max(_halAvgFilterWinSize, _marginEnd);

            _onlyAllAirLineUpdate = onlyAllAirLineUpdate;
            //_dynamicAirDataUpdateTask = Task.Run(new Action(DynamicAirDataUpdateTaskExe));
        }

        private void DynamicAirDataUpdateTaskExe()
        {
            while (!_exit)
            {
                DynamicUpdateAirDatas datas;
                while (_dynamicAirUpdateDatasQueue.TryDequeue(out datas))
                {
                    try
                    {
                        var updatedAir = this.CalculateUpdatedAir(datas, this.ReferenceAir);
                        lock (_tempAirDatas)
                        {
                            _tempAirDatas.AddLast(updatedAir);
                            if (_tempAirDatas.Count > 3)
                            {
                                _tempAirDatas.RemoveFirst();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e);
                    }
                }

                //大概是50线一组，50*4=200ms，为了减少数据堆积，设为50
                Thread.Sleep(50);
            }
        }

        private ScanlineData CalculateUpdatedAir(DynamicUpdateAirDatas datas, ScanlineData reference)
        {

            var recs = GetAirRegions(datas);

            //更新连续满度区域中参考满度
            if (recs != null && recs.Count > 0)
            {
                foreach (var rec in recs)
                {
                    UpdateReferenceAir(rec, datas, reference, 0);
                }

                if (reference != null)
                {
                    return DeepCopy.DeepCopyByBin(reference);

                }
                return null;
                //return reference.Clone();
            }

            return null;
        }

        public List<AirRecInLine> GetAirRegions(DynamicUpdateAirDatas datas)
        {         
            if (_avgFilterWinSize >= 3)
            {
                if (_fastAvgFilterHelper == null)
                {
                    _fastAvgFilterHelper = new FastAvgFilterHelper(datas.Count, datas.ChannelsCount, _avgFilterWinSize);
                }

                if (!_fastAvgFilterHelper.Filter(datas.NormalizedScanlineDatas))
                {
                    return null;
                }
            }
            List<ushort[]> smoothedImage = datas.NormalizedScanlineDatas;
            //不更新置白区域的满度
            var start = Math.Min(_marginBegin, datas.ChannelsCount - 1);
            var end = Math.Min(datas.ChannelsCount - _marginEnd - 1, datas.ChannelsCount - 1);

            List<AirRecsInLine> airRecsInLines = GetAirRecsInLines(datas, smoothedImage, start, end);

            return CalContinuousAirChannelsInAllLines(airRecsInLines, start, end);
        }

        private List<AirRecsInLine> GetAirRecsInLines(DynamicUpdateAirDatas datas, List<ushort[]> smoothedImage, int channelStart, int channelEnd)
        {
            var airRecsInLines = new List<AirRecsInLine>(datas.Count);

            //因为采用5*5的滤波，查找边界的时候,去掉窗口大小的影响
            for (int i = 0; i < datas.Count; i++)
            {
                var rec = GetDynamicAirUpdateDataInLine(smoothedImage[i], datas.ChannelsCount, datas.Count, channelStart, channelEnd);

                airRecsInLines.Add(rec);
            }
            return airRecsInLines;
        }

        private List<AirRecInLine> CalContinuousAirChannelsInAllLines(List<AirRecsInLine> airRecsInLines, int channelStart, int channelEnd)
        {
            if (airRecsInLines == null || airRecsInLines.Count == 0)
                return null;
            var result = new List<AirRecInLine>();

            //先判断是否全是白线，如果全是白线，则只返回一个rec，否则返回左右两个rec
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
                //TODO：test
                //Tracer.TraceInfo("Find All air Rec,view is " + _viewIndex.ToString() +  ",start = " + start + ", end = " + end);

                result.Add(new AirRecInLine { XStart = start, XEnd = end });
            }
            else if (!_onlyAllAirLineUpdate)
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

                    //TODO：test
                    //Tracer.TraceInfo("Find Left Rec, view is " + _viewIndex.ToString() + ",start = " + left.XStart + ", end = " + left.XEnd);
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
                    //TODO：test
                    //Tracer.TraceInfo("Find Right Rec,view is " + _viewIndex.ToString() + ", start = " + right.XStart + ", end = " + right.XEnd);
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

                if (end - start + 1 < MinRecWidth)
                {
                    return null;
                }

                return new AirRecInLine() { XStart = start, XEnd = end };
            }
            return null;
        }


        private AirRecsInLine GetDynamicAirUpdateDataInLine(ushort[] smoothedImage, int width, int height, int start, int end)
        {
            if (smoothedImage == null || end <= start )
                return null;

            AirRecsInLine result = new AirRecsInLine();

            int recStart = start;
            int recEnd = start;

            for (int i = start; i < end - _findBorderUpdate; i++)
            {
                //var current = smoothedImage[i];
                //var next = smoothedImage[ i + 1];

                ////连续两个点判断为非air点，则认为找到了边界，不在进行寻找,TODO：是否需要加强限制。连续两点大于阈值或者检测到连续低于阈值后回退到连续高于阈值
                //if (current >= _airTh || next >= _airTh)
                //{
                //    recEnd = i;
                //    continue;
                //}
                //break;

                //if (smoothedImage[i]<30000)     //加额外判断
                //{
                //    break;
                //}
                if (findRecEnd(smoothedImage, i))
                {
                    recEnd = i;
                    continue;
                }
                break;
            }

            //最终找到的边界和end比较,如果小于两个像素，则认为是白线，直接返回，不在进行右边空白线的判断
            if (Math.Abs(recEnd - end) <= _findBorderUpdate + 2)
            {
                recEnd = end;
                result.AirRec = new AirRecInLine { XStart = recStart, XEnd = recEnd };
                return result;
            }

            var leftRec = new AirRecInLine { XStart = recStart, XEnd = recEnd };

            //记录找到的左边界的结尾
            int leftRecEnd = recEnd;

            recStart = end;
            recEnd = end;

            for (int i = end; i > leftRecEnd + _findBorderUpdate; i--)
            {
                //var current = smoothedImage[i];
                //var before = smoothedImage[i - 1];
                //if (current >= _airTh || before >= _airTh)
                //{
                //    recStart = i;
                //    continue;
                //}
                //break;
                if (findRecStart(smoothedImage, i))
                {
                    recStart = i;
                    continue;
                }
                //if (smoothedImage[i]<30000)     //加额外判断
                //{
                //    break;
                //}

                break;
            }

            if (Math.Abs(recStart - start) <= AirRegionTolerance)
            {
                recStart = start;
                result.AirRec = new AirRecInLine { XStart = recStart, XEnd = recEnd };
                return result;
            }

            var rightRec = new AirRecInLine { XStart = recStart, XEnd = recEnd };

            if (Math.Abs(leftRec.XEnd - rightRec.XStart) <= AirRegionTolerance)
            {
                result.AirRec = new AirRecInLine() { XStart = leftRec.XStart, XEnd = rightRec.XEnd };
                return result;
            }
            result.LeftRec = leftRec;
            result.RightRec = rightRec;
            return result;
        }


        private bool findRecEnd(ushort[] smoothedImage, int currentIndex)
        {
            for (int j = 0; j < _findBorderUpdate; j++)
            {
                int current = smoothedImage[currentIndex + j];
                if (current >= this._airTh)
                    return true;
            }
            return false;
        }

        private bool findRecStart(ushort[] smoothedImage, int currentIndex)
        {
            for (int j = 0; j < _findBorderUpdate; j++)
            {
                int current = smoothedImage[currentIndex - j];
                if (current >= this._airTh)
                    return true;
            }
            return false;
        }
        private void UpdateReferenceAir(AirRecInLine rec, DynamicUpdateAirDatas rawDatas, ScanlineData referData,
            double weight)
        {
            if (rec != null && rawDatas != null && rawDatas.Count > 0 && referData != null)
            {
                int[] leSum = null;
                int[] heSum = null;
                if (rawDatas.RawScanlineDatas[0].Low != null)
                {
                    leSum = new int[rec.Width];
                }
                if (rawDatas.RawScanlineDatas[0].High != null)
                {
                    heSum = new int[rec.Width];
                }

                var count = rawDatas.Count;
                // 对所有线数据的相同位置探测器的输出进行累加，求和
                var datas = rawDatas.RawScanlineDatas;
                foreach (var data in datas)
                {
                    var line = data;

                    for (int i = 0; i <= rec.XEnd - rec.XStart; i++)
                    {
                        if (line.Low != null && leSum != null)
                        {
                            leSum[i] += line.Low[i + rec.XStart];
                        }

                        if (line.High != null && heSum != null)
                        {
                            heSum[i] += line.High[i + rec.XStart];
                        }
                    }
                }

                int linesCount = datas.Count;

                // 对累加和除以线数，得到均值
                for (int i = 0; i < rec.Width; i++)
                {
                    var index = i + rec.XStart;

                    if (leSum != null)
                    {
                        var lowAvg = (ushort)(leSum[i] / linesCount);
                        //if (IsAverageValid(lowAvg,referData.Low[index]))
                        {
                            referData.Low[index] = MergeAir(referData.Low[index], lowAvg, weight);
                        }                        
                    }
                    if (heSum != null)
                    {
                        var highAvg = (ushort)(heSum[i] / linesCount);
                        //if (IsAverageValid(highAvg, referData.High[index]))
                        {
                            referData.High[index] = MergeAir(referData.High[index], highAvg, weight);
                        }                        
                    }
                }
            }
        }

        private ushort MergeAir(ushort refData, ushort newAvgData, double weight)
        {
            double result = (refData * weight + newAvgData) / (1 + weight);
            if (result > 65535)
            {
                result = 65535;
            }
            if (result < 0)
            {
                result = 0;
            }
            return (ushort)result;
        }

        public void StopService()
        {
            if (!_exit && _dynamicAirDataUpdateTask != null)
            {
                _exit = true;
                _dynamicAirDataUpdateTask.Wait();
                _dynamicAirDataUpdateTask = null;
            }
        }

        public void Enqueue(DynamicUpdateAirDatas data)
        {
            //_dynamicAirUpdateDatasQueue.Enqueue(data);
            var updatedAir = this.CalculateUpdatedAir(data, this.ReferenceAir);
            lock (_tempAirDatas)
            {
                _tempAirDatas.AddLast(updatedAir);
                if (_tempAirDatas.Count > 3)
                {
                    _tempAirDatas.RemoveFirst();
                }
            }
        }

        public bool TryGetAirData(out ScanlineData air)
        {
            ScanlineData newAir = null;
            lock (_tempAirDatas)
            {
                var alldata = _tempAirDatas.ToList();
                if (alldata.Count == 3)
                {
                    if (alldata[2] != null)
                    {
                        if (alldata[1] != null)
                        {
                            newAir = alldata[1];
                        }
                        else
                        {
                            newAir = alldata[2];
                        }
                    }
                    else
                    {
                        if (alldata[0] != null)
                        {
                            newAir = alldata[0];
                        }
                        else
                        {
                            if (alldata[1] != null)
                            {
                                newAir = alldata[1];
                            }
                        }
                    }
                }
                else if (alldata.Count == 2)
                {
                    if (alldata[0] != null)
                    {
                        newAir = alldata[0];
                    }
                    else
                    {
                        if (alldata[1] != null)
                        {
                            newAir = alldata[1];
                        }
                    }
                }
                else if (alldata.Count == 1)
                {
                    if (alldata[0] != null)
                    {
                        newAir = alldata[0];
                    }
                }
            }
            if (newAir != null)
            {
                air = newAir.Clone();
            }
            else
            {
                air = null;
            }
            return air != null;
        }

        public void ClearAirData()
        {
            lock (_tempAirDatas)
            {
                _tempAirDatas.Clear();
            }
        }
    }
}

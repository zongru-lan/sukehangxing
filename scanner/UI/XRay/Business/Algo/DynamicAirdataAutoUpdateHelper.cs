using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    public class BeltEdgePosition
    {
        public int Edge1Start { get; set; }
        public int Edge1End { get; set; }
        public int Edge2Start { get; set; }
        public int Edge2End { get; set; }

        public DetectViewIndex ViewIndex { get; private set; }

        public BeltEdgePosition(DetectViewIndex viewIndex, int firstStart = 0, int firstEnd = 0, int secondStart = 0, int secondEnd = 0)
        {
            this.ViewIndex = viewIndex;
            this.Edge1Start = firstStart;
            this.Edge1End = firstEnd;
            this.Edge2Start = secondStart;
            this.Edge2End = secondEnd;
        }
    }

    public class DynamicUpdateAirDatas
    {
        public List<RawScanlineData> RawScanlineDatas { get; private set; }
        public List<ushort[]> NormalizedScanlineDatas { get; private set; }

        public int Count
        {
            get { return RawScanlineDatas.Count; }
        }

        public int MaxCount { get; private set; }

        public int ChannelsCount { get; private set; }

        public static int SizeOfUshort = sizeof(ushort);

        public DynamicUpdateAirDatas(int maxLinesCount, int channelsCount)
        {
            this.MaxCount = maxLinesCount;
            this.ChannelsCount = channelsCount;
            RawScanlineDatas = new List<RawScanlineData>();
            NormalizedScanlineDatas = new List<ushort[]>();
            //for (int i = 0; i < this.MaxCount; i++)
            //{
            //    LowScanlineDatas[i] = new ushort[this.ChannelsCount];
            //}
        }

        public void CopyData(ushort[] datas,int idx)
        {
            try
            {
                var temp = new ushort[datas.Length];
                Buffer.BlockCopy(datas, 0, temp, 0, datas.Length * SizeOfUshort);
                NormalizedScanlineDatas.Add(temp);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        public void ClearCache()
        {
            if (RawScanlineDatas != null)
            {
                RawScanlineDatas.Clear();
            }
        }
    }

    public class AirRecInLine
    {
        public int XStart { get; set; }
        public int XEnd { get; set; }

        public int Width
        {
            get { return XEnd - XStart + 1; }
        }

        public bool XIntersectWith(AirRecInLine b)
        {
            return (b.XStart < this.XEnd) && (this.XStart < b.XEnd);
        }
    }

    public class AirRecsInLine
    {
        /// <summary>
        /// 一线数据中如果非白线，只有左右两个air区域，如果是白线，则left,right不赋值，给airRec赋值
        /// </summary>
        public AirRecInLine LeftRec { get; set; }

        public AirRecInLine RightRec { get; set; }

        public AirRecInLine AirRec { get; set; }

        /// <summary>
        /// 是不是全白线
        /// </summary>
        public bool IsAir
        {
            get { return this.AirRec != null; }
        }
    }

    public class FastAvgFilterHelper
    {

        public LinkedList<int> SumsOfCols = new LinkedList<int>();

        public ushort[] DstData { get; private set; }

        private int _width;
        private int _height;

        private int _winSize;
        private int _num;
        private int _semiSize;

        public FastAvgFilterHelper(int linesCount, int channelsCount, int winSize)
        {
            _width = channelsCount;
            _height = linesCount;
            DstData = new ushort[_width * _height];

            _winSize = winSize;
            _num = winSize * winSize;
            _semiSize = winSize >> 1;
        }

        /// <summary>
        /// 仅适用在单线程模式
        /// </summary>
        /// <param name="src"></param>
        /// <param name="?"></param>
        public bool Filter(List<ushort[]> src)
        {
            if (src == null || src.Count < 1 || src[0].Length < _winSize)
            {
                return false;
            }

            try
            {
                for (int x = 0; x < src.Count; x++)
                {
                    ushort[] ori = new ushort[_width];
                    Buffer.BlockCopy(src[x], 0, ori, 0, sizeof(ushort) * _width);
                    for (int y = 0; y < _width; y++)
                    {
                        if (y < _semiSize || y >= _width - _semiSize)
                        {
                            SumsOfCols.Clear();
                            for (int k = -_semiSize; k <= _semiSize; k++)
                            {
                                int idx = y + k;
                                if(idx > -1 && idx < _width)
                                {
                                    SumsOfCols.AddLast(ori[idx]);
                                }
                            }
                            src[x][y] = (ushort)SumsOfCols.Max();
                        }
                        else
                        {
                            SumsOfCols.RemoveFirst();
                            SumsOfCols.AddLast(ori[y + _semiSize]);
                            src[x][y] = (ushort)SumsOfCols.Max();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                return false;
            }
            return true;
        }

        public bool FilterMax(ushort[] src)
        {
            if (src == null || src.Length < _winSize)
            {
                return false;
            }

            ushort max = 0;
            try
            {
                for (int x = 0; x < _height; x++)
                {
                    for (int y = 0; y < _width; y++)
                    {
                        int currentIndex = x * _width + y;

                        max = 0;
                        for (int i =  - _semiSize; i <= _semiSize; i++)
                        {
                            for (int j =  - _semiSize; j <=  _semiSize; j++)
                            {

                                if ((x+i < 0) || (x+i >= _height) || (j+y < 0) || (j+y >= _width))
                                {
                                    continue;
                                }

                                max = Math.Max(max, src[(x + i) * _width + y + j]);
                            }
                        }

                        DstData[currentIndex] = max;
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                return false;
            }
            return true;
        }
    }
}

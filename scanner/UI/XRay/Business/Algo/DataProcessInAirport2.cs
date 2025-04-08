using Emgu.CV;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.ImagePlant.Classify;

namespace UI.XRay.Business.Algo
{
    public class DataProcessInAirport2
    {
        [DllImport("ImageEdgeEnhanceLib.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int EdgeEnhance(IntPtr src, IntPtr dst, int height, int width, int stride, IntPtr alpha, IntPtr beta, IntPtr seg, float gamma1, float gamma2);

        [DllImport("MaterialFilterLib.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int MaterialFilter(IntPtr src, IntPtr material, IntPtr dst, int height, int width, int stride, IntPtr alpha, IntPtr beta, IntPtr seg, int derta, int N);

        public double[] Alpha { set; private get; }
        public double[] Beta { set; private get; }
        public double[] Seg { set; private get; }

        double[] seg_y, alpha_y, beta_y;//从CSV文件中读出的系数
        IntPtr pAlpha = new IntPtr(0);
        IntPtr pBeta = new IntPtr(0);
        IntPtr pSeg = new IntPtr(0);

        int _linesCacheCount = 50;
        int _cacheLinesCount = 8;
        bool _isFirstBundles = true;
        List<DisplayScanlineDataBundle> DataList = new List<DisplayScanlineDataBundle>();
        List<DisplayScanlineData> _cacheLinesView1 = new List<DisplayScanlineData>();
        List<DisplayScanlineData> _cacheLinesView2 = new List<DisplayScanlineData>();

        private MaterialColorMapper _mapper;

        private int gamma1xishu = 40;
        private int gamma2xishu = 40;

        public DataProcessInAirport2()
        {
            _mapper = new MaterialColorMapper();

            if (!ScannerConfig.Read(ConfigPath.PreProcImageProcessLinesCount, out _linesCacheCount))
            {
                _linesCacheCount = 50;
            }
            if (!ScannerConfig.Read(ConfigPath.gamma1, out gamma1xishu))
            {
                gamma1xishu = 40;
            }
            if (!ScannerConfig.Read(ConfigPath.gamma2, out gamma2xishu))
            {
                gamma2xishu = 40;
            }
            Tracer.TraceInfo($"[DataProcessInAirport2] linesCacheCount: {_linesCacheCount}");

            ReadCsv(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seg.bin"), out seg_y);
            ReadCsv(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alpha.bin"), out alpha_y);
            ReadCsv(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "beta.bin"), out beta_y);

            pAlpha = Marshal.UnsafeAddrOfPinnedArrayElement(alpha_y, 0);
            pBeta = Marshal.UnsafeAddrOfPinnedArrayElement(beta_y, 0);
            pSeg = Marshal.UnsafeAddrOfPinnedArrayElement(seg_y, 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<DisplayScanlineDataBundle> AirPortDataProcess(DisplayScanlineDataBundle line)
        {
            if (DataList.Count >= _linesCacheCount)
            {
                DataList.Clear();
            }
            DataList.Add(line);
            if (DataList.Count >= _linesCacheCount)
            {
                //Filter
                List<DisplayScanlineData> DisplayScanlineDataview1 = null;
                List<DisplayScanlineData> DisplayScanlineDataview2 = null;

                if (line.View2Data != null)
                {
                    Parallel.Invoke(
                     () => DataProcess(DataList.Select(c => c.View1Data).ToList(), DetectViewIndex.View1, out DisplayScanlineDataview1),
                     () => DataProcess(DataList.Select(c => c.View2Data).ToList(), DetectViewIndex.View2, out DisplayScanlineDataview2)
                     );
                }
                else
                {
                    DataProcess(DataList.Select(c => c.View1Data).ToList(), DetectViewIndex.View1, out DisplayScanlineDataview1);
                }
                

                List<DisplayScanlineDataBundle> list = new List<DisplayScanlineDataBundle>();
                for (int i = 0; i < _linesCacheCount; i++)
                {
                    if (DisplayScanlineDataview2 != null)
                    {
                        var bundle = new DisplayScanlineDataBundle(DisplayScanlineDataview1[i], DisplayScanlineDataview2[i]);
                        list.Add(bundle);
                    }
                    else
                    {
                        var bundle = new DisplayScanlineDataBundle(DisplayScanlineDataview1[i], null);
                        list.Add(bundle);
                    }
                }
                if (_isFirstBundles)
                {
                    list.RemoveRange(0, _cacheLinesCount >> 1);
                    _isFirstBundles = false;
                }
                return list;
            }
            else
            {
                return new List<DisplayScanlineDataBundle>(0);
            }
        }
        public void DataProcess(List<DisplayScanlineData> inputdata, DetectViewIndex view, out List<DisplayScanlineData> outputdata)
        {
            if (inputdata.Contains(null))
            {
                outputdata = inputdata;
                return;
            }
            var allLength = inputdata.Select(d => d.XRayData.Length).Distinct().ToList();
            if (allLength.Count > 1)
            {
                outputdata = inputdata;
                return;
            }

            outputdata = new List<DisplayScanlineData>();
            var cacheLines = new List<DisplayScanlineData>();
            if (view == DetectViewIndex.View1)
            {
                cacheLines = _cacheLinesView1;
            }
            else
            {
                cacheLines = _cacheLinesView2;
            }

            int _rows = inputdata.Count;
            if (_rows < 1)
                return;
            if (inputdata[0] == null)
            {
                return;
            }
            int _cols = inputdata[0].XRayData.Length;
            if (_cols < 1)
                return;

            int margin = (int)(_cacheLinesCount >> 1);

            if (cacheLines == null || cacheLines.Count != _cacheLinesCount)
            {
                cacheLines = new List<DisplayScanlineData>();
                for (int i = 0; i < _cacheLinesCount; i++)
                {
                    cacheLines.Add(inputdata[0]);
                }
            }

            //强度
            Matrix<ushort> RawData = new Matrix<ushort>(_rows + _cacheLinesCount, _cols);
            for (int i = 0; i < cacheLines.Count; i++)
            {
                Buffer.BlockCopy(cacheLines[i].XRayData, 0, RawData.Data, i * _cols * sizeof(ushort), _cols * sizeof(ushort));
            }
            for (int i = 0; i < inputdata.Count; i++)
            {
                Buffer.BlockCopy(inputdata[i].XRayData, 0, RawData.Data, (i + cacheLines.Count) * _cols * sizeof(ushort), _cols * sizeof(ushort));
            }

            //材料
            Matrix<ushort> materialimg = new Matrix<ushort>(_rows + _cacheLinesCount, _cols);
            for (int i = 0; i < _cacheLinesCount; i++)
            {
                Buffer.BlockCopy(cacheLines[i].Material, 0, materialimg.Data, i * _cols * sizeof(ushort), _cols * sizeof(ushort));
            }
            for (int i = 0; i < _rows; i++)
            {
                Buffer.BlockCopy(inputdata[i].Material, 0, materialimg.Data, (i + _cacheLinesCount) * _cols * sizeof(ushort), _cols * sizeof(ushort));
            }
            List<ushort[]> material = MaterialFilter(RawData, materialimg);
            List<ushort[]> EnhancedData = EdgeIntensity(RawData);

            ushort[] colorindex = new ushort[_rows];
            try
            {
                for (int i = 0; i < _rows; i++)
                {
                    _mapper.Map(material[i + margin], out colorindex);

                    if (i < margin)
                    {
                        ClassifiedLineData cld = new ClassifiedLineData(cacheLines[margin + i].ViewIndex, cacheLines[margin + i].XRayData, EnhancedData[i + margin], material[i + margin], cacheLines[margin + i].LowData, cacheLines[margin + i].HighData, cacheLines[margin + i].IsAir);
                        cld.SetOriginalFusedData(cacheLines[margin + i].OriginalFused);
                        DisplayScanlineData ds = new DisplayScanlineData(cld, colorindex, inputdata[i].LineNumber);
                        outputdata.Add(ds);
                    }
                    else
                    {
                        ClassifiedLineData cld = new ClassifiedLineData(inputdata[i - margin].ViewIndex, inputdata[i - margin].XRayData, EnhancedData[i + margin], material[i + margin], inputdata[i - margin].LowData, inputdata[i - margin].HighData, inputdata[i - margin].IsAir);
                        cld.SetOriginalFusedData(inputdata[i - margin].OriginalFused);
                        DisplayScanlineData ds = new DisplayScanlineData(cld, colorindex, inputdata[i].LineNumber);
                        outputdata.Add(ds);
                    }
                }

                cacheLines.Clear();
                if (view == DetectViewIndex.View1)
                {
                    _cacheLinesView1.Clear();
                    for (int i = 0; i < _cacheLinesCount; i++)
                    {
                        _cacheLinesView1.Add(DeepCopy.DeepCopyByBin(inputdata[inputdata.Count - _cacheLinesCount + i]));
                    }
                }
                else
                {
                    _cacheLinesView2.Clear();
                    for (int i = 0; i < _cacheLinesCount; i++)
                    {
                        _cacheLinesView2.Add(DeepCopy.DeepCopyByBin(inputdata[inputdata.Count - _cacheLinesCount + i]));
                    }
                }

            }
            catch (Exception ex)
            {
#line 227
                Tracer.TraceError("[DataProcess] Error occured in DataProcess");
                Tracer.TraceException(ex);
#line default
#if DEBUG

                Console.WriteLine("Error !");
                Console.WriteLine(ex.ToString());
#endif
            }
        }

        /// <summary>
        /// 材质滤波
        /// </summary>
        /// <param name="RawDataList"></param>
        /// <param name="material"></param>
        /// <param name="materialfilter"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private List<ushort[]> MaterialFilter(Matrix<ushort> RawDataList, Matrix<ushort> material)
        {
            var materialfilter = new List<ushort[]>();
            int cols1 = material.Cols;
            int rows1 = material.Rows;
            try
            {
                Matrix<ushort> materialout_16 = new Matrix<ushort>(material.Rows, material.Cols);

                MaterialFilter(RawDataList.Mat.DataPointer, material.Mat.DataPointer, materialout_16.Mat.DataPointer,
                    RawDataList.Mat.Height, RawDataList.Mat.Width, RawDataList.Mat.Step, pAlpha, pBeta, pSeg, 10, 10);

                int cols = material.Cols;
                int rows = material.Rows;
                for (int i = 0; i < rows; i++)
                {
                    ushort[] temp = new ushort[cols];
                    //Buffer.BlockCopy(material.Data, i * cols * sizeof(ushort), temp, 0, cols * sizeof(ushort));
                    Buffer.BlockCopy(materialout_16.Data, i * cols * sizeof(ushort), temp, 0, cols * sizeof(ushort));
                    unsafe
                    {
                        fixed(ushort* ptr = &temp[0])
                        {
                            for (int j = 0; j < cols; j++)
                            {
                                if (*(ptr + j) == 253 || *(ptr + j) == 252)
                                    *(ptr + j) = 0;
                            }
                        }
                    }
                    
                    materialfilter.Add(temp);
                }

            }
            catch (Exception ex)
            {
                Tracer.TraceError("[DataProcess] Error occured in MaterialFilter");
                Tracer.TraceException(ex);
#if DEBUG
                Console.WriteLine("MaterialFilter Error： " + ex.ToString());
#endif
            }
            return materialfilter;
        }

        /// <summary>
        /// 边缘增强
        /// </summary>
        /// <param name="rawdataview"></param>
        /// <param name="rawdataview2"></param>
        /// <param name="dataview"></param>
        /// <param name="dataview2"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private List<ushort[]> EdgeIntensity(Matrix<ushort> rawdataview)
        {
            var dataview = new List<ushort[]>();
            int cols = rawdataview.Cols;
            int rows = rawdataview.Rows;

            try
            {
                Matrix<UInt16> outm1 = new Matrix<ushort>(rawdataview.Rows, rawdataview.Cols);

                EdgeEnhance(rawdataview.Mat.DataPointer, outm1.Mat.DataPointer, rawdataview.Mat.Height, rawdataview.Mat.Width, rawdataview.Mat.Step, pAlpha, pBeta, pSeg, 0.1f*gamma1xishu, 0.1f*gamma2xishu);

                for (int i = 0; i < rows; i++)
                {
                    ushort[] tempdst1 = new ushort[cols];
                    Buffer.BlockCopy(outm1.Data, i * cols * sizeof(ushort), tempdst1, 0, cols * sizeof(ushort));
                    dataview.Add(tempdst1);
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceError("[DataProcess] Error occured in EdgeIntensity");
                Tracer.TraceException(ex);
#if DEBUG
                Console.WriteLine("Edge enhance:" + ex.ToString());
#endif
            }
            return dataview;
        }


        public void ClearCache()
        {
            DataList.Clear();
            _cacheLinesView1.Clear();
            _cacheLinesView2.Clear();
            _isFirstBundles = true;
        }

        static void ReadCsv(string filename, out double[] output)
        {
            System.Runtime.Serialization.IFormatter binaryformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            using (Stream st = new FileStream(filename, FileMode.Open))
            {
                output = (double[])binaryformatter.Deserialize(st);
            }
        }

        public void CalcEnhancedData(List<DisplayScanlineDataBundle> bundles)
        {
            Parallel.Invoke(
                () => DataProcessForCalc(bundles.Select(c => c.View1Data).ToList()),
                () => DataProcessForCalc(bundles.Select(c => c.View2Data).ToList())
                );
        }
        void DataProcessForCalc(List<DisplayScanlineData> inputdata)
        {
            int _rows = inputdata.Count;
            if (_rows < 5)
                return;
            if (inputdata[0] == null)
            {
                return;
            }
            int _cols = inputdata[0].XRayData.Length;
            if (_cols < 5 )
                return;

            //已经有增强值，不再计算
            if (inputdata[0].XRayDataEnhanced != null)
            {
                return;
            }

            //强度
            Matrix<ushort> RawData = new Matrix<ushort>(_rows, _cols);
            for (int i = 0; i < inputdata.Count; i++)
            {
                Buffer.BlockCopy(inputdata[i].XRayData, 0, RawData.Data, i * _cols * sizeof(ushort), _cols * sizeof(ushort));
            }

            List<ushort[]> EnhancedData = EdgeIntensity(RawData);
            if(EnhancedData != null && EnhancedData.Count == inputdata.Count)
            {
                for (int i = 0; i < _rows; i++)
                {
                    inputdata[i].XRayDataEnhanced = EnhancedData[i];
                }
            }            
        }
    }
}

using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.Common.Tracers;

namespace UI.XRay.Business.Algo
{
    public class WienerFilter2
    {
        [DllImport("WienerFilterLib.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int Wiener(IntPtr src, IntPtr dst, int height, int width, int stride, int size1, int size2);

        int _linesCacheCount = 50;

        List<ScanlineDataBundle> BundleList = new List<ScanlineDataBundle>();

        int _cacheLinesCount = 8;
        bool _isFirstBundles = false;

        List<ushort[]> view1_high_cache = new List<ushort[]>();
        List<ushort[]> view1_low_cache = new List<ushort[]>();
        List<ushort[]> view1_fused_cache = new List<ushort[]>();
        List<ushort[]> view2_high_cache = new List<ushort[]>();
        List<ushort[]> view2_low_cache = new List<ushort[]>();
        List<ushort[]> view2_fused_cache = new List<ushort[]>();

        public WienerFilter2()
        {
            if (!ScannerConfig.Read(ConfigPath.PreProcWienerLinesCount, out _linesCacheCount))
            {
                _linesCacheCount = 50;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcImageProcessCacheCount, out _cacheLinesCount))
            {
                _cacheLinesCount = 8;
            }
            Tracer.TraceInfo($"[WienerFilter] linesCacheCount: {_linesCacheCount}, cacheLinesCount: {_cacheLinesCount}");
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ClearCache()
        {
            BundleList.Clear();
            view1_high_cache.Clear();
            view1_low_cache.Clear();
            view1_fused_cache.Clear();
            view2_high_cache.Clear();
            view2_low_cache.Clear();
            view2_fused_cache.Clear();

            _isFirstBundles = true;
        }

        public List<ScanlineDataBundle> WienerProcess(ScanlineDataBundle line)
        {
            if (BundleList.Count >= _linesCacheCount)
            {
                BundleList.Clear();
            }
            BundleList.Add(line);
            if (BundleList.Count >= _linesCacheCount)
            {
                List<ushort[]> view1_high_out = new List<ushort[]>();
                List<ushort[]> view1_low_out = new List<ushort[]>();
                List<ushort[]> view1_fused_out = new List<ushort[]>();
                List<ushort[]> view2_high_out = new List<ushort[]>();
                List<ushort[]> view2_low_out = new List<ushort[]>();
                List<ushort[]> view2_fused_out = new List<ushort[]>();


                List<ScanlineDataBundle> newData = new List<ScanlineDataBundle>();
                if (line.View2LineData != null)
                {
                    Parallel.Invoke(
                        () => Wiener_highlow(BundleList.Select(s => s.View1LineData.High).ToList(), 1, out view1_high_out),
                        () => Wiener_highlow(BundleList.Select(s => s.View1LineData.Low).ToList(), 2, out view1_low_out),
                        () => Wiener_fused(BundleList.Select(s => s.View1LineData.Fused).ToList(), 3, out view1_fused_out),
                        () => Wiener_highlow(BundleList.Select(s => s.View2LineData.High).ToList(), 4, out view2_high_out),
                        () => Wiener_highlow(BundleList.Select(s => s.View2LineData.Low).ToList(), 5, out view2_low_out),
                        () => Wiener_fused(BundleList.Select(s => s.View2LineData.Fused).ToList(), 6, out view2_fused_out)
                        );

                    for (int i = 0; i < _linesCacheCount; i++)
                    {
                        newData.Add(new ScanlineDataBundle(new ScanlineData(DetectViewIndex.View1, view1_low_out[i], view1_high_out[i], view1_fused_out[i]),
                                                            new ScanlineData(DetectViewIndex.View2, view2_low_out[i], view2_high_out[i], view2_fused_out[i])));
                    }
                }
                else
                {
                    Parallel.Invoke(
                       () => Wiener_highlow(BundleList.Select(s => s.View1LineData.High).ToList(), 1, out view1_high_out),
                       () => Wiener_highlow(BundleList.Select(s => s.View1LineData.Low).ToList(), 2, out view1_low_out),
                       () => Wiener_fused(BundleList.Select(s => s.View1LineData.Fused).ToList(), 3, out view1_fused_out));

                    for (int i = 0; i < _linesCacheCount; i++)
                    {
                        newData.Add(new ScanlineDataBundle(new ScanlineData(DetectViewIndex.View1, view1_low_out[i], view1_high_out[i], view1_fused_out[i]), null));
                    }
                }
 

                if (_isFirstBundles)
                {
                    newData.RemoveRange(0, _cacheLinesCount >> 1);
                    _isFirstBundles = false;
                }
                return newData;
            }
            else
            {
                return new List<ScanlineDataBundle>(0);
            }
        }

        List<ushort[]> GetCacheList(int index)
        {
            switch (index)  
            {
                case 1:
                    return view1_high_cache;
                case 2:
                    return view1_low_cache;
                case 3:
                    return view1_fused_cache;
                case 4:
                    return view2_high_cache;
                case 5:
                    return view2_low_cache;
                case 6:
                    return view2_fused_cache;
                default:
                    return view1_high_cache;
            }
        }

        void SetCacheList(List<ushort[]> cache,int index)
        {
            switch (index)
            {
                case 1:
                    view1_high_cache.Clear();
                    view1_high_cache = cache;
                    break;
                case 2:
                    view1_low_cache.Clear();
                    view1_low_cache = cache;
                    break;
                case 3:
                    view1_fused_cache.Clear();
                    view1_fused_cache = cache;
                    break;
                case 4:
                    view2_high_cache.Clear();
                    view2_high_cache = cache;
                    break;
                case 5:
                    view2_low_cache.Clear();
                    view2_low_cache = cache;
                    break;
                case 6:
                    view2_fused_cache.Clear();
                    view2_fused_cache = cache;
                    break;
                default:
                    view1_high_cache.Clear();
                    view1_high_cache = cache;
                    break;
            }
        }

        int[] errorCount = new int[3] { 0, 0, 0 };
        public void Wiener_highlow(List<ushort[]> lines, int index,out List<ushort[]> output)
        {
            output = DeepCopy.DeepCopyByBin(lines);

            int _arrayRows = lines.Count;
            if (_arrayRows != _linesCacheCount)
            {
                //if (errorCount[0] % 1 == 0)
                {
                    Tracer.TraceError("[" + errorCount[0].ToString() + "] " + "Wiener_highlow: _arrrayRows: " + _arrayRows.ToString() + ", _linesCacheCount: " + _linesCacheCount.ToString());
                }
                errorCount[0]++;
                return;
            }
            if (lines.Contains(null))
            {
                //if (errorCount[1] % 1 == 0)
                {
                    Tracer.TraceError("[" + errorCount[1].ToString() + "] " + "Wiener_highlow: Contains null");
                }
                errorCount[1]++;
                return;
            }
            var _arrayLens = lines.Select(l => l.Length).Distinct();
            if (_arrayLens.Count() != 1)
            {
                var lenStr = "";
                foreach (var len in _arrayLens.ToList())
                {
                    lenStr += len.ToString() + " ";
                }
                //if (errorCount[2] % 1 == 0)
                {
                    Tracer.TraceError("[" + errorCount[2].ToString() + "] " + "Wiener_highlow: _arrayLens: " + lenStr);
                }
                errorCount[2]++;
                return;
            }
            int _arrayCols = _arrayLens.ToList()[0];

            List<ushort[]> _cacheLinesOriginal = new List<ushort[]>();
            for (int i = 0; i < _cacheLinesCount; i++)
            {
                _cacheLinesOriginal.Add((ushort[])lines[lines.Count - _cacheLinesCount + i].Clone());
            }

            int height = _arrayRows + _cacheLinesCount;
            int width = _arrayCols;
            Matrix<ushort> _imgHigh = new Matrix<ushort>(height, width);

            var cache = GetCacheList(index);

            try
            {
                if (cache.Count != _cacheLinesCount)
                {
                    cache.Clear();
                    for (int i = 0; i < _cacheLinesCount; i++)
                    {
                        cache.Add((ushort[])lines[0].Clone());
                    }
                }
                for (int i = 0; i < _cacheLinesCount; i++)
                {
                    Buffer.BlockCopy(cache[i], 0, _imgHigh.Data, i * _arrayCols * sizeof(ushort), _arrayCols * sizeof(ushort));
                }
                for (int i = 0; i < _arrayRows; i++)
                {
                    Buffer.BlockCopy(lines[i], 0, _imgHigh.Data, (i + _cacheLinesCount) * _arrayCols * sizeof(ushort), _arrayCols * sizeof(ushort));
                }

                Matrix<ushort> filtered = new Matrix<ushort>(_imgHigh.Rows, _imgHigh.Cols);
                Wiener(_imgHigh.Mat.DataPointer, filtered.Mat.DataPointer, _imgHigh.Mat.Height, _imgHigh.Mat.Width, _imgHigh.Mat.Step, 3, 3);

                for (int i = 0; i < _arrayRows; i++)
                {
                    Buffer.BlockCopy(filtered.Data, (i + _cacheLinesCount / 2) * _arrayCols * sizeof(ushort), output[i], 0, _arrayCols * sizeof(ushort));
                }
                filtered.Dispose();

                SetCacheList(_cacheLinesOriginal,index);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            _imgHigh.Dispose();
        }

        public void Wiener_fused(List<ushort[]> lines, int index,out List<ushort[]> output)
        {
            output = DeepCopy.DeepCopyByBin(lines);

            int _arrayRows = lines.Count;
            if (_arrayRows < 1) return;
            int _arrayCols = lines[0].Length;
            if (_arrayRows < 1) return;

            int margin = _cacheLinesCount >> 1;

            List<ushort[]> _cacheLinesOriginal = new List<ushort[]>();
            for (int i = 0; i < _cacheLinesCount; i++)
            {
                _cacheLinesOriginal.Add((ushort[])lines[lines.Count - _cacheLinesCount + i].Clone());
            }

            int height = _arrayRows + _cacheLinesCount;
            int width = _arrayCols;

            var cache = GetCacheList(index);

            Matrix<ushort> _imgHigh = new Matrix<ushort>(height, width);
            try
            {
                if (cache.Count != _cacheLinesCount)
                {
                    cache.Clear();
                    for (int i = 0; i < _cacheLinesCount; i++)
                    {
                        cache.Add((ushort[])lines[0].Clone());
                    }
                }
                for (int i = 0; i < _cacheLinesCount; i++)
                {
                    Buffer.BlockCopy(cache[i], 0, _imgHigh.Data, i * _arrayCols * sizeof(ushort), _arrayCols * sizeof(ushort));
                }
                for (int i = 0; i < _arrayRows; i++)
                {
                    Buffer.BlockCopy(lines[i], 0, _imgHigh.Data, (i + _cacheLinesCount) * _arrayCols * sizeof(ushort), _arrayCols * sizeof(ushort));
                }

                for (int i = 0; i < _arrayRows; i++)
                {
                    Buffer.BlockCopy(_imgHigh.Data, (i + _cacheLinesCount / 2) * _arrayCols * sizeof(ushort), output[i], 0, _arrayCols * sizeof(ushort));
                }                

                SetCacheList(_cacheLinesOriginal,index);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            _imgHigh.Dispose();
        }
    }
}

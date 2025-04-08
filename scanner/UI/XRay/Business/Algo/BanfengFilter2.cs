using Emgu.CV;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    public class BanfengFilter2
    {
        int _cacheLinesCount = 8;
        int _linesCacheCount = 24;
        bool _isFirstBundles = true;

        BanfengProcessor _bfProcessor1 = null;
        BanfengProcessor _bfProcessor2 = null;

        BanfengPointsReader _bfReader1 = null;
        BanfengPointsReader _bfReader2 = null;

        double[] seg_y;

        List<ScanlineDataBundle> _bundleList = new List<ScanlineDataBundle>();
        List<ScanlineDataBundle> _tailOfLast = new List<ScanlineDataBundle>();
        public BanfengFilter2()
        {
            LoadSetting();
            Init();
        }

        public void Init()
        {
            _bfProcessor1 = new BanfengProcessor();
            _bfReader1 = new BanfengPointsReader(0);
            _bfProcessor1.SetSeg(seg_y);
            _bfProcessor1.SetBadPoints(_bfReader1.GetViewPositon());

            _bfProcessor2 = new BanfengProcessor();
            _bfReader2 = new BanfengPointsReader(1);
            _bfProcessor2.SetSeg(seg_y);
            _bfProcessor2.SetBadPoints(_bfReader2.GetViewPositon());
        }

        public List<ScanlineDataBundle> Process(ScanlineDataBundle line)
        {
            if (!_bfProcessor1.HasBadPoints() && !_bfProcessor2.HasBadPoints())
            {
                return new List<ScanlineDataBundle>() { line };
            }

            if (_bundleList.Count >= _linesCacheCount)
            {
                _bundleList.Clear();
            }
            _bundleList.Add(line);
            if (_bundleList.Count >= _linesCacheCount)
            {
                var lastlines = new List<ScanlineDataBundle>(_cacheLinesCount);
                for (int i = _linesCacheCount - _cacheLinesCount; i < _linesCacheCount; i++)
                {
                    lastlines.Add(DeepCopy.DeepCopyByBin(_bundleList[i]));
                }

                if (_tailOfLast.Count != _cacheLinesCount)
                {
                    _tailOfLast = DeepCopy.DeepCopyByBin(lastlines);
                }

                List<ScanlineDataBundle> newData = new List<ScanlineDataBundle>();
                if (line.View2LineData != null)
                {
                    List<UInt16[]> view1_out = new List<UInt16[]>();
                    List<UInt16[]> view2_out = new List<UInt16[]>();
                    Parallel.Invoke(
                        () => Banfeng_View1(_bundleList.Select(s => s.View1LineData.Fused).ToList(), out view1_out),
                        () => Banfeng_View2(_bundleList.Select(s => s.View2LineData.Fused).ToList(), out view2_out)
                        );

                    for (int i = 0; i < _cacheLinesCount / 2; i++)
                    {
                        Buffer.BlockCopy(view1_out[i], 0, _tailOfLast[i + _cacheLinesCount / 2].View1LineData.Fused, 0, sizeof(UInt16) * view1_out[i].Length);
                        Buffer.BlockCopy(view2_out[i], 0, _tailOfLast[i + _cacheLinesCount / 2].View2LineData.Fused, 0, sizeof(UInt16) * view2_out[i].Length);
                        newData.Add(_tailOfLast[i + _cacheLinesCount / 2]);
                    }
                    for (int i = 0; i < _linesCacheCount - _cacheLinesCount / 2; i++)
                    {
                        Buffer.BlockCopy(view1_out[i + _cacheLinesCount / 2], 0, _bundleList[i].View1LineData.Fused, 0, sizeof(UInt16) * view1_out[i + _cacheLinesCount / 2].Length);
                        Buffer.BlockCopy(view2_out[i + _cacheLinesCount / 2], 0, _bundleList[i].View2LineData.Fused, 0, sizeof(UInt16) * view2_out[i + _cacheLinesCount / 2].Length);
                        newData.Add(_bundleList[i]);
                    }
                }
                else
                {
                    List<UInt16[]> view1_fused_out = new List<UInt16[]>();
                    Banfeng_View1(_bundleList.Select(s => s.View1LineData.Fused).ToList(), out view1_fused_out);

                    for (int i = 0; i < _cacheLinesCount / 2; i++)
                    {
                        Buffer.BlockCopy(view1_fused_out[i], 0, _tailOfLast[i + _cacheLinesCount / 2].View1LineData.Fused, 0, sizeof(UInt16) * view1_fused_out[i].Length);
                        newData.Add(_tailOfLast[i + _cacheLinesCount / 2]);
                    }
                    for (int i = 0; i < _linesCacheCount - _cacheLinesCount / 2; i++)
                    {
                        Buffer.BlockCopy(view1_fused_out[i + _cacheLinesCount / 2], 0, _bundleList[i].View1LineData.Fused, 0, sizeof(UInt16) * view1_fused_out[i + _cacheLinesCount / 2].Length);
                        newData.Add(_bundleList[i]);
                    }
                }

                if (_isFirstBundles)
                {
                    newData.RemoveRange(0, _cacheLinesCount >> 1);
                    _isFirstBundles = false;
                }

                _tailOfLast = lastlines;

                return newData;
            }
            else
            {
                return new List<ScanlineDataBundle>(0);
            }
        }

        public void ClearCache()
        {
            _bundleList.Clear();
            _tailOfLast.Clear();
            _isFirstBundles = true;
        }

        private void Banfeng_View1(List<UInt16[]> lines, out List<UInt16[]> output)
        {
            output = lines;
            if (!_bfProcessor1.HasBadPoints())
            {
                return;
            }

            int _arrayRows = lines.Count;
            int _arrayCols = lines[0].Length;

            List<UInt16[]> _cacheLinesOriginal = new List<UInt16[]>();
            for (int i = 0; i < _cacheLinesCount; i++)
            {
                _cacheLinesOriginal.Add((UInt16[])lines[lines.Count - _cacheLinesCount + i].Clone());
            }

            int height = _arrayRows + _cacheLinesCount;
            int width = _arrayCols;
            Matrix<UInt16> _imgFuse = new Matrix<UInt16>(height, width);

            var cache = _tailOfLast.Select(l => l.View1LineData.Fused).ToList();

            if (cache.Count != _cacheLinesCount)
            {
                cache.Clear();
                for (int i = 0; i < _cacheLinesCount; i++)
                {
                    cache.Add((UInt16[])lines[0].Clone());
                }
            }
            for (int i = 0; i < _cacheLinesCount; i++)
            {
                Buffer.BlockCopy(cache[i], 0, _imgFuse.Data, i * _arrayCols * sizeof(UInt16), _arrayCols * sizeof(UInt16));
            }
            for (int i = 0; i < _arrayRows; i++)
            {
                Buffer.BlockCopy(lines[i], 0, _imgFuse.Data, (i + _cacheLinesCount) * _arrayCols * sizeof(UInt16), _arrayCols * sizeof(UInt16));
            }

            _bfProcessor1.Process(_imgFuse);

            for (int i = 0; i < _arrayRows; i++)
            {
                Buffer.BlockCopy(_imgFuse.Data, (i + _cacheLinesCount / 2) * _arrayCols * sizeof(UInt16), output[i], 0, _arrayCols * sizeof(UInt16));
            }

            _imgFuse.Dispose();
        }

        private void Banfeng_View2(List<UInt16[]> lines, out List<UInt16[]> output)
        {
            output = lines;

            if (!_bfProcessor2.HasBadPoints())
            {
                return;
            }

            int _arrayRows = lines.Count;
            int _arrayCols = lines[0].Length;

            List<UInt16[]> _cacheLinesOriginal = new List<UInt16[]>();
            for (int i = 0; i < _cacheLinesCount; i++)
            {
                _cacheLinesOriginal.Add((UInt16[])lines[lines.Count - _cacheLinesCount + i].Clone());
            }

            int height = _arrayRows + _cacheLinesCount;
            int width = _arrayCols;
            Matrix<UInt16> _imgFuse = new Matrix<UInt16>(height, width);

            var cache = _tailOfLast.Select(l => l.View2LineData.Fused).ToList();

            if (cache.Count != _cacheLinesCount)
            {
                cache.Clear();
                for (int i = 0; i < _cacheLinesCount; i++)
                {
                    cache.Add((UInt16[])lines[0].Clone());
                }
            }
            for (int i = 0; i < _cacheLinesCount; i++)
            {
                Buffer.BlockCopy(cache[i], 0, _imgFuse.Data, i * _arrayCols * sizeof(UInt16), _arrayCols * sizeof(UInt16));
            }
            for (int i = 0; i < _arrayRows; i++)
            {
                Buffer.BlockCopy(lines[i], 0, _imgFuse.Data, (i + _cacheLinesCount) * _arrayCols * sizeof(UInt16), _arrayCols * sizeof(UInt16));
            }

            _bfProcessor2.Process(_imgFuse);

            for (int i = 0; i < _arrayRows; i++)
            {
                Buffer.BlockCopy(_imgFuse.Data, (i + _cacheLinesCount / 2) * _arrayCols * sizeof(UInt16), output[i], 0, _arrayCols * sizeof(UInt16));
            }

            _imgFuse.Dispose();
        }

        void LoadSetting()
        {
            if (!ScannerConfig.Read(ConfigPath.PreProcWienerLinesCount, out _linesCacheCount))
            {
                _linesCacheCount = 50;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcImageProcessCacheCount, out _cacheLinesCount))
            {
                _cacheLinesCount = 8;
            }
            ReadCsv(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seg.bin"), out seg_y);
        }

        void ReadCsv(string filename, out double[] output)
        {
            System.Runtime.Serialization.IFormatter binaryformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            using (Stream st = new FileStream(filename, FileMode.Open))
            {
                output = (double[])binaryformatter.Deserialize(st);
            }
        }
    }
}

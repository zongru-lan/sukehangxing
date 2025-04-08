using Emgu.CV;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    public class BanfengAlgo
    {
        #region 外部接口函数 
        [DllImport("Security.Algo.BanFeng.dll", EntryPoint = "HwiProcesssBanFengInit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static void ProcesssBanFengInit(int viewId, IntPtr seg, IntPtr badLinesGroup, int groups, int groupMaxLine, int stride);

        [DllImport("Security.Algo.BanFeng.dll", EntryPoint = "HwiProcesssBanFengHighLow", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static bool ProcesssBanFengHighLow(int viewId, IntPtr srcFuse, IntPtr srcHigh, IntPtr srcLow, IntPtr dstFuse, IntPtr dstHigh, IntPtr dstLow, IntPtr srcMaterial, int lines, int lineHeight, int stride);

        [DllImport("Security.Algo.BanFeng.dll", EntryPoint = "HwiProcesssBanFengFuseMaterial", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static bool ProcessBanFengFuseMaterial(int viewId, IntPtr src, IntPtr srcMaterial, IntPtr dst, IntPtr dstMaterial, int lines, int lineHeight, int stride);

        #endregion

        private double[] seg_y;
        private IntPtr pSeg = new IntPtr(0);
        string ViewBadChannelFlagsSettingFilePath = ConfigPath.View1BadChannelFlagsSettingFilePath;
        private int _viewId = 0;
        private bool _isTwoBadChannelFlags;

        private List<List<int>> _viewBadList { get; set; }
        public BanfengAlgo(int viewCount, bool isAnother = false)
        {
            ReadCsv(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seg.bin"), out seg_y);
            pSeg = Marshal.UnsafeAddrOfPinnedArrayElement(seg_y, 0);
            Init(viewCount, isAnother);            
        }

        public void Init(int viewCount, bool isAnother = false)
        {
            _viewId = viewCount;
            
            if (isAnother)
            {
                if (viewCount == 0)
                {
                    ViewBadChannelFlagsSettingFilePath = ConfigPath.View1NewBadChannelFlagsSettingFilePath;
                }
                else
                {
                    ViewBadChannelFlagsSettingFilePath = ConfigPath.View2NewBadChannelFlagsSettingFilePath;
                }
            }
            else
            {
                if (viewCount == 0)
                {
                    ViewBadChannelFlagsSettingFilePath = ConfigPath.View1BadChannelFlagsSettingFilePath;
                }
                else
                {
                    ViewBadChannelFlagsSettingFilePath = ConfigPath.View2BadChannelFlagsSettingFilePath;
                }
            }

            var _viewHorizonalPoints = CheckViewFile(ViewBadChannelFlagsSettingFilePath);

            GetViewBadList(_viewHorizonalPoints);

        }

        private List<ChannelBadFlag> CheckViewFile(string filePath)
        {
            var _viewHorizonalPoints = new List<ChannelBadFlag>();
            if (!File.Exists(filePath))
            {
                return _viewHorizonalPoints;
            }
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 100000))
            {
                var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                _viewHorizonalPoints = formatter.Deserialize(stream) as List<ChannelBadFlag>;
            }
            return _viewHorizonalPoints;
        }

        public void GetViewBadList(List<ChannelBadFlag> _viewHorizonalPoints)
        {
            _viewBadList = new List<List<int>>();
            for (int i = 0; i < _viewHorizonalPoints.Count - 2; i++)
            {
                if (_viewHorizonalPoints[i].IsBad)
                {
                    List<int> list = new List<int>();
                    int index = i;
                    list.Add(i);
                    for (int j = 1; j < 3; j++)
                    {
                        if (_viewHorizonalPoints[index + j].IsBad)
                        {
                            list.Add(index + j);
                            i++;
                        }
                    }
                    _viewBadList.Add(list);
                }
            }
            SetBanFengInit(pSeg, _viewBadList);

        }

        public void SetBanFengInit(IntPtr seg, List<List<int>> list)
        {
            if (list.Count <= 0)
            {
                return;
            }
            //目前每组最多支持3线
            Matrix<int> badLines = new Matrix<int>(list.Count, 3);
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list[i].Count; j++)
                {
                    badLines[i, j] = list[i][j];
                }
            }
            ProcesssBanFengInit(_viewId, seg, badLines.Mat.DataPointer, badLines.Mat.Height, badLines.Mat.Width, badLines.Mat.Step);
            Tracer.TraceInfo($"trace ban feng algo view{_viewId}");
        }
        public bool HasBadPoints()
        {
            if (_viewBadList == null || _viewBadList.Count == 0)
                return false;
            if (seg_y == null || seg_y.Length < 65536)
                return false;

            //if (_viewBadList.Where(b => b.Length < 4).Count() == 0)
            //    return false;

            return true;
        }
        bool isFirst = true;
        int cacheNum = 1;
        private List<ScanlineData> headLineScan = new List<ScanlineData>();

        public void ProcessViewBanFengImgOldScan(ScanlineData[] list,ClassifiedLineData[] classifieds)
        {
            if (list == null || list.Length == 0)
            {
                if (!isFirst)
                {
                    if (headLineClass.Count > 0)
                    {
                        list = headLineScan.ToArray();
                        headLineScan = new List<ScanlineData>();
                        return;
                    }
                }
                return;
            }
            var newList = list.ToList();
            if (list.Length > cacheNum)
            {
                if (isFirst)
                {
                    for (int i = 0; i < cacheNum * 2; i++)
                    {
                        newList.Insert(0, list.First());
                    }
                }
                else
                {
                    newList.InsertRange(0, headLineScan);
                }
            }
            else
            {
                headLineScan = new List<ScanlineData>();
            }
            list = newList.ToArray();

            int nLines = list.Length;
            int nLineHeight = list[0].Low.Length;
            Matrix<ushort> _imgFuseSrc = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgFuseDst = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgHighSrc = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgHighDst = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgLowSrc = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgLowDst = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgMaterialSrc = new Matrix<ushort>(nLines, nLineHeight);

            for (int i = 0; i < list.Length; i++)
            {
                Buffer.BlockCopy(list[i].Low, 0, _imgLowSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(list[i].Low, 0, _imgLowDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(list[i].High, 0, _imgHighSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(list[i].High, 0, _imgHighDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(list[i].Fused, 0, _imgFuseSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(list[i].Fused, 0, _imgFuseDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(classifieds[i].Material, 0, _imgMaterialSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
            }
            if (!ProcesssBanFengHighLow(_viewId, _imgFuseSrc.Mat.DataPointer, _imgHighSrc.Mat.DataPointer, _imgLowSrc.Mat.DataPointer,
                _imgFuseDst.Mat.DataPointer, _imgHighDst.Mat.DataPointer,_imgLowDst.Mat.DataPointer,
                 _imgMaterialSrc.Mat.DataPointer, _imgFuseSrc.Mat.Height, _imgFuseSrc.Mat.Width, _imgFuseSrc.Mat.Step))
            {
                Tracer.TraceInfo($"process view{_viewId} ban feng img all failure!");
            }
            headLineScan = new List<ScanlineData>();
            for (int i = 0; i < list.Length; i++)
            {
                Buffer.BlockCopy(_imgFuseDst.Data, i * nLineHeight * sizeof(ushort), list[i].Fused, 0, nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(_imgHighDst.Data, i * nLineHeight * sizeof(ushort), list[i].High, 0, nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(_imgLowDst.Data, i * nLineHeight * sizeof(ushort), list[i].Low, 0, nLineHeight * sizeof(ushort));

                var _headLine = headLineScan.ToList();
                if (i + cacheNum * 2 >= list.Length)
                {
                    _headLine.Add(list[i]);
                }
                headLineScan = _headLine.ToList();
            }


            newList = list.ToList();

            newList.RemoveRange(0, cacheNum);

            newList.RemoveRange(newList.Count - cacheNum, cacheNum);

            if (isFirst)
            {
                newList.RemoveRange(0, cacheNum);
            }
            list = newList.ToArray();
            isFirst = false;
        }

        private List<ClassifiedLineData> headLineClass = new List<ClassifiedLineData>();

        #region 新改法
        private List<ClassifiedLineData> listClassifyCrossLines = null;
        private List<ScanlineData> listScanCrossLines = null;
        private readonly int _crossLines = 4;

        public void ProcessViewBanFengHighLow(List<ScanlineData> scanList, List<ClassifiedLineData> classifyList)
        {
            if (classifyList == null || classifyList.Count == 0 || !HasBadPoints())
            {
                return;
            }
            if (listScanCrossLines == null)
            {
                listScanCrossLines = new List<ScanlineData>(_crossLines);
                listClassifyCrossLines = new List<ClassifiedLineData>(_crossLines);
                //第1次数据复制第1线数据
                for (int i = 0; i < _crossLines; i++)
                {
                    listClassifyCrossLines.Add(DeepCopy.DeepCopyByBin(classifyList[0]));
                    listScanCrossLines.Add(DeepCopy.DeepCopyByBin(scanList[0]));
                }
                Tracer.TraceInfo($"first process ban feng data!");
            }
            if (listClassifyCrossLines.Count >= _crossLines)
            {
                int nLines = classifyList.Count;
                int nLineHeight = classifyList[0].HighData.Length;
                Matrix<ushort> _imgFuseSrc = new Matrix<ushort>(nLines + _crossLines, nLineHeight);
                Matrix<ushort> _imgHighSrc = new Matrix<ushort>(nLines + _crossLines, nLineHeight);
                Matrix<ushort> _imgLowSrc = new Matrix<ushort>(nLines + _crossLines, nLineHeight);
                Matrix<ushort> _imgFuseDst = new Matrix<ushort>(nLines + _crossLines, nLineHeight);
                Matrix<ushort> _imgHighDst = new Matrix<ushort>(nLines + _crossLines, nLineHeight);
                Matrix<ushort> _imgLowDst = new Matrix<ushort>(nLines + _crossLines, nLineHeight);
                Matrix<ushort> _imgMaterialSrc = new Matrix<ushort>(nLines + _crossLines, nLineHeight);
                //拷贝交叉数据
                for (int i = 0; i < _crossLines; i++)
                {
                    Buffer.BlockCopy(listScanCrossLines[i].High, 0, _imgHighSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(listScanCrossLines[i].High, 0, _imgHighDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(listScanCrossLines[i].Low, 0, _imgLowSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(listScanCrossLines[i].Low, 0, _imgLowDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(listClassifyCrossLines[i].XRayData, 0, _imgFuseSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(listClassifyCrossLines[i].XRayData, 0, _imgFuseDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(listClassifyCrossLines[i].Material, 0, _imgMaterialSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                }
                //本次数据预留几列交叉数据
                for (int i = 0; i < _crossLines; i++)
                {
                    Buffer.BlockCopy(scanList[scanList.Count - _crossLines + i].High, 0, listScanCrossLines[i].High, 0, nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(scanList[scanList.Count - _crossLines + i].Low, 0, listScanCrossLines[i].Low, 0, nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(classifyList[classifyList.Count - _crossLines + i].XRayData, 0, listClassifyCrossLines[i].XRayData, 0, nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(classifyList[classifyList.Count - _crossLines + i].Material, 0, listClassifyCrossLines[i].Material, 0, nLineHeight * sizeof(ushort));
                }
                //拷贝本次数据
                for (int i = 0; i < classifyList.Count; i++)
                {
                    Buffer.BlockCopy(scanList[i].High, 0, _imgHighSrc.Data, (i + _crossLines) * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(scanList[i].High, 0, _imgHighDst.Data, (i + _crossLines) * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(scanList[i].Low, 0, _imgLowSrc.Data, (i + _crossLines) * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(scanList[i].Low, 0, _imgLowDst.Data, (i + _crossLines) * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(classifyList[i].XRayData, 0, _imgFuseSrc.Data, (i + _crossLines) * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(classifyList[i].XRayData, 0, _imgFuseDst.Data, (i + _crossLines) * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(classifyList[i].Material, 0, _imgMaterialSrc.Data, (i + _crossLines) * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                }
                try
                {
                    if (!ProcesssBanFengHighLow(_viewId, _imgFuseSrc.Mat.DataPointer, _imgHighSrc.Mat.DataPointer, _imgLowSrc.Mat.DataPointer, _imgFuseDst.Mat.DataPointer,
                    _imgHighDst.Mat.DataPointer, _imgLowDst.Mat.DataPointer, _imgMaterialSrc.Mat.DataPointer, _imgFuseSrc.Mat.Height, _imgFuseSrc.Mat.Width, _imgFuseSrc.Mat.Step))
                    {
                        Tracer.TraceInfo($"process view{_viewId} ban feng high low failure!");
                    }
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                }
                //将结果拷贝回去
                for (int i = 0; i < scanList.Count; i++)
                {
                    Buffer.BlockCopy(_imgFuseDst.Data, (i + _crossLines / 2) * nLineHeight * sizeof(ushort), scanList[i].Fused, 0, nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(_imgHighDst.Data, (i + _crossLines / 2) * nLineHeight * sizeof(ushort), scanList[i].High, 0, nLineHeight * sizeof(ushort));
                    Buffer.BlockCopy(_imgLowDst.Data, (i + _crossLines / 2) * nLineHeight * sizeof(ushort), scanList[i].Low, 0, nLineHeight * sizeof(ushort));
                }
            }
        }
        #endregion


        public void ProcessViewBanFengImgOldClass(ClassifiedLineData[] list)
        {

            if (list == null || list.Length == 0)
            {
                if (!isFirst)
                {
                    if (headLineClass.Count > 0)
                    {
                        list = headLineClass.ToArray();
                        headLineClass = new List<ClassifiedLineData>();
                        return;
                    }
                }
                return;
            }
            var newList = list.ToList();
            if (list.Length > cacheNum)
            {
                if (isFirst)
                {
                    for (int i = 0; i < cacheNum * 2; i++)
                    {
                        newList.Insert(0, list.First());
                    }
                }
                else
                {
                    newList.InsertRange(0, headLineClass);
                }
            }
            else
            {
                headLineClass = new List<ClassifiedLineData>();
            }
            list = newList.ToArray();

            int nLines = list.Length;
            int nLineHeight = list[0].XRayData.Length;
            Matrix<ushort> _imgFuseSrc = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgFuseDst = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgMaterialSrc = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgMaterialDst= new Matrix<ushort>(nLines, nLineHeight);
            for (int i = 0; i < list.Length; i++)
            {
                Buffer.BlockCopy(list[i].Material, 0, _imgMaterialSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(list[i].Material, 0, _imgMaterialDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(list[i].XRayData, 0, _imgFuseSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(list[i].XRayData, 0, _imgFuseDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
            }
            if (!ProcessBanFengFuseMaterial(_viewId, _imgFuseSrc.Mat.DataPointer, _imgMaterialSrc.Mat.DataPointer, _imgFuseDst.Mat.DataPointer, _imgMaterialDst.Mat.DataPointer,_imgFuseSrc.Mat.Height, _imgFuseSrc.Mat.Width, _imgFuseSrc.Mat.Step))
            {
                Tracer.TraceInfo($"process view{_viewId} ban feng img failure!");
            }


            headLineClass = new List<ClassifiedLineData>();
            for (int i = 0; i < list.Length; i++)
            {
                Buffer.BlockCopy(_imgFuseDst.Data, i * nLineHeight * sizeof(ushort), list[i].XRayData, 0, nLineHeight * sizeof(ushort));
                var _headLine = headLineClass.ToList();
                if (i + cacheNum * 2 >= list.Length)
                {
                    _headLine.Add(list[i]);
                }
                headLineClass = _headLine.ToList();
            }


            newList = list.ToList();

            newList.RemoveRange(0, cacheNum);

            newList.RemoveRange(newList.Count - cacheNum, cacheNum);

            if (isFirst)
            {
                newList.RemoveRange(0, cacheNum);
            }
            list = newList.ToArray();
            isFirst = false;
        }

        public void ProcessViewBanFengImgHist(int viewIndex, List<DisplayScanlineDataBundle> bundles)
        {
            if (bundles == null || bundles.Count <= 0)
                return;
            int nLines = bundles.Count;
            int nLineHeight = 0;
            if (viewIndex == 0)
                nLineHeight = bundles[0].View1Data.OriginalFused.Length;
            else
                nLineHeight = bundles[0].View2Data.OriginalFused.Length;
            Tracer.TraceDebug($"[banfeng] ProcessViewBanFengImgHist nLines:{nLines},nLineHeight:{nLineHeight}");
            Matrix<ushort> _imgFuseSrc = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgFuseDst = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgMaterialSrc = new Matrix<ushort>(nLines, nLineHeight);
            Matrix<ushort> _imgMaterialDst = new Matrix<ushort>(nLines, nLineHeight);
            for (int i = 0; i < nLines; i++)
            {
                DisplayScanlineData lineData = null;
                if (viewIndex == 0)
                    lineData = bundles[i].View1Data;
                else
                    lineData = bundles[i].View2Data;
                Buffer.BlockCopy(lineData.Material, 0, _imgMaterialSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(lineData.Material, 0, _imgMaterialDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(lineData.OriginalFused, 0, _imgFuseSrc.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
                Buffer.BlockCopy(lineData.OriginalFused, 0, _imgFuseDst.Data, i * nLineHeight * sizeof(ushort), nLineHeight * sizeof(ushort));
            }
            _imgFuseSrc.Save($"D:\\TestTiff\\{viewIndex}_Src.tiff");
            if (!ProcessBanFengFuseMaterial(_viewId, _imgFuseSrc.Mat.DataPointer, _imgMaterialSrc.Mat.DataPointer, _imgFuseDst.Mat.DataPointer, _imgMaterialDst.Mat.DataPointer, _imgFuseSrc.Mat.Height, _imgFuseSrc.Mat.Width, _imgFuseSrc.Mat.Step))
            {
                Tracer.TraceInfo($"process view{_viewId} ban feng img failure!");
            }
            else
            {
                Tracer.TraceInfo($"process view{_viewId} ban feng img success!");
            }
            _imgFuseDst.Save($"D:\\TestTiff\\{viewIndex}_Dst.tiff");
            for (int i = 0; i < nLines; i++)
            {
                DisplayScanlineData lineData = null;
                if (viewIndex == 0)
                    lineData = bundles[i].View1Data;
                else
                    lineData = bundles[i].View2Data;
                Buffer.BlockCopy(_imgFuseDst.Data, i * nLineHeight * sizeof(ushort), lineData.OriginalFused, 0, nLineHeight * sizeof(ushort));
            }
        }
        public void ClearHeadLine()
        {
            headLineScan = new List<ScanlineData>();
            headLineClass=new List<ClassifiedLineData>();
        }

        public void SetIsFirst(bool _isFirst)
        {
            isFirst = _isFirst;
        }

        #region 读取alpha.bin，beta.bin，seg.bin文件
        static void ReadCsv(string filename, out double[] output)
        {
            System.Runtime.Serialization.IFormatter binaryformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            using (Stream st = new FileStream(filename, FileMode.Open))
            {
                output = (double[])binaryformatter.Deserialize(st);
            }
        }
        #endregion
    }
}

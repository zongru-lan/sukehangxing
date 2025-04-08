using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using System.IO;
using UI.XRay.Business.Entities;
using System.Runtime.CompilerServices;
using UI.XRay.Business.Algo;

namespace UI.XRay.Flows.Services.DataProcess
{
    public class ShapeCorrectionService
    {
        public ShapeCorrectionService()
        {
            try
            {
                LoadSettings();
                ExchangeDirectionConfig.Service.GetView1ChannelsCount(out _view1DataLength);
                ExchangeDirectionConfig.Service.GetView2ChannelsCount(out _view2DataLength);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private int[] View1_F1;
        private int[] View1_F2;
        private double[] View1_t;
        private int[] View2_F1;
        private int[] View2_F2;
        private double[] View2_t;

        ushort[] RawDataView1_High = null;
        ushort[] RawDataView1_Low = null;
        ushort[] RawDataView2_High = null;
        ushort[] RawDataView2_Low = null;

        ushort[] ProcessView_XrayData = null;
        ushort[] ProcessView_EnhancedXrayData = null;
        ushort[] ProcessView_Material = null;
        ushort[] ProcessView_ColorIndex = null;
        ushort[] ProcessView_Low = null;
        ushort[] ProcessView_High = null;

        ushort[] ProcessView_OriData = null;

        private bool _initSuccess_ce = false;
        private bool _initSuccess_zheng = false;

        public int View1ImageHeight { get; set; }
        public int View2ImageHeight { get; set; }

        private int _view1DataLength;
        private int _view2DataLength;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ScanlineCorrection(ScanlineDataBundle bundle)
        {
            if (!_initSuccess_ce) return;

            if (bundle.View1LineData != null)
            {
                _view1DataLength = bundle.View1LineData.High.Length;
                if (RawDataView1_High == null)
                    RawDataView1_High = new ushort[_view1DataLength];
                if (RawDataView1_Low == null)
                    RawDataView1_Low = new ushort[_view1DataLength];

                Buffer.BlockCopy(bundle.View1LineData.High, 0, RawDataView1_High, 0, _view1DataLength * sizeof(ushort));
                Buffer.BlockCopy(bundle.View1LineData.Low, 0, RawDataView1_Low, 0, _view1DataLength * sizeof(ushort));

                int length = Math.Min(View1_F1.Length, _view1DataLength);
                for (int i = 0; i < length; i++)
                {
                    if (View1_F1[i] >= 0 && View1_F1[i] < length && View1_F2[i] >= 0 && View1_F2[i] < length)
                    {
                        if (View1_F2[i] == 0)
                        {
                            bundle.View1LineData.High[i] = (ushort)(RawDataView1_High[View1_F1[i]]);
                            bundle.View1LineData.Low[i] = (ushort)(RawDataView1_Low[View1_F1[i]]);
                        }
                        else
                        {
                            bundle.View1LineData.High[i] = (ushort)(RawDataView1_High[View1_F1[i]] * View1_t[i] + RawDataView1_High[View1_F2[i]] * (1 - View1_t[i]));
                            bundle.View1LineData.Low[i] = (ushort)(RawDataView1_Low[View1_F1[i]] * View1_t[i] + RawDataView1_Low[View1_F2[i]] * (1 - View1_t[i]));
                        }
                    }
                }
            }
            if (!_initSuccess_zheng) return;
            if (bundle.View2LineData != null)
            {
                _view2DataLength = bundle.View2LineData.High.Length;
                if (RawDataView2_High == null)
                    RawDataView2_High = new ushort[_view2DataLength];
                if (RawDataView2_Low == null)
                    RawDataView2_Low = new ushort[_view2DataLength];

                Buffer.BlockCopy(bundle.View2LineData.High, 0, RawDataView2_High, 0, _view2DataLength * sizeof(ushort));
                Buffer.BlockCopy(bundle.View2LineData.Low, 0, RawDataView2_Low, 0, _view2DataLength * sizeof(ushort));

                int length = Math.Min(View2_F1.Length, _view2DataLength);
                for (int i = 0; i < length; i++)
                {
                    if (View2_F1[i] >= 0 && View2_F1[i] < length && View2_F2[i] >= 0 && View2_F2[i] < length)
                    {
                        if (View2_F2[i] == 0)
                        {
                            bundle.View2LineData.High[i] = (ushort)(RawDataView2_High[View2_F1[i]]);
                            bundle.View2LineData.Low[i] = (ushort)(RawDataView2_Low[View2_F1[i]]);
                        }
                        else
                        {
                            bundle.View2LineData.High[i] = (ushort)(RawDataView2_High[View2_F1[i]] * View2_t[i] + RawDataView2_High[View2_F2[i]] * (1 - View2_t[i]));
                            bundle.View2LineData.Low[i] = (ushort)(RawDataView2_Low[View2_F1[i]] * View2_t[i] + RawDataView2_Low[View2_F2[i]] * (1 - View2_t[i]));
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public DisplayScanlineData DisplayScanlineCorrection(DisplayScanlineData data, DetectViewIndex view)
        {
            if (data == null) return data;
            if (DetectViewIndex.View1 == view)
            {
                if (!_initSuccess_ce) return data;
                if (_view1DataLength != data.XRayData.Length) return data;
                if (View1_F1.Length != View1ImageHeight || View1_F2.Length  != View1ImageHeight || View1_t.Length != View1ImageHeight)
                {
                    return data;
                }

                ProcessView_XrayData = new ushort[View1ImageHeight];
                Calc1(data.XRayData, ProcessView_XrayData);

                ProcessView_EnhancedXrayData = new ushort[View1ImageHeight];
                if (data.XRayDataEnhanced != null) Calc1(data.XRayDataEnhanced, ProcessView_EnhancedXrayData);

                ProcessView_Low = new ushort[View1ImageHeight];
                if (data.LowData != null) Calc1(data.LowData, ProcessView_Low);

                ProcessView_High = new ushort[View1ImageHeight];
                if (data.HighData != null) Calc1(data.HighData, ProcessView_High);

                ProcessView_Material = new ushort[View1ImageHeight];
                Calc1(data.Material, ProcessView_Material);

                ProcessView_ColorIndex = new ushort[View1ImageHeight];
                Calc1(data.ColorIndex, ProcessView_ColorIndex);

                DisplayScanlineData ds = new DisplayScanlineData(view, ProcessView_XrayData, ProcessView_EnhancedXrayData,
                    ProcessView_Material, ProcessView_ColorIndex, ProcessView_Low, ProcessView_High, data.LineNumber, data.IsAir);
                return ds;
            }
            else
            {
                if (!_initSuccess_zheng) return data;
                if (_view2DataLength != data.XRayData.Length) return data;
                if (View2_F1.Length != View2ImageHeight || View2_F2.Length != View2ImageHeight || View2_t.Length != View2ImageHeight)
                {
                    return data;
                }

                ProcessView_XrayData = new ushort[View2ImageHeight];
                Calc2(data.XRayData, ProcessView_XrayData);

                ProcessView_EnhancedXrayData = new ushort[View2ImageHeight];
                if (data.XRayDataEnhanced != null) Calc2(data.XRayDataEnhanced, ProcessView_EnhancedXrayData);

                ProcessView_Low = new ushort[View2ImageHeight];
                if (data.LowData != null) Calc2(data.LowData, ProcessView_Low);

                ProcessView_High = new ushort[View2ImageHeight];
                if (data.HighData != null) Calc2(data.HighData, ProcessView_High);

                ProcessView_Material = new ushort[View2ImageHeight];
                Calc2(data.Material, ProcessView_Material);

                ProcessView_ColorIndex = new ushort[View2ImageHeight];
                Calc2(data.ColorIndex, ProcessView_ColorIndex);

                DisplayScanlineData ds = new DisplayScanlineData(view, ProcessView_XrayData, ProcessView_EnhancedXrayData,
                    ProcessView_Material, ProcessView_ColorIndex, ProcessView_Low, ProcessView_High, data.LineNumber, data.IsAir);
                return ds;
            }
        }

        public DisplayScanlineData HistDisplayScanlineCorrection(DisplayScanlineData data, DetectViewIndex view)
        {
            if (data == null) return data;
            if (DetectViewIndex.View1 == view)
            {
                if (!_initSuccess_ce) return data;
                ProcessView_OriData = new ushort[View1ImageHeight];
                Calc1(data.OriginalFused, ProcessView_OriData);
                data.SetOriginalFusedData(ProcessView_OriData);
                return data;
            }
            else
            {
                if (!_initSuccess_zheng) return data;
                ProcessView_OriData = new ushort[View2ImageHeight];
                Calc2(data.OriginalFused, ProcessView_OriData);
                data.SetOriginalFusedData(ProcessView_OriData);
                return data;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Calc1(ushort[] source, ushort[] process)
        {
            int sourceLength = source.Length;
            if (source == null) process = null;
            for (int i = 0; i < View1ImageHeight; i++)
            {
                if (View1_F1[i] >= 1 && View1_F1[i] <= sourceLength && View1_F2[i] >= 1 && View1_F2[i] <= sourceLength)
                {
                    process[i] = (ushort)(source[View1_F1[i] - 1] * View1_t[i] + source[View1_F2[i] - 1] * (1 - View1_t[i]));
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Calc2(ushort[] source, ushort[] process)
        {
            int sourceLength = source.Length;
            for (int i = 0; i < View2ImageHeight; i++)
            {
                if (View2_F1[i] >= 1 && View2_F1[i] <= sourceLength && View2_F2[i] >= 1 && View2_F2[i] <= sourceLength)
                {
                    process[i] = (ushort)(source[View2_F1[i] - 1] * View2_t[i] + source[View2_F2[i] - 1] * (1 - View2_t[i]));
                }
            }
        }
        private void LoadSettings()
        {
            try
            {
                string cefile = ExchangeDirectionConfig.Service.GetView1ShapeFilePath();
                string zhengfile = ExchangeDirectionConfig.Service.GetView2ShapeFilePath();
                if (ReadCeFile(cefile))
                {
                    _initSuccess_ce = true;
                }
                else
                {
                    _initSuccess_ce = false;
                }
                if (ReadZhengFile(zhengfile))
                {
                    _initSuccess_zheng = true;
                }
                else
                {
                    _initSuccess_zheng = false;
                }
            }
            catch (Exception e)
            {
                _initSuccess_ce = false;
                _initSuccess_zheng = false;
                Tracer.TraceException(e);
            }
        }
        bool ReadCeFile(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    FileStream fs = new FileStream(file, FileMode.Open);
                    StreamReader reader = new StreamReader(fs);

                    string sStuName = string.Empty;
                    List<string> strList = new List<string>();
                    while ((sStuName = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(sStuName.Trim()))
                        {
                            strList.Add(sStuName.Trim().ToString());
                        }                        
                    }

                    var csvRows = strList.Count;

                    View1_t = new double[csvRows];
                    View1_F1 = new int[csvRows];
                    View1_F2 = new int[csvRows];
                    
                    for (int i = 0; i < csvRows; i++)
                    {
                        string[] strArray = strList[i].Split(',');
                        if (strArray.Length == 3)
                        {
                            View1_t[i] = double.Parse(strArray[0], new System.Globalization.CultureInfo("zh-CN"));
                            View1_F1[i] = int.Parse(strArray[1]);
                            View1_F2[i] = int.Parse(strArray[2]);                 
                        }
                        else
                        {
                            View1_t[i] = 0;
                            View1_F1[i] = 1;
                            View1_F2[i] = 1;
                        }
                    }

                    reader.Close();
                    fs.Close();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                return false;
            }
        }

        bool ReadZhengFile(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    FileStream fs = new FileStream(file, FileMode.Open);
                    StreamReader reader = new StreamReader(fs);

                    string sStuName = string.Empty;
                    List<string> strList = new List<string>();
                    while ((sStuName = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(sStuName.Trim()))
                        {
                            strList.Add(sStuName.Trim().ToString());
                        }                        
                    }

                    _view2DataLength = strList.Count;

                    View2_t = new double[_view2DataLength];
                    View2_F1 = new int[_view2DataLength];
                    View2_F2 = new int[_view2DataLength];
                    for (int i = 0; i < _view2DataLength; i++)
                    {
                        string[] strArray = strList[i].Split(',');
                        if (strArray.Length == 3)
                        {
                            View2_t[i] = double.Parse(strArray[0], new System.Globalization.CultureInfo("zh-CN"));
                            View2_F1[i] = int.Parse(strArray[1]);
                            View2_F2[i] = int.Parse(strArray[2]);
                        }
                        else
                        {
                            View2_t[i] = 0;
                            View2_F1[i] = 1;
                            View2_F2[i] = 1;
                        }
                    }

                    reader.Close();
                    fs.Close();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    public class HistogramEquation
    {
        public void Histogram(ImageViewData viewData)
        {
            EnhenceImage(viewData.ScanLines, viewData.ScanLines.Length, viewData.ScanLines[0].XRayData.Length, 0, 500 * 16, 1000 * 16, 4000 * 16);

        }
        private void EnhenceImage(ClassifiedLineData[] InputDataList, int ImageWidth, int ImageHeight,
            int HistoBegin, int HistoEnd, int StretchBegin, int StretchEnd)
        {
            List<ushort[]> middatalist = new List<ushort[]>();
            List<ushort[]> midoutputdatalist = new List<ushort[]>();
            foreach (var u in InputDataList)
            {
                middatalist.Add(u.XRayData);
            }
            if (HistoBegin < 0 || StretchBegin < 0 || HistoBegin >= HistoEnd || StretchBegin >= StretchEnd)
                return;

            float[] histogram = new float[65536];
            float[] sum_histogram = new float[65536];
            Array.Clear(histogram, 0, 65536);
            Array.Clear(sum_histogram, 0, 65536);

            int size = 0;
            foreach (var f in InputDataList)
                size += f.Material.Length;
            int imagesize = ImageWidth * ImageHeight;
            int sum = 0;
            int v = 0;

            // 局部灰度区间统计直方图并归一化
            //foreach (ushort[] f in InputDataList)
            foreach (ushort[] f in middatalist)
            {
                foreach (ushort data in f)
                {
                    if (data >= HistoBegin && data <= HistoEnd)
                    {
                        if (data < 0.0f)
                            v = 0;
                        else if (data > 65535.0f)
                            v = 65535;
                        else
                            v = Convert.ToInt32(data);

                        histogram[v]++;
                        sum++;
                    }
                }
            }
            int count = 0;
            for (int i = HistoBegin; i <= 65535; i++)
            {
                histogram[i] /= sum;

                if (i == HistoBegin)
                    sum_histogram[i] = histogram[i];
                else if (i <= HistoEnd)
                    sum_histogram[i] = sum_histogram[i - 1] + histogram[i];
                else
                    sum_histogram[i] = 1.0F;
            }

            //均衡化
            //foreach (ushort[] f in InputDataList)
            foreach (ushort[] f in middatalist)
            {
                ushort[] des = new ushort[f.Length];
                int i = 0;
                foreach (ushort data in f)
                {
                    if (data < 0.0f)
                        v = 0;
                    else if (data > 65535.0f)
                        v = 65535;
                    else
                        v = Convert.ToInt32(data);
                    int tempint = (int)(sum_histogram[v] * 65535);
                    if (tempint > 65535)
                        tempint = 65535;
                    des[i] = (ushort)tempint;
                    i++;
                }
                midoutputdatalist.Add(des);
            }

            //灰度拉伸
            for (int i = 0; i < midoutputdatalist.Count; i++)
            {
                for (int j = 0; j < midoutputdatalist[i].Length; j++)
                {
                    if (midoutputdatalist[i][j] < StretchBegin)
                        midoutputdatalist[i][j] = 0;
                    else if (midoutputdatalist[i][j] > StretchEnd)
                        midoutputdatalist[i][j] = 65535;
                    else
                        midoutputdatalist[i][j] = (ushort)(65535.0f / (StretchEnd - StretchBegin) * midoutputdatalist[i][j] - 65535.0f / (StretchEnd - StretchBegin) * StretchBegin);
                }
            }

            for (int i = 0; i < midoutputdatalist.Count; i++)
            {
                for (int j = 0; j < midoutputdatalist[i].Length; j++)
                {
                    if (InputDataList[i].XRayData[j] < 3000)
                    {
                        InputDataList[i].XRayData[j] = midoutputdatalist[i][j];
                        if (InputDataList[i].XRayDataEnhanced != null)
                        {
                            InputDataList[i].XRayDataEnhanced[j] = midoutputdatalist[i][j];
                        }
                    }
                }
            }
        }
    }
}

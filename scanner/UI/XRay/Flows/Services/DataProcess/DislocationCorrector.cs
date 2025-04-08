using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.DataProcess
{
    /// <summary>
    /// <para>错茬矫正器</para>
    /// <para>Last Edit Time：2024-12-21 15:18</para>
    /// </summary>
    public class DislocationCorrector
    {
        #region Fields
        private readonly int bytesPerDM;
        private readonly int singleEnergyLineBytes;
        private readonly int singleEnergyLinePixels;
        private int[] pointOffsets;
        private readonly int[] copyOffsets;
        private int maxOffset;

        private byte[][] rawLinesBuffer;
        private int bufferPointer;
        private bool cacheFinished;

        private ushort[][] _leDataBuffer;
        private ushort[][] _heDataBuffer;
        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pixelPerDM">每块探测器的像素数，通常是64</param>
        /// <param name="pointOffsets">探测器的错位点数</param>
        public DislocationCorrector(int pixelPerDM, int[] pointOffsets)
        {
            bytesPerDM = pixelPerDM * sizeof(ushort);
            singleEnergyLineBytes = pointOffsets.Length * bytesPerDM;
            singleEnergyLinePixels = pointOffsets.Length * pixelPerDM;
            NormalizeOffsets(ref pointOffsets);
            this.pointOffsets = pointOffsets;
            copyOffsets = new int[pointOffsets.Length];
            for (int i = 0; i < pointOffsets.Length; i++)
            {
                copyOffsets[i] = i * bytesPerDM;
            }
            maxOffset = GetMaxOffset(pointOffsets);
            rawLinesBuffer = new byte[maxOffset + 1][];
            _leDataBuffer = new ushort[maxOffset + 1][];
            _heDataBuffer = new ushort[maxOffset + 1][];
        }

        #region Methods
        /// <summary>
        /// 设置错位点数，需要先停止采集再调用，不然肯定出错
        /// </summary>
        /// <param name="offsets">错位点数</param>
        public void SetOffsets(int[] offsets)
        {
            NormalizeOffsets(ref offsets);
            this.pointOffsets = offsets;
            maxOffset = GetMaxOffset(offsets);
            bufferPointer = 0;
            cacheFinished = false;
            rawLinesBuffer = new byte[maxOffset + 1][];
        }

        /// <summary>
        /// 获取校正后的数据
        /// </summary>
        /// <param name="rawData">原始数据</param>
        /// <param name="lineId">线编号</param>
        /// <param name="newLineId">校正返回线的线编号</param>
        /// <returns>返回高低能数据，先低后高；未匹配完成时返回null</returns>
        public ushort[][] GetCorrectedData(byte[] rawData, int lineId, out int newLineId)
        {
            if (maxOffset == 0)
            {
                newLineId = lineId;
                int copyOffset = 0;
                ushort[][] ret = new ushort[2][];
                for (int i = 0; i < 2; i++)
                {
                    ret[i] = new ushort[singleEnergyLinePixels];
                    Buffer.BlockCopy(rawData, copyOffset, ret[i], 0, singleEnergyLineBytes);
                    copyOffset += singleEnergyLineBytes;
                }
                return ret;
            }
            if (!cacheFinished)
            {
                rawLinesBuffer[bufferPointer] = rawData;
                bufferPointer++;
                if (bufferPointer > maxOffset)
                {
                    cacheFinished = true;
                    bufferPointer = 0;
                }
                newLineId = lineId;
                return null;
            }
            rawLinesBuffer[bufferPointer] = rawData;
            bufferPointer++;

            ushort[] low = new ushort[singleEnergyLinePixels];
            ushort[] high = new ushort[singleEnergyLinePixels];
            for (int dm = 0; dm < pointOffsets.Length; dm++)
            {
                var src = rawLinesBuffer[(bufferPointer + pointOffsets[dm]) % rawLinesBuffer.Length];
                Buffer.BlockCopy(src, copyOffsets[dm], low, copyOffsets[dm], bytesPerDM);
                Buffer.BlockCopy(src, copyOffsets[dm] + singleEnergyLineBytes, high, copyOffsets[dm], bytesPerDM);
            }
            newLineId = lineId - maxOffset;

            if (bufferPointer > maxOffset)
            {
                bufferPointer = 0;
            }

            return new ushort[][] { low, high };
        }

        public ushort[][] GetCorrectedData(ushort[] leData, ushort[] heData)
        {
            if (!cacheFinished)
            {
                _leDataBuffer[bufferPointer] = leData;
                _heDataBuffer[bufferPointer] = heData;
                bufferPointer++;
                if (bufferPointer > maxOffset)
                {
                    cacheFinished = true;
                    bufferPointer = 0;
                }
                return null;
            }
            _leDataBuffer[bufferPointer] = leData;
            _heDataBuffer[bufferPointer] = heData;
            bufferPointer++;
            ushort[] low = new ushort[singleEnergyLinePixels];
            ushort[] high = new ushort[singleEnergyLinePixels];
            for (int dm = 0; dm < pointOffsets.Length; dm++)
            {
                Buffer.BlockCopy(_leDataBuffer[(bufferPointer + pointOffsets[dm]) % _leDataBuffer.Length], copyOffsets[dm], low, copyOffsets[dm], bytesPerDM);
                Buffer.BlockCopy(_heDataBuffer[(bufferPointer + pointOffsets[dm]) % _heDataBuffer.Length], copyOffsets[dm], high, copyOffsets[dm], bytesPerDM);
            }
            if (bufferPointer > maxOffset)
            {
                bufferPointer = 0;
            }
            return new ushort[][] { low , high };
        }

        /// <summary>
        /// 校正传入的数据并返回
        /// </summary>
        /// <param name="lines">需要校正的数据</param>
        /// <returns>校正后的数据；如果输入有误则返回null</returns>
        public ClassifiedLineData[] GetCorrectedLineList(ClassifiedLineData[] lines)
        {
            if (lines.Length < maxOffset)
            {
                return null;
            }
            ClassifiedLineData[] ret = new ClassifiedLineData[lines.Length - maxOffset];
            int length = lines[0].XRayData.Length;
            for (int i = 0; i < maxOffset; i++)
            {
                ClassifiedLineData line = new ClassifiedLineData(lines[0].ViewIndex, new ushort[length], new ushort[length], new ushort[length]);
                ret[i] = line;
            }
            for (int i = 0; i < lines.Length - maxOffset; i++)
            {
                ushort[] le = new ushort[length];
                for (int dm = 0; dm < pointOffsets.Length; dm++)
                {
                    Buffer.BlockCopy(lines[i + pointOffsets[dm]].XRayData, copyOffsets[dm], le, copyOffsets[dm], bytesPerDM);
                }
                ClassifiedLineData line = new ClassifiedLineData(lines[i].ViewIndex, le, lines[i].XRayDataEnhanced, lines[i].Material, lines[i].IsAir);
                ret[i] = line;
            }
            return ret;
        }
        #endregion

        #region Tools
        private void NormalizeOffsets(ref int[] offsets)
        {
            int min = offsets[0];
            for (int i = 1; i < offsets.Length; i++)
            {
                if (offsets[i] < min)
                {
                    min = offsets[i];
                }
            }
            if (min != 0)
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    offsets[i] -= min;
                }
            }
        }

        private int GetMaxOffset(int[] offsets)
        {
            int max = offsets[0];
            for (int i = 1; i < offsets.Length; i++)
            {
                if (offsets[i] > max)
                {
                    max = offsets[i];
                }
            }
            return max;
        }
        #endregion
    }
}

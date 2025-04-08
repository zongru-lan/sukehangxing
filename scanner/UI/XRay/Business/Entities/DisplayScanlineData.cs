using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UI.Common.Tracers;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 编号排序的线数据：供图像处理、显示、回拉定位时使用
    /// </summary>
    [Serializable]
    public class DisplayScanlineData : ClassifiedLineData
    {
        /// <summary>
        /// 构造一线用于显示的扫描线，包含物质分类、颜色索引等。
        /// </summary>
        /// <param name="data">包含物质分类和融合图像的一线数据</param>
        /// <param name="colorIndex">与物质分类对应的颜色表索引</param>
        /// <param name="lineNumber">本线数据在整个数据区中的编号</param>
        public DisplayScanlineData(ClassifiedLineData data, ushort[] colorIndex, int lineNumber)
        {
            this.ViewIndex = data.ViewIndex;
            this.LowData = data.LowData;
            this.HighData = data.HighData;
            this.OriginalFused = data.OriginalFused;
            this.XRayData = data.XRayData;
            this.Material = data.Material;
            this.XRayDataEnhanced = data.XRayDataEnhanced;
            this.LineNumber = lineNumber;
            this.ColorIndex = colorIndex;
            base.IsAir = data.IsAir;
        }

        public DisplayScanlineData(DetectViewIndex viewIndex, ushort[] xrayData, ushort[] xrayDataEnhanced, ushort[] material, ushort[] colorIndex, int lineNumber, bool isAir)
        {
            this.ViewIndex = ViewIndex;
            this.XRayData = xrayData;
            this.XRayDataEnhanced = xrayDataEnhanced;
            this.Material = material;
            this.LineNumber = lineNumber;
            this.ColorIndex = colorIndex;
            base.IsAir = isAir;
        }

        public DisplayScanlineData(DetectViewIndex viewIndex, ushort[] xrayData, ushort[] xrayDataEnhanced, ushort[] material, ushort[] colorIndex, ushort[] oriFuse, int lineNumber, bool isAir)
        {
            this.ViewIndex = ViewIndex;
            this.OriginalFused = oriFuse;
            this.XRayData = xrayData;
            this.XRayDataEnhanced = xrayDataEnhanced;
            this.Material = material;
            this.LineNumber = lineNumber;
            this.ColorIndex = colorIndex;
            base.IsAir = isAir;
        }

        public DisplayScanlineData(DetectViewIndex viewIndex, ushort[] xrayData, ushort[] xrayDataEnhanced, ushort[] material, ushort[] colorIndex, ushort[] low, ushort[] high, int lineNumber, bool isAir)
        {
            this.ViewIndex = ViewIndex;
            this.XRayData = xrayData;
            this.XRayDataEnhanced = xrayDataEnhanced;
            this.Material = material;
            this.LineNumber = lineNumber;
            this.ColorIndex = colorIndex;
            this.LowData = low;
            this.HighData = high;
            base.IsAir = isAir;
        }

        /// <summary>
        /// 本线数据在整个数据区中的编号，编号取值可以为负数。
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// color index base on 0. range is [0, 31]
        /// </summary>
        public ushort[] ColorIndex { get; protected set; }


        /// <summary>
        /// 提取数据，并转换为XRayMatLineData对象
        /// </summary>
        /// <returns></returns>
        public ClassifiedLineData ToXRayMatLineData()
        {
            var result = new ClassifiedLineData(this.ViewIndex, this.XRayData, this.XRayDataEnhanced, this.Material, this.IsAir);
            result.SetOriginalFusedData(this.OriginalFused);
            return result;
            //return new ClassifiedLineData(this.ViewIndex, this.XRayData, this.XRayDataEnhanced, this.Material, this.IsAir);
        }

        public ClassifiedLineData ToXRayMatLineDataWithHighLow()
        {
            var result = new ClassifiedLineData(this.ViewIndex, this.XRayData, this.XRayDataEnhanced, this.Material, this.LowData, this.HighData);
            result.SetOriginalFusedData(this.OriginalFused);
            return result;
            //return new ClassifiedLineData(this.ViewIndex, this.XRayData, this.XRayDataEnhanced, this.Material, this.LowData, this.HighData);
        }

        /// <summary>
        /// 记录线数据生成时间
        /// </summary>
        public DateTime? CreatedTime { get;  set; }
        
    }

    /// <summary>
    /// 表示两个视角的编号排序的线数据组合，两线数据在物理空间中具有对应关系
    /// </summary>
    [Serializable]
    public class DisplayScanlineDataBundle
    {
        public DisplayScanlineDataBundle(DisplayScanlineData view1, DisplayScanlineData view2)
        {
            //if (view1 == null)
            //{
            //    throw new ArgumentNullException("view1");
            //}

            View1Data = view1;
            View2Data = view2;
        }

        /// <summary>
        /// 视角1的线数据
        /// </summary>
        public DisplayScanlineData View1Data { get; private set; }

        /// <summary>
        /// 视角2的线数据
        /// </summary>
        public DisplayScanlineData View2Data { get; private set; }

        /// <summary>
        /// 视角1的数据的线编号
        /// </summary>
        public int LineNumber
        {
            get { return View1Data == null ? 0 : View1Data.LineNumber; }
            set
            {
                if (View1Data != null)
                    View1Data.LineNumber = value;
                if (View2Data != null)
                    View2Data.LineNumber = value;
            }
        }

        /// <summary>
        /// 判断当前扫描线束是否为空气扫描线：如果只有一个视角，则只判定第一视角；如果有两个视角，则判定两个视角的“与”
        /// </summary>
        public bool IsAir
        {
            get
            {
                bool view1Air = (View1Data == null || View1Data.IsAir);
                bool view2Air = (View2Data == null || View2Data.IsAir);
                return view1Air && view2Air;
            }
        }
    }

    public class DeepCopy
    {
        public static T DeepCopyByBin<T>(T obj)
        {
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {
                
                BinaryFormatter bf = new BinaryFormatter();
                //序列化成流
                
                bf.Serialize(ms, obj);
                
                ms.Seek(0, SeekOrigin.Begin);
                //反序列化成对象
             
                retval = bf.Deserialize(ms);
               
                ms.Close();
              
                
            }
            return (T)retval;
        }
    }

    public  static class Converter  //yxc
    {

    }
}

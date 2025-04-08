using System;

namespace UI.XRay.Business.Entities
{

    /// <summary>
    /// Represents a line of corrected (normalized and bad detector modified) x-ray data.
    /// </summary>
    [Serializable]
    public class ScanlineData
    {
        /// <summary>
        /// Construct from input parameters
        /// </summary>
        /// <param name="viewIndex">view index which generates the data</param>
        /// <param name="le">low energy data</param>
        /// <param name="he">high energy data</param>
        public ScanlineData(DetectViewIndex viewIndex, ushort[] le, ushort[] he)
        {
            if (le == null)
            {
                // 低能不能为空
                throw new ArgumentNullException("le");
            }

            ViewIndex = viewIndex;
            Low = le;
            High = he;
            LineLength = le.Length;

            if (le != null && he != null && le.Length != he.Length)
            {
                throw new ArgumentException("HE and LE should have same length.");
            }
        }
        /// <summary>
        /// Construct from input parameters
        /// </summary>
        /// <param name="viewIndex">view index which generates the data</param>
        /// <param name="le">low energy data</param>
        /// <param name="he">high energy data</param>
        public ScanlineData(DetectViewIndex viewIndex, ushort[] le, ushort[] he, ushort[] fused)
        {
            if (le == null)
            {
                // 低能不能为空
                throw new ArgumentNullException("le");
            }

            ViewIndex = viewIndex;
            Low = le;
            High = he;
            Fused = fused;
            LineLength = le.Length;

            if (le != null && he != null && le.Length != he.Length)
            {
                throw new ArgumentException("HE and LE should have same length.");
            }
        }

        /// <summary>
        /// 根据能量类型，探测点数初始化一线空数据
        /// </summary>
        /// <param name="view">视角</param>
        /// <param name="sensorType">能量类型</param>
        /// <param name="detectorCount">某一种能量，如低能的探测点数据</param>
        public ScanlineData(DetectViewIndex view, XRaySensorType sensorType, int detectorCount)
        {
            if (detectorCount <=0 )
            {
                throw new ArgumentNullException("detectorCount", "detectorCount should > 0");
            }

            ViewIndex = view;

            if (sensorType == XRaySensorType.Dual)
            {
                Low = new ushort[detectorCount];
                High = new ushort[detectorCount];
            }
            else if (sensorType == XRaySensorType.Single)
            {
                Low = new ushort[detectorCount];
            }

            LineLength = detectorCount;
        }

        /// <summary>
        /// The view generates this data
        /// </summary>
        public DetectViewIndex ViewIndex { get; private set; }

        /// <summary>
        /// 能量类型，至少应该是单能量，及le不允许为空
        /// </summary>
        public XRaySensorType XRaySensor
        {
            get { return Low != null && High != null ? XRaySensorType.Dual : XRaySensorType.Single; }
        }

        /// <summary>
        /// A line of Low energy X-Ray data
        /// </summary>
        public ushort[] Low { get; private set; }

        /// <summary>
        /// A line of High energy X-Ray data. For non-Dual energy system, this member is null.
        /// </summary>
        public ushort[] High { get; private set; }

        /// <summary>
        /// A line of Fused Data From high and low energy X-Ray data.
        /// </summary>
        public ushort[] Fused { get; set; }

        /// <summary>
        /// Original a line of Fused Data From high and low energy X-Ray data.
        /// </summary>
        public ushort[] OriginalFused { get; set; }

        /// <summary>
        /// length of the X-Ray line data (pixel count)
        /// </summary>
        public int LineLength { get; private set; }

        public void BackupFusedData()
        {
            OriginalFused = new ushort[Fused.Length];
            Buffer.BlockCopy(Fused, 0, OriginalFused, 0, Fused.Length * sizeof(ushort));
        }

        public void SetOriginalFusedData(ushort[] originalFusedData)
        {
            this.OriginalFused = originalFusedData;
        }

        public ScanlineData Clone()
        {
            ushort[] le = null, he = null, fused = null;

            var pixelCount = this.LineLength * sizeof(ushort);

            if (this.Low != null)
            {
                le = new ushort[this.LineLength];
                Buffer.BlockCopy(this.Low, 0, le, 0, pixelCount);
            }
            if (this.High != null)
            {
                he = new ushort[this.LineLength];
                Buffer.BlockCopy(this.High, 0, he, 0, pixelCount);
            }
            if (this.Fused != null)
            {
                fused = new ushort[this.LineLength];
                Buffer.BlockCopy(this.Fused, 0, fused, 0, pixelCount);
            }
            var ret = new ScanlineData(this.ViewIndex, le, he, fused);
            if (this.OriginalFused != null)
            {
                var fused_unmodified = new ushort[this.LineLength];
                Buffer.BlockCopy(this.OriginalFused, 0, fused_unmodified, 0, pixelCount);
                ret.SetOriginalFusedData(fused_unmodified);
            }
            return ret;
        }
    }

    /// <summary>
    /// Represents a bundle of X-Ray line data: a line of view1 + (a line of view2)
    /// </summary>
    [Serializable]
    public class ScanlineDataBundle
    {
      //  public long tag;//yxc
        /// <summary>
        /// Construct a data bundle from input lines data
        /// </summary>
        /// <param name="view1">line data of view1</param>
        /// <param name="view2">line data of view2</param>
        public ScanlineDataBundle(ScanlineData view1, ScanlineData view2)
        {
            View1LineData = view1;
            View2LineData = view2;
        }

        /// <summary>
        /// corrected View1 line data
        /// </summary>
        public ScanlineData View1LineData { get; private set; }

        /// <summary>
        /// Corrected View2 line data
        /// </summary>
        public ScanlineData View2LineData { get; private set; }

        public ScanlineDataBundle Clone()
        {
            ScanlineData view1LineData = null;
            ScanlineData view2LineData = null;

            if (this.View1LineData != null)
            {
                view1LineData = this.View1LineData.Clone();
            }
            if (this.View2LineData != null)
            {
                view2LineData = this.View2LineData.Clone();
            }

            return new ScanlineDataBundle(view1LineData, view2LineData);
        }
    }
}

using System;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// Represents a line of raw X-Ray scanning data
    /// </summary>
    [Serializable]
    public class RawScanlineData : ScanlineData
    {
        /// <summary>
        /// 构造新数据的实例
        /// </summary>
        /// <param name="viewIndex">视角索引</param>
        /// <param name="le">低能数据</param>
        /// <param name="he">高能数据</param>
        public RawScanlineData(DetectViewIndex viewIndex, ushort[] le, ushort[] he)
            : base(viewIndex, le, he)
        {
            LineNumber = 1;
        }

        /// <summary>
        /// 是否是本底值
        /// </summary>
        public bool IsGround { get; set; }

        /// <summary>
        /// The number of this line, incremental
        /// </summary>
        public int LineNumber { get; private set; }
    }

    [Serializable]
    public class RawScanlineDataBundle
    {
        public RawScanlineData View1RawData;
        public RawScanlineData View2RawData;
      //  public long DataTag;  //yxc

        public RawScanlineDataBundle(RawScanlineData view1, RawScanlineData view2)
        {
            View1RawData = view1;
            View2RawData = view2;
        }
    }

}

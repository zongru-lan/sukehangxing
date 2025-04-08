using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// Represents an X-Ray image file, which contains a series of X-Ray lines data for at most two views (For dual views).
    /// <remarks></remarks>
    /// </summary>
    [Serializable]
    public class XRayScanlinesImage
    {
        /// <summary>
        /// 图像在数据库表中对应的记录的编号，即主键
        /// </summary>
        public long ImageRecordId { get; set; }

        /// <summary>
        /// 设备类型字符串，如 6550等
        /// </summary>
        public string MachineType { get; set; }

        /// <summary>
        /// serial number of the machine
        /// </summary>
        public string MachineNumber { get; set; }

        /// <summary>
        /// 图像生成的时间
        /// </summary>
        public DateTime ScanningTime { get; set; }

        /// <summary>
        /// The operator id logged in who scanned this image
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// 获取或设置此图像在显示时的注入Tip
        /// </summary>
        public TipInjection TipInjection { get; set; }

        /// <summary>
        /// Count of X-Ray views
        /// </summary>
        public int ViewsCount
        {
            get
            {
                if (View1Data != null && View2Data == null)
                {
                    return 1;
                }

                if (View1Data != null && View2Data != null)
                {
                    return 2;
                }

                // 视角1必须有数据
                return 0;
            }
        }

        /// <summary>
        /// 视角1的图像数据
        /// </summary>
        public ImageViewData View1Data { get; private set; }

        /// <summary>
        /// 视角2的图像数据
        /// </summary>
        public ImageViewData View2Data { get; private set; }

        /// <summary>
        /// 图像的缩略图，默认返回第一视角图像的缩略图
        /// </summary>
        public Bitmap Thumbnail { get { return View2Data == null ? View1Data.Thumbnail == null ? null : View1Data.Thumbnail : View2Data.Thumbnail; } }
        //public Bitmap Thumbnail { get { return  View1Data.Thumbnail != null ?  View1Data.Thumbnail : View2Data.Thumbnail; } }

        /// <summary>
        /// Vertical scale, default to 1.0.
        /// </summary>
        public float VerticalScale { get { return View1Data == null ? 1 : View1Data.VerticalScale; } }

        /// <summary>
        /// 构造函数：根据输入的线数据及相关信息，构造一个新的实例
        /// </summary>
        /// <param name="xrayData">X-Ray lines data </param>
        /// <param name="machineNumber">machine number</param>
        /// <param name="accountId">operator id</param>
        /// <param name="xg1Current">current of X-Ray source 1</param>
        /// <param name="xg1Voltage">voltage of X-Ray source 1</param>
        /// <param name="xg2Current">current of X-Ray source 2</param>
        /// <param name="xg2Voltage">voltage of X-Ray source 2</param>
        /// <param name="verticalScale">Vertical scale, default to 1.0.</param>
        public XRayScanlinesImage(IEnumerable<ClassifiedLineDataBundle> xrayData, string machineType,
            string machineNumber, string accountId,
            float xg1Current, float xg1Voltage, float xg2Current, float xg2Voltage, float verticalScale = 1.0F)
        {
            var bundles = xrayData as ClassifiedLineDataBundle[] ?? xrayData.ToArray();
            if (xrayData == null || !bundles.Any())
            {
                throw new ArgumentException("Input XRay data is null or empty.", "xrayData");
            }

            var view1Lines = new ClassifiedLineData[bundles.Count()];
            ClassifiedLineData[] view2Lines = null;

            if (bundles.Last().View2Data != null)
            {
                view2Lines = new ClassifiedLineData[bundles.Length];
            }

            // 将数据加入到view1和view2的数据缓冲区中
            for (int i = 0; i < bundles.Length; i++)
            {
                view1Lines[i] = bundles[i].View1Data;
                if (view2Lines != null)
                {
                    view2Lines[i] = bundles[i].View2Data;
                }
            }

            ScanningTime = DateTime.Now;
            AccountId = accountId;
            MachineType = machineType;
            MachineNumber = machineNumber;

            View1Data = new ImageViewData(view1Lines, DetectViewIndex.View1, view1Lines[0].XRayData.Length, 0, verticalScale)
            {
                XRayGCurrent = xg1Current,
                XRayGVoltage = xg1Voltage
            };

            if (view2Lines != null)
            {
                View2Data = new ImageViewData(view2Lines,DetectViewIndex.View2, view2Lines[0].XRayData.Length, 0, verticalScale)
                {
                    XRayGCurrent = xg2Current,
                    XRayGVoltage = xg2Voltage
                };
            }
        }

        public XRayScanlinesImage(IEnumerable<ClassifiedLineDataBundle> xrayData)
        {
            var bundles = xrayData as ClassifiedLineDataBundle[] ?? xrayData.ToArray();
            if (xrayData == null || !bundles.Any())
            {
                throw new ArgumentException("Input XRay data is null or empty.", "xrayData");
            }

            var view1Lines = new ClassifiedLineData[bundles.Count()];
            ClassifiedLineData[] view2Lines = null;

            if (bundles.Last().View2Data != null)
            {
                view2Lines = new ClassifiedLineData[bundles.Length];
            }

            // 将数据加入到view1和view2的数据缓冲区中
            for (int i = 0; i < bundles.Length; i++)
            {
                view1Lines[i] = bundles[i].View1Data;
                if (view2Lines != null)
                {
                    view2Lines[i] = bundles[i].View2Data;
                }
            }

            ScanningTime = DateTime.Now;


            View1Data = new ImageViewData(view1Lines, DetectViewIndex.View1, view1Lines[0].XRayData.Length, 0)
            {

            };

            if (view2Lines != null)
            {
                View2Data = new ImageViewData(view2Lines, DetectViewIndex.View2, view2Lines[0].XRayData.Length, 0)
                {

                };
            }
        }

        /// <summary>
        /// 压缩图像数据，以节约存储空间
        /// </summary>
        private void Compress()
        {
            if (View1Data != null)
            {
                View1Data.Compress();
            }

            if (View2Data != null)
            {
                View2Data.Compress();
            }
        }

        /// <summary>
        /// 解压缩图像数据。
        /// </summary>
        /// <remarks>当访问图像数据时，内部会自动执行解压缩</remarks>
        private void Decompress()
        {
            if (View1Data != null)
            {
                View1Data.Decompress();
            }

            if (View2Data != null)
            {
                View2Data.Decompress();
            }
        }

        /// <summary>
        /// 使用二进制序列化存储，将图像序列化至指定的数据流
        /// </summary>
        /// <param name="stream">接收序列化结果的数据流</param>
        /// <param name="compress">是否压缩存储的标志。true表示先压缩再存储，false表示不压缩，直接存储</param>
        public void Serialize(Stream stream, bool compress = true)
        {
            if (compress)
            {
                Compress();
            }

            // 使用二进制序列化存储。
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, this);
        }

        /// <summary>
        /// 将指定的XRayImage对象序列化至磁盘,保存为文件
        /// </summary>
        /// <remarks>该函数可能会因为文件操作发生异常，外部调用者需要注意捕获异常</remarks>
        /// <param name="image">要保存的图像文件</param>
        /// <param name="filePath">完整的路径名</param>
        public static void SaveToDiskFile(XRayScanlinesImage image, string filePath)
        {
            using (Stream fs = new FileStream(
                filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 1024 * 1024))
            {
                image.Serialize(fs);
            }
        }

        /// <summary>
        /// Load an X-Ray image from a disk file.
        /// </summary>
        /// <remarks>Please use try catch to call this method to catch possible exceptions.</remarks>
        /// <param name="filePath">the full path of the image</param>
        public static XRayScanlinesImage LoadFromDiskFile(string filePath)
        {
            IFormatter formatter = new BinaryFormatter();

            const int bufferSize = 1024 * 1024;

            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
            {
                var result = formatter.Deserialize(stream) as XRayScanlinesImage;
                return result;
            }
        }
    }

    /// <summary>
    /// 表示XRayImage中一个视角图像的数据信息
    /// </summary>
    [Serializable]
    public class ImageViewData
    {
        /// <summary>
        /// 此视角图像所包含的所有扫描线数据
        /// 此字段不参与序列化（序列化时，将其压缩后序列化至一个Stream中）
        /// </summary>
        [NonSerialized]
        private ClassifiedLineData[] _scanLines;

        /// <summary>
        /// 获取图像的扫描线，如果扫描线仍处于压缩流中，则先进行解压缩，并释放压缩流
        /// </summary>
        public ClassifiedLineData[] ScanLines
        {
            get
            {
                if (_scanLines == null && _compressedScanLinesStream != null)
                {
                    // 从压缩数据流中恢复线数据，并释放压缩数据流以降低内存使用率
                    // 使用静态函数解压缩
                    Decompress();
                }
                return _scanLines;
            }
        }

        /// <summary>
        /// 此视角对应的视角编号
        /// </summary>
        public DetectViewIndex ViewIndex { get; private set; }

        /// <summary>
        /// 视角图像线数据的压缩内存流：在存储至磁盘的时候，对_scanLines进行压缩得到；在从磁盘读取出后，将其解压缩成_scanLines
        /// </summary>
        private MemoryStream _compressedScanLinesStream;

        /// <summary>
        /// 此图像的缩略图：由于缩略图需要依赖于缩放比例等，因此在存储之前生成缩略图
        /// </summary>
        private Bitmap _thumbnail;

        /// <summary>
        /// 图像的标记区域，包括自动的和人手动的各种标记
        /// </summary>
        public List<MarkerRegion> TagRegions { get; set; }

        /// <summary>
        /// Lines count of view1.
        /// </summary>
        public int ScanLinesCount
        {
            get { return ScanLines == null ? 0 : ScanLines.Length; }
        }

        /// <summary>
        /// 每一扫描线的长度，可能小于实际的探测通道个数（只保存有效的一部分图像数据，剔除图像的空白区域）
        /// </summary>
        public int ScanLineLength
        {
            get { return ScanLines == null ? 0 : ScanLines[0].XRayData.Length; }
        }

        /// <summary>
        /// 实际的物理探测通道个数
        /// </summary>
        public int ChannelsCount { get; private set; }

        /// <summary>
        /// 图像扫描线的第一个像素对应的探测通道的编号。
        /// 为了节约存储空间，可以只存储一部分有效的图像数据区，不保存图像上下部的空白
        /// </summary>
        /// <remarks>有可能会将扫描线中的空白区域剔除</remarks>
        public int StartChannelNumber { get; private set; }

        /// <summary>
        /// 图像纵向的缩放比例，默认值为1
        /// </summary>
        /// <remarks>沿探测器排列方向的缩放比例</remarks>
        public float VerticalScale { get; set; }

        /// <summary>
        /// 此视角对应的射线源的束流值，单位为mA
        /// </summary>
        public float XRayGCurrent { get; set; }

        /// <summary>
        /// 此视角对应的射线源的高压，单位为kV
        /// </summary>
        public float XRayGVoltage { get; set; }

        /// <summary>
        /// thumbnail image 宽和高的最大值为160
        /// </summary>
        public Bitmap Thumbnail
        {
            get { return _thumbnail; }
            set { _thumbnail = value; }
        }

        /// <summary>
        /// 构造函数：构造图像一个视角的扫描数据
        /// </summary>
        /// <param name="scanlines">一个视角的扫描数据</param>
        /// <param name="viewIndex">产生视角数据的视角编号</param>
        /// <param name="verticalScale">图像沿探测器排列方向的缩放比例</param>
        /// <param name="channelsCount">此视角的探测通道总计数</param>
        /// <param name="scanlineOriPos">每一扫描在原探测通道中的起始位置</param>
        public ImageViewData(ClassifiedLineData[] scanlines, DetectViewIndex viewIndex, int channelsCount, int scanlineOriPos, float verticalScale = 1.0f)
        {
            _scanLines = scanlines;
            ViewIndex = viewIndex;
            TagRegions = new List<MarkerRegion>();
            VerticalScale = 1;
            ChannelsCount = channelsCount;
            StartChannelNumber = scanlineOriPos;
        }

        /// <summary>
        /// 压缩图像数据：将图像数据扫描线压缩成字节流，以节约空间
        /// </summary>
        /// <remarks>在序列化之前，需要调用此函数进行压缩，以节约空间</remarks>
        internal void Compress()
        {
            // 将图像数据（线数据）进行压缩
            if (_scanLines != null)
            {
                _compressedScanLinesStream = CompressLinesToStream(_scanLines);

                //_scanLines = null;
            }
        }

        /// <summary>
        /// 解压缩图像数据。在访问内部数据时，会自动执行解压缩
        /// </summary>
        public void Decompress()
        {
            _scanLines = DecompressStreamToLines(_compressedScanLinesStream);
            _compressedScanLinesStream.Close();
            _compressedScanLinesStream.Dispose();
            _compressedScanLinesStream = null;
        }

        /// <summary>
        /// 将压缩内存流解压缩成线数据
        /// </summary>
        /// <param name="dataStream">要被解压缩的内存流</param>
        /// <returns>解压缩后生成的线数据</returns>
        private static ClassifiedLineData[] DecompressStreamToLines(MemoryStream dataStream)
        {
            IFormatter formatter = new BinaryFormatter();
            // 使用GZip进行解压缩，因保存时使用了压缩
            using (var ms = new MemoryStream(dataStream.ToArray()))
            {
                var gzipStream = new GZipStream(ms, CompressionMode.Decompress);
                return formatter.Deserialize(gzipStream) as ClassifiedLineData[];
            }
        }

        /// <summary>
        /// 将线数据压缩成内存流
        /// </summary>
        /// <param name="lines">要被压缩的线数据</param>
        /// <returns>压缩线数据后得到的内存流</returns>
        private static MemoryStream CompressLinesToStream(ClassifiedLineData[] lines)
        {
            // 使用二进制序列化存储。
            IFormatter formatter = new BinaryFormatter();

            // 先将线数据序列化至内存中
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, lines);
                var buffer = ms.ToArray();
                var zipStream = new MemoryStream();
                // 使用GZip进行压缩，可以达到很好的数据压缩比，序列化到内存
                var gzipStream = new GZipStream(zipStream, CompressionMode.Compress);
                gzipStream.Write(buffer, 0, buffer.Length);
                // 释放资源
                gzipStream.Close();
                gzipStream.Dispose();
                return zipStream;
            }
        }
    }
}

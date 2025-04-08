using GalaSoft.MvvmLight;
using JugdeHttp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;
using UI.XRay.ImagePlant;

namespace UI.XRay.Flows.Services
{
    public class AIJudgeImageService
    {
        public static AIJudgeImageService Service { get; private set; }

        static AIJudgeImageService()
        {
            Service = new AIJudgeImageService();
        }
        protected AIJudgeImageService()
        {
            _markBoxQueue = new ConcurrentQueue<XRayImageForJudge>();
            InitConfig();
        }

        public void Init()
        {
            _dangerList = ReadDangerItems();

            if (_isEnable)
            {
                _isRunning = true;
                _judgeThread = new Thread(JudgeThreadExecute);
                _judgeThread.IsBackground = true;
                _judgeThread.Start();
            }
        }

        bool ExitRemotePath
        {
            get { return Directory.Exists(string.Format("//{0}/{1}/", _algoIp, _algoPath)); }
        }

        public void AddXRayImage(LinkedList<DisplayScanlineDataBundle> scanlines, string filePath)
        {
            if (!_isEnable) return;

            XRayImageForJudge xrayinfo = new XRayImageForJudge();
            xrayinfo.StartNum = scanlines.First.Value.LineNumber;
            xrayinfo.EndNum = scanlines.Last.Value.LineNumber;
            xrayinfo.BagID = Path.GetFileNameWithoutExtension(filePath);

            var view1Data = scanlines.Select(x => x.View1Data).ToList();
            xrayinfo.View1Image = ScanlinesToBmp(view1Data);
            if (scanlines.First.Value.View2Data != null)
            {
                var view2Data = scanlines.Select(x => x.View2Data).ToList();
                xrayinfo.View2Image = ScanlinesToBmp(view2Data);
            }
            else
            {
                xrayinfo.View2Image = null;
            }

            _markBoxQueue.Enqueue(xrayinfo);
        }

        public void Stop()
        {
            _isRunning = false;
            if (_judgeThread != null)
            {
                _judgeThread.Join();
                _judgeThread = null;
            }
        }

        private void JudgeThreadExecute()
        {
            XRayImageForJudge xrayimage;
            while (_isRunning)
            {
                if (_markBoxQueue.Count > 0)
                {
                    while (_markBoxQueue.TryDequeue(out xrayimage))
                    {
                        GetPictureJudegeResult(xrayimage);
                    }
                }
                System.Threading.Thread.Sleep(1);
            }
        }

        private void GetPictureJudegeResult(XRayImageForJudge image)
        {
            try
            {
                string viewPath = string.Format("//{0}/{1}/{2}/{3}/{4}/", _algoIp, _algoPath, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                string basePath = string.Format("//{0}/{1}", _algoIp, _algoPath);
                if (!Directory.Exists(basePath)) return;
                if (!Directory.Exists(viewPath)) Directory.CreateDirectory(viewPath);

                string view1path = Path.Combine(viewPath, image.BagID + "_H.jpg");
                ImageCompressAndSave(image.View1Image, view1path, ImageFormat.Jpeg);
                var view1AlgoResult = DoPicJudge(view1path);
                ResolvingAlgoResult(image, view1AlgoResult, DetectViewIndex.View1);


                if (image.View2Image != null)
                {
                    string view2path = Path.Combine(viewPath, image.BagID + "_V.jpg");
                    ImageCompressAndSave(image.View2Image, view2path, ImageFormat.Jpeg);
                    var viewAlgoResult = DoPicJudge(view2path);
                    ResolvingAlgoResult(image, viewAlgoResult, DetectViewIndex.View2);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }
        private string DoPicJudge(string picpath)
        {
            try
            {
                RestClient client = new RestClient("http://" + _algoIp + ":5000/"); // http本地地址，端口
                string AlgResult = string.Empty;
                new System.Threading.Tasks.TaskFactory().StartNew(() =>
                {
                    string uri = "/detect?path=" + picpath;
                    AlgResult = client.Get(uri);
                }).Wait(5000);
                Tracer.TraceInfo(string.Format("Send image to {0}", picpath));
                return AlgResult;
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            return null;
        }

        private void ImageCompressAndSave(Bitmap bitmap, string dstPath, ImageFormat format, int flag = 75)
        {
            //ImageFormat tFormat = bitmap.RawFormat;
            //EncoderParameters eps = new EncoderParameters();
            //long[] quality = new long[1];
            //quality[0] = flag;
            //EncoderParameter ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            //eps.Param[0] = ep;
            try
            {
                //ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();
                //ImageCodecInfo jpegICIInfo = null;
                //for (int x = 0; x < arrayICI.Length; x++)
                //{
                //    if (arrayICI[x].FormatDescription.Equals("JPEG"))
                //    {
                //        jpegICIInfo = arrayICI[x];
                //        break;
                //    }
                //}
                //if (jpegICIInfo != null)
                //{
                //    bitmap.Save(dstPath, jpegICIInfo, eps);
                //    //bitmap.Save(dstPath);
                //}
                //else
                //{

                bitmap.Save(dstPath, format);

                //}
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public Bitmap ScanlinesToBmp(List<DisplayScanlineData> scanlines)
        {
            if (scanlines == null || scanlines.Count < 1)
            {
                return null;
            }
            int linesCount = scanlines.Count();
            var data = new ushort[linesCount][];
            var material = new ushort[linesCount][];

            for (int i = 0; i < linesCount; i++)
            {
                data[i] = scanlines[i].XRayData;
                material[i] = scanlines[i].Material;
            }

            var _view1Processor = new UI.XRay.ImagePlant.Cpu.ImageProcessor(DisplayColorMode.MaterialColor);

            _view1Processor.AttachImageData(data, material);


            var bmp = _view1Processor.GetBitmap();
            //bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bmp;
        }

        private void ResolvingAlgoResult(XRayImageForJudge image, string algoResult, DetectViewIndex view)
        {
            string[] objectArray = algoResult.Split(';');
            List<MarkBox> boxList = new List<MarkBox>();
            foreach (var obj in objectArray)
            {
                if (string.IsNullOrWhiteSpace(obj))
                    continue;
                string[] boxprop = obj.Split(',');
                if (boxprop.Length != 6) continue;

                MarkBox box = new MarkBox();
                box.View = view;
                box.Type = int.Parse(boxprop[0]);
                box.X = int.Parse(boxprop[1]) + image.StartNum;
                box.Y = int.Parse(boxprop[2]);
                box.Width = int.Parse(boxprop[3]) - int.Parse(boxprop[1]);
                box.Height = int.Parse(boxprop[4]) - int.Parse(boxprop[2]);
                box.Confidence = double.Parse(boxprop[5]);

                var sele = from rect in _dangerList
                           where rect.Type == box.Type
                           select rect;
                if (sele.Count() < 1) continue;
                if (sele.First<DangerEntity>().Confidence > box.Confidence) continue;

                box.RectColor = Color.FromArgb(Convert.ToInt32(sele.First<DangerEntity>().RectColor, 16));
                box.Name = TranslationService.FindTranslation("Danger", sele.First<DangerEntity>().Name);

                boxList.Add(box);
            }

            Tracer.TraceInfo(string.Format("view:{0} Recieve box count:{1}", view, boxList.Count));

            if (BoxReady != null)
            {
                boxList.Sort();
                BoxReady(boxList);
            }
        }

        public List<DangerEntity> ReadDangerItems()
        {
            List<DangerEntity> dangerList = new List<DangerEntity>();
            try
            {
                if (!File.Exists(_aISettingPath)) return dangerList;
                using (FileStream fsRead = new FileStream(_aISettingPath, FileMode.Open))
                {
                    int fsLen = (int)fsRead.Length;
                    byte[] buffer = new byte[fsLen];
                    int result = fsRead.Read(buffer, 0, buffer.Length);
                    string xml = System.Text.Encoding.UTF8.GetString(buffer);
                    //反序列化
                    using (StringReader sr = new StringReader(xml))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<DangerEntity>));
                        dangerList = (List<DangerEntity>)serializer.Deserialize(sr);
                    }
                }
            }
            catch (Exception e)
            {
                return dangerList;
            }
            return dangerList;
        }

        public void WriteDangerItems(List<DangerEntity> dangerList)
        {
            if (File.Exists(_aISettingPath)) File.Delete(_aISettingPath);
            using (StringWriter sw = new StringWriter())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<DangerEntity>));
                serializer.Serialize(sw, dangerList);
                using (FileStream fs = new FileStream(_aISettingPath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    byte[] buffer = Encoding.Default.GetBytes(sw.ToString());
                    fs.Write(buffer, 0, buffer.Length);
                }
            }
        }

        private void InitConfig()
        {
            if (!ScannerConfig.Read(ConfigPath.IntellisenseSettingFilePath, out _aISettingPath))
            {
                _aISettingPath = @"D:\\SecurityScanner\\Config\\Dangers.xml";
            }
            if (!ScannerConfig.Read(ConfigPath.AutoDetectionSendImageToAlgoEnable, out _isEnable))
            {
                _isEnable = false;
            }
            if (!ScannerConfig.Read(ConfigPath.AutoDetectoinSendImageAlgoIp, out _algoIp))
            {
                _algoIp = "";
            }
            if (!ScannerConfig.Read(ConfigPath.AutoDetectoinSendImageAlgoPath, out _algoPath))
            {
                _algoPath = "PublicFile";
            }
        }

        private string _algoIp;
        private string _algoPath;

        /// <summary>
        /// 发出去框信息
        /// </summary>
        public event Action<List<MarkBox>> BoxReady;

        ConcurrentQueue<XRayImageForJudge> _markBoxQueue;

        private Thread _judgeThread;
        private bool _isRunning;
        private List<DangerEntity> _dangerList;

        private bool _isEnable;

        private string _aISettingPath;
    }

    public class XRayImageForJudge
    {
        public string BagID { get; set; }
        public int StartNum { get; set; }
        public int EndNum { get; set; }
        public Bitmap View1Image { get; set; }
        public Bitmap View2Image { get; set; }
    }
    public class DangerEntity : ObservableObject
    {
        public DangerEntity(int type, string name, string rectcolor, double confidence)
        {
            Type = type;
            Name = name;
            RectColor = rectcolor;
            Confidence = confidence;
        }
        public DangerEntity()
        {

        }
        public int Type { get; set; }
        public string Name { get; set; }
        public string RectColor { get; set; }

        public double Confidence { get; set; }
    }
    public class MarkBox : IComparable<MarkBox>
    {
        public DetectViewIndex View { get; set; }
        public int Type { set; get; }

        public string Name { get; set; }

        public Color RectColor { get; set; }

        public double Confidence { get; set; }
        public int Height { set; get; }

        public int Width { set; get; }
        public int X { set; get; }
        public int Y { set; get; }

        public int CompareTo(MarkBox other)
        {
            if (other == null) return 1;
            return X - other.X;
        }
    }
}

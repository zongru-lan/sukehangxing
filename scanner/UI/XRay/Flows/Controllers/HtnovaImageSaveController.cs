using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Control;
using UI.XRay.Flows.Services;

namespace UI.XRay.Flows.Controllers
{
    class HtnovaImageSaveController
    {
        private readonly ConcurrentQueue<KeyValuePair<XrayCadRegions, XRayScanlinesImage>> _queue =
            new ConcurrentQueue<KeyValuePair<XrayCadRegions, XRayScanlinesImage>>();

        private Task _saveImageTask;

        private bool _exit = false;

        private readonly XRayImageProcessor _xRayImageProcessor;

        private XRayImageProcessor _xRayImageProcessor1;

        /// <summary>
        /// todo：后续从配置中读取
        /// </summary>
        private readonly string _fileDirecotry = @"D:\Htnova";

        private const string XrayResultFileName = "XRayResult.xml";

        private string _systemStateDirectory = "SystemState";
        private string _scannerStateFileName = "ScannerState.xml";
        private string _conveyorStateFileName = "ConveyorState.xml";

        private readonly string _xrayResultXmlPath;

        /// <summary>
        /// 保存探测区域种类和消息的关系的字典
        /// </summary>
        private readonly Dictionary<MarkerRegionType, string> _alarmMessages;

        private readonly List<MarkerRegionType> _view1RegionTypes = new List<MarkerRegionType>();
        private readonly List<MarkerRegionType> _view2RegionTypes = new List<MarkerRegionType>();

        public HtnovaImageSaveController()
        {
            try
            {
                ScannerConfig.Read(ConfigPath.HtonvaJpgImageStorePath, out _fileDirecotry);
                if (string.IsNullOrWhiteSpace(_fileDirecotry))
                {
                    _fileDirecotry = @"D:\Htnova";
                }

                if (!Directory.Exists(_fileDirecotry))
                {
                    Directory.CreateDirectory(_fileDirecotry);
                }

                _systemStateDirectory = Path.Combine(_fileDirecotry, _systemStateDirectory);
                if (string.IsNullOrWhiteSpace(_systemStateDirectory))
                {
                    _systemStateDirectory = _fileDirecotry + @"\" + "SystemState";
                }
                if (!Directory.Exists(_systemStateDirectory))
                {
                    Directory.CreateDirectory(_systemStateDirectory);
                }

                _scannerStateFileName = Path.Combine(_systemStateDirectory, _scannerStateFileName);
                _conveyorStateFileName = Path.Combine(_systemStateDirectory, _conveyorStateFileName);

                //订阅控制系统传送带方向变化的事件
                ControlService.ServicePart.ConveyorDirectionChanged += ServicePart_ConveyorDirectionChanged;
                ControlService.ServicePart.SystemShutDown += ServicePart_SystemShutDown;

                //当系统状态监控停止的时候正是关机的时刻

                _xRayImageProcessor = new XRayImageProcessor();

                _alarmMessages = new Dictionary<MarkerRegionType, string>
                {
                    {MarkerRegionType.Drug, "毒品"},
                    {MarkerRegionType.Explosives, "爆炸物"},
                    {MarkerRegionType.Gun, "枪"},
                    {MarkerRegionType.Knife, "刀"},
                    {MarkerRegionType.Tip, "Tip"},
                    {MarkerRegionType.UnPenetratable, "穿不透区域"}
                };

                _xrayResultXmlPath = Path.Combine(_fileDirecotry, XrayResultFileName);

                _saveImageTask = Task.Factory.StartNew(SaveImageThread);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        void ServicePart_SystemShutDown(object sender, bool e)
        {
            try
            {
                if (!e)
                {
                    ScannerStateElement element = new ScannerStateElement {ScannerState = 0};
                    XmlHelper.Save(element, _scannerStateFileName);

                    //延时5s
                    Thread.Sleep(5000);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        void ServicePart_ConveyorDirectionChanged(object sender, Control.ConveyorDirectionChangedEventArgs e)
        {
            try
            {
                ConveyorDirection currentDir = e.New;
                ConveyorStateElement element = new ConveyorStateElement { ConveyorState = (int)currentDir };
                XmlHelper.Save(element, _conveyorStateFileName);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }


        private void SaveImageThread()
        {
            while (!_exit)
            {
                KeyValuePair<XrayCadRegions, XRayScanlinesImage> xrayImage;
                while (_queue.TryDequeue(out xrayImage))
                {
                    try
                    {
                        if (xrayImage.Value != null && !string.IsNullOrWhiteSpace(xrayImage.Key.FileName))
                        {
                            ClearAllJpg(_fileDirecotry);

                            SaveJpg(xrayImage.Value, xrayImage.Key.FileName);

                            UpdataImageResult(xrayImage.Key);
                        }
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e);
                    }
                }
                Thread.Sleep(15);
            }
        }

        private void ClearAllJpg(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(directory);
                    FileInfo[] files = dirInfo.GetFiles("*.jpg");
                    foreach (var file in files)
                    {
                        File.Delete(file.FullName);
                    }
                }
                else
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        /// <summary>
        /// 更新指定目录下xml文件
        /// </summary>
        private void UpdataImageResult(XrayCadRegions regions)
        {
            try
            {
                _view1RegionTypes.Clear();
                _view2RegionTypes.Clear();

                XRayResultElement result = new XRayResultElement();

                if (!regions.View1MarkerRegionsDetected && !regions.View2MarkerRegionsDetected)
                {
                    result.AlarmMessage = string.Empty;
                    result.IsAlarm = false;
                }
                else
                {
                    //目前XRayScanlinesImage中没有危险品信息
                    string view1AlarmMsg = GenerateAlarmMsg(_view1RegionTypes, regions, DetectViewIndex.View1);
                    string view2AlarmMsg = GenerateAlarmMsg(_view2RegionTypes, regions, DetectViewIndex.View2);
                    result.AlarmMessage = view1AlarmMsg + view2AlarmMsg;
                    result.IsAlarm = true;
                }

                XmlHelper.Save(result, _xrayResultXmlPath);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private string GenerateAlarmMsg(List<MarkerRegionType> regionTypes, XrayCadRegions regions, DetectViewIndex viewIndex)
        {
            bool regionDeteced = viewIndex == DetectViewIndex.View1
                ? regions.View1MarkerRegionsDetected
                : regions.View2MarkerRegionsDetected;
            var rgs = viewIndex == DetectViewIndex.View1 ? regions.View1MarkerRegions : regions.View2MarkerRegions;

            //目前XRayScanlinesImage中没有危险品信息
            if (regionDeteced)
            {
                foreach (var region in rgs)
                {
                    if (!regionTypes.Exists((i) => i == region.RegionType))
                    {
                        regionTypes.Add(region.RegionType);
                    }
                }
                StringBuilder builder = new StringBuilder(200);

                builder.Append(viewIndex == DetectViewIndex.View1 ? "视角1：" : "视角2：");
                foreach (var regionType in regionTypes)
                {
                    builder.Append(_alarmMessages[regionType]);
                    builder.Append(";");
                }
                return builder.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// 保存jpg到指定目录
        /// </summary>
        /// <param name="xrayImage"></param>
        /// <param name="path"></param>
        private void SaveJpg(XRayScanlinesImage xrayImage, string path)
        {
            try
            {
                _xRayImageProcessor.AttachImageData(xrayImage.View1Data);
                var bmp = _xRayImageProcessor.GetBitmap();

                if (xrayImage.ViewsCount == 2 || xrayImage.View2Data != null)
                {
                    if (_xRayImageProcessor1 == null)
                    {
                        _xRayImageProcessor1 = new XRayImageProcessor();
                    }
                    _xRayImageProcessor.AttachImageData(xrayImage.View2Data);
                    var view2Bmp = _xRayImageProcessor.GetBitmap();
                    //合并图像
                    bmp = BitmapHelper.CombineBmp(bmp, view2Bmp);
                }

                string fileNameWithoutExtension;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                }
                else
                {
                    fileNameWithoutExtension = xrayImage.AccountId + xrayImage.MachineNumber +
                                               xrayImage.ScanningTime.ToString("_yyMMddHHmmss_");
                }

                bmp.Save(Path.Combine(_fileDirecotry, fileNameWithoutExtension + ".jpg"),
                    ImageFormat.Jpeg);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        /// <summary>
        /// 存入队列
        /// </summary>
        /// <param name="image"></param>
        public void Enqueue(KeyValuePair<XrayCadRegions,XRayScanlinesImage> image)
        {
            _queue.Enqueue(image);
        }



        public void Exit()
        {
            _exit = true;

            _saveImageTask.Wait();
            _saveImageTask.Dispose();

            ControlService.ServicePart.ConveyorDirectionChanged -= ServicePart_ConveyorDirectionChanged;
        }
    }

    #region xml

    public static class XmlHelper
    {
        public static bool Save(XRayResultElement element, string filePath)
        {
            try
            {
                WriteXml(typeof(XRayResultElement), filePath, element);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                return false;
            }
            return true;
        }

        public static bool Save(ScannerStateElement scannerState, string filePath)
        {
            try
            {
                WriteXml(typeof(ScannerStateElement), filePath, scannerState);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                return false;
            }
            return true;
        }

        public static bool Save(ConveyorStateElement conveyorState, string filePath)
        {
            try
            {
                WriteXml(typeof(ConveyorStateElement), filePath, conveyorState);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                return false;
            }
            return true;
        }

        private static void WriteXml(Type type, string filePath, object content)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var xmlSerializer = new XmlSerializer(type);

                var settings = new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = true,
                    IndentChars = " ",
                    NewLineChars = "\r\n"
                };
                settings.Encoding = Encoding.UTF8;
                //settings.OmitXmlDeclaration = true; 

                // 强制指定命名空间，覆盖默认的命名空间 
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);

                using (XmlWriter xmlWriter = XmlWriter.Create(fileStream, settings))
                {
                    xmlSerializer.Serialize(xmlWriter, content, namespaces);
                }
            }
        }
    }

    [XmlRoot("XRayResult")]
    public class XRayResultElement
    {
        [XmlElement]
        public bool IsAlarm { get; set; }
        [XmlElement]
        public String AlarmMessage { get; set; }
    }

    [XmlRoot("ScannerState")]
    public class ScannerStateElement
    {
        [XmlElement]
        public int ScannerState { get; set; }
    }

    [XmlRoot("ConveyorState")]
    public class ConveyorStateElement
    {
        [XmlElement]
        public int ConveyorState { get; set; }
    }

#endregion
}

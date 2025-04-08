using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 控制
    /// </summary>
    public class ExchangeDirectionConfig
    {
        public static ExchangeDirectionConfig Service { get; private set; }

        static ExchangeDirectionConfig()
        {
            Service = new ExchangeDirectionConfig();
        }
        public ExchangeDirectionConfig()
        {
            ReadConfig();
            InitImageSuffix();
        }
        public void Init()
        {

        }
        public void ReadConfig()
        {
            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewCount))
            {
                _viewCount = 1;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangePESensor, out _isExchangePESensor))
            {
                _isExchangePESensor = false;
            }
            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangeXrayGen, out _isExchangeXrayGen))
            {
                _isExchangeXrayGen = false;
            }
            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangeDetector, out _isExchangeDetector))
            {
                _isExchangeDetector = false;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangeConveyor, out _isExchangeConveyor))
            {
                _isExchangeConveyor = false;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1Height, out _view1Height))
            {
                _view1Height = 64;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage2Height, out _view2Height))
            {
                _view2Height = 64;
            }

            if (!ScannerConfig.Read(ConfigPath.CaptureSysBoardCount, out _captureCardCount))
            {
                _captureCardCount = 1;
            }

            _view1ChannelCount = GetView1Channels();
            _view2ChannelCount = GetView2Channels();
        }

        public int GetView1ImageHeight()
        {
            //if (_isExchangeDetector && _viewCount > 1)
            //{
            //    return _view2Height;
            //}
            return _view1Height;
        }

        public int GetView2ImageHeight()
        {
            //if (_isExchangeDetector)
            //{
            //    return _view1Height;
            //}
            return _view2Height;
        }

        public bool GetView1ChannelsCount(out int count)
        {
            //if (_isExchangeDetector && _viewCount > 1)
            //{
            //    count = _view2ChannelCount;
            //    return true;
            //}
            count = _view1ChannelCount;
            return true;
        }

        public bool GetView2ChannelsCount(out int count)
        {
            //if (_isExchangeDetector)
            //{
            //    count = _view1ChannelCount;
            //    return true;
            //}
            count = _view2ChannelCount;
            return true;
        }

        public string GetView1ShapeFilePath()
        {
            //if (_isExchangeDetector)
            //{
            //    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zheng.csv");
            //}
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ce.csv");
        }
        public string GetView2ShapeFilePath()
        {
            //if (_isExchangeDetector)
            //{
            //    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ce.csv");
            //}
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "zheng.csv");
        }

        public bool GetView1CardsDist(out int[] dist)
        {
            if (_captureCardCount > 1)
            {
                if (_isExchangeDetector && _viewCount > 1)
                {
                    var view2Cards = LoadCardsDist(ConfigPath.CaptureSysDTView2CardsDist);
                    if (view2Cards != null)
                    {
                        dist = view2Cards;
                        return true;
                    }
                }
                else
                {
                    var view1Cards = LoadCardsDist(ConfigPath.CaptureSysDTView1CardsDist);
                    if (view1Cards != null)
                    {
                        dist = view1Cards;
                        return true;
                    }
                }                
            }
            else
            {
                if (_isExchangeDetector && _viewCount > 1)
                {
                    var view1Cards = LoadCardsDist(ConfigPath.CaptureSysDTView1CardsDist);
                    if (view1Cards != null)
                    {
                        var second = view1Cards[1];
                        view1Cards[1] = view1Cards[0];
                        view1Cards[0] = second;
                        dist = view1Cards;
                        return true;
                    }
                }
                else
                {
                    var view1Cards = LoadCardsDist(ConfigPath.CaptureSysDTView1CardsDist);
                    if (view1Cards != null)
                    {
                        dist = view1Cards;
                        return true;
                    }
                }
            }
            

            dist = null;
            return false;
        }

        /// <summary>
        /// 获取视角2的探测卡分布情况
        /// </summary>
        /// <param name="dist"></param>
        /// <returns></returns>
        public bool GetView2CardsDist(out int[] dist)
        {
            if (_captureCardCount > 1)
            {
                if (_isExchangeDetector)
                {
                    var view1Cards = LoadCardsDist(ConfigPath.CaptureSysDTView1CardsDist);
                    if (view1Cards != null)
                    {
                        dist = view1Cards;
                        return true;
                    }
                }
                else
                {
                    var view2Cards = LoadCardsDist(ConfigPath.CaptureSysDTView2CardsDist);
                    if (view2Cards != null)
                    {
                        dist = view2Cards;
                        return true;
                    }
                }                
            }
            else
            {
                if (_isExchangeDetector && _viewCount > 1)
                {
                    var view2Cards = LoadCardsDist(ConfigPath.CaptureSysDTView2CardsDist);
                    if (view2Cards != null)
                    {
                        var second = view2Cards[1];
                        view2Cards[1] = view2Cards[0];
                        view2Cards[0] = second;
                        dist = view2Cards;
                        return true;
                    }
                }
                else
                {
                    var view2Cards = LoadCardsDist(ConfigPath.CaptureSysDTView2CardsDist);
                    if (view2Cards != null)
                    {
                        dist = view2Cards;
                        return true;
                    }
                }
            }            

            dist = null;
            return false;
        }

        private bool _isExchangePESensor = false;
        public bool IsExchangePeSensor
        {
            get { return _viewCount > 1 &&_isExchangePESensor; }
        }
        private bool _isExchangeXrayGen = false;
        public bool IsExchangeXrayGen
        {
            get { return  _viewCount > 1 && _isExchangeXrayGen; }
        }
        private bool _isExchangeDetector = false;
        public bool IsExchangeDetector
        {
            get { return _isExchangeDetector; }
        }

        private bool _isExchangeConveyor;

        public bool IsExchangeConveyor
        {
            get { return _isExchangeConveyor; }
        }


        private int _viewCount;

        private int _captureCardCount;

        /// <summary>
        /// 图像1高度
        /// </summary>
        private int _view1Height;

        /// <summary>
        /// 图像2高度
        /// </summary>
        private int _view2Height;

        private int _view1ChannelCount;

        private int _view2ChannelCount;

        private int GetView1Channels()
        {
            int capturecount = 1;

            if (!ScannerConfig.Read(ConfigPath.CaptureSysBoardCount, out capturecount))
            {
                capturecount = 1;
            }

            // 加载视角1的配置
            var view1Cards = LoadCardsDist(ConfigPath.CaptureSysDTView1CardsDist);

            if (capturecount == 1 && _viewCount == 2)
            {
                return view1Cards[0] * 64;
            }
            else
            {
                if (view1Cards != null)
                {
                    return (view1Cards[0] + view1Cards[1] + view1Cards[2] + view1Cards[3]) * 64;
                }
            }

            return 64;
        }

        /// <summary>
        /// 获取视角2的探测通道数
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private int GetView2Channels()
        {
            int capturecount = 1;

            if (!ScannerConfig.Read(ConfigPath.CaptureSysBoardCount, out capturecount))
            {
                capturecount = 1;
            }

            // 加载视角1的配置
            var view1Cards = LoadCardsDist(ConfigPath.CaptureSysDTView1CardsDist);
            var view2Cards = LoadCardsDist(ConfigPath.CaptureSysDTView2CardsDist);
            if (capturecount == 1 && _viewCount == 2)
            {
                return view1Cards[1] * 64;
            }
            else
            {
                if (view2Cards != null)
                {
                    return (view2Cards[0] + view2Cards[1] + view2Cards[2] + view2Cards[3]) * 64;
                }
            }
            return 64;
        }


        /// <summary>
        /// 加载某个视角的探测板分布情况
        /// </summary>
        /// <param name="configPath">视角的探测板配置路径</param>
        /// <returns>成功则返回其探测板分布数组，数组长度为4；失败则返回null</returns>
        private static int[] LoadCardsDist(string configPath)
        {
            var cards = "0,0,0,0";
            if (ScannerConfig.Read(configPath, out cards))
            {
                var cardsStr = cards.Split(new[] { ',' });
                if (cardsStr.Length == 4)
                {
                    var result = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        int.TryParse(cardsStr[i], out result[i]);
                    }
                    return result;
                }
            }

            return null;
        }


        private void InitImageSuffix()
        {
            if (!ScannerConfig.Read(ConfigPath.SingleViewIsSendSingleViewImage, out _isSendSingleViewImage))
            {
                _isSendSingleViewImage = false;
            }

            string para = "";
            if (!ScannerConfig.Read(ConfigPath.SingleViewImageFileSuffix, out para))
            {
                para = "_H;_V;_All";
                ScannerConfig.Write(ConfigPath.SingleViewImageFileSuffix, para);
            }
            
            ImageSuffixes = new string[] {"_H","_V","All" };

            List<string> temp = new List<string>();
            var frags = para.Split(';');
            for (int i = 0; i < frags.Length; i++)
            {
                temp.Add(frags[i]);
            }

            for (int i = 0; i < Math.Min(temp.Count,ImageSuffixes.Length); i++)
            {
                ImageSuffixes[i] = temp[i];
            }
        }

        private string[] ImageSuffixes ;

        private bool _isSendSingleViewImage;

        public bool IsSendSingleViewImage
        {
            get { return _isSendSingleViewImage; }
            set { _isSendSingleViewImage = value; }
        }

        public string GetSingleViewImageSuffix(DetectViewIndex view)
        {
            switch (view)
            {
                case DetectViewIndex.All:
                    return ImageSuffixes[2];
                case DetectViewIndex.View1:
                    return ImageSuffixes[0];
                case DetectViewIndex.View2:
                    return ImageSuffixes[1];
                default:
                    break;
            }
            return ImageSuffixes[0];
        }
    }

}

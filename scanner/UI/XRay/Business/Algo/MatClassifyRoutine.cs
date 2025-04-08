
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
// 
// Description: MatClassifyRoutine 类定义物质分类的处理逻辑    
// 输入数据：RawScanlineData
// 输出数据：ProcessedXRayLineData
// 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.ImagePlant;
using UI.XRay.ImagePlant.Classify;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 物质分类计算处理逻辑：针对归一化后的数据，计算物质分类和数据融合
    /// </summary>
    public class MatClassifyRoutine
    {
        /// <summary>
        /// 视角1显示的能量数据类型
        /// </summary>
        private const ShowingEnergyType View1ShowingEnergy = ShowingEnergyType.Fused;

        /// <summary>
        /// 视角2显示的能量数据类型
        /// </summary>
        private const ShowingEnergyType View2ShowingEnergy = ShowingEnergyType.Fused;

        /// <summary>
        /// 物质分类器
        /// </summary>
        private readonly MaterialClassifier _view1Classifier;

        private readonly MaterialClassifier _view2Classifier;

        private readonly MaterialClassifier _view3Classifier;

        private readonly MaterialClassifier _view4Classifier;

        /// <summary>
        /// 高低能融合算法
        /// </summary>
        private HighLowEnergyFusion _fuser = new HighLowEnergyFusion();


        /// <summary>
        /// 进行物质分类时的滤波窗口的大小
        /// </summary>
        private const int FilterWindowSize = 5;

        private const int HalfWindowSize = 2;

        private const string Mat1Str = "Mat.class";
        private const string Mat2Str = "Mat2.class";
        private const string Mat3Str = "Mat3.class";
        private const string Mat4Str = "Mat4.class";

        private int DividedView = 0;

        private bool ExchangeDivideDirection = true;

        private int DividedIndex = 640;

        private int DividedIndex1 = 840;

        private int view1HighThr = 0;

        private int view1LowThr = 0;

        private int view2HighThr = 2156;

        private int view2LowThr = 2134;
        /// <summary>
        /// 构造物质分类逻辑
        /// </summary>
        public MatClassifyRoutine()
        {
            try
            {
                ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
                if (!ScannerConfig.Read(ConfigPath.MatDividedView, out DividedView))
                {
                    DividedView = 0;
                }
                if (!ScannerConfig.Read(ConfigPath.MatExchangeDivideDirection, out ExchangeDivideDirection))
                {
                    ExchangeDivideDirection = true;
                }
                if (!ScannerConfig.Read(ConfigPath.MatDividedIndex, out DividedIndex))
                {
                    DividedIndex = 640;
                }
                if (!ScannerConfig.Read(ConfigPath.PreProcHistogramNum1, out DividedIndex1))
                {
                    DividedIndex1 = 840;
                }
                if (!ScannerConfig.Read(ConfigPath.PreProcHistogramView1High, out view1HighThr))
                {
                    view1HighThr = 0;
                    ScannerConfig.Write(ConfigPath.PreProcHistogramView1High, view1HighThr);
                }
                if (!ScannerConfig.Read(ConfigPath.PreProcHistogramView1Low, out view1LowThr))
                {
                    view1LowThr = 0;
                    ScannerConfig.Write(ConfigPath.PreProcHistogramView1Low, view1LowThr);
                }
                if (!ScannerConfig.Read(ConfigPath.PreProcHistogramView2High, out view2HighThr))
                {
                    view2HighThr = 2156;
                    ScannerConfig.Write(ConfigPath.PreProcHistogramView2High, view2HighThr);
                }
                if (!ScannerConfig.Read(ConfigPath.PreProcHistogramView2Low, out view2LowThr))
                {
                    view2LowThr = 2134;
                    ScannerConfig.Write(ConfigPath.PreProcHistogramView2Low, view2LowThr);
                }

                _view1Classifier = new MaterialClassifier(view1HighThr, view1LowThr);//侧视角没问题，暂时不启用  特殊情况，主视角是第一视角时需更改注册表，相当于view1变成主视角
                InitialViewClassifier(_view1Classifier, DetectViewIndex.View1);

                _view2Classifier = new MaterialClassifier(view2HighThr, view2LowThr);
                InitialViewClassifier(_view2Classifier, DetectViewIndex.View2);

                _view3Classifier = new MaterialClassifier();//第三个物质分类器对应的是顶视角侧盒，不需要看穿透力
                InitialViewClassifier(_view3Classifier, DetectViewIndex.View2, DividedView);

                _view4Classifier = new MaterialClassifier();//第三个物质分类器对应的是顶视角侧盒，不需要看穿透力
                InitialViewClassifier4(_view4Classifier);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Unexpected exception caught in MatClassifyLogic constructor.");
            }
        }
        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramView1High, out view1HighThr))
            {
                view1HighThr = 0;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramView1Low, out view1LowThr))
            {
                view1LowThr = 0;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramView2High, out view2HighThr))
            {
                view2HighThr = 2156;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramView2Low, out view2LowThr))
            {
                view2LowThr = 2134;
            }

            _view1Classifier.heThr = view1HighThr;
            _view1Classifier.leThr = view1LowThr;

            _view2Classifier.heThr = view2HighThr;
            _view2Classifier.leThr = view2LowThr;
        }

        #region 实现接口 IMatClassifyLogic

        private void InitialViewClassifier(MaterialClassifier classifier, DetectViewIndex viewIndex, int DividedView = 0)
        {
            int viewClassifyIndex;

            string configPath = viewIndex == DetectViewIndex.View1
                ? ConfigPath.ImagesImage1Classification
                : ConfigPath.ImagesImage2Classification;

            if (!ScannerConfig.Read(configPath, out viewClassifyIndex))
            {
                viewClassifyIndex = viewIndex == DetectViewIndex.View1 ? 1 : 2;
            }
            Tracer.TraceInfo($"[MatClassifyRoutine] viewClassifyIndex: {viewClassifyIndex}");

            // 从配置文件中读取穿不透阈值及背景阈值
            classifier.UnPenetratableUpper = 3000;
            classifier.BackgroundLower = 64000;

            // 从配置中读取物质分类文件的名称
            string matFileName;

            switch (viewClassifyIndex)
            {
                case 1:
                    matFileName = Mat1Str;
                    break;
                case 2:
                    matFileName = Mat2Str;
                    break;
                case 3:
                    matFileName = Mat3Str;
                    break;
                default:
                    matFileName = Mat1Str;
                    break;
            }

            if (DividedView == 2 || DividedView == 1)
            {
                matFileName = Mat3Str;
            }

            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, matFileName);
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024000))
            {
                LoadMatClassLut(classifier, stream);
            }
        }

        private void InitialViewClassifier4(MaterialClassifier classifier)
        {
            if(DividedView != 3 && DividedView != 4) 
                return;
            // 从配置文件中读取穿不透阈值及背景阈值
            classifier.UnPenetratableUpper = 3000;
            classifier.BackgroundLower = 64000;

            // 从配置中读取物质分类文件的名称
            string matFileName = Mat4Str;
          
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, matFileName);
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024000))
            {
                LoadMatClassLut(classifier, stream);
            }
        }

        /// <summary>
        /// 从指定的流中读取物质分类查找表
        /// </summary>
        /// <param name="classifier"></param>
        /// <param name="matClassStream">包含物质分类查找表的流</param>
        public void LoadMatClassLut(MaterialClassifier classifier, Stream matClassStream)
        {
            classifier.LoadMatClassLutFromStream(matClassStream);
        }

        /// <summary>
        /// 分类及融合结束后，向外传递结果
        /// </summary>
        public event EventHandler<ClassifiedLineDataBundle> ScanlineClassified;

        /// <summary>
        /// 对一线数据进行物质分类，并进行融合
        /// </summary>
        /// <param name="bundle">要进行物质分类与融合的一线数据</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ClassifyBundle(ScanlineDataBundle bundle)
        {

            // 分别对视角1和视角2的数据同步进行分类和融合
            ClassifiedLineData view1Result = null, view2Result = null;

            if (DividedView == 1)
            {
                view1Result = ClassifyAndFuseDivided(_view1Classifier, _view3Classifier, bundle.View1LineData, DividedIndex, View1ShowingEnergy);
            }
            else if(DividedView == 3)
            {
                view1Result = ClassifyAndFuse16580(_view3Classifier,_view1Classifier, _view4Classifier, bundle.View1LineData, DividedIndex, DividedIndex1,View1ShowingEnergy);
            }
            else
            {
                view1Result = ClassifyAndFuse(_view1Classifier, bundle.View1LineData, View1ShowingEnergy);
            }

            if (bundle.View2LineData != null)
            {
                if (DividedView == 2)
                {
                    view2Result = ClassifyAndFuseDivided(_view2Classifier, _view3Classifier, bundle.View2LineData, DividedIndex, View2ShowingEnergy);
                }
                else if(DividedView == 4)
                {
                    view2Result = ClassifyAndFuse16580(_view3Classifier, _view2Classifier, _view4Classifier, bundle.View2LineData, DividedIndex, DividedIndex1, View2ShowingEnergy);
                }
                else
                {
                    view2Result = ClassifyAndFuse(_view2Classifier, bundle.View2LineData, View2ShowingEnergy);
                }

            }

            // 只要有一视角的结果数据，都向外传递
            if (view1Result != null || view2Result != null)
            {
                view1Result.SetOriginalFusedData(bundle.View1LineData.OriginalFused);
                if (bundle.View2LineData != null)
                {
                    view2Result.SetOriginalFusedData(bundle.View2LineData.OriginalFused);
                }
                var handler = ScanlineClassified;
                if (handler != null)
                {
                    var temp = new ClassifiedLineDataBundle(view1Result, view2Result);//yxc
                                                                                      //  temp.tag = bundle.tag; //yxc
                    handler(this, temp);
                }
            }

        }
        public ClassifiedLineDataBundle GetClassifyBundle(ScanlineDataBundle bundle)
        {
            // 分别对视角1和视角2的数据同步进行分类和融合
            ClassifiedLineData view1Result = null, view2Result = null;

            if (DividedView == 1)
            {
                view1Result = ClassifyAndFuseDivided(_view1Classifier, _view3Classifier, bundle.View1LineData, DividedIndex, View1ShowingEnergy);
            }
            else
            {
                view1Result = ClassifyAndFuse(_view1Classifier, bundle.View1LineData, View1ShowingEnergy);
            }

            if (bundle.View2LineData != null)
            {
                if (DividedView == 2)
                {
                    view2Result = ClassifyAndFuseDivided(_view2Classifier, _view3Classifier, bundle.View2LineData, DividedIndex, View2ShowingEnergy);
                }
                else
                {
                    view2Result = ClassifyAndFuse(_view2Classifier, bundle.View2LineData, View2ShowingEnergy);
                }
            }

            // 只要有一视角的结果数据，都向外传递
            if (view1Result != null || view2Result != null)
            {
                return  new ClassifiedLineDataBundle(view1Result, view2Result);

            }
            return null;
        }
        #endregion

        /// <summary>
        /// 对5列数据进行分类和融合，只处理中间的一列，其他的四列用于滤波
        /// </summary>
        /// <param name="classifier"></param>
        /// <param name="line">输入的线数必须是5线</param>
        /// <param name="showingEnergyType"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private ClassifiedLineData ClassifyAndFuse(MaterialClassifier classifier, ScanlineData line, ShowingEnergyType showingEnergyType)
        {
            //// 分别取低能和高能的数据
            //var list = line.ToList();

            //// 取中间的一线数据作为原始数据
            var sourceLine = line;

            // 分别取低能和高能数据
            //var leLines = list.Select(data => data.Low).ToList();

            //List<ushort[]> heLines = null;
            //if (sourceLine.High != null) 
            //    heLines = list.Select(data => data.High).ToList();

            ushort[] material = null;
            ushort[] showingGray = null;

            // 如果高能数据不为空，则进行物质分类
            if (sourceLine != null && classifier != null)
            {
                classifier.ClassifyLine(sourceLine.Low, sourceLine.High, out material);
            }
            else
            {
                // 如果高能数据为空，则无法进行物质分类，将物质分类全部初始化为0
                material = new ushort[sourceLine.LineLength];
            }
            return new ClassifiedLineData(sourceLine.ViewIndex, sourceLine.Fused, (ushort[])sourceLine.Fused.Clone(), material, sourceLine.Low, sourceLine.High);
            /*
            // 以下对数据进行融合

            // 对于双能量数据，可以进行高低能融合
            if (showingEnergyType == ShowingEnergyType.Fused && 
                sourceLine.XRaySensor == XRaySensorType.Dual)
            {
                _fuser.Fuse(sourceLine.Low, sourceLine.High, out showingGray);
            }
            else if (showingEnergyType == ShowingEnergyType.High && sourceLine.High != null)
            {
                // 显示高能数据
                showingGray = new ushort[sourceLine.LineLength];
                sourceLine.High.CopyTo(showingGray, 0);
            }
            else
            {
                // 对于其他情况，都显示低能，因为按照协议，低能数据肯定不能为空
                showingGray = new ushort[sourceLine.LineLength];
                sourceLine.Low.CopyTo(showingGray, 0);
            }

            return new ClassifiedLineData(sourceLine.ViewIndex, showingGray, material);
            */
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public ushort ClassifyPixel(DetectViewIndex view, ushort le, ushort he)
        {
            ushort material;
            if (view == DetectViewIndex.View1)
            {
                _view1Classifier.ClassifyPixel(le, he, out material);
            }
            else
            {
                _view2Classifier.ClassifyPixel(le, he, out material);
            }
            return material;
        }

        /// <summary>
        /// 对5列数据进行分类和融合，只处理中间的一列，其他的四列用于滤波
        /// </summary>
        /// <param name="classifier"></param>
        /// <param name="line">输入的线数必须是5线</param>
        /// <param name="showingEnergyType"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private ClassifiedLineData ClassifyAndFuseDivided(MaterialClassifier classifier1, MaterialClassifier classifier2, ScanlineData line, int DividedIndex, ShowingEnergyType showingEnergyType)
        {
            //// 取中间的一线数据作为原始数据
            var sourceLine = line;
            ushort[] firstHigh = null;
            ushort[] firstLow = null;

            ushort[] secondHigh = null;
            ushort[] secondLow = null;

            SplitArray(sourceLine.High, DividedIndex, ref firstHigh, ref secondHigh);
            SplitArray(sourceLine.Low, DividedIndex, ref firstLow, ref secondLow);

            ushort[] firstmaterial = null;
            ushort[] secondmaterial = null;

            ushort[] newmaterial = new ushort[line.LineLength];

            // 如果高能数据不为空，则进行物质分类
            if (sourceLine != null && classifier1 != null)
            {
                if (ExchangeDivideDirection)
                {
                    classifier1.ClassifyLine(firstLow, firstHigh, out firstmaterial);
                    classifier2.ClassifyLine(secondLow, secondHigh, out secondmaterial);
                }
                else
                {
                    classifier2.ClassifyLine(firstLow, firstHigh, out firstmaterial);
                    classifier1.ClassifyLine(secondLow, secondHigh, out secondmaterial);
                }

                Array.Copy(firstmaterial, 0, newmaterial, 0, firstmaterial.Length);
                Array.Copy(secondmaterial, 0, newmaterial, firstmaterial.Length, secondmaterial.Length);
            }
            else
            {
                // 如果高能数据为空，则无法进行物质分类，将物质分类全部初始化为0
                newmaterial = new ushort[sourceLine.LineLength];
            }
            return new ClassifiedLineData(sourceLine.ViewIndex, sourceLine.Fused, (ushort[])sourceLine.Fused.Clone(), newmaterial, sourceLine.Low, sourceLine.High);

        }

        /// <summary>
        /// 对5列数据进行分类和融合，只处理中间的一列，其他的四列用于滤波
        /// </summary>
        /// <param name="classifier"></param>
        /// <param name="line">输入的线数必须是5线</param>
        /// <param name="showingEnergyType"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private ClassifiedLineData ClassifyAndFuse16580(MaterialClassifier classifier1, MaterialClassifier classifier2, MaterialClassifier classifier3, ScanlineData line, int DividedIndex, int DividedIndex1, ShowingEnergyType showingEnergyType)
        {
            //// 取中间的一线数据作为原始数据
            var sourceLine = line;
            ushort[] tempHigh = null;
            ushort[] tempLow = null;

            ushort[] firstHigh = null;
            ushort[] firstLow = null;

            ushort[] secondHigh = null;
            ushort[] secondLow = null;

            ushort[] thirdHigh = null;
            ushort[] thirdLow = null;

            SplitArray(sourceLine.High, DividedIndex, ref firstHigh, ref tempHigh);
            SplitArray(sourceLine.Low, DividedIndex, ref firstLow, ref tempLow);

            SplitArray(tempHigh, DividedIndex1 - DividedIndex, ref secondHigh, ref thirdHigh);
            SplitArray(tempLow, DividedIndex1 - DividedIndex, ref secondLow, ref thirdLow);

            ushort[] firstmaterial = null;
            ushort[] secondmaterial = null;
            ushort[] thirdmaterial = null;

            ushort[] newmaterial = new ushort[line.LineLength];

            // 如果高能数据不为空，则进行物质分类
            if (sourceLine != null && classifier1 != null)
            {
                if (ExchangeDivideDirection)
                {
                    classifier1.ClassifyLine(firstLow, firstHigh, out firstmaterial);
                    classifier2.ClassifyLine(secondLow, secondHigh, out secondmaterial);
                    classifier3.ClassifyLine(thirdLow, thirdHigh, out thirdmaterial);
                }
                else
                {
                    classifier3.ClassifyLine(firstLow, firstHigh, out firstmaterial);
                    classifier2.ClassifyLine(secondLow, secondHigh, out secondmaterial);
                    classifier1.ClassifyLine(thirdLow, thirdHigh, out thirdmaterial);
                }

                Array.Copy(firstmaterial, 0, newmaterial, 0, firstmaterial.Length);
                Array.Copy(secondmaterial, 0, newmaterial, firstmaterial.Length, secondmaterial.Length);
                Array.Copy(thirdmaterial, 0, newmaterial, secondmaterial.Length+ firstmaterial.Length, thirdmaterial.Length);
            }
            else
            {
                // 如果高能数据为空，则无法进行物质分类，将物质分类全部初始化为0
                newmaterial = new ushort[sourceLine.LineLength];
            }
            return new ClassifiedLineData(sourceLine.ViewIndex, sourceLine.Fused, (ushort[])sourceLine.Fused.Clone(), newmaterial, sourceLine.Low, sourceLine.High);

        }

        private void SplitArray(ushort[] sourceLine, int dividedIndex, ref ushort[] firstArray, ref ushort[] secondArray)
        {
            if (dividedIndex < 0 || dividedIndex >= sourceLine.Length)
            {
                throw new ArgumentException("Invalid dividedIndex value");
            }

            firstArray = new ushort[dividedIndex + 1];
            secondArray = new ushort[sourceLine.Length - dividedIndex - 1];

            Array.Copy(sourceLine, 0, firstArray, 0, dividedIndex + 1);
            Array.Copy(sourceLine, dividedIndex + 1, secondArray, 0, sourceLine.Length - dividedIndex - 1);
        }
    }
}

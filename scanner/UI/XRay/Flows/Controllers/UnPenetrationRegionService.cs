using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 针对穿不透区域进行增强操作
    /// </summary>
    public class UnPenetrationRegionService
    {
        [DllImport("CirclePenetrationLib.dll", CallingConvention = CallingConvention.Cdecl)]     //你生成的.dll 文件名
        private extern static int Image_Enhance(IntPtr src, IntPtr dst, int height, int width, int stride, float thres4, float thres5, int dishijiao_flag);

        [DllImport("ImagePenetrateEnhance.dll", CharSet=CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]     //你生成的.dll 文件名
        private extern static int PenetrateEnhance(IntPtr src, IntPtr dst, int height, int width, int stride);

        public static UnPenetrationRegionService Service { get; private set; }

        static UnPenetrationRegionService()
        {
            Service = new UnPenetrationRegionService();
        }

        protected UnPenetrationRegionService()
        {
            //if (!ScannerConfig.Read(ConfigPath.AlgoEnhanceView1Circle4, out _view1Circle4Thres))
            //{
            //    _view1Circle4Thres = 1.0f;
            //}
            //if (!ScannerConfig.Read(ConfigPath.AlgoEnhanceView1Circle5, out _view1Circle5Thres))
            //{
            //    _view1Circle5Thres = 1.0f;
            //}
            //if (!ScannerConfig.Read(ConfigPath.AlgoEnhanceView2Circle4, out _view2Circle4Thres))
            //{
            //    _view2Circle4Thres = 1.0f;
            //}
            //if (!ScannerConfig.Read(ConfigPath.AlgoEnhanceView2Circle5, out _view2Circle5Thres))
            //{
            //    _view2Circle5Thres = 1.0f;
            //}
            ScannerConfig.ConfigChanged += ScannerConfig_ConfigChanged;
            _isInit = true;
            ScannerConfig.Read(ConfigPath.PreProcOpenNewPenetrationAlgorithm, out _openNewPenetrationAlgorithm);
        }

        void ScannerConfig_ConfigChanged(object sender, EventArgs e)
        {
            //if (!ScannerConfig.Read(ConfigPath.AlgoEnhanceView1Circle4, out _view1Circle4Thres))
            //{
            //    _view1Circle4Thres = 1.0f;
            //}
            //if (!ScannerConfig.Read(ConfigPath.AlgoEnhanceView1Circle5, out _view1Circle5Thres))
            //{
            //    _view1Circle5Thres = 1.0f;
            //}
            //if (!ScannerConfig.Read(ConfigPath.AlgoEnhanceView2Circle4, out _view2Circle4Thres))
            //{
            //    _view2Circle4Thres = 1.0f;
            //}
            //if (!ScannerConfig.Read(ConfigPath.AlgoEnhanceView2Circle5, out _view2Circle5Thres))
            //{
            //    _view2Circle5Thres = 1.0f;
            //}
        }

        public void Close()
        {
            if (_isInit)
            {
                ScannerConfig.ConfigChanged -= ScannerConfig_ConfigChanged;
            }            
        }

        private bool _isInit = false;

        private float _view1Circle4Thres;

        private float _view1Circle5Thres;

        private float _view2Circle4Thres;

        private float _view2Circle5Thres;
        private bool _openNewPenetrationAlgorithm;


        /// <summary>
        /// 当前显示控件最新线编号
        /// </summary>
        private int _minLineNumber;

        /// <summary>
        /// 当前显示控件最大线编号
        /// </summary>
        private int _maxLineNumber;


        /// <summary>
        /// 前后未显示区域线数量
        /// </summary>
        int _foreMargin, _backMargin;
        bool _isDualView;

        private LinkedList<DisplayScanlineData> DisplayData_View1 = new LinkedList<DisplayScanlineData>();
        private LinkedList<DisplayScanlineData> DisplayData_View2 = new LinkedList<DisplayScanlineData>();

        LinkedList<ushort[]> afterProcessLines_view1 = new LinkedList<ushort[]>();
        LinkedList<ushort[]> afterProcessLines_view2 = new LinkedList<ushort[]>();

        /// <summary>
        /// 当前控件显示的图像
        /// </summary>
        private LinkedList<DisplayScanlineData> CurrentScreenView1ScanLines = new LinkedList<DisplayScanlineData>();
        private LinkedList<DisplayScanlineData> CurrentScreenView2ScanLines = new LinkedList<DisplayScanlineData>();

        /// <summary>
        /// 应用特殊穿透
        /// </summary>
        /// <param name="minlinenumber"></param>
        /// <param name="maxlinenumber"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<DisplayScanlineDataBundle> TestboxSpecialPenetration(LinkedList<DisplayScanlineData> currentScreenView1ScanLines,LinkedList<DisplayScanlineData> currentScreenView2ScanLines,int minlinenumber, int maxlinenumber)
        {
            DisplayData_View1.Clear();
            DisplayData_View2.Clear();
            afterProcessLines_view1.Clear();
            afterProcessLines_view2.Clear();

            _minLineNumber = minlinenumber;
            _maxLineNumber = maxlinenumber;
            CurrentScreenView1ScanLines = currentScreenView1ScanLines;
            CurrentScreenView2ScanLines = currentScreenView2ScanLines;

            afterProcessLines_view1 = new LinkedList<ushort[]>();
            afterProcessLines_view2 = new LinkedList<ushort[]>();

            if (currentScreenView1ScanLines.Count < 1)
            {
                return null;
            }

            var images = GetCurrentImage();
            GetImageArray(images);
            CutImageMargin();
            return CopyNewDataToLines();
        }

        /// <summary>
        /// 获取当前显示控件中的图像
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private List<XRayFileInfoForMark> GetCurrentImage()
        {
            var allImages = PaintingRegionsService.Service.GetXRayFile;
            var currentFiles = new List<XRayFileInfoForMark>();
            foreach (var image in allImages)
            {
                if ((image.StartLineNumber >= _minLineNumber && image.StartLineNumber <= _maxLineNumber) || (image.EndLineNumber >= _minLineNumber && image.EndLineNumber <= _maxLineNumber))
                {
                    currentFiles.Add(image);
                }
            }
            currentFiles.Sort(delegate(XRayFileInfoForMark i1, XRayFileInfoForMark i2)
            {
                return i1.StartLineNumber.CompareTo(i2.StartLineNumber);
            });

            if (currentFiles.Count > 0)
            {
                _foreMargin = _minLineNumber - currentFiles.First().StartLineNumber;
                _backMargin = currentFiles.Last().EndLineNumber - _maxLineNumber;
            }
            else
            {
                _foreMargin = _minLineNumber;
                _backMargin = -_maxLineNumber;
            }
          

            return currentFiles;
        }

        /// <summary>
        /// 获取增强后的数据
        /// </summary>
        /// <param name="currentfiles"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void GetImageArray(List<XRayFileInfoForMark> currentfiles)
        {           
            int maxNum = -1;

            foreach (var image in currentfiles)
            {
                var xrayImage = XRayScanlinesImage.LoadFromDiskFile(image.FilePath);
                var displaylines = xrayImage.ToDisplayXRayMatLineDataBundles(image.EndLineNumber).ToList();
                maxNum -= displaylines.Count;
                if (displaylines.Count < 1) continue;
                _isDualView = xrayImage.View2Data != null;

                //线数据组装成图像
                int height = xrayImage.View1Data.ScanLines.Count();
                int width1 = xrayImage.View1Data.ScanLines[0].XRayData.Length;
                var scanlines_view1 = xrayImage.View1Data.ScanLines.ToList();

                _view1Circle4Thres = ChangeFlag(width1);
                var originData_view1 = ImageCircleEnhance(scanlines_view1, width1, height,_view1Circle4Thres,_view1Circle5Thres,0);

                //拆分图像为线数据
                for (int i = 0; i < height; i++)
                {
                    var temp = new ushort[width1];
                    Buffer.BlockCopy(originData_view1, i * width1 * sizeof(ushort), temp, 0, width1 * sizeof(ushort));
                    afterProcessLines_view1.AddLast(temp);
                    DisplayData_View1.AddLast(displaylines[i].View1Data);
                }

                if (_isDualView)
                {
                    int width2 = xrayImage.View2Data.ScanLines[0].XRayData.Length;
                    var scanlines_view2 = xrayImage.View2Data.ScanLines.ToList();

                    _view2Circle4Thres = ChangeFlag(width2);
                    var originData_view2 = ImageCircleEnhance(scanlines_view2, width2, height,_view2Circle4Thres,_view2Circle5Thres,1);
                    for (int i = 0; i < height; i++)
                    {
                        var temp2 = new ushort[width2];
                        Buffer.BlockCopy(originData_view2, i * width2 * sizeof(ushort), temp2, 0, width2 * sizeof(ushort));
                        afterProcessLines_view2.AddLast(temp2);
                        DisplayData_View2.AddLast(displaylines[i].View2Data);
                    }
                }
            }

            //最后一个图像可能未保存
            if (_backMargin < 0)
            {
                if (CurrentScreenView1ScanLines.Count < -_backMargin)
                {
                    return;
                }
                int height = -_backMargin;
                var currentScreenView1List = CurrentScreenView1ScanLines.Skip(CurrentScreenView1ScanLines.Count -height).Take(height).ToList();
                int width1 = currentScreenView1List[0].XRayData.Length;

                _view1Circle4Thres = ChangeFlag(width1);
                var originData_view1 = ImageCircleEnhance(currentScreenView1List, width1, height, _view1Circle4Thres, _view1Circle5Thres,0);
                //拆分图像为线数据
                for (int i = 0; i < height; i++)
                {
                    var temp = new ushort[width1];
                    Buffer.BlockCopy(originData_view1, i * width1 * sizeof(ushort), temp, 0, width1 * sizeof(ushort));
                    afterProcessLines_view1.AddLast(temp);
                    DisplayData_View1.AddLast(currentScreenView1List[i]);
                }

                if (_isDualView)
                {
                    var currentScreenView2List = CurrentScreenView2ScanLines.Skip(CurrentScreenView2ScanLines.Count - height).Take(height).ToList();
                    int width2 = currentScreenView2List[0].XRayData.Length;

                    _view2Circle4Thres = ChangeFlag(width2); 
                    var originData_view2 = ImageCircleEnhance(currentScreenView2List, width2, height, _view2Circle4Thres, _view2Circle5Thres,1);

                    for (int i = 0; i < height; i++)
                    {
                        var temp2 = new ushort[width2];
                        Buffer.BlockCopy(originData_view2, i * width2 * sizeof(ushort), temp2, 0, width2 * sizeof(ushort));
                        afterProcessLines_view2.AddLast(temp2);
                        DisplayData_View2.AddLast(currentScreenView2List[i]);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private ushort[,] ImageCircleEnhance(List<ClassifiedLineData> scanlines, int width, int height,float thresh4,float thresh5,int dishijiao_flag)
        {
            Matrix<ushort> input_view2 = new Matrix<ushort>(height, width);
            Matrix<ushort> output_view2_rotate = new Matrix<ushort>(width, height);

            for (int i = 0; i < height; i++)
            {
                Buffer.BlockCopy(scanlines[i].XRayData, 0, input_view2.Data, width * i * sizeof(ushort), width * sizeof(ushort));
            }

            ushort[,] OriginData_view2 = (ushort[,])input_view2.Data;
            var input_view2_rotate = rotateImage1(input_view2, -90);

            try
            {
                //穿不透区域处理过程                
                if (_openNewPenetrationAlgorithm)
                    PenetrateEnhance(input_view2_rotate.Mat.DataPointer, output_view2_rotate.Mat.DataPointer, input_view2_rotate.Height, input_view2_rotate.Width, input_view2_rotate.Mat.Step);
                else
                    Image_Enhance(input_view2_rotate.Mat.DataPointer, output_view2_rotate.Mat.DataPointer, input_view2_rotate.Height, input_view2_rotate.Width, input_view2_rotate.Mat.Step, thresh4, thresh5, dishijiao_flag);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return OriginData_view2;
            }            

            ushort[,] rotatedData_view2 = (ushort[,])output_view2_rotate.Data;
            CopyUnPeneRegion(rotatedData_view2, OriginData_view2, 3000);

            input_view2.Dispose();
            input_view2_rotate.Dispose();

            return OriginData_view2;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private ushort[,] ImageCircleEnhance(List<DisplayScanlineData> scanlines, int width, int height, float thresh4, float thresh5, int dishijiao_flag)
        {
            Matrix<ushort> input_view2 = new Matrix<ushort>(height, width);
            Matrix<ushort> output_view2_rotate = new Matrix<ushort>(width, height);
            for (int i = 0; i < height; i++)
            {
                Buffer.BlockCopy(scanlines[i].XRayData, 0, input_view2.Data, width * i * sizeof(ushort), width * sizeof(ushort));
            }
            ushort[,] OriginData_view2 = (ushort[,])input_view2.Data;
            var input_view2_rotate = rotateImage1(input_view2, -90);

            try
            {
                //穿不透区域处理过程                
                if (_openNewPenetrationAlgorithm)
                    PenetrateEnhance(input_view2_rotate.Mat.DataPointer, output_view2_rotate.Mat.DataPointer, input_view2_rotate.Height, input_view2_rotate.Width, input_view2_rotate.Mat.Step);
                else
                    Image_Enhance(input_view2_rotate.Mat.DataPointer, output_view2_rotate.Mat.DataPointer, input_view2_rotate.Height, input_view2_rotate.Width, input_view2_rotate.Mat.Step, thresh4, thresh5, dishijiao_flag);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return OriginData_view2;
            }            

            ushort[,] rotatedData_view2 = (ushort[,])output_view2_rotate.Data;
            CopyUnPeneRegion(rotatedData_view2, OriginData_view2, 3000);

            input_view2.Dispose();
            output_view2_rotate.Dispose();

            return OriginData_view2;
        }


        /// <summary>
        /// 去除前后未显示区域
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void CutImageMargin()
        {
            for (int i = 0; i < _foreMargin; i++)
            {
                afterProcessLines_view1.RemoveFirst();
                DisplayData_View1.RemoveFirst();
                if (_isDualView)
                {
                    afterProcessLines_view2.RemoveFirst();
                    DisplayData_View2.RemoveFirst();
                }

            }
            for (int i = 0; i < _backMargin; i++)
            {
                afterProcessLines_view1.RemoveLast();
                DisplayData_View1.RemoveLast();

                if (_isDualView)
                {
                    afterProcessLines_view2.RemoveLast();
                    DisplayData_View2.RemoveLast();
                }
            }
        }

        /// <summary>
        /// 用当前数据生成新显示数据
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private List<DisplayScanlineDataBundle> CopyNewDataToLines()
        {
            var afterProcessLines_View1_List = afterProcessLines_view1.ToList();
            var afterProcessLines_View2_List = afterProcessLines_view2.ToList();
            var DisplayData_View1_List = DisplayData_View1.ToList();
            var DisplayData_View2_List = DisplayData_View2.ToList();

            for (int i = 0; i < DisplayData_View1_List.Count; i++)
            {
                if (DisplayData_View1_List[i].XRayDataEnhanced != null)
                {
                    Buffer.BlockCopy(afterProcessLines_View1_List[i], 0, DisplayData_View1_List[i].XRayDataEnhanced, 0, afterProcessLines_View1_List[i].Length * sizeof(ushort));
                }
                if (_isDualView)
                {
                    if (DisplayData_View2_List[i].XRayDataEnhanced != null)
                    {
                        Buffer.BlockCopy(afterProcessLines_View2_List[i], 0, DisplayData_View2_List[i].XRayDataEnhanced, 0, afterProcessLines_View2_List[i].Length * sizeof(ushort));
                    }
                }
            }

            var afterProcessDisplayLines = new List<DisplayScanlineDataBundle>();
            for (int i = 0; i < DisplayData_View1_List.Count; i++)
            {
                if (_isDualView)
                {
                    afterProcessDisplayLines.Add(new DisplayScanlineDataBundle(DisplayData_View1_List[i], DisplayData_View2_List[i]));
                }
                else
                {
                    afterProcessDisplayLines.Add(new DisplayScanlineDataBundle(DisplayData_View1_List[i], null));
                }
            }

            return afterProcessDisplayLines;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        Matrix<ushort> rotateImage1(Matrix<ushort> modelImage, int degree)
        {
            double angle = degree * Math.PI / 180; // 弧度  
            double a = Math.Sin(angle), b = Math.Cos(angle);
            int width = modelImage.Width;
            int height = modelImage.Height;
            int width_rotate = Convert.ToInt32(height * Math.Abs(a) + width * Math.Abs(b));
            int height_rotate = Convert.ToInt32(width * Math.Abs(a) + height * Math.Abs(b));
            //旋转数组map
            // [ m0  m1  m2 ] ===>  [ A11  A12   b1 ]
            // [ m3  m4  m5 ] ===>  [ A21  A22   b2 ]
            //float[] map = new float[6];
            //此处为修改点，opencv可以直接使用数组，但emgucv似乎不认，所以改为了Matrix。
            Matrix<float> map_matrix_temp = new Matrix<float>(2, 3);

            // 旋转中心
            PointF center = new PointF(width / 2 - 0.5f, height / 2 - 0.5f);
            CvInvoke.GetRotationMatrix2D(center, degree, 1.0, map_matrix_temp);

            map_matrix_temp[0, 2] += (width_rotate - width) / 2;
            map_matrix_temp[1, 2] += (height_rotate - height) / 2;

            Matrix<ushort> img_rotate = new Matrix<ushort>(height_rotate,width_rotate);

            //对图像做仿射变换
            //CV_WARP_FILL_OUTLIERS - 填充所有输出图像的象素。
            //如果部分象素落在输入图像的边界外，那么它们的值设定为 fillval.
            //CV_WARP_INVERSE_MAP - 指定 map_matrix 是输出图像到输入图像的反变换，
            CvInvoke.WarpAffine(modelImage, img_rotate, map_matrix_temp, new Size(width_rotate, height_rotate),
                Inter.Nearest, Warp.Default, BorderType.Transparent, new MCvScalar(0d, 0d, 0d, 0d));

            map_matrix_temp.Dispose();

            return img_rotate;
        }

        /// <summary>
        /// 从处理后图像中取出穿不透区域，替换到原始数据中
        /// </summary>
        /// <param name="rotated"></param>
        /// <param name="source"></param>
        /// <param name="thresh"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void CopyUnPeneRegion(ushort[,] rotated,ushort[,] source,int thresh)
        {
            int x = rotated.GetUpperBound(0); //一维 
            int y = rotated.GetUpperBound(1); //二维 

            for (int j = 0; j <= y; j++)
            {
                for (int i = 0; i <= x; i++)
                {
                    if (source[j,i]< thresh)
                    {
                        source[j, i] = rotated[i, y - j];
                    }
                    
                }
            }
        }

        private float ChangeFlag(int width)
        {
            if (width == 640 || width == 704)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }
}

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Algo
{
    public class BanfengProcessor
    {
        private double[] _seg_y;
        private double[,] _mask1, _mask2, _mask3;

        private List<int[]> _badpoints = null;

        public BanfengProcessor()
        {
            var value = 1.0 / 6;
            _mask1 = new double[,] { { value, 0, value }, { value, 0, value }, { value, 0, value } };
            _mask2 = new double[,] { { value, 0, 0, value }, { value, 0, 0, value }, { value, 0, 0, value } };
            _mask3 = new double[,] { { value, 0, 0, 0, value }, { value, 0, 0, 0, value }, { value, 0, 0, 0, value } };
        }

        public void SetBadPoints(List<int[]> bads)
        {
            _badpoints = bads;
        }

        public void SetSeg(double[] seg)
        {
            _seg_y = new double[seg.Length];
            for (int i = 0; i < seg.Length; i++)
            {
                _seg_y[i] = seg[i] * 4;
            }
        }

        public bool HasBadPoints()
        {
            if (_badpoints == null || _badpoints.Count == 0)
                return false;
            if (_seg_y == null || _seg_y.Length < 65536)
                return false;

            if (_badpoints.Where(b => b.Length < 4).Count() == 0)
                return false;

            return true;
        }

        public Matrix<UInt16> Process(Matrix<UInt16> img)
        {
            if (!HasBadPoints())
            {
                return img;
            }

            var edge = Edge_Intensity(img);
            var locate1 = CalcLocate1(edge, img);
            for (int i = 0; i < _badpoints.Count; i++)
            {
                CalcNew(_badpoints[i], locate1, img);
            }

            locate1.Dispose();
            edge.Dispose();
            return img;
        }

        List<UInt16> templist = new List<ushort>(15);
        Random rnd = new Random();
        int aa = 120;

        private void CalcNew(int[] N, Matrix<byte> locate, Matrix<UInt16> img)
        {
            double sum = 0;
            for (int i = 2; i < img.Rows - 2; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = N[0] - 1; k <= N[N.Length - 1] + 1; k++)
                    {
                        if (locate.Data[i + j, k] < 1)
                        {
                            goto NoProc;
                        }
                    }
                }

                if (N.Length == 1)
                {
                    templist.Clear();
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int k = N[0] - 1; k <= N[0] + 1; k++)
                        {
                            templist.Add(img.Data[i + j, k]);
                        }
                    }
                    templist.Sort();
                    var newV = templist[(int)((templist.Count - 1) / 2)] + rnd.Next(-aa, aa);
                    newV = Math.Max(newV, 0);
                    newV = Math.Min(newV, 65530);
                    img.Data[i, N[0]] = (ushort)newV;
                }
                else if (N.Length == 2)
                {
                    sum = 0;
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int k = N[0] - 1; k <= N[1] + 1; k++)
                        {
                            sum += (img.Data[i + j, k] * _mask2[j + 1, k + 1 - N[0]]);
                        }
                    }
                    img.Data[i, N[0]] = (UInt16)sum;

                    templist.Clear();
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int k = N[1] - 1; k <= N[1] + 1; k++)
                        {
                            templist.Add(img.Data[i + j, k]);
                        }
                    }
                    templist.Sort();
                    var newV = templist[(int)((templist.Count - 1) / 2)] + rnd.Next(-aa, aa);
                    newV = Math.Max(newV, 0);
                    newV = Math.Min(newV, 65530);
                    img.Data[i, N[1]] = (ushort)newV;
                }
                else if (N.Length == 3)
                {
                    templist.Clear();
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int k = N[0] - 1; k <= N[2] + 1; k++)
                        {
                            templist.Add(img.Data[i + j, k]);
                        }
                    }
                    templist.Sort();
                    var newV = templist[(int)((templist.Count - 1) / 2)] + rnd.Next(-aa, aa);
                    newV = Math.Max(newV, 0);
                    newV = Math.Min(newV, 65530);
                    img.Data[i, N[1]] = (ushort)newV;

                    sum = 0;
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int k = N[0] - 1; k <= N[0] + 1; k++)
                        {
                            sum += (img.Data[i + j, k] * _mask1[j + 1, k + 1 - N[0]]);
                        }
                    }
                    img.Data[i, N[0]] = (UInt16)sum;

                    sum = 0;
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int k = N[2] - 1; k <= N[2] + 1; k++)
                        {
                            sum += (img.Data[i + j, k] * _mask1[j + 1, k + 1 - N[2]]);
                        }
                    }
                    img.Data[i, N[2]] = (UInt16)sum;
                }
            NoProc:
                continue;
            }
        }

        private Matrix<byte> CalcLocate1(Matrix<UInt16> edge, Matrix<UInt16> img)
        {
            var locate1 = new Matrix<byte>(edge.Rows, edge.Cols, 1);
            for (int i = 0; i < edge.Rows; i++)
            {
                for (int j = 0; j < edge.Cols; j++)
                {
                    if (edge.Data[i, j] < _seg_y[img.Data[i, j]] - 1)
                    {
                        locate1.Data[i, j] = 1;
                    }
                    else
                    {
                        locate1.Data[i, j] = 0;
                    }
                }
            }
            return locate1;
        }

        private Matrix<UInt16> Edge_Intensity(Matrix<UInt16> img)
        {
            var gx = new Matrix<float>(img.Rows, img.Cols, 1);
            CvInvoke.Sobel(img, gx, Emgu.CV.CvEnum.DepthType.Cv32F, 1, 0, 3, 1, 0, Emgu.CV.CvEnum.BorderType.Replicate);
            var gy = new Matrix<float>(img.Rows, img.Cols, 1);
            CvInvoke.Sobel(img, gy, Emgu.CV.CvEnum.DepthType.Cv32F, 0, 1, 3, 1, 0, Emgu.CV.CvEnum.BorderType.Replicate);

            var gxx = new Matrix<float>(img.Rows, img.Cols, 1);
            CvInvoke.Multiply(gx, gx, gxx);
            var gyy = new Matrix<float>(img.Rows, img.Cols, 1);
            CvInvoke.Multiply(gy, gy, gyy);

            var gxxyy = new Matrix<float>(img.Rows, img.Cols, 1);
            CvInvoke.Add(gxx, gyy, gxxyy);

            var sqrt = new Matrix<float>(img.Rows, img.Cols, 1);
            CvInvoke.Sqrt(gxxyy, sqrt);

            var rtn = new Matrix<UInt16>(img.Rows, img.Cols, 1);
            rtn = sqrt.Convert<UInt16>();

            gx.Dispose();
            gy.Dispose();
            gxx.Dispose();
            gyy.Dispose();
            gxxyy.Dispose();
            sqrt.Dispose();

            return rtn;
        }
    }
}

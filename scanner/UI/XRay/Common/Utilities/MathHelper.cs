using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Common.Utilities
{
    public static class MathHelper
    {
        /// <summary>
        /// 快速计算浮点数的平方根的倒数
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static float InverseSqrtFast(float x)
        {
            float xhalf = 0.5f*x;
            int i = 0x5f375a86 - (FloatToIntBits(x) >> 1);
            x = IntBitsToFloat(i);
            x = x*(1.5f - xhalf*x*x);
            return x;
        }

        public static float IntBitsToFloat(int i)
        {
            byte[] bytes = BitConverter.GetBytes(i);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static int FloatToIntBits(float x)
        {
            byte[] bytes = BitConverter.GetBytes(x);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}

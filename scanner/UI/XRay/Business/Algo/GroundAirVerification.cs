//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
// 
// Description: GroundAirVerification.cs实现GroundAirVerification工具类，对本底和满度按照规则进行检验，以判断是否是合理的本底、满度.
// 每次本底、满度更新产生后，需要加入到这个缓存中，并从这个缓存中获取正确的本底或满度
// 
// 每一时刻存储两个满度A和B，A为当前正在使用的满度，B为最近一次尝试更新的满度；
//      每次更新满度时，先将新满度C与A进行对比：如果符合规则，则使用C替换A和B，更新成功；
//         如果不符合规则，则将C与B进行对比：
//            如果符合规则，则使用C替换A和B，更新成功；
//            如果不符合规则，则使用C替换B，更新失败（记录更新失败的Log），等待下一次更新流程再来更新满度。  
// 如果当前没有满度缓存，则Verify默认通过
// 对本底的更新，采用与上相同的策略
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 表示本底、满度更新的规则检查及缓存。
    /// </summary>
    public class GroundAirVerification
    {
        /// <summary>
        /// 当前正在使用的本底或者满度，A通道
        /// </summary>
        public ScanlineData Current { get; private set; }

        /// <summary>
        /// 手动校正
        /// </summary>
        public ScanlineData Standard { get; private set; }

        /// <summary>
        /// 上一次更新得到的本底或者满度，A通道
        /// </summary>
        protected ScanlineData LastUpdate { get; private set; }

        /// <summary>
        /// 变化率的阈值
        /// </summary>
        public double ChangeRate { get; private set; }


        /// <summary>
        /// 校验器的构造函数
        /// </summary>
        /// <param name="changeRate">本底或满度的变化率阈值</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public GroundAirVerification(double changeRate)
        {
            if (changeRate <= 0 || changeRate >= 0.8)
            {
                throw new ArgumentOutOfRangeException("changeRate", changeRate,
                    "Invalid change rate for air or ground verification");
            }

            ChangeRate = changeRate;
        }

        /// <summary>
        /// 设置标准的本底或者满度：如手动标定的
        /// </summary>
        /// <param name="line">标准的本底或者满度数据</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetStandard(ScanlineData line)
        {
            if (line != null)
            {
                Current = LastUpdate = line;
                Standard = DeepCopy.DeepCopyByBin(line);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdateAir(ScanlineData line)
        {
            if (line != null)
            {
                Current = LastUpdate = line;
            }
        }

        /// <summary>
        /// 缓存最新的本底或满度数据，并进行检查。
        /// </summary>
        /// <param name="value">刚刚更新的本底或者满度数据</param>
        /// <param name="changeRate">本底或满度的变化率</param>
        /// <returns>true表示本次更新成功，可以使用本次的新数据；false表示本次更新失败，将使用原来的满度或本底</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Verify(ScanlineData value, bool isAlwaysUseNew, out double changeRate)
        {
            // 当前为空，则直接使用
            if (Current == null)
            {
                Current = value;
                LastUpdate = Current;
                changeRate = 0;
                return true;
            }

            changeRate = GetManhattanChangeRate(value, Current);
            if (changeRate <= ChangeRate || isAlwaysUseNew)
            {
                Current = LastUpdate = value;
                return true;
            }

            changeRate = GetManhattanChangeRate(value, LastUpdate);
            if (changeRate <= ChangeRate)
            {
                Current = LastUpdate = value;
                return true;
            }

            //如果变化率小于强制更新阈值，才进行LastUpdate更新
            //过大的变化率认为是在包裹上，不进行LastUpdate更新
            if (changeRate <= ChangeRate + 0.03)
            {
                LastUpdate = value;
            }
            //新值与当前或上一次的更新都不一致，更新失败
            return false;
        }

        /// <summary>
        /// 计算曼哈顿距离变化率：通过Manhattan距离进行度量
        /// </summary>
        /// <param name="newValue">新的本底或者满度数据</param>
        /// <param name="old">旧的本底或者满度数据</param>
        /// <returns></returns>
        private double GetManhattanChangeRate(ScanlineData newValue, ScanlineData old)
        {
            // todo: 根据第一视角的低能数据进行对比，如果差异较小，则返回true；如果差异较大，则返回false
            var newLE = newValue.Low;
            ushort[] oldLE = null;
            if (old != null)
            {
                oldLE = old.Low;
            }
            else
            {
                oldLE = newValue.Low;
            }

            // 求两次低能数据的Manhattan（曼哈顿）距离：同等对待每个探测点
            var manhattanDist = newLE.Zip(oldLE, (arg1, arg2) => Math.Abs(arg1 - arg2)).Sum();

            // 原值的和
            var oldSum = oldLE.Sum(arg => arg);
            if (oldSum == 0)
            {
                // 避免极端情况除0导致异常
                return 0;
            }

            // 变化率：曼哈顿距离 / 原向量的和
            double rate = (double) manhattanDist / oldSum;

            // todo: 是否需要根据不同的射线源，不同的探测板，设置不同的参数？或者根据开机时候采集的本底、满度自动计算一个合适的值？
            return rate;
        }
    }
}

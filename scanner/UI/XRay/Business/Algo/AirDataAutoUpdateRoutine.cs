using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 属性数据更新例程，用于自动更新某个视角的满度或本底。
    /// 用法：每次输入一线数据，当缓冲器满后，对缓冲区中的数据求均值，并输出均值结果作为满度
    /// </summary>
    public class AirDataAutoUpdateRoutine:AirDataAutoUpdateRoutineBase
    {
        public AirDataAutoUpdateRoutine(DetectViewIndex viewIndex)
        {
            ViewIndex = viewIndex;
            int lines = 24;
            if (!ScannerConfig.Read(ConfigPath.PreProcDynamicUpdateLineCount, out lines))
            {
                lines = 24;
            }
            BeforeScanningUpdateCacheLinesCount = lines;
        }

        public override void SetReferenceAir(ScanlineData reference)
        {
        }
        public override void ClearAirData()
        {
            
        }
    }
}

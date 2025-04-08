using System;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 射线探测器类型：单能量、双能量
    /// </summary>
    [Serializable]
    public enum XRaySensorType
    {
        /// <summary>
        /// 未知类型，非法
        /// </summary>
        None = 0,

        /// <summary>
        /// 单能量
        /// </summary>
        Single = 1,

        /// <summary>
        /// 双能量
        /// </summary>
        Dual = 2
    }

    /// <summary>
    /// 显示能量类型枚举
    /// </summary>
    public enum ShowingEnergyType
    {
        /// <summary>
        /// 显示高低能混合能量的数据
        /// </summary>
        Fused = 0,

        /// <summary>
        /// 显示低能量数据
        /// </summary>
        Low = 1,

        /// <summary>
        /// 显示高能量数据
        /// </summary>
        High = 2
    }

    /// <summary>
    /// 视角定义，对于单视角系统，只有View1；对于双视角系统有View1和View2
    /// 视角编号规则：从入口处开始，分别为View1和View2
    /// </summary>
    [Serializable]
    public enum DetectViewIndex
    {
        /// <summary>
        /// All Represents all views
        /// </summary>
        All = 0,

        /// <summary>
        /// Represents the first view
        /// </summary>
        View1 = 1,

        /// <summary>
        /// Represents the second view.
        /// </summary>
        View2 = 2
    }

    /// <summary>
    /// 培训图像的生成模式
    /// </summary>
    public enum TrainingImageLoopMode
    {
        /// <summary>
        /// 不循环，仅从头到尾依次显示
        /// </summary>
        NoLoop,

        /// <summary>
        /// 按文件名顺序循环
        /// </summary>
        SequentialLoop = 1,

        /// <summary>
        /// 随机循环模式
        /// </summary>
        RandomLoop = 2
    }

    /// <summary>
    /// Tip图像库名称
    /// </summary>
    public enum TipLibrary
    {
        /// <summary>
        /// Tip 刀
        /// </summary>
        Knives,

        /// <summary>
        /// Tip 爆炸物
        /// </summary>
        Explosives,

        /// <summary>
        /// Tip 枪
        /// </summary>
        Guns,

        /// <summary>
        /// Tip 其他
        /// </summary>
        Others
    }

    /// <summary>
    /// 电机方向，只用在电机方向更新
    /// </summary>
    public enum MotorDirection
    {
        Stop,
        MoveLeft,
        MoveRight
    }
    /// <summary>
    /// 不同探测器类型
    /// </summary>
    public enum CaptureSysTypeEnum
    {
        DtGCUSTD = 1,
        TYM = 2,
        Dt
    }
    /// <summary>
    /// 采集板连接模式
    /// </summary>
    public enum DetSysTypeEnum
    {
        USB = 0,
        TCP = 1,
        UDP = 2,
    }

    /// <summary>
    /// Tip注入的三步骤：准备注入、正在注入、注入完毕
    /// </summary>
    public enum TipInjectionStep
    {
        Preparing,

        Injecting,

        Injected,
    }

    /// <summary>
    /// 图像导出效果
    /// </summary>
    public enum ExportImageEffects
    {
        None = 0,
        Regular = 1,
        SuperEnhance = 2,
        Absorptivity = 3,
        Grey = 4,
        OS = 5,
        MS = 6,
        Inverse = 7,
        SlicePenetrate = 8,
        HighPenetrate = 9,
        LowPenetrate = 10,
        SuperPenetrate = 11,
        Zeff7 = 12,
        Zeff8 = 13,
        Zeff9 = 14
    }
}

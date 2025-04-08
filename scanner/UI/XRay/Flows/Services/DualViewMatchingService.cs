using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.Entities;
using UI.XRay.Control;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 双视角数据同步服务.
    /// 此类的实例，是多线程安全的
    /// </summary>
    public class DualViewMatchingService
    {
        /// <summary>
        /// 两个视角之间配准时需要丢弃的线数
        /// </summary>
        public int LinesBetweenViews { get; private set; }     


        /// <summary>
        /// 配准过程中，用于缓存输送机运转方向的第一个视角的数据
        /// 正转时，缓存第一个视角的数据；反转时，缓存第二个视角的数据
        /// </summary>
        private readonly Queue<RawScanlineData> _cacheQueue = new Queue<RawScanlineData>();   //队列 可以lock的容器 存储的是rawscanlinedata类型的元素

        [MethodImpl(MethodImplOptions.Synchronized)]
        public DualViewMatchingService()
        {
            LinesBetweenViews = ConfigHelper.GetDualViewMatchLines();
            
        }


        /// <summary>
        /// 重置为尚未配准的状态
        /// 当输送机停止时，调用此函数重置
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ResetToUnmatched()                        //Dequeue() 移除并返回Queue的开头的对象   Enqueue() 向Queue的末尾添加一个对象
        {
            try
            {
                lock (_cacheQueue)
                {
                    while (_cacheQueue.Count > 0)
                    {
                        _cacheQueue.Dequeue();
                    }
                }
                
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 配准两个视角的数据
        /// </summary>
        /// <param name="scanline">一线新采集的数据</param>
        /// <param name="conveyorDirection">当前输送机的转动方向</param>
        /// <returns>如果仍处于配准过程中，则返回null；如果配准成功，则返回配准后的数据</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public RawScanlineDataBundle Match(RawScanlineDataBundle scanline, ConveyorDirection conveyorDirection)
        {
            if (LinesBetweenViews <= 0)
            {
                return scanline;
            }
            lock (_cacheQueue)
            {
                if (!ExchangeDirectionConfig.Service.IsExchangeDetector)
                {
                    // 根据输送机方向不同，缓存不同视角的数据
                    if (conveyorDirection == ConveyorDirection.MoveForward && scanline.View1RawData != null)
                    {
                        _cacheQueue.Enqueue(scanline.View1RawData);
                    }
                    else if (conveyorDirection == ConveyorDirection.MoveBackward && scanline.View2RawData != null)
                    {
                        _cacheQueue.Enqueue(scanline.View2RawData);
                    }


                    if (_cacheQueue.Count >= LinesBetweenViews)
                    {
                        // 配准完毕，取出队列末尾的数据，向外输出
                        var cachedLine = _cacheQueue.Dequeue();
                        if (conveyorDirection == ConveyorDirection.MoveForward)
                        {
                            return new RawScanlineDataBundle(cachedLine, scanline.View2RawData);  //最后得到对齐的一个Bundle
                        }

                        if (conveyorDirection == ConveyorDirection.MoveBackward)
                        {
                            return new RawScanlineDataBundle(scanline.View1RawData, cachedLine);
                        }
                    }
                }
                else
                {
                    if (conveyorDirection == ConveyorDirection.MoveForward && scanline.View2RawData != null)
                    {
                        _cacheQueue.Enqueue(scanline.View2RawData);
                    }
                    else if (conveyorDirection == ConveyorDirection.MoveBackward && scanline.View1RawData != null)
                    {
                        _cacheQueue.Enqueue(scanline.View1RawData);
                    }


                    if (_cacheQueue.Count >= LinesBetweenViews)
                    {
                        // 配准完毕，取出队列末尾的数据，向外输出
                        var cachedLine = _cacheQueue.Dequeue();
                        if (conveyorDirection == ConveyorDirection.MoveForward)
                        {
                            return new RawScanlineDataBundle(scanline.View1RawData,cachedLine);  //最后得到对齐的一个Bundle
                        }

                        if (conveyorDirection == ConveyorDirection.MoveBackward)
                        {
                            return new RawScanlineDataBundle(cachedLine,scanline.View2RawData);
                        }
                    }
                }
            }            

            return null;
        }
    }
}

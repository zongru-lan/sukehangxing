using System;
using System.Collections.Generic;
using System.Text;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Business.DataAccess.Config;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 行李间隙过滤器：自动滤除行李间隙
    /// </summary>
    public class ObjectSeparater
    {
        /// <summary>
        /// 正在检测包裹，外部订阅此事件接收包裹的图像数据
        /// </summary>
        //public event EventHandler<ClassifiedLineDataBundle> ObjectScanning;

        /// <summary>
        /// 一个包裹检测完毕，外部订阅此事件对数据进行分包
        /// </summary>
        //public event EventHandler ObjectScannedOver;

        /// <summary>
        /// 输出流，在外部检测输出流，并获取输出流中的数据
        /// </summary>
        public Queue<DisplayScanlineDataBundle> OutputQueue { get; private set; }

        /// <summary>
        /// 每组扫描线缓存的上限：每次缓存此数量的扫描线后，进行一次是否为空气值的判断
        /// </summary>
        private const int LinesCacheCapacity = 24;

        /// <summary>
        /// 每个被检测物体图像的扫描线数量上限
        /// </summary>
        public int ObjectMaxScanLines = 1200;

        /// <summary>
        /// 是否显示并保存包裹间空白区域
        /// </summary>
        private readonly bool _showBlankSpaces = true;

        /// <summary>
        /// 当前扫描线缓存：每次缓存LinesCacheCapacity线，然后一次性进行判断。如果发现LinesCacheCapacity线中有1线不是空气，则认为不是空气
        /// </summary>
        private List<DisplayScanlineDataBundle> _newLinesCache = new List<DisplayScanlineDataBundle>(LinesCacheCapacity + 1);

        /// <summary>
        /// 缓存的当前空气扫描线数据的链表，其中可能同时存储多组空气数据
        /// </summary>
        private readonly LinkedList<List<DisplayScanlineDataBundle>> _cachedAirLinesList = new LinkedList<List<DisplayScanlineDataBundle>>();

        /// <summary>
        /// 当前是否在扫描物体，而不是物体的前后空隙
        /// </summary>
        private bool _scanningObject;

        /// <summary>
        /// 当前尚未存储的物体的扫描线的数量，不包含空气数据
        /// </summary>
        private int _objectImageLinesCount = 0;

        /// <summary>
        /// 构造函数，构造一个用于物体图像分离的实例
        /// </summary>
        /// <param name="showBlankSpace">是否显示物体图像之间的空白区域</param>
        public ObjectSeparater(bool showBlankSpace)
        {
            _showBlankSpaces = showBlankSpace;
            OutputQueue = new Queue<DisplayScanlineDataBundle>();
            ReadConfig();
        }
        private void ReadConfig()
        {
            if (!ScannerConfig.Read(ConfigPath.BagMaxScanLines,out ObjectMaxScanLines))
            {
                ObjectMaxScanLines = 1200;
            }
            Tracer.TraceInfo($"[ObjectSeparater] ObjectMaxScanLines: {ObjectMaxScanLines}");
        }
        public void ClearLinesCache()
        {
            _newLinesCache.Clear();
        }

        /// <summary>
        /// 进行包裹统计,进行分包，并在订阅事件中进行处理
        /// </summary>
        /// <param name="bundle">要检测的扫描线束</param>
        public bool Separate(DisplayScanlineDataBundle bundle)
        {
            // 如果配置为显示空白区域，则先将扫描线数据送出去进行显示,保证所有图像数据都会被显示
            // 接下来还会对数据进行缓存，并判断是否为空白区域，是为了进行包裹图像分离和计算，不会再显示
            if (_showBlankSpaces)
            {
                // 送出去显示的数据，都是尚未存储的数据，因此在此处进行计数
                _objectImageLinesCount += 1;

                OutputQueue.Enqueue(bundle);

                //if (ObjectScanning != null)
                //{
                //    ObjectScanning(this, bundle);
                //}
            }

            bool objectScannedOver = false;

            // 先将扫描线加入缓冲区：当缓冲区满以后，才判断缓冲区中是否为空气数据
            _newLinesCache.Add(bundle);

            // 若本次cache 已满,开始进行判断
            if (_newLinesCache.Count >= LinesCacheCapacity)
            {
                // 当前缓冲区中是否都是空气数据
                var isAllAir = IsAllAir(_newLinesCache);
                // 本次缓存中都是空气值
                if (isAllAir)
                {
                    objectScannedOver = OnAirLinesDetected();
                }
                else
                {
                    // 本次缓存中不是空气值，即本次缓存中的数据是物体扫描的数据
                    OnNonAirLinesDetected();
                }

                // 重新申请缓存，准备缓存下一组扫描线
                _newLinesCache = new List<DisplayScanlineDataBundle>(LinesCacheCapacity + 1);
            }

            // 如果统计的一个物体的图像的数据量过多，则自动截断，将当前尚未存储的数据作为一个物体的图像进行存储
            if (_objectImageLinesCount >= ObjectMaxScanLines)
            {
                var builder = new StringBuilder();
                builder.Append(string.Format("More than {0} scan lines are waiting for save, ", ObjectMaxScanLines));
                builder.Append("but has not detected the air between images, so save all as one image.");
                Tracer.TraceInfo(builder.ToString());

                //RaiseObjectScannedOverEvent();
                _objectImageLinesCount = 0;
                objectScannedOver = true;
            }

            return objectScannedOver;
        }

        /// <summary>
        /// 当缓存中都是空气数据时，调用此函数处理一组空气数据
        /// </summary>
        private bool OnAirLinesDetected()
        {
            if (!_scanningObject)
            {
                // 当前状态：物体扫描前的空气扫描
                // 当前扫描空气，尚未扫描到物体

                _cachedAirLinesList.AddLast(_newLinesCache);

                // 最多同时保留两组空气值数据,如果空气数据过多,则删除最早的空气数据
                while (_cachedAirLinesList.Count >= 2)
                {
                    _cachedAirLinesList.RemoveFirst();
                }
            }
            else
            {
                // 当前状态：物体扫描后的空气扫描或物体中的空气扫描
                // 当前正在扫描物体的时候，本次扫描到空气，先将空气值加入到空气队列中
                _cachedAirLinesList.AddLast(_newLinesCache);

                if (_cachedAirLinesList.Count >= 2)
                {
                    // 若连续扫描两组空气，则表明物体扫描结束，进入空气扫描状态
                    _scanningObject = false;

                    // 本次的空气值不显示，但上一次的空气值需要显示，作为物体图像后部的空白，并从队列中移除
                    // 队列中将会继续保留一组空气数据，可能会作为下一个物体图像的开始
                    RaiseLugScanningEventIfNotShowBlankSpaces(_cachedAirLinesList.First.Value);
                    _cachedAirLinesList.RemoveFirst();

                    // 触发事件，结束一幅图像的存储
                    //RaiseObjectScannedOverEvent();
                    _objectImageLinesCount = 0;
                    return true;
                }

                // 以下注释仅仅为了维持逻辑清晰
                // 只有当前一组空气值：则继续等待，将根据下一组数据是否为空气再判断
                // 如果下一组数据还是空气，则会跳转到上面的逻辑：物体扫描结束
                // 如果下一组数据不是空气，则表明此组空气有可能是误判等，将作为物体图像处理
            }

            return false;
        }

        /// <summary>
        /// 当缓存中并非全是空气数据时，即至少有一线数据是非空气数据，调用此函数进行处理
        /// </summary>
        private void OnNonAirLinesDetected()
        {
            if (!_scanningObject)
            {
                // 如果当前不是物体扫描状态，将转换为物体扫描状态
                _scanningObject = true;

                if (_cachedAirLinesList.Count >= 1)
                {
                    // 如果在此之前，已经扫描了一些空气数据，则优先显示此物体之前的一部分空气数据
                    RaiseLugScanningEventIfNotShowBlankSpaces(_cachedAirLinesList.Last.Value);

                    // 并清空所有空气数据
                    _cachedAirLinesList.Clear();
                }

                // 然后显示此次的物体扫描数据
                RaiseLugScanningEventIfNotShowBlankSpaces(_newLinesCache);
            }
            else
            {
                // 如果当前已经处于物体扫描状态

                // 首先查看是否有缓存的尚未显示的空气值，如果有，优先显示这些空气值
                // 这些空气值有可能是两个物体之间的非常小的空隙导致的，将会在此忽略这些空气值，将其当做物体数据
                if (_cachedAirLinesList.Count >= 1)
                {
                    var firstLine = _cachedAirLinesList.First;
                    while (firstLine != null)
                    {
                        RaiseLugScanningEventIfNotShowBlankSpaces(firstLine.Value);
                        firstLine = firstLine.Next;
                    }

                    // 累积的空气值显示完毕后，需要清除
                    _cachedAirLinesList.Clear();
                }
                // 然后，显示本次扫描的物体数据
                RaiseLugScanningEventIfNotShowBlankSpaces(_newLinesCache);
            }
        }

        /// <summary>
        /// 当配置为不显示物体图像间空白区域时，触发物体扫描中事件，并累计已显示但未存储的扫描线数量；当配置为
        /// 显示物体图像间空白区域时，不会触发物体扫描中事件，函数不执行任何有意义的逻辑
        /// </summary>
        /// <param name="bundles">要进行显示的图像数据</param>
        private void RaiseLugScanningEventIfNotShowBlankSpaces(IEnumerable<DisplayScanlineDataBundle> bundles)
        {
            if (!_showBlankSpaces)
            {
                // 送出去显示的数据，都是尚未存储的数据，因此在此处进行计数
                _objectImageLinesCount += LinesCacheCapacity;

                //if (ObjectScanning == null) return;

                foreach (var bundle in bundles)
                {
                    OutputQueue.Enqueue(bundle);
                    //ObjectScanning(this, bundle);
                }
            }
        }

        /// <summary>
        /// 触发物体扫描结束事件，并将累计扫描线数量清零
        /// </summary>
        //private void RaiseObjectScannedOverEvent()
        //{
        //    _objectImageLinesCount = 0;

        //    if (ObjectScannedOver != null)
        //    {
        //        ObjectScannedOver(this, null);
        //    }
        //}

        public void ResetImageLinesCache()
        {
            lock (_cachedAirLinesList)
            {
                _cachedAirLinesList.Clear();
                OutputQueue.Clear();
                _scanningObject = false;
                _objectImageLinesCount = 0;
            }
        }


        /// <summary>
        /// 判断输入的扫描线是否都是Air
        /// </summary>
        /// <param name="bundles">要检查的扫描线的列表</param>
        /// <returns>true表示所有扫描线都是空气值</returns>
        private bool IsAllAir(List<DisplayScanlineDataBundle> bundles)
        {
            for (int i = 0; i < bundles.Count; i++)
            {
                if (!bundles[i].IsAir)
                {
                    return false;
                }
            }
            return true;
        }
    }
}

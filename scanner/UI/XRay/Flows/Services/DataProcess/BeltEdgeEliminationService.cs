using System;
using System.Runtime.CompilerServices;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.DataProcess
{
    /// <summary>
    /// 皮带上下边缘剔除服务
    /// </summary>
    public class BeltEdgeEliminationService
    {
        /// <summary>
        /// 视角1的上下边缘置白像素
        /// </summary>
        private int _view1MarginChannelsAtBegin;

        private int _view2MarginChannelsAtBegin;

        //视角2的上下边缘置白像素
        private int _view1MarginChannelsAtEnd;

        private int _view2MarginChannelsAtEnd;

        /// <summary>
        /// 视角个数
        /// </summary>
        private int _viewCount;

        public BeltEdgeEliminationService()
        {
            try
            {
                ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;

                LoadSettings();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 加载视角的上下边缘置白设置
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void LoadSettings()
        {
            // 获取视角个数
            _viewCount = 1;
            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewCount))
            {
                _viewCount = 1;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtBegin, out _view1MarginChannelsAtBegin))
            {
                _view1MarginChannelsAtBegin = 5;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtEnd, out _view1MarginChannelsAtEnd))
            {
                _view1MarginChannelsAtEnd = 5;
            }

            if (_viewCount == 2)
            {
                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtBegin, out _view2MarginChannelsAtBegin))
                {
                    _view2MarginChannelsAtBegin = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtEnd, out _view2MarginChannelsAtEnd))
                {
                    _view2MarginChannelsAtEnd = 5;
                }
            }
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            LoadSettings();
        }

        /// <summary>
        /// 设置扫描线上下边缘的空白
        /// </summary>
        /// <param name="bundle"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Eliminate(ScanlineDataBundle bundle)
        {
            // 在归一化完成后，对图像的上下边缘进行强制剔除处理
            if (bundle.View1LineData != null)
            {
                if (bundle.View1LineData.Low != null)
                {
                    Eliminate(bundle.View1LineData.Low, _view1MarginChannelsAtBegin, _view1MarginChannelsAtEnd);
                }

                if (bundle.View1LineData.High != null)
                {
                    Eliminate(bundle.View1LineData.High, _view1MarginChannelsAtBegin, _view1MarginChannelsAtEnd);
                }
                if (bundle.View1LineData.Fused != null)
                {
                    Eliminate(bundle.View1LineData.Fused, _view1MarginChannelsAtBegin, _view1MarginChannelsAtEnd);
                }
            }

            // 处理第二视角的强制边缘置白
            if (bundle.View2LineData != null)
            {
                if (bundle.View2LineData.Low != null)
                {
                    Eliminate(bundle.View2LineData.Low, _view2MarginChannelsAtBegin, _view2MarginChannelsAtEnd);
                }

                if (bundle.View2LineData.High != null)
                {
                    Eliminate(bundle.View2LineData.High, _view2MarginChannelsAtBegin, _view2MarginChannelsAtEnd);
                }
                if (bundle.View2LineData.Fused != null)
                {
                    Eliminate(bundle.View2LineData.Fused, _view2MarginChannelsAtBegin, _view2MarginChannelsAtEnd);
                }
            }

        }

        /// <summary>
        /// 设置上下空白边缘
        /// </summary>
        /// <param name="data">要处理的数据</param>
        /// <param name="marginAtBegin">起始探测通道编号</param>
        /// <param name="marginAtEnd">结束探测通道编号</param>
        private void Eliminate(ushort[] data, int marginAtBegin, int marginAtEnd)
        {
            if (data != null && data.Length > 0)
            {
                for (int i = 0; i < marginAtBegin && i < data.Length; i++)
                {
                    data[i] = 65530;
                }

                for (int i = Math.Max(data.Length - marginAtEnd, 0); i < data.Length; i++)
                {
                    data[i] = 65530;
                }
            }
        }
    }
}

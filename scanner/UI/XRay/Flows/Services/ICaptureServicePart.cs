////////////////////////////////////////////////////////////////////////////////////////
//
// 成像部件接口：支持单视角和双视角，对外通过事件提供的数据中直接包含单视角或双视角的数据
//
////////////////////////////////////////////////////////////////////////////////////////

using System;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 支持单视角和双视角的图像采集服务组件接口
    /// </summary>
    public interface ICaptureServicePart
    {
        /// <summary>
        /// Fired when received one scanline. (one view or two views)
        /// </summary>
        event EventHandler<RawScanlineDataBundle> ScanlineCaptured;

        /// <summary>
        /// Fired when imaging part has been successfully opened.
        /// </summary>
        event EventHandler DeviceOpenedWeakEvent;

        /// <summary>
        /// Fired when imaging part is closed.
        /// </summary>
        event EventHandler DeviceClosedWeakEvent;

        /// <summary>
        /// Event to update connection state. True for Ok while False for error.
        /// </summary>
        event EventHandler<bool> ConnectionStateUpdated;

        ///// <summary>
        ///// Detect channels count of view1
        ///// </summary>
        //int View1ChannelsCount { get; }

        ///// <summary>
        ///// Detect channels count of view2
        ///// </summary>
        //int View2ChannelsCount { get; }

        /// <summary>
        /// True if alive, False for dead.
        /// </summary>
        bool Alive { get; }

        /// <summary>
        /// Is device already openned
        /// </summary>
        bool IsOpened { get; }

        /// <summary>
        /// Is dual view mode.
        /// </summary>
        bool IsDualView { get; }

        /// <summary>
        /// Line integration time, in microsecond.
        /// </summary>
        int IntegrationTime { get; }
 
        /// <summary>
        /// Open all views of Imaging Part to receive data.
        /// </summary>
        /// <returns>true if success, or false for error.</returns>
        bool Open();

        /// <summary>
        /// Start capture image.
        /// </summary>
        /// <returns></returns>
        bool StartCapture();

        /// <summary>
        /// Stop capture image.
        /// </summary>
        void StopCapture();

        /// <summary>
        /// Close all views of this part and no data will be received.
        /// </summary>
        void Close();

        /// <summary>
        /// Set Integartion Time
        /// </summary>
        /// <param name="integrationTime"></param>        
        void SetIntegrationTime(float integrationTime);

        void ClearLinesCache();
    }
}

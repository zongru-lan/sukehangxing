using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Control;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.HttpServices;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Flows.TRSNetwork.Models;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private DateTime _bootTime = DateTime.Now;
        public string LockID = "";//加密锁的ID
        public App()
        {
            TimeSpan a = TimeSpan.FromSeconds(10);
            Tracer.Initialize();
            Tracer.TraceInfo("Scanner App is starting now.");

            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            this.Exit += OnExit;

            // 检查是否已经选择了合适的型号，如果未选择，则退出。
            if (!CheckModel())
            {
                MessageBox.Show("Please select a model in Configer.exe before running this app.", "Fatal error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                this.Shutdown();
            }

            ExchangeDirectionConfig.Service.Init();

            TranslationService.LoadLanguageFile();

            SystemStartShutdownService.StartService();

            StartServicesAsync();

            //ImageProcessAlgoRecommendService.Service().StartService();

            //开启服务的时候记录开机时间,用于系统开机时间和当前开机后射线的工作时间的计算
            WorkHoursController.Instance.BootTime = _bootTime;

            // 订阅系统关机事件，在关机时，关闭安检机硬件
            SystemEvents.SessionEnded += SystemEventsOnSessionEnded;

            //设备登录
            Global.Instance.Load("sys");
            Global.Instance.Load("app");

            if (!TRSNetWorkService.Service.SingleMode)
            {
                Task.Run(() =>
                {
                    try
                    {
                        TRSNetWorkService.Service.StartService();
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }
                });

                Task.Run(() =>
                {
                    try
                    {
                        NetCommandService.Instance.Start();
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }
                });
                Task.Run(async() =>
                {
                    try
                    {
                        await HttpAiJudgeServices.StartHttpService();
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }
                });
            }

        }

        /// <summary>
        /// 软加密开机验证方法
        /// </summary>
        private void VerifySoft(out bool PerAuthorized, out uint LockLimitedTimes)
        {
            PerAuthorized = false;
            LockLimitedTimes = 0;
            bool verifyResult = false;

            byte[] bsKey2 = { 0x38, 0x29, 0x1D, 0xE7, 0x3F, 0x82, 0xDB, 0x11 };

            string key;

            if (ScannerConfig.Read(ConfigPath.SystemDecryptStr, out key))
            {
                VerifySoftResult result = SoftLockerService.VerifySoft(key, bsKey2, out PerAuthorized, out LockLimitedTimes);
                switch (result)
                {
                    case VerifySoftResult.None:
                        MessageBox.Show("Find the key failed, please make sure the key is exit.", "Fatal error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Shutdown();
                        break;
                    case VerifySoftResult.DecryptFalse:
                        MessageBox.Show("Decrypt the key failed, please make sure the key is right.", "Fatal error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Shutdown();
                        break;
                    case VerifySoftResult.HardwareNotFit:
                        MessageBox.Show("Improper hardware, please make sure the key is right.", "Fatal error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Shutdown();
                        break;
                    case VerifySoftResult.DateOverdue:
                        MessageBox.Show(
                            "Copyright is overdue! Please contact the provider to extend the term of authorization!",
                            "Fatal error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Shutdown();
                        break;
                }
            }
        }

        /// <summary>
        /// 用户注销或者关闭计算机时，先按程序正常退出处理，然后关闭安检机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="sessionEndedEventArgs"></param>
        private void SystemEventsOnSessionEnded(object sender, SessionEndedEventArgs sessionEndedEventArgs)
        {
            ShutdownScanner();
            ControlService.ServicePart.Close();
        }

        private void ShutdownScanner()
        {
            ControlService.ServicePart.ShutdownScanner();
        }

        private void OnAppExit()
        {
            Task.Run(() =>
            {
                int totalCount = -1;
                if (ControlService.ServicePart.GetTotalBagCount(ref totalCount))
                {
                    Tracer.TraceInfo($"[AppExit] Get total bag count from control board: {totalCount}");
                }
                else
                {
                    Tracer.TraceInfo("[AppExit] Failed to get total bag count from control board");
                }
            });
            // 因为出现过关机日志没有保存下来的情况，所以移动到最前面并异步执行
            Task.Run(() =>
            {
                SaveBootLog();
            });

            Task.Run(() =>
            {
                SaveDevicePartsWorkTimeLogs();
            });

            //清除操作日志
            Task.Run(() =>
            {
                try
                {
                    var _controller = new OperationLogsRetrievalController();
                    _controller.CleanEarlyRecord(DateTime.Now.AddDays(-15));
                }
                catch (Exception exception)
                {
                    Tracer.TraceError("Failed to clean early operation records on app start: " + exception.Message);
                }
            });

            //清理D盘日志
            Task.Run(() =>
            {
                try
                {
                    ClearLogsService.Service.ClearLogs(30);
                }
                catch (Exception e)
                {
                    Tracer.TraceError("Failed to clear early logs on app start: " + e.Message);
                }
            });

            try
            {
                LoginAccountManager.Service.Logout();
            }
            catch (Exception e)
            {
                Tracer.TraceError("Exception when LoginAccountManager.Logout() on Exit(): " + e.Message);
            }

            try
            {
                // 停止磁盘清理服务
                OldImagesCleanupService.Service.StopService();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            try
            {
                // 停止保存图片服务中的失败重新保存线程
                Save4ImagesService.Service.StopService();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            try
            {
                SystemStatesMonitor.Monitor.StopService();

                // 停止图像采集服务 
                CaptureService.ServicePart.StopCapture();
                CaptureService.ServicePart.Close();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            //ImageProcessAlgoRecommendService.Service().StopService();

            // 停止控制服务
            try
            {
                KeyboardLedControlService.Service.StopService();
                ControlService.ServicePart.DriveConveyor(ConveyorDirection.Stop);
                ControlService.ServicePart.RadiateXRay(false);
                ControlService.ServicePart.PowerOnPESensors(false);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Failed to close ControlService on Exit.");
            }

            // 存储物体计数
            try
            {
                BagCounterService.Service.SaveBagCount();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            try
            {
                //记录当前关机时间为上次预热时间，考虑开机时射线会运行
                XRayGenWarmupHelper.SaveWarmupRecord();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }

            try
            {
                TouchKeyboardService.Service.CloseKeyboard("HXkeyboard");
            }
            catch (Exception e)
            {
                Tracer.TraceError(e.ToString());
            }
            try
            {
                HttpNetworkController.Controller.Close();
            }
            catch (Exception e)
            {
                Tracer.TraceError(e.ToString());
            }

            //try
            //{
            //    AIJudgeImageService.Service.Stop();
            //}
            //catch (Exception e)
            //{
            //    Tracer.TraceException(e);
            //}

            try
            {
                TRSNetWorkService.Service.Stop();
                NetCommandService.Instance.Stop();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            try
            {
                // 关闭专用键盘服务
                ScannerKeyboardPart.Keyboard.Close();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            try
            {
                PaintingRegionsService.Service.Close();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        // 程序正常退出时，不关闭安检机，仅停止射线、电机等
        private void OnExit(object sender, ExitEventArgs exitEventArgs)
        {
            OnAppExit();
            //设备登出
            //TRSNetWorkService.Service.DeviceLogout();
            ControlService.ServicePart.Close();
            Tracer.UnInitialize();

            //退出软件到桌面时锁定windows到注销界面
            WindowsHelper.AutoLockWorkStation();
        }

        /// <summary>
        /// 保存开机日志
        /// </summary>
        private void SaveBootLog()
        {
            try
            {
                Tracer.TraceInfo("[AppExit] Saving boot log");
                var set = new BootLogDbSet();
                set.Add(new BootLog(_bootTime, DateTime.Now));
            }
            catch (Exception e)
            {
                Tracer.TraceError("[AppExit] Error occured in SaveBootLog");
                Tracer.TraceException(e, "Exception happened when try to save boot log on Exit");
            }
        }

        /// <summary>
        /// 保存零部件的工作时间统计
        /// </summary>
        private void SaveDevicePartsWorkTimeLogs()
        {
            try
            {
                Tracer.TraceInfo("[AppExit] Saving device parts work time log");
                DevicePartsWorkTimingService.ServicePart.Cleanup();
            }
            catch (Exception e)
            {
                Tracer.TraceError("[AppExit] Error occured in SaveDevicePartsWorkTimeLogs");
                Tracer.TraceException(e, "Exception happened when try to save Device Parts Work Time on Exit");
            }
        }

        /// <summary>
        /// 异步启动系统中几个主要的服务项
        /// </summary>
        public void StartServicesAsync()
        {
            // 程序启动后，以异步的方式，先访问一次数据库，避免直接访问时需要较长的时间，以此来加快程序启动速度
            Task.Run(() =>
            {
                try
                {
                    var controller = new AccountDbSet();
                    controller.SelectAll();
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            });

            Task.Run(() =>
            {
                try
                {
                    // 启动专用键盘服务
                    ScannerKeyboardPart.Keyboard.Open();
                    KeyboardLedControlService.Service.StartService();

                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to start Keyboard service.");
                }

                // 异步启动图像自动清理服务
                try
                {
                    // 启动图像清理服务
                    OldImagesCleanupService.Service.StartService();
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to start OldImagesCleanupService");
                }
            });

            // 异步启动控制服务
            Task.Run(() =>
            {
                try
                {
                    ControlService.ServicePart.Open();
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to start ControlService.");
                }
            });

            //try
            //{
            //    AIJudgeImageService.Service.Init();
            //}
            //catch (Exception e)
            //{
            //    Tracer.TraceException(e);
            //}

            // 启动图像数据采集服务移至MainViewModel，以提升启动速度

            Task.Run(() =>
            {
                ReadConfigService.Service.Init();
                DetectConstantLine.Service.Init();
            });
        }

        /// <summary>
        /// 检查是否已经选择了适当的设备型号
        /// </summary>
        /// <returns></returns>
        private bool CheckModel()
        {
            string model;
            if (ScannerConfig.Read(ConfigPath.SystemModel, out model))
            {
                return !string.IsNullOrWhiteSpace(model);
            }

            return false;
        }

        /// <summary>
        /// 处理程序中未能合理捕获的异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dispatcherUnhandledExceptionEventArgs"></param>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            var builder = new StringBuilder(200);
            builder.AppendLine("Unhandled Exception: " + dispatcherUnhandledExceptionEventArgs.Exception.Message);
            builder.AppendLine("StackTrace: " + dispatcherUnhandledExceptionEventArgs.Exception.StackTrace);
            builder.AppendLine("Source: " + dispatcherUnhandledExceptionEventArgs.Exception.Source);
            builder.AppendLine("InnerException: " + dispatcherUnhandledExceptionEventArgs.Exception.InnerException);

            dispatcherUnhandledExceptionEventArgs.Handled = true;

            Tracer.TraceException(dispatcherUnhandledExceptionEventArgs.Exception, "Unhandled exception");
            Tracer.TraceError(builder.ToString());
            MessageBox.Show(builder.ToString(), "System Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Process thisProc = Process.GetCurrentProcess();

            if (Process.GetProcessesByName(thisProc.ProcessName).Length > 1)
            {
                Application.Current.Shutdown();
                return;
            }
            base.OnStartup(e);
        }
    }
}

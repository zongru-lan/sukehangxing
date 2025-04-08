using System;
using System.IO;
using System.Net.Http;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.TRSNetwork;
using UI.Common.Tracers;
using System.Reflection;
using System.Collections.Generic;
using static UI.XRay.Flows.HttpServices.HttpAiJudgeServices;
using System.Windows.Markup;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;


namespace UI.XRay.Flows.HttpServices
{
    public class HttpAiJudgeServices
    {

        public static HttpAiJudgeServices service;
        public static HttpAiJudgeServices Service
        {
            get
            {
                if (service == null)
                    service = new HttpAiJudgeServices();
                return service;
            }
        }
        private static string IP = "110.52.210.203";
        private static string Port = "9016";
        private static string BaseDeviceUrl => $"http://{IP}:{Port}/ski/device/v1/";
        private static string BaseDetectUrl => $"http://{IP}:{Port}/ski/detect/v1/";
        private static CancellationTokenSource _serviceCTS;
        private static DateTime _tokenExpireTime;
        private static readonly object _syncLock = new object();
        private static bool _isHeartbeatRunning = false;




        public static DeviceInfo deviceInfo = new DeviceInfo
        {
            Vender = "Ski",
            Software = "SKScan",//协议默认
            IsBigEnd = false
        };
        public struct DeviceInfo
        {
            public string DeviceID;        // 必需: 安检机设备号
            public string Vender;          // 必需: 厂商唯一标识
            public string Model;           // 必需: 安检机型号代码
            public string Software;        // 必需: 软件代号
            public string Version;         // 必需: 软件版本号
            public string At;              // 可选: 标记安检机所在位置，具体实施约定
            public bool IsBigEnd;          // 必需: 是否使用大端传输，默认：否(X86)
            public string Token;           // 必需: 用于后续请求的 Token
            public string PackageID;       // 可选: 包裹 ID（如果有）

            // 图像字段: 可选，只有在进行图像传输时需要填写
            public byte[] ImageM;          // 主视角图片
            public byte[] ImageS;          // 侧视角图片
            public byte[] ImageT;          // 三视角图片

            // 原子序数字段: 可选
            public byte[] AtomM;           // 主视角原子序数
            public byte[] AtomS;           // 侧视角原子序数
            public byte[] AtomT;           // 三视角原子序数

            // 高低能数据: 可选
            public byte[] PowerMH;         // 主视角高能
            public byte[] PowerML;         // 主视角低能
            public byte[] PowerSH;         // 侧视角高能
            public byte[] PowerSL;         // 侧视角低能
            public byte[] PowerTH;         // 三视角高能
            public byte[] PowerTL;         // 三视角低能

            // 尺寸和扩展字段: 必需
            public string Size;            // 必需: 图像、原子序数和高低能宽高的 JSON 字符串
            public string Extend;          // 必需: 扩展信息 JSON 字符串
        }

        public struct SizeData
        {
            // 必需: 图像、原子序数和高低能的尺寸信息
            public ViewData Main;  // 必需: 主视角宽高数据
            public ViewData Sub;   // 侧视角宽高数据
            public ViewData Third; // 三视角宽高数据
        }


        public struct ViewData
        {
            // 必需: 图像、原子序数和高低能的宽高
            public int[] Image;  // 必需: 图像的宽高数组
            public int[] Atom;   // 必需: 原子序数的宽高数组
            public int[] Power;  // 必需: 高低能的宽高数组
        }
        public static ViewData MainViewData = new ViewData
        {
            Image = new int[2] { 1200, 1216 },
            Atom = new int[2] { 0, 0 },
            Power = new int[2] { 1200, 1216}

        };
        public static ViewData SubViewData = new ViewData
        {
            Image = new int[2] { 1200, 1152 },
            Atom = new int[2] { 0, 0 },
            Power = new int[2] { 1200, 1152 }
        };
        static int Margindata = 50;

        public struct ExtendData
        {
            // 扩展字段: 必需
            public MarginData Margin;  // 必需: 边距信息
            public SliceData Slice;    // 必需: 分片信息
        }

        public struct MarginData
        {
            // 必需: 包裹图像在安检机通道内的上下边距
            public int Top;    // 上边距，必需
            public int Bottom; // 下边距，必需
        }

        public struct SliceData
        {
            // 必需: 分片 ID，-1 表示不分片
            public int ID;  // 必需
        }
        public enum ProtocolService
        {
            Hello,
            Ping,
            Detect,
            RiskLevels,
            RiskObjects
        };
        public class HelloApiResponse
        {
            public int Code { get; set; }
            public string Msg { get; set; }
            public bool IsSuccess { get; set; }
            public HelloResponseData Data { get; set; }
        }
        public class HelloResponseData
        {
            public string ID { get; set; }
            public string Version { get; set; }
            public DateTime Date { get; set; }
            public string Token { get; set; }
        }
        public class PingApiResponse
        {
            public int Code { get; set; }
            public string Msg { get; set; }
            public bool IsSuccess { get; set; }
            public HelloResponseData Data { get; set; }
        }
        public class PingApiResponseData
        {
            public int Status { get; set; }
            public string TimeStamp { get; set; }
        }
        // Detect 返回的主要数据类
        public class DetectApiResponse
        {
            public string ID { get; set; }
            public List<DetectionResult> Main { get; set; }
            public List<DetectionResult> Sub { get; set; }
            public List<DetectionResult> Third { get; set; }
            public ExtendResult Extend { get; set; }
            public int TimeSpan { get; set; }
            public string TimeStamp { get; set; }
        }

        // 单个检测结果
        public class DetectionResult
        {
            public ObjectResult Object { get; set; }
            public string Rect { get; set; }
            public MaterialResult Material { get; set; }
            public MeasureResult Measure { get; set; }
        }

        // 物体检测结果
        public class ObjectResult
        {
            public string ID { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string Label { get; set; }
            public float Score { get; set; }
            public string RiskLevel { get; set; }
            public string Color { get; set; }
            public ObjectExtResult ObjectExt { get; set; }
        }

        // 物体扩展结果
        public class ObjectExtResult
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string RiskLevel { get; set; }
            public string Color { get; set; }
        }

        // 材料识别结果
        public class MaterialResult
        {
            public string ID { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string Label { get; set; }
            public float Score { get; set; }
            public string RiskLevel { get; set; }
            public string Color { get; set; }
            public ObjectExtResult ObjectExt { get; set; }
        }

        // 尺寸预估结果
        public class MeasureResult
        {
            public float Len { get; set; }
            public float Vol { get; set; }
        }

        // 扩展结果
        public class ExtendResult
        {
            public int AtSlice { get; set; }
        }


        private static async Task InitializeDeviceInfo()
        {
            await Task.Run(() =>
            {
                ScannerConfig.Read(ConfigPath.SystemMachineNum, out deviceInfo.DeviceID);
                ScannerConfig.Read(ConfigPath.SystemModel, out deviceInfo.Model);
                ScannerConfig.Read(ConfigPath.SystemSoftwareVersion, out deviceInfo.Version);
                deviceInfo.Version = "V1.0-20221012";
            });
        }

        // 修改StartHttpService方法，增加重试控制
        public static async Task StartHttpService()
        {
            // 取消之前的任务
            _serviceCTS?.Cancel();
            _serviceCTS = new CancellationTokenSource();

            // 初始化设备信息
            await InitializeDeviceInfo();

            // 重试逻辑封装
            async Task<bool> TryConnect()
            {
                const int maxRetries = 5;
                int retryCount = 0;

                while (retryCount < maxRetries && !_serviceCTS.IsCancellationRequested)
                {
                    try
                    {
                        // 尝试加载已有Token
                        var savedToken = AuthTokenStorage.TryLoadToken();
                        if (!string.IsNullOrEmpty(savedToken))
                        {
                            deviceInfo.Token = savedToken;
                            if (await ValidateToken())
                            {
                                Tracer.TraceInfo("检测到有效Token，跳过登录");
                                return true;
                            }
                        }

                        // 执行登录流程
                        string result = await HttpInterfaceAsync(ProtocolService.Hello, deviceInfo);
                        HelloApiResponse helloResult = ParseHello(result);

                        if (helloResult?.IsSuccess == true)
                        {
                            deviceInfo.Token = helloResult.Data.Token;
                            AuthTokenStorage.SaveToken(deviceInfo.Token);
                            _tokenExpireTime = DateTime.Now.AddHours(24);
                            return true;
                        }

                        Tracer.TraceError($"登录失败，{helloResult?.Msg ?? "未知错误"}");
                    }
                    catch (Exception ex)
                    {
                        Tracer.TraceException(ex);
                    }

                    retryCount++;
                    await Task.Delay(5000, _serviceCTS.Token); // 5秒后重试
                }
                return false;
            }

            // 启动连接循环
            bool isConnected = false;
            while (!isConnected && !_serviceCTS.IsCancellationRequested)
            {
                isConnected = await TryConnect();
                if (!isConnected)
                {
                    Tracer.TraceInfo("登录失败，30秒后重新尝试...");
                    await Task.Delay(30000, _serviceCTS.Token);
                }
            }
            if (isConnected)
            {
                StartHeartbeatService();
            }
        }


        // 新增Token有效性验证方法
        private static async Task<bool> ValidateToken()
        {
            try
            {
                string pingResult = await HttpInterfaceAsync(ProtocolService.Ping, deviceInfo);
                var pingResponse = ParsePing(pingResult);
                return pingResponse?.IsSuccess == true;
            }
            catch
            {
                return false;
            }
        }

        // 增强版心跳服务
        private static void StartHeartbeatService()
        {
            lock (_syncLock)
            {
                if (_isHeartbeatRunning) return;

                _isHeartbeatRunning = true;
                Task.Run(async () =>
                {
                    var retryCount = 0;
                    const int maxRetries = 3;
                    DateTime? lastSuccessTime = null;

                    while (!_serviceCTS.IsCancellationRequested)
                    {
                        try
                        {
                            // 主动检查Token有效期
                            if (DateTime.UtcNow > _tokenExpireTime.AddMinutes(-5)) // 提前5分钟续期
                            {
                                Tracer.TraceWarning("Token即将过期，主动续期");
                                throw new TokenExpiredException();
                            }

                            // 心跳请求
                            string pingResult = await HttpInterfaceAsync(ProtocolService.Ping, deviceInfo);
                            var pingResponse = ParsePing(pingResult);

                            if (pingResponse?.IsSuccess == true)
                            {
                                retryCount = 0;
                                lastSuccessTime = DateTime.Now;
                                await Task.Delay(10000, _serviceCTS.Token);
                                continue;
                            }

                            // 处理心跳失败
                            HandleHeartbeatFailure(ref retryCount, maxRetries);
                        }
                        catch (TokenExpiredException)
                        {
                            Tracer.TraceWarning("Token已过期，触发重新登录");
                            AuthTokenStorage.SaveToken(null);
                            await StartHttpService();
                            break;
                        }
                        catch (Exception ex)
                        {
                            Tracer.TraceException(ex);
                            HandleHeartbeatFailure(ref retryCount, maxRetries);
                        }

                        await Task.Delay(5000, _serviceCTS.Token);
                    }
                    _isHeartbeatRunning = false;
                }, _serviceCTS.Token);
            }
        }

        private static void HandleHeartbeatFailure(ref int retryCount, int maxRetries)
        {
            Tracer.TraceError($"心跳检测失败，已连续失败{retryCount + 1}次");
            if (++retryCount >= maxRetries)
            {
                Tracer.TraceError("心跳连续失败，触发重新登录");
                AuthTokenStorage.SaveToken(null);
                throw new TokenInvalidException(); // 触发上层恢复机制
            }
        }
        public static async Task<string> SafeHttpRequest(Func<Task<string>> requestAction)
        {
            const int maxRetries = 2;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    return await requestAction();
                }
                catch (TokenInvalidException)
                {
                    Tracer.TraceInfo("检测到Token失效，尝试重新认证...");
                    await StartHttpService();
                    attempt++;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("401"))
                {
                    Tracer.TraceWarning("服务端返回401未授权，清除本地Token");
                    AuthTokenStorage.SaveToken(null);
                    await StartHttpService();
                    attempt++;
                }
            }
            throw new ApplicationException("请求失败且无法自动恢复");
        }

        public class TokenExpiredException : Exception { }
        public class TokenInvalidException : Exception { }

        public static async Task DetectHttpService()
        {

            await SafeHttpRequest(async () =>
            {
                string result = await HttpInterfaceAsync(ProtocolService.Detect, deviceInfo);
                DetectApiResponse detectResult = ParseDetect(result);
                Tracer.TraceInfo($"接收识别数据成功,包裹ID是{detectResult.ID}");
                return result;
            });
        }
        // 用于处理高能和低能数据并转换为字节格式
        public static void ProcessHighLowData(IEnumerable<DisplayScanlineDataBundle> bundleList, bool isBigEnd)
        {
            // 用于保存所有的高能和低能数据（不转换为字节数组）
            List<ushort> highDataMainList = new List<ushort>();
            List<ushort> lowDataMainList = new List<ushort>();
            List<ushort> highDataSubList = new List<ushort>();
            List<ushort> lowDataSubList = new List<ushort>();

            // 遍历 bundleList
            foreach (var bundle in bundleList)
            {
                if (bundle.View1Data != null)
                {
                    // 提取高能和低能数据
                    var highSubData = bundle.View1Data.HighData.Reverse();
                    var lowSubData = bundle.View1Data.LowData.Reverse();

                    // 如果存在 HighData，则添加到列表中
                    if (highSubData != null)
                    {
                        highDataSubList.AddRange(highSubData);  // 添加高能数据
                    }

                    // 如果存在 LowData，则添加到列表中
                    if (lowSubData != null)
                    {
                        lowDataSubList.AddRange(lowSubData);   // 添加低能数据
                    }
                }

                if (bundle.View2Data != null)
                {
                    // 提取视角2的高能和低能数据（如果存在）
                    var highMainData = bundle.View2Data.HighData.Reverse();
                    var lowMainData = bundle.View2Data.LowData.Reverse();

                    // 如果存在 HighData，则添加到列表中
                    if (highMainData != null)
                    {
                        highDataMainList.AddRange(highMainData);  // 添加高能数据
                    }

                    // 如果存在 LowData，则添加到列表中
                    if (lowMainData != null)
                    {
                        lowDataMainList.AddRange(lowMainData);   // 添加低能数据
                    }
                }
            }
            deviceInfo.PowerMH = ConvertToByteArray(highDataMainList.ToArray(), isBigEnd);
            deviceInfo.PowerML =  ConvertToByteArray(lowDataMainList.ToArray(), isBigEnd);
            deviceInfo.PowerSH  = ConvertToByteArray(highDataSubList.ToArray(), isBigEnd);
            deviceInfo.PowerSL = ConvertToByteArray(lowDataSubList.ToArray(), isBigEnd);

        }

        //需要
        public static void AssignImage(string imagePath)
        {
            try
            {
                // 读取图片文件
                using (var image = new Bitmap(imagePath))
                {
                    // 获取图片的宽度和高度
                    int width = image.Width;
                    int height = image.Height;
                    MainViewData.Image[0] = width / 2;
                    MainViewData.Image[1] = 1216;
                    SubViewData.Image[0] = width / 2;
                    SubViewData.Image[1] = 1152;

                    // 计算分割点（图片宽度的一半）
                    int middle = width / 2;

                    // 创建左边和右边的部分
                    var leftImage = new Bitmap(middle, MainViewData.Image[1]);
                    var rightImage = new Bitmap(middle, SubViewData.Image[1]);

                    // 从原始图片中复制左半部分到左图像
                    using (var g = Graphics.FromImage(leftImage))
                    {
                        g.DrawImage(image, new Rectangle(0, 0, middle, height), new Rectangle(0, 0, middle, height), GraphicsUnit.Pixel);
                    }

                    // 从原始图片中复制右半部分到右图像
                    using (var g = Graphics.FromImage(rightImage))
                    {
                        g.DrawImage(image, new Rectangle(0, 0, middle, height), new Rectangle(middle, 0, middle, height), GraphicsUnit.Pixel);
                    }

                    // 将字节数组赋值给对应的视角
                    byte[] leftImageBytes;
                    byte[] rightImageBytes;

                    // 将左半部分转为字节数组
                    using (var ms = new MemoryStream())
                    {
                        leftImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        leftImageBytes = ms.ToArray();
                    }

                    // 将右半部分转为字节数组
                    using (var ms = new MemoryStream())
                    {
                        rightImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        rightImageBytes = ms.ToArray();
                    }

                    // 根据传入的视角索引，赋值图片字节

                    deviceInfo.ImageS = leftImageBytes; // 左半部分
                    //测试
                    deviceInfo.AtomM = leftImageBytes;
                    //测试
                    deviceInfo.ImageM = rightImageBytes; // 右半部分

                }
            }
            catch (Exception ex)
            {
                // 捕获异常并记录错误
                Tracer.TraceError($"Failed to assign Image: {ex.Message}");
            }
        }
        private static void AddImageField(MultipartFormDataContent formData, byte[] data, string fieldName, string fileName)
        {
            if (data == null) return;

            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            formData.Add(content, fieldName, fileName);
        }

        // 添加二进制数据字段
        private static void AddBinaryField(MultipartFormDataContent formData, byte[] data, string fieldName, string fileName)
        {
            if (data == null) return;

            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            formData.Add(content, fieldName, fileName);
        }

        // 添加高低能数据转
        private static void AddPowerData(MultipartFormDataContent formData, byte[] data, string fieldName)
        {
            if (data == null) return;
            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            formData.Add(content, fieldName, $"file");
        }
        // 修改ConvertToByteArray方法
        private static byte[] ConvertToByteArray(ushort[] data, bool isBigEnd)
        {
            byte[] buffer = new byte[data.Length * 2];

            for (int i = 0; i < data.Length; i++)
            {
                if (isBigEnd)
                {
                    buffer[i * 2 ] = (byte)(data[i] >> 8);    // 高字节
                    buffer[i * 2 + 1] = (byte)data[i];            // 低字节
                }
                else
                {
                    buffer[i * 2 ] = (byte)data[i];            // 低字节
                    buffer[i * 2 + 1] = (byte)(data[i] >> 8);     // 高字节
                }
            }
            return buffer;
        }

        public static byte[] GenerateAtom(string ImagePath)
        {

            Bitmap image = new Bitmap(ImagePath);
            int Height = image.Height;
            int Width = image.Width;
            //  初始化二维数组存储灰度值
            double[,] material_num = new double[Height,Width];
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Color pixelColor = image.GetPixel(i,j);
                    material_num[i,j] = pixelColor.G;         
                }

            double[] yuanzixushu = { 5.53, 7.51, 8.85, 9.45, 10, 13, 15.62, 16.8, 18.12, 19.68, 20, 26 };
            int[] huidu = { 5, 20, 50, 60, 90, 110, 120, 130, 170, 190, 210, 233 }; //  可能要变
            double[,] cailiaozhi_num = new double[Height, Width];

            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    double material_num_temp = material_num[i,j];
                    int left_quyu = FindLeftQuyu(material_num_temp, huidu);
                    int right_quyu = FindRightQuyu(material_num_temp, huidu);

                    double yuanzixushu_temp;

                    if (material_num_temp == 1 || material_num_temp == 2)
                        yuanzixushu_temp = 27;
                    else if (material_num_temp == 50)
                        yuanzixushu_temp = 0;
                    else if (left_quyu == -1)
                        yuanzixushu_temp = 5.53;
                    else if (right_quyu == -1)
                        yuanzixushu_temp = 26;
                    else
                    {
                        double left_xishu, right_xishu;

                        if (left_quyu == right_quyu)
                        {
                            left_xishu = 1;
                            right_xishu = 0;
                        }
                        else
                        {
                            left_xishu = (huidu[right_quyu] - material_num_temp) / (huidu[right_quyu] - huidu[left_quyu]);
                            right_xishu = (material_num_temp - huidu[left_quyu]) / (huidu[right_quyu] - huidu[left_quyu]);
                        }

                        yuanzixushu_temp = yuanzixushu[left_quyu] * left_xishu + yuanzixushu[right_quyu] * right_xishu;
                    }
                    cailiaozhi_num[i,j] = yuanzixushu_temp;
                }
            }

            return Convert2ByteArray(cailiaozhi_num, Height, Width);
        }

        public static Tuple<double, double> FindMinMax(double[,] cailiaozhi_num, int Height, int Width)
        {
            if (cailiaozhi_num == null || cailiaozhi_num.Length == 0) 
                return new Tuple<double, double>(0, 0);

            // 初始化最小值为双精度浮点数的最大可能值
            // 初始化最大值为双精度浮点数的最小可能值
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    // 更新最小值
                    if (cailiaozhi_num[i,j] < minValue)
                        minValue = cailiaozhi_num[i,j];

                    // 更新最大值
                    if (cailiaozhi_num[i,j] > maxValue)
                        maxValue = cailiaozhi_num[i,j];
                }
            return new Tuple<double, double>(maxValue, minValue);
        }

        public static byte[] Convert2ByteArray(double[,] cailiaozhi_num, int Height, int Width)
        {
            byte[] cailiaozhi_Num = new byte[Height * Width];  //单像素一字节
            // 将 double 的范围 [min_Value, max_Value] 映射到 byte 的范围 [0, 255]  即一个字节
            Tuple<double, double> MaxandMin = FindMinMax(cailiaozhi_num, Height, Width);
            double max_Value = MaxandMin.Item1;
            double min_Value = MaxandMin.Item2;
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    //double normalizedValue = (Math.Log(cailiaozhi_num[i, j] - min_Value + 1) / Math.Log(max_Value - min_Value + 1));
                    // 缩放到 [0,1] 区间
                    double normalizedValue = (cailiaozhi_num[i,j] - min_Value) / (max_Value - min_Value);
                    // 缩放到 byte 范围 [0, 255]
                    cailiaozhi_Num[i * Width + j] = (byte)(normalizedValue * 255);
                }
            return cailiaozhi_Num;
        }
        public static int FindLeftQuyu(double material_num_temp, int [] huidu)
        {
            for(int i = huidu.Length - 1;i>=0;i--)    //找最大的
                if (material_num_temp >= huidu[i])    //先找出所有大于等于huidu的下标  再取出最大的那个
                    return i;
            return -1;
        }

        public static int FindRightQuyu(double material_num_temp, int[] huidu)
        {
            for (int i = 0;i <= huidu.Length - 1; i++)  //找最小的
                if (material_num_temp <= huidu[i])      //先找出所有小于等于huidu的下标  再取出最小的那个
                    return i;
            return -1;
        }

       

        public static async Task<string> HttpInterfaceAsync(ProtocolService Protocol, DeviceInfo deviceInfo)
        {
            string service = "";


            switch (Protocol)
            {
                case ProtocolService.Hello:
                    var postHelloData = new
                    {
                        DeviceID = deviceInfo.DeviceID,  // 必需
                        Vender = deviceInfo.Vender,      // 必需
                        Model = deviceInfo.Model,        // 必需
                        Software = deviceInfo.Software,  // 必需
                        Version = deviceInfo.Version,    // 必需
                        At = deviceInfo.At,              // 可选
                        IsBigEnd = deviceInfo.IsBigEnd   // 必需
                    };
                    string jsonPayload = JsonConvert.SerializeObject(postHelloData);
                    service = "hello";
                    string url = BaseDeviceUrl + service;
                    return await PostAsync(url, jsonPayload);

                case ProtocolService.Ping:
                    var postPingData = new
                    {
                        DeviceID = deviceInfo.DeviceID,  // 必需
                        Token = deviceInfo.Token         // 必需
                    };
                    string jsonPingload = JsonConvert.SerializeObject(postPingData);
                    service = "ping";
                    string urlPing = BaseDeviceUrl + service;
                    return await PostAsync(urlPing, jsonPingload);

                case ProtocolService.Detect:
                    var postDetectData = new
                    {
                        DeviceID = deviceInfo.DeviceID,
                        PackageID = deviceInfo.PackageID,
                        Token = deviceInfo.Token,
                        Size = deviceInfo.Size,
                        Extend = deviceInfo.Extend
                    };

                    var formData = new MultipartFormDataContent();

                    // ============ Size字段处理（数组格式） ============
                    var sizeDataItem = new SizeData
                    {
                        Main = MainViewData,
                        Sub = SubViewData                      // 明确省略三视角
                    };

                    var sizeList = new List<SizeData> { sizeDataItem };
                    string sizeJson = JsonConvert.SerializeObject(sizeList, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    formData.Add(new StringContent(sizeJson), "Size");
                    // ==============================================

                    // 必需字段
                    formData.Add(new StringContent(deviceInfo.DeviceID), "DeviceID");
                    formData.Add(new StringContent(deviceInfo.PackageID), "PackageID");
                    formData.Add(new StringContent(deviceInfo.Token), "Token");

                    // 图片上传
                    AddImageField(formData, deviceInfo.ImageM, "ImageM", "file");
                    AddImageField(formData, deviceInfo.ImageS, "ImageS", "file");
                    AddImageField(formData, deviceInfo.ImageT, "ImageT", "file");

                    // 原子序数数据
                    AddBinaryField(formData, deviceInfo.AtomM, "AtomM", "file");

                    // 高低能数据处理
                    AddPowerData(formData, deviceInfo.PowerMH, "PowerMH");
                    AddPowerData(formData, deviceInfo.PowerML, "PowerML");

                    // Extend字段
                    var extendData = new ExtendData
                    {
                        Margin = new MarginData { Top = Margindata, Bottom = Margindata },
                        Slice = new SliceData { ID = 0 }
                    };
                    string extendJson = JsonConvert.SerializeObject(extendData, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    formData.Add(new StringContent(extendJson), "Extend");

                    LogFormData(formData);
                    string urlDetect = BaseDetectUrl + "detect";
                    return await PostAsync(urlDetect, formData);

                default:
                    Console.WriteLine("Invalid Protocol");
                    return service;
            }
        }
        private static void LogFormData(MultipartFormDataContent formData)
        {
            // 遍历 formData 以打印每个字段和其对应的内容
            foreach (var content in formData)
            {
                // 获取表单字段名称
                var fieldName = content.Headers.ContentDisposition?.Name;

                // 打印表单字段名称
                Tracer.TraceInfo($"Key:                                 {fieldName}");

                // 如果内容是 ByteArrayContent，则打印内容的长度和文件名
                if (content is ByteArrayContent byteArrayContent)
                {
                    Tracer.TraceInfo($"Content Length:                 {byteArrayContent.Headers.ContentLength}");
                    var fileName = byteArrayContent.Headers.ContentDisposition?.FileName;
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        Tracer.TraceInfo($"File Name:                  {fileName}");
                    }
                }

                // 如果内容是 StringContent，则打印内容
                if (content is StringContent stringContent)
                {
                    Tracer.TraceInfo($"String Content:             {stringContent.ReadAsStringAsync().Result}");
                }
            }
        }


        public static HelloApiResponse ParseHello(string jsonResponse)
        {
            try
            {
                return JsonConvert.DeserializeObject<HelloApiResponse>(jsonResponse);
            }
            // 将所有信息格式化为字符串并返回
            catch (Exception ex)
            {
                Tracer.TraceError($"我连接失败！！！！！！！！！！！！: {ex}");//测试中
                return null;
            }
        }
        public static PingApiResponse ParsePing(string jsonResponse)
        {
            try
            {
                return JsonConvert.DeserializeObject<PingApiResponse>(jsonResponse);
            }
            // 将所有信息格式化为字符串并返回
            catch (Exception ex)
            {
                return null;
            }
        }
        public static DetectApiResponse ParseDetect(string jsonResponse)
        {
            try
            {
                return JsonConvert.DeserializeObject<DetectApiResponse>(jsonResponse);
            }
            // 将所有信息格式化为字符串并返回
            catch (Exception ex)
            {
                return null;
            }
        }

        static async Task<string> PostAsync(string url, string data)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(data, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return $"Error: {response.StatusCode}";
                }
            }
        }

        static async Task<string> PostAsync(string url, MultipartFormDataContent formData)
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(url, formData);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    return $"Error: {response.StatusCode}";
                }
            }
        }

    }
    public static class AuthTokenStorage
    {
        private const string TokenFile = "authtoken.dat";
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static void SaveToken(string token)
        {
            if (!IsTokenValid(token))
            {
                throw new ArgumentException("无效的Token格式");
            }
            try
            {
                var data = new TokenData
                {
                    Token = token ?? throw new ArgumentNullException(nameof(token)),
                    ExpireTime = DateTime.UtcNow.AddHours(24).ToBinary()
                };

                File.WriteAllText(TokenFile,
                    JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Tracer.TraceError($"保存Token失败: {ex.Message}");
                throw;
            }
        }



        public static bool IsTokenValid(string token)
        {
            return !string.IsNullOrEmpty(token) &&
                   token.Length >= 32 &&
                   token.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }
        public static string TryLoadToken()
        {
            if (!File.Exists(TokenFile)) return null;

            try
            {
                var json = File.ReadAllText(TokenFile);
                var data = JsonConvert.DeserializeObject<TokenData>(json);

                // 关键修复点：添加空引用检查
                if (data?.Token == null || data.ExpireTime == 0)
                {
                    File.Delete(TokenFile); // 删除无效token文件
                    return null;
                }

                return DateTime.FromBinary(data.ExpireTime) > DateTime.UtcNow
                       ? data.Token
                       : null;
            }
            catch (Exception ex) // 捕获所有可能的异常
            {
                Tracer.TraceError($"加载Token失败: {ex.Message}");
                File.Delete(TokenFile); // 清理损坏文件
                return null;
            }
        }

        private class TokenData
        {
            public string Token { get; set; }
            public long ExpireTime { get; set; }
        }
    }

}


/*
public static byte[] GenerateAtom(ushort[] material_num)
{
    // 原子序数标定数据（需确认这些值是否对应原始ushort范围）
    double[] yuanzixushu = { 5.53, 7.51, 8.85, 9.45, 10, 13, 15.62, 16.8, 18.12, 19.68, 20, 26 };
    // 灰度标定点（需确认值是否对应原始ushort范围，例如0-65535）
    int[] huidu = { 5, 19, 45, 60, 85, 115, 128, 140, 165, 190, 215, 240 };

    byte[] result = new byte[material_num.Length];

    for (int i = 0; i < material_num.Length; i++)
    {
        ushort raw_value = material_num[i]; // 直接使用原始ushort值
        double yuanzixushu_temp;

        // 特殊值判断（需确认1/2/50是否为原始数据的特殊标识）
        if (raw_value == 1 || raw_value == 2)
        {
            yuanzixushu_temp = 27;  // 背景
        }
        else if (raw_value == 50)
        {
            yuanzixushu_temp = 0;   // 穿不透
        }
        else
        {
            // 查找最近的标定区间
            int left_quyu = FindLeftQuyu(raw_value, huidu);
            int right_quyu = FindRightQuyu(raw_value, huidu);

            // 边界处理
            if (left_quyu == -1)
            {
                yuanzixushu_temp = 26;  // 超过最大标定值
            }
            else if (right_quyu == -1)
            {
                yuanzixushu_temp = 5.53; // 低于最小标定值
            }
            else
            {
                // 计算插值权重
                double left_xishu = (huidu[right_quyu] - raw_value) / (double)(huidu[right_quyu] - huidu[left_quyu]);
                double right_xishu = (raw_value - huidu[left_quyu]) / (double)(huidu[right_quyu] - huidu[left_quyu]);

                // 计算原子序数
                yuanzixushu_temp = yuanzixushu[left_quyu] * left_xishu + yuanzixushu[right_quyu] * right_xishu;
            }
        }

        result[i] = (byte)yuanzixushu_temp;
    }

    return result;
}

public static int FindLeftQuyu(double material_num_temp, int[] huidu)
{
    for (int i = huidu.Length - 1; i >= 0; i--)
        if (material_num_temp >= huidu[i])
            return i;
    return -1;
}

public static int FindRightQuyu(double material_num_temp, int[] huidu)
{
    for (int i = 0; i <= huidu.Length - 1; i++)
        if (material_num_temp <= huidu[i])
            return i;
    return -1;
}
*/